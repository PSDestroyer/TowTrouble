"""Compose reviewed local translations. No network or translation service is used."""
from pathlib import Path
import json,re,hashlib
root=Path(__file__).resolve().parents[2]/'Localization'
source=json.loads((root/'Source.json').read_text(encoding="utf-8"));allterms={}
for file in ['Core.json','RecoveryTerms.json','Help.json','Details.json','Critical.json','Polish.json','Compact.json','Gameplay.json','Results.json']:
 p=root/'Authored'/file
 if p.exists():
  for code,items in json.loads(p.read_text(encoding="utf-8")).items():allterms.setdefault(code,{}).update(items)
mappings=json.loads((root/'Authored/Mappings.json').read_text(encoding="utf-8"))
# Canonical English terms remain stable because navigation and controller glyphs use them.
synonyms={'Your workshop':'Workshop','Replay reward':'Reward','Recovered':'Vehicle freed','Car stuck':'Recovery','Your name':'Name','Tires':'Wheels','Motor':'Engine','Fender':'Fenders','Wheel':'Wheels','Bumper':'Front bumper','Attachment Top':'Roof','Motor Carb':'Intake','Motor Exhaust':'Exhaust','Classic':'Stock','Historic':'Stock','Hot Rod':'Hot Rod','Resume':'Continue','Contracts':'Recoveries','No breaks':'No cable breaks','Tabs':'Next tab','Activate highlighted button':'Select','Scroll review':'Navigate','Scroll':'Navigate','Drive / reverse':'Accelerate','Steer left':'Left','Steer right':'Right','Attach / detach cable':'Attach cable','Detach':'Detach cable','Cable load':'Cable tension','Other driver\'s cable load':'Crew cable tension','Top speed':'Speed','Rated pull':'Pull','Drive':'Pull','Ride height':'Ground clearance','Snow grip':'Snow','Sand grip':'Sand','Mud grip':'Mud','minimum':'Minimum length','maximum':'Maximum length','Not ready':'Not ready'}
helpkeys=list(next(iter(json.loads((root/'Authored/Help.json').read_text(encoding="utf-8")).values())).keys())
# Data-driven stat lines preserve every number and sign without duplicating prose per model.
for terms in allterms.values():
 for alias,concept in synonyms.items():
  if concept in terms:terms[alias]=terms[concept]
 for key in ['Choose your language','Text language','Language changes apply immediately.']:terms[key]=terms['Language']
 terms['Car stuck']=terms['Recovery']+' · '+terms['Locked']
 terms['Replay reward']=terms['Replay']+' · '+terms['Reward']
 terms['Your workshop']=terms['Workshop']
existing={x['source'].casefold() for x in source}
for terms in allterms.values():
 for key in terms:
  if key.casefold() not in existing:
   source.append({'source':key,'key':'ui.'+hashlib.sha256(key.encode()).hexdigest()[:16],'contexts':['authored localization concept']});existing.add(key.casefold())
# Additional composed keys are explicit, bounded UI sentences, never arbitrary player data.
formatkeys=['Make {0} your Stock vehicle?','Abandon "{0}" without payment?','Restore the default {0} settings? Career progress is kept.','Choose a keyboard key for {0}. Duplicate and reserved bindings are rejected.','Already used for {0}. Choose another key, or Esc to cancel.','Keep the new display settings?\n\nReverting in {0} seconds.','Replace setup {0} with your currently fitted setup?\n\nThe part you\'re only previewing will not be saved.','{0} fitted and saved','{0} fitted setup saved','{0} setup fitted and saved','Part sold · {0} CR returned','NEED {0} CR','BUY & FIT · {0} CR','Base  {0} CR   +   Bonuses  {1} CR','{0} completed recoveries','{0} available recoveries','{0} part(s) will return to Stock','Equipped on {0}','Used in {0} setup','Delivered to {0}','LANGUAGE: {0}','Sell {0} for {1} CR?\n\nYou\'ll need to buy it again to use it later.','Hold {0} to reel in','Drive a little using {0} and {1}','{0} to drive and reverse. {1} to steer. Brake before tight turns.']
formatkeys += ['the drop-off','Replay base  {0} CR   +   Bonuses  {1} CR','Base  {0} CR   +   Bonuses  {1} CR  ·  First recovery +25%']
for key in formatkeys:
 if key.casefold() not in existing:source.append({'source':key,'key':'ui.'+hashlib.sha256(key.encode()).hexdigest()[:16],'contexts':['composed UI template']});existing.add(key.casefold())
(root/'Source.json').write_text(json.dumps(source,ensure_ascii=False,indent=2)+'\n',encoding="utf-8")
coverage={}
for code,terms in allterms.items():
 lookup={k.casefold():v for k,v in terms.items()};values={};kinds={}
 def t(key):return lookup.get(key.casefold(),key)
 def helper(index):return t(helpkeys[index])
 def compose(text):
  lower=text.strip().casefold()
  if lower in lookup:return t(text.strip()),'authored'
  if text in mappings:
   concept=mappings[text];value=t(concept)
   return value,'reviewed mapping'
  return text,'fallback'
 for item in source:
  text=item['source'];value,kind=compose(text)
  # Preserve stats explicitly; general help mappings must never swallow numerical specifications.
  if re.match(r'^.+ · four-wheel set\. Road ',text):
   typ,rest=text.split(' · ',1);value=t(typ)+' · '+t('Four-wheel set')
   for label,num,unit in re.findall(r'(Road|mud|sand|snow|Mass|Mud resistance|Speed) ([+-]?[\d.]+)(%| kg)',rest):value+=' · '+t(label)+': '+num+unit
   kind='structured stats'
  elif re.match(r'^(Off-road|Utility|Light|Heavy|Balanced|Heavy-duty|Sport|Compact): ',text):
   m=re.search(r'(\d+) kg · ([\d.]+) kN · (\d+) km/h',text)
   if m:value=t('Mass')+': '+m[1]+' kg · '+t('Pull')+': '+m[2]+' kN · '+t('Speed')+': '+m[3]+' km/h. '+t(text.split('km/h. ',1)[1]);kind='structured stats'
  elif text.startswith('Break threshold ') or text.startswith('First cable upgrade. Break threshold '):
   m=re.search(r'(\d+) kN',text)
   if m:value=t('Break threshold')+': '+m[1]+' kN. '+helper(1);kind='structured stats'
  elif re.match(r'^\d+ kN; up to ',text):
   m=re.match(r'^(\d+) kN; up to ([\d.]+) m/s',text)
   value=t('Winch force')+': '+m[1]+' kN · '+t('Reel speed')+': '+m[2]+' m/s. '+t('The winch slows under load and stalls at its limit.')+' '+t('Grip still limits usable pull');kind='structured stats'
   recommendation=re.search(r'Recommended: (\d+) kN cable',text)
   if recommendation:value+=' · '+t('Recommended')+': '+t('Cable')+' '+recommendation[1]+' kN'
  elif text=='Nominal drive +20%; usable force remains grip-limited.':value=t('Pull')+' +20%. '+t('Grip still limits usable pull');kind='structured stats'
  elif text=='Nominal drive +35%. Full price from stock engine.':value=t('Pull')+' +35% · '+t('Stock')+' → '+t('Engine level')+' 3';kind='structured stats'
  elif text=='Drive +25%; top speed -20%.':value=t('Pull')+' +25% · '+t('Speed')+' -20%';kind='structured stats'
  elif text=='Ride height +15 cm; improved support on uneven ground.':value=t('Ground clearance')+' +15 cm · '+t('Off-road suspension');kind='structured stats'
  # Scenarios with an obstructing tree keep the mandatory first step.
  if text in ['The wheels dug two deep muddy ruts. Clear the tree first, then pull straight out of the tracks.','The left wheels fell into the forest ditch. Move the tree and recover along the forward ramp.','Silt beside the reeds swallowed the rear wheels. Clear the tree and pull from firm ground.','The wheels slid into an icy ditch. Clear the fallen tree and keep the cable aligned with the ditch.','One side fell into the beach ditch. Clear the fallen trunk and use the forward ramp.']:
   scenario=36 if 'ruts' in text else 37 if 'Silt' in text else 39 if 'icy' in text else 38
   value=helper(2)+' '+helper(scenario);kind='reviewed scenario'
  # Technical quantities and short metric fragments retain their exact numeric suffixes.
  if re.match(r'^(Snow grip|Sand grip|Nisip) [+-]',text):
   label='Snow' if text.startswith('Snow') else 'Sand';value=t(label)+' '+text.split(' ')[-1];kind='structured stats'
  if text in ['All-round · 1.00×','Universale · 1.00×']:value=t('All-round')+' · 1.00×';kind='structured stats'
  if value!=text:
   lead=text[:len(text)-len(text.lstrip())];trail=text[len(text.rstrip()):]
   value=lead+value.strip()+trail
  values[text]=value;kinds[text]=kind
 # Preserve the distinction between a local-play limitation, a dropped connection and readiness.
 for key in list(values):
  if key in terms:values[key]=terms[key];kinds[key]='authored'
 for key in ['Your game is ready.','Connected. Get your crew ready.']:
  values[key]=t('Ready');kinds[key]='reviewed mapping'
 values['Using reset removes the no-reset bonus']=t('Using reset removes the no-reset bonus')
 values['Pull · Weight · Top speed']=' · '.join(t(k) for k in ['Pull','Weight','Speed'])
 values['No resets +10%  ·  No breaks +10%']=t('No resets')+' +10% · '+t('No cable breaks')+' +10%'
 values['First recovery +25%  ·  ']=t('First recovery')+' +25% · '
 values['  ·  First recovery +25%']=' · '+t('First recovery')+' +25%'
 values['Four-wheel set · mass ']=t('Four-wheel set')+' · '+t('Mass')+' '
 values[' kg · top speed ']=' kg · '+t('Speed')+' '
 values[' km/h.\nMud resistance: ']=' km/h.\n'+t('Mud resistance')+': '
 values['×. Compare surface grip below.']='× · '+t('Grip')
 values['Only local play is available on this platform.']=t('Only local play is available on this platform.')
 values['A small crew. A heavy pull.']=t('A small crew. A heavy pull.')
 values['Gentle feedback for cable load, winching, impacts and recovery events. Turn it off at any time.']=t('Gentle feedback for cable load, winching, impacts and recovery events. Turn it off at any time.')
 # Translate compound labels using reviewed terminology. Longest source wins; brands/design identifiers are retained.
 tokenkeys=sorted([k for k in terms if '{' not in k and '\n' not in k and k.strip()==k and len(k)>1],key=len,reverse=True)
 patt=re.compile(r'(?<![\w])(?:'+'|'.join(re.escape(k) for k in tokenkeys)+r')(?![\w])',re.I)
 for text,value in list(values.items()):
  if kinds[text]=='fallback':
   value=patt.sub(lambda m:t(m[0]),text)
   if value!=text:kinds[text]='composed terminology'
   values[text]=value
 explicit={
  'Sell {0} for {1} CR?\n\nYou\'ll need to buy it again to use it later.':t('Sell')+' {0} · {1} CR?\n'+helper(19),
  'Hold {0} to reel in':'{0} · '+t('Reel in'),
  'Drive a little using {0} and {1}':'{0} / {1} · '+t('Accelerate')+' / '+t('Reverse'),
  '{0} to drive and reverse. {1} to steer. Brake before tight turns.':'{0} · '+t('Accelerate')+' / '+t('Reverse')+'\n{1} · '+t('Steer')+'\n'+t('Brake'),
  'Make {0} your Stock vehicle?':t('Choose')+' {0}?\n\n'+helper(6),
  'Abandon "{0}" without payment?':'{0}\n\n'+helper(28),
  'Restore the default {0} settings? Career progress is kept.':'{0}\n\n'+helper(14),
  'Choose a keyboard key for {0}. Duplicate and reserved bindings are rejected.':'{0}\n\n'+helper(9),
  'Already used for {0}. Choose another key, or Esc to cancel.':'{0}\n'+t('Press a key. Esc cancels.'),
  'Keep the new display settings?\n\nReverting in {0} seconds.':helper(17)+'\n'+t('Time')+': {0} s',
  "Replace setup {0} with your currently fitted setup?\n\nThe part you're only previewing will not be saved.":'{0}\n\n'+helper(21)+'\n'+helper(20),
  '{0} fitted and saved':'{0} · '+t('Fitted and saved'),'{0} fitted setup saved':'{0} · '+t('Saved'),'{0} setup fitted and saved':'{0} · '+t('Fitted and saved'),
  'Part sold · {0} CR returned':t('Part sold')+' · {0} CR · '+t('Returned'),
  'NEED {0} CR':t('Need')+' {0} CR','BUY & FIT · {0} CR':t('Buy & fit')+' · {0} CR',
  'Replay base  {0} CR   +   Bonuses  {1} CR':t('Replay')+' · '+t('Base')+' {0} CR + '+t('Bonuses')+' {1} CR',
  'Base  {0} CR   +   Bonuses  {1} CR  ·  First recovery +25%':t('Base')+' {0} CR + '+t('Bonuses')+' {1} CR · '+t('First recovery')+' +25%',
  'Base  {0} CR   +   Bonuses  {1} CR':t('Base')+' {0} CR + '+t('Bonuses')+' {1} CR',
  '{0} completed recoveries':'{0} · '+t('Completed'),'{0} available recoveries':'{0} · '+t('Available'),
  '{0} part(s) will return to Stock':'{0} · '+t('Stock'),
  'Equipped on {0}':t('Equipped')+' · {0}','Used in {0} setup':t('Saved setups')+' · {0}',
  'Delivered to {0}':t('Delivery')+' · {0}','LANGUAGE: {0}':t('Language')+': {0}',
 }
 for key,value in explicit.items():values[key]=value;kinds[key]='reviewed format'
 # Small labels and instructional fragments are deliberately compact in narrow HUD areas.
 values.update({
  'Select letters with the D-pad or left stick. Choose Done to save, or Back to cancel.':'D-pad / Left stick · '+t('Select')+'\n'+t('Done')+' · '+t('Saved')+' / '+t('Back')+' · '+t('Cancel'),
  'Third-party notices could not be loaded.':t('Warning')+' · '+t('Third-party notices'),
  'Appearance only · changes the look of your vehicle.':t('This part only changes appearance.'),
  'Appearance only':t('This part only changes appearance.'),
  'No matching parts. Switch to All or choose another tire type.':t('No matching parts')+'. '+t('Choose All or another tire type to see more parts.'),
  'No matching parts. Switch to All or another tire type.':t('No matching parts')+'. '+t('Choose All or another tire type to see more parts.'),
  'Price: low to high':'CR 0-9',
  'Hold ':t('Reel in')+' · ',
  ' for ':' · ',
  ' fitted setup saved':' · '+t('Saved'),
  ' setup fitted and saved':' · '+t('Fitted and saved'),
  'Replace setup ':t('Saved setups')+' · ',
  ' to reel in':' · '+t('Reel in'),
  ' cm clearance':' cm · '+t('Ground clearance'),
  'Pay out cable':t('Pay out'),
  ' · Choose a recovery':' · '+t('Choose a recovery'),
  'TRAINING · ':t('Training')+' · ',
  'WELCOME TO TOW TROUBLE':'TOW TROUBLE',
  'Preparing your vehicle…':t('Loading')+'…',
  'Waiting for a friend…':t('Connecting')+' · '+t('Crew'),
  'Waiting for your friend':t('Connecting')+' · '+t('Crew'),
  'Waiting for another driver':t('Connecting')+' · '+t('Crew'),
  'Ready to depart':t('Ready'),
  'Getting ready':t('Loading'),
  'Your crew is ready':t('Your crew')+' · '+t('Ready'),
  'You are hosting':t('Host'),
  'Connected to host':t('Host')+' · '+t('Ready'),
  'Direct IP / LAN · UDP 7777 · 2 players':'IP / LAN · UDP 7777 · 2',
  'the practice bay':t('Training'),
  '  Reel in / pay out':'  '+t('Reel in')+' / '+t('Pay out'),
  'TOW TROUBLE  •  SOLO / CO-OP':'TOW TROUBLE',
  ' · Drive / reverse\n':' · '+t('Accelerate')+' / '+t('Reverse')+'\n',
  'Grass / gravel':t('Road'), 'Ditch / gravel':t('Road'), 'Mud / gravel':t('Mud')+' / '+t('Road'),
  'End training? Your career progress is unchanged.':t('End training')+'? '+helper(22),
  'Take your time. Solo play is paused.':t('Paused'),
  'Take your time. The game is paused.':t('Paused'),
  'MAKE IT COMFORTABLE':t('Make it comfortable'),
  'PREVIEW FIRST':t('Preview first'),
  'NEXT CONTRACT':t('Next contract'),
 })
 values.update({
  'Performance reduces visual cost. Quality keeps more detail. Apply changes to preview the selected preset.':t('Performance reduces detail. Quality keeps more detail. Apply to preview.'),
  'Reverses vertical camera movement for mouse and right stick.':t('Invert vertical camera movement.'),
  'Show short contextual help while learning. Mission objectives and cable warnings always remain visible.':t('Show contextual hints. Objectives and cable warnings stay visible.'),
  'Contextual hints help you learn at your own pace. Reset Progress starts a new career and keeps your preferences.':t('Hints help you learn. Reset Progress erases the career and keeps preferences.'),
  'Slows under load':t('Slows under load'),
  'Reverting in {0} seconds.':t('Time')+': {0} s',
  'Already used for {0}':t('Controls')+' · '+t('Locked')+': {0}',
  'MODE: {0}':t('Display')+': {0}',
 })
 # Numeric formatted settings and control hints reuse terms, retaining placeholder spelling exactly.
 for text in values:
  if '{' in text and text not in explicit and text not in ['Reverting in {0} seconds.','Already used for {0}','MODE: {0}']:
   value=patt.sub(lambda m:t(m[0]),text);values[text]=value
   if value!=text:kinds[text]='reviewed format'
 values[' · Drive    Left stick · Steer']=' · '+t('Drive vehicle')+'    '+t('Left stick')+' · '+t('Steer')
 values['+10% bonus']='+10% · '+t('Bonuses')
 values['Bonus missed']=t('Bonus missed')
 values['Replay base  ']=t('Replay')+' · '+t('Base')+'  '
 values[' CR   +   Bonuses  ']=' CR + '+t('Bonuses')+'  '
 values['the drop-off']=t('the drop-off')
 values={k:v.replace('\u00ad','').replace('→','>') for k,v in values.items()}
 (root/'Translations'/f'{code}.json').write_text(json.dumps(values,ensure_ascii=False,indent=2)+'\n',encoding="utf-8")
 coverage[code]={'authoredConcepts':len(terms),'translatedEntries':sum(v!=k for k,v in values.items()),'fallback':[k for k,v in values.items() if v==k],'kinds':{k:sum(v==k for v in kinds.values()) for k in sorted(set(kinds.values()))}}
print('source',len(source),'locales',len(allterms),'ro translated',coverage['ro']['translatedEntries'])
(root/'Coverage.json').write_text(json.dumps(coverage,ensure_ascii=False,indent=2)+'\n',encoding="utf-8")
