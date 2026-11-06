# Tow Trouble — demo conform documentului de proiect

Sursa de cerințe este `Docs/Design/TOW_TROUBLE-source.md`, copia documentului utilizatorului. Selecția anterioară Low_Poly_Environment_World a fost greșită: folderul și referințele sale au fost eliminate. Materialul derivat și cele două prefabricate procedurale vechi au fost eliminate.

## Asseturi integrate

- **Stylized Nature & Environment MEGA PACK — So Stylized**: terenul, vegetația, stâncile, apa și cerul. Scena demonstrativă originală este adaptată în copie; hărțile de înălțime, texturi, detalii și arbori sunt rotite împreună, iar culoarul de recuperare este nivelat. Asseturile sursă nu sunt modificate.
- **Low Poly Road Pack — Broken Vector**: accesul rutier spre atelier, cu material armonizat.
- **Hot Rod Constructor: Modular Battle Cars — Slava Z.**, produs 303248: Scout, Hauler, Buddy și ținta de recuperat, construite din prefabricatele Historic_Taxi, Historic_Villager și Historic_Bulldog. Fără arme.
- **Hot Rod Constructor: Modular Off-Road Cars — Slava Z.**, produs 346948: Ranger, din Goblin_Offroad. Ambele pachete au fost descărcate prin Package Manager din achizițiile contului Unity conectat.
- **Living Mud URP 1.0 — Dinopunch**: materialul reactiv și textura de urme pentru zona contractului 03.
- **FishNet 4.7.2**, Tugboat UDP 7777: autoritate fizică pe gazdă, două mașini și două cabluri pe aceeași țintă.

## Pornire și flux

Scena de pornire rămâne `Assets/TowTrouble/Scenes/CanyonRecovery.unity` (numele intern al scenei inițiale); conținutul jucabil este acum **Valea Verde**.

Titlu → configurare inițială → meniu principal vertical → contracte / vehicule / cooperare → misiune → rezultate. „Continuă” apare când există progres salvat. Setările și progresul sunt salvate local, într-un profil versionat.

Prima versiune are cele trei contracte cerute de secțiunea 8 din MD:

1. **Primul cârlig**: tutorial, țintă pe iarbă și livrare scurtă.
2. **La marginea drumului**: țintă în șanț, cu unghi de tragere spre drum.
3. **Roți în noroi**: rezistență crescută, recuperare și livrare peste trecerea de lemn.

Celelalte nouă contracte sunt etapa ulterioară din MD; nu sunt prezentate drept conținut jucabil. Filele Lacul Albastru și Canionul Roșu explică această etapă.

## Cooperare

„Joacă în doi” → gazda creează sesiunea; al doilea jucător introduce IP-ul gazdei. Pentru două instanțe pe același calculator: `127.0.0.1`. În LAN se folosește adresa locală a gazdei. Ambii aleg mașina și apasă Pregătit. Gazda pornește după conectarea și pregătirea ambilor jucători.

Lobby-ul folosește aceleași două locuri de parcare, nume, culori albastru/portocaliu și starea pregătit. Gazda alege contractul. Meniul de pauză este local în online. Relay-ul, matchmaking-ul public și integrarea Steam sunt etape ulterioare.

## Controale implicite

| Acțiune | Tastatură / mouse | Controller |
| --- | --- | --- |
| Condus | WASD / săgeți | Stick stânga; RT/LT |
| Frână | Space | B |
| Conectare / desprindere | F | A |
| Scurtare / alungire cablu | Q / E | LB / RB |
| Punct sigur | R | Select |
| Rotire cameră | Mouse dreapta | Stick dreapta |
| Recentrare cameră | C | Y |
| Semnale gata / trage / oprește | 1 / 2 / 3 | D-pad sus / dreapta / jos |
| Pauză / reluare | Escape | Start |

Tastele de condus, frână și cablu pot fi schimbate în Setări. Selectarea cu controllerul are contur și deplasare vizibilă. În Vehicule, modelul poate fi rotit cu mouse-ul sau stickul drept. Culorile disponibile sunt original, crem, măsliniu și muștar.

## Interfață și progres

HUD-ul urmează compoziția conceptelor: obiectiv sus stânga, viteză jos dreapta, cablu contextual jos stânga, CO-OP numai în cooperare. Tensiunea folosește verde / galben / roșu și text. Cablul colegului și numele colorate apar în cooperare.

Rezultatele arată timpul, resetările, cablurile rupte și medaliile pentru finalizare, lipsa resetărilor și lipsa cablurilor rupte. Finalizarea deschide contractul următor și vehiculele alternative. Profilul păstrează separat obiectivele opționale.

Setările includ volum general/efecte/UI, calitate, mod fereastră și rezoluție, luminozitate, taste, sensibilitate și inversare cameră, dimensiune text/HUD, mișcare redusă, simboluri tensiune, indicatoare tutorial și vibrații. Sunetul de motor este încă un sunet sintetic de prototip; în MD nu este selectată o bibliotecă audio finală.

## Verificare

Rezultatele pentru refacerea curentă sunt păstrate separat în `Docs/Validation/SpecRefit`. Rapoartele mai vechi din `Docs/Validation` descriu prototipul anterior și nu confirmă aspectul sau conținutul actual.

Conform instrucțiunii explicite a utilizatorului, nu se mai creează builduri. Validarea continuă numai în Unity Editor / Play Mode. Arhivele și rapoartele native anterioare nu confirmă versiunea curentă.

Verificarea în Editor: 10 teste EditMode trecute; navigare cu controller, setări și toate cele trei contracte finalizate în același Play Mode, fără cabluri rupte; pauză solo verificată. Două instanțe Editor au verificat autoritatea FishNet, finalizarea cooperativă, deconectarea și revenirea în lobby. Scena are zero scripturi lipsă, zero referințe la pachetul eliminat și zero materiale cu shader incompatibil. Rapoarte: `EditorSmoke.json`, `EditorCoopSmoke.json`, `EditorChecks.json`.

Aspectul este încă de demo: marginile drumului/pârâului, așezarea atelierului și compoziția decorului necesită finisare pentru apropierea de concepte. Există avertismente Unity pentru coliziunea arborilor nativi pe teren, serviciul AI al Editorului și suprafețele de adâncime Metal; testele curente nu au înregistrat erori Console.

## Living Mud — integrarea nouă

În contractul **03 — Roți în noroi**, noroiul simplu este înlocuit de o suprafață Living Mud cu material umed, margine mai uscată și făgașe discrete. Urmele celor 12 roți se compun pe GPU din pozițiile mașinilor existente, inclusiv pe client. Fiecare sesiune are material și texturi proprii; revenirea în lobby/reînceperea contractului curăță urmele. Escape nu mai este preluat de componenta demonstrativă Living Mud.

Noroiul rămâne dezactivat în scena inițială și se activează prin sistemul existent numai pentru contractul 03. Deschide `Assets/TowTrouble/Scenes/CanyonRecovery.unity`, pornește tu Play Mode și alege contractul 03 pentru test. Urmele nu modifică geometria de coliziune în timpul rulării. Actualizarea de fizică descrisă mai jos aplică rezistența la contactele reale ale roților.

Componentele necesare și dependențele lor ocupă aproximativ 83 MB. Scenele demo, pachetul VFX Graph și manifestul original al arhivei nu au fost importate. Instrumentul `Tow Trouble/Integrate Living Mud in Tutorial` reaplică doar zona de noroi; regenerarea demo-ului include aceeași integrare.

Verificarea acestei integrări: compilare fără erori, shader DMS și shader pentru urme suportate, zero legături sau scripturi lipsă, coliziune pe layer 8, previzualizare statică și citire GPU cu 13.698 pixeli marcați. **Nu s-a pornit Play Mode și nu s-au creat builduri.** Condusul, remorcarea și sincronizarea vizuală a urmelor rămân pentru testul utilizatorului. Captura din `Docs/Validation/LivingMud/Editor-StaticPreview.png` este o previzualizare statică cu urme de exemplu, eliminate înainte de salvarea scenei.

### Corecție urme și murdărire anvelope — 2026-10-31

Shaderul DMS rotește coordonatele cu 180° când `_Flip_UV` este 0. Suprafața tutorialului folosește acum `_Flip_UV = 1`, iar urmele sunt plasate cu `RaycastHit.textureCoord` de la același tip de contact folosit de suspensie. Nu se mai estimează UV-urile din bounding box.

`RecoveryWheelMud` aplică shaderul original Living Mud `Texture with Mud` pe roțile tuturor celor patru modele, pentru fiecare variantă de vopsea. Caroseria folosește în continuare materialele existente. Murdărirea crește pe fiecare roată aflată în noroi, rămâne vizibilă după ieșire și se reduce lent; lobby-ul și reînceperea curăță starea. Materialele comune nu sunt modificate în timpul rulării; nivelul de noroi se transmite cu MaterialPropertyBlock.

Verificare statică: toate cele patru contacte ale țintei produc pixeli marcați în dreptul UV-ului corect; materialul pentru anvelope și shaderul de urme sunt suportate. `Editor-WheelContact-Fixed.png` arată o previzualizare cu murdărire de exemplu, restaurată apoi la starea inițială. Rularea efectivă și testul cu doi jucători rămân la utilizator.

Pentru retest: pornește contractul 03, intră cu o mașină în noroi și verifică urmele direct la contactul roților și acumularea de noroi pe anvelope. Ieși pe pietriș: roțile trebuie să rămână murdare. Verifică și schimbarea vehiculului/culorii în lobby.


### Tracțiune și feedback în noroi — 2026-10-31

`RecoveryMudTerrain` definește aderența longitudinală/laterală și rezistența suprafeței. Solverul gazdei verifică separat fiecare roată: numai contactele cu acest collider primesc comportamentul de noroi. Forțele de accelerare, frânare și virare împart aceeași limită de aderență. Accelerația agresivă scade tracțiunea și produce patinare; rezistența crește cu masa și dispare pentru roțile care ies de pe suprafață. Pentru ținta contractului 03, rezistența veche de 1700 este distribuită peste cele patru contacte posibile, nu aplicată după coordonata Z. Contractul 02 păstrează rezistența șanțului.

Fiecare roată are rotație și patinare separate. În noroi, viteza și patinarea controlează stropii și acumularea pe anvelope; praful uscat scade. Motorul răspunde la accelerație, patinare și sarcina cablului. HUD-ul afișează un indiciu contextual dacă indicatoarele tutorial sunt activate. Cele 12 emițătoare au maximum 336 particule în total și nu pornesc automat în Editor.

FishNet transmite aceste valori vizuale în snapshoturile existente de 15 Hz. Gazda păstrează autoritatea fizică. Ambii jucători trebuie să folosească aceeași versiune actualizată a proiectului, deoarece structura snapshotului s-a extins.

Validare: compilare reușită, limitele forțelor calculate pentru toate cele patru modele, tracțiune simetrică în marșarier, rezistență opusă mișcării și zero referințe/scripturi lipsă în scenă. Serializatoarele generate FishNet au trecut verificarea scriere/citire pentru toate câmpurile VehicleFrame (68 bytes). Raport: `Docs/Validation/LivingMud/PhysicsAndFeedback.json`. Aceste verificări statice nu confirmă senzația de condus sau sincronizarea într-o sesiune reală. Nu s-a pornit Play Mode și nu s-au făcut builduri pentru această actualizare.

Pentru testul utilizatorului în Editor:

1. Contractul 03: intră cu una, apoi cu toate roțile în noroi. Compară accelerația progresivă (controller) sau impulsuri scurte W cu accelerația continuă la maximum; urmărește tracțiunea, patinarea, stropii și sunetul motorului.
2. Trage ținta cu cablul și verifică sarcina, frâna Space, marșarierul și revenirea aderenței după ieșirea din noroi.
3. Reîncepe și revino în lobby: urmele, murdărirea și particulele trebuie curățate. Verifică și contractele 01/02 pe teren uscat.
4. În cooperare, verifică pe ambele calculatoare roțile și stropii colegului, recuperarea comună și resetarea sesiunii.

Instrumentul `Tow Trouble/Integrate Mud Physics and Feedback` reaplică legăturile în tutorial. Scenele originale `M_Demonstration_Mega` și `M_Demonstration_Desert` nu sunt modificate.


## Garaj, economie și contracte în explorare — 2026-10-31

Designul curent este `Docs/Design/TOW_TROUBLE-open-world.md`. Documentul sursă anterior rămâne referința istorică a tutorialului. Punctele 1–4 sunt integrate în zona existentă; adaptarea Mega și Desert este etapa 5, separată și neefectuată.

### Cum testezi în Editor

1. Deschide `Assets/TowTrouble/Scenes/CanyonRecovery.unity` și pornește tu Play Mode.
2. Din meniul principal alege **Explorează**. Pornești în curtea atelierului existent, cu configurația salvată. **Tutorial / Contracte** păstrează fluxul vechi.
3. Oprește în marcajul garajului și apasă **G** (controller: X). Garajul este disponibil fără cablu și fără intervenție activă. Rotește mașina cu mouse-ul pe zona liberă sau stickul drept.
4. Alege categoria și piesa. Previzualizarea nu cumpără. **Cumpără și montează** debitează o singură dată și salvează; piesele deținute se montează gratuit. Schimbarea caroseriei poate demonta piese incompatibile din configurație, păstrându-le în inventar.
5. Salvează configurația montată în Pădure / Munte / Nisip. Încarcă altă configurație, închide și redeschide Play Mode pentru verificarea persistenței. Vânzarea plătește 70%; piesa trebuie demontată și eliminată din preseturi înainte de vânzare.
6. Ieși din garaj. **Tab** (controller: apăsare stick stânga) deschide intervențiile: distanță, teren, dificultate, recompensă și pregătire recomandată. Acceptă o intervenție și condu la țintă; mașina jucătorului nu este teleportată.
7. Recuperează și livrează. Rezultatele arată recompensa și soldul; **Continuă explorarea** păstrează mașina la poziția curentă. Repetarea ecranului nu trebuie să dubleze plata. Abandonarea prin Tab cere confirmare și nu plătește.

Nu există încă trasee noi în Mega, deșert jucabil sau misiuni Greu/Expert. Cele trei recuperări existente oferă recompense de bază 450 / 800 / 1300 CR. Duratele scurte ale tutorialului nu validează economia pe oră a viitoarei lumi deschise. Roțile pentru nisip și iarnă sunt disponibile ca aspect/configurație; comportamentul pe suprafețele respective se conectează la etapa biomurilor.

### Conținut și salvare

Catalogul conține 380 intrări în 16 categorii, incluzând echipamentul inițial și configurațiile funcționale. Audit: `Docs/Design/HotRod-AssetAudit.csv` (717 prefaburi sursă). Sumar: `Docs/Design/HotRod-Catalog.md`. Compatibilitatea verifică familia și socketurile; potrivirea vizuală a combinațiilor necesită verificare. Prefaburile derivate ocupă aproximativ 10 MB, cu referințe la mesh-urile/texturile existente.

Profilul v2 migrează profilul v1 păstrând progresul, setările și culorile. Contractele istorice finalizate primesc o singură creditare de migrare. Se păstrează copia profilului anterior în PlayerPrefs, iar verificările agentului nu scriu în profilul real. Motorul nivel 3 costă 8800 CR direct sau 6000 CR la schimb cu motorul nivel 2; schimbul înlocuiește referințele vechi din configurații/preseturi.

Gazda validează starea, participantul și piesele catalogului. Configurațiile se transmit la schimbare. Contractul nu poate fi acceptat cât timp un coechipier este în garaj. Banii rămân progres local al fiecărui jucător, nu economie competitivă cu backend.

### Dovezi și limite

Compilare fără erori; 14 verificări Edit Mode de economie/migrare/configurații au trecut, invocate direct fără Test Runner; 38 de caroserii asamblate static, kit de aripi cu toate cele patru piese vizibile; zero scripturi și referințe de prefab lipsă. Panourile au fost randate într-o scenă temporară, la text normal și 130%, fără depășiri ale zonelor de text și fără rularea jocului. Rapoarte în `Docs/Validation/Garage`.

**Nu s-au creat builduri și nu s-a pornit Play Mode.** Condusul cu ansamblurile noi, interacțiunile efective, persistența pe profilul real și sesiunea FishNet rămân pentru testul utilizatorului.


## Finisare UI după logo — 2026-10-31

Tema folosește acum butoane aurii, panouri cu margini, titluri rotunjite și pictograme native. Garajul grupează piesele în Stil / Tehnic / Tractare; contractele au fotografii statice din tutorial și recompense distincte. HUD-ul rămâne discret, cu obiectiv, cablu și viteză în pozițiile din referințe. Camera garajului pornește în fața atelierului și evită pereții.

Compilare fără erori, 27 verificări statice de pagini RO/EN inclusiv text 130%, două verificări HUD și 14 aserțiuni Edit Mode de regresie reușite. Fonturile includ diacritice. Rapoarte și imagini: `Docs/Validation/UI-Finish`; direcția vizuală: `Docs/Design/UI-Style.md`. Profilul folosit în imagini este fictiv. Nu s-a pornit Play Mode și nu s-au creat builduri. Utilizatorul verifică interacțiunile efective în Editor.


## Lumea deschisă — prima hartă, 2026-10-31

Deschide `Assets/TowTrouble/Scenes/OpenWorldRecovery.unity` (lăsată activă în Editor), pornește tu Play Mode și alege **Explorează**. Din tutorial, Explorează încarcă aceeași hartă. Butonul Tutorial / Contracte din meniul hărții revine la scena tutorialului. Registrul EditorBuildSettings conține ambele scene pentru această trecere; nu s-au creat builduri.

Harta are 24 de recuperări, 8 predări A–H, garaj central G, șase biomuri, râu/poduri și acces la vârf pe serpentine. Tab deschide contractele. Available afișează următoarele trei recuperări nefinalizate, fără pagini; după finalizare apare următoarea. Completed păstrează toate recuperările finalizate într-o listă cu derulare verticală și permite rejucarea lor. G deschide garajul lângă marcaj, cu mașina oprită. Nu sunt teleportate mașinile jucătorilor la acceptarea unui contract. Ținta este activată la amplasamentul său.

Începe cu R01 și predarea A. Apoi verifică R03 în noroi, R17 pe nisip și R13 pe zăpadă, după montarea anvelopelor potrivite. Verifică remorcarea la altitudine, intrarea în predare și R pe drum/pod. Nu toate contractele Expert sunt potrivite configurației inițiale; nu există blocări artificiale de regiune. La finalizare, continuă explorarea.

Gazda FishNet păstrează autoritatea asupra fizicii și misiunii. Pentru cooperare, ambii jucători trebuie să ruleze aceeași versiune și noua scenă. Conectarea din meniul tutorialului duce la harta open-world. Testul multiplayer nu a fost efectuat.

Validare: compilare fără erori, 19 aserțiuni pure Edit Mode invocate direct fără Test Runner, toate punctele conectate în rețeaua de drumuri, 16 randări statice ale panoului RO/EN la text 130%. Rapoarte în `Docs/Validation/OpenWorld`. Fotografii de contract statice, cu mașina pozată, fără simulare. Nu s-a pornit Play Mode și nu s-au creat builduri.


### Corecție pornire solo — 2026-10-31

Blocajul NullReferenceException la pornirea hărții venea din ținta inițial inactivă: ResetRecovery apela SetSpawn înainte ca Awake să pregătească Rigidbody. RecoveryVehicle are acum inițializare comună, executată o singură dată, înainte de spawn/reset/simulare. Activarea ulterioară păstrează poziția și geometria. Nu au fost editate scenele sau aspectul hărții în această corecție.

Aceeași reproducere izolată nu mai aruncă excepția; cinci verificări Edit Mode și verificările celor trei prefaburi au trecut. Rapoarte: Docs/Validation/SoloStartup. Nu s-a pornit Play Mode, Test Runner, server/client sau build. Pentru retest: Play → Explorează (solo), apoi Tab → R01 → Acceptă.

## Aspectul hărții open world — 2026-10-31

OpenWorldRecovery are acum relieful și drumurile netezite, trasee cu curbe, texturi și margini estompate, atelier detaliat, vegetație pe biomuri, apă și poduri refăcute, iluminare proprie și nori nativi. Cele 24 de recuperări și 8 predări sunt păstrate. Tutorialul și scenele originale Mega/Desert nu au fost modificate. Imagini comparative și verificări statice: `Docs/Validation/VisualPolish`.

Pentru această etapă nu s-a pornit Play Mode și nu s-au creat builduri. Utilizatorul verifică în Editor: Explorează → garaj → Tab → R01, apoi accesul spre lac, deșert și zăpadă. Performanța și comportamentul fizic după remodelare necesită această verificare.

Recovery-board controls (2026-11-06): Q/E changes Available/Completed, W/S or Up/Down selects a recovery, Enter accepts or opens abandonment confirmation, Esc returns to driving. Controller navigation uses shoulders and D-pad/left stick. Prompts follow Xbox (A/B, LB/RB), PlayStation (Cross/Circle, L1/R1) or Nintendo (A/B, L/R, east A confirms). This board/confirmation is the first family-aware menu; broader full-game gamepad support remains planned. Validation: Docs/Validation/BoardControls; authorized isolated Solo passed29 checks with virtual input, including Nintendo's swapped physical face positions. Physical controllers and a multiplayer peer were not tested; no build was created.


## Garage UX and controls — 2026-11-06

Current garage flow in OpenWorldRecovery: stop at the workshop with no cable or active recovery, then G (Xbox X / PlayStation Square / Nintendo Y). Highlight a recovery in the board before entering the garage to prepare for that job. Equipment advice uses the fitted build, surface needs and nominal breakout load; terrain, slope and positioning still affect actual towing.

- Style contains appearance parts. Tech contains Body, Wheels, Engine, Transmission, Suspension and Chassis. Towing contains Cable and Winch. Parts stay in the three-column grid, ordered by price then readable model/level order; duplicate bodies are labeled Classic / Hot Rod.
- Selection previews one part against the fitted setup. Equipped, Owned and Preview badges distinguish states. Enter / controller Confirm buys and fits only that part, or fits an owned part free. Leaving discards the preview. Body changes list incompatible fitted parts before payment; those parts remain owned.
- Compare fitted → preview below the car. Wheels show Road/Mud/Snow/Sand grip; mass is a tradeoff rather than an automatic upgrade. All/Owned and tire-role filters narrow the catalogue.
- Saved setups opens four legacy-compatible slots with Empty/Saved/Active states. NAME cycles a controller-friendly label. SAVE FITTED saves mounted equipment, never an unbought preview. Applying is free; overwriting asks first. Selling pays 70% and asks for confirmation; fitted or saved parts explain why they cannot be sold.

| Garage action | Keyboard | Controller |
| --- | --- | --- |
| Change Style / Tech / Towing | Q / E | LB/RB, L1/R1, L/R |
| Select category / grid item | WASD / arrows; left from first grid column enters categories, right returns | D-pad / left stick; same layout |
| Buy & fit / fit owned | Enter | Xbox A / PlayStation Cross / Nintendo A (east) |
| Back / cancel review / drive | Esc | Xbox B / PlayStation Circle / Nintendo B (south) |
| All / Owned | Tab | X / Square / Y |
| Tire role | T / R | LT/RT, L2/R2, ZL/ZR |
| Recommended part | F | Y / Triangle / X |
| Saved setups | P | Menu / Options / + |
| Sell selected unused owned part | Delete | View / Share / − |
| Rotate car / reset view | Drag free car area / C | Right stick / RS press (R3) |

Enter/Confirm activates the highlighted button inside Saved setups and confirmation dialogs. Modal reviews block camera rotation and accept no held-button carryover. Garage and Completed lists share smooth scrolling and the reduced-motion setting. Gamepad prompts switch with the last input device. Full gamepad support in other gameplay/menus remains a separate task.

Validation of this garage iteration: 57 direct-invoked NUnit cases, 36 static layouts at text100/130%, and 62 actual Play/Solo checks using keyboard and virtual controller devices passed. Player preferences were restored exactly and Unity left stopped. Physical controller hardware and co-op peers still need testing. Evidence: `Docs/Validation/GarageUX`.


## Alegerea primei mașini — 2026-11-06

La primul joc, după setările inițiale și înainte de Solo/Co-op, jucătorul alege gratuit exact un model: Scout, Hauler, Buddy sau Ranger. Previzualizarea nu oferă mașina; alegerea cere confirmare și se salvează. Modelul ales devine propria mașină Stock. Celelalte variante costă1.200CR fiecare și sunt alternative cu mici diferențe, nu upgrade-uri de putere. Misiunile nu acordă alte mașini gratuit.

| Starter | Forță de bază | Masă | Viteză maximă |
| --- | --- | --- | --- |
| Scout | 8,4kN | 930kg | 60km/h |
| Hauler | 8,8kN | 960kg | 56km/h |
| Buddy | 8,0kN | 900kg | 64km/h |
| Ranger | 8,6kN | 950kg | 58km/h |

Prima caroserie cumpărabilă, Midjet Wood, costă1.400CR și oferă10kN,980kg,74km/h: îmbunătățește forța, masa de ancorare și viteza față de oricare starter. Toate cele37 de caroserii cumpărabile au forța de bază mai mare decât toate cele patru variante Stock. Comparația păstrează aceeași echipare; aderența, panta și sarcina limitează forța utilizabilă. Plafonul general rămâne80km/h. Masele vehiculelor de recuperat nu au fost reduse.

Cablurile rămân11kN inclus,14kN200CR,18kN700CR,26kN1.400CR,36kN3.400CR. Două intervenții inițiale Local+Standard, terminate curat pentru prima dată, plătesc împreună1.562CR și permit prima caroserie dacă nu se cumpără alte piese între timp.

Profilul v4 păstrează progresul, banii, inventarul, configurațiile și accesul deja deținut din salvările existente. Un profil vechi fără progres sau investiții primește ecranul de alegere. Nu reseta profilul real pentru test: folosește o salvare separată. Detalii: `Docs/Design/StarterProgression.md`.

Vehicles și confirmarea folosesc WASD/săgeți sau D-pad/LS pentru navigare, Enter/Confirm pentru butonul evidențiat și Esc/Back pentru anulare. Nintendo A est confirmă/B sud anulează; Xbox A/B și PlayStation Cross/Circle urmează aceleași acțiuni. Alegerea gratuită nu se repetă la revenirea în meniul principal.

## Economia integrată și începutul campaniei — actualizare 2026-11-06

Această secțiune înlocuiește prețurile și bugetele independente de mai sus. Primele șase intervenții sunt Workshop meadow, Forest ruts, Fishing shore, Reed bank, Gravel bend, Below the mountain road. Cele trei carduri Available și lista Completed se păstrează. Apoi ofertele cresc după dificultate; completările existente rămân valide.

Primele două oferă împreună2375CR la prima finalizare chiar fără bonusurile de performanță. Body1400 sau motor800+roți de noroi600 sunt două căi de pregătire verificate. Motor3 costă2600 total,1800 după Motor2; reductor/suspensie1000; troliu14kN1600; cabluri14/18/26/36kN200/1200/2000/4200. Rejucările rămân plătite, cu baza redusă afișată înainte de acceptare și proporție mai bună la dificultăți mari.

Recompensele includ distanța reală pe drum, urcarea, livrarea și pregătirea estimată. Timpul jucătorului nu mărește plata și nu există cronometru-limită. Estimarea este calibrată cu trei drumuri și o extragere/livrare reală Ranger; toate24 încă nu au cronometrare completă cu fiecare mașină. Detalii și limite: `Docs/Validation/CareerEconomy/README.md`.


## Meniul și tutorialul actual — 2026-11-06

Această secțiune înlocuiește vechile intrări Vehicles / Tutorial-Contracts din meniul principal. Jocul și UI-ul sunt în engleză. Main are Play Solo, Play Co-op, How to Play și Settings; Credits/Quit sunt secundare. Prima configurare oferă exact un starter gratuit, cu confirmare; ulterior alte body-uri se cumpără în garajul din gameplay. Mașina din Main și statisticile reflectă echiparea montată.

How to Play oferă trei pagini de ghid și antrenament opțional pe OpenWorldRecovery, fără o scenă separată. Antrenamentul folosește o copie nesalvată a profilului, mașină de probă, conuri temporare și 5.000 CR de probă. Include condus, cameră, conectare, troliu, extragere, livrare și garaj. La sfârșit sau abandon se restabilesc banii și echiparea carierei; preferințele modificate rămân salvate. Nu se acordă recompense sau misiuni finalizate.

Navigare generală: WASD/săgeți sau D-pad/left stick; Enter / Confirm; Esc / Back; Q/E sau shoulders pentru taburi. Controllerul Nintendo folosește A est pentru confirmare și B sud pentru revenire. Indiciile comută după dispozitiv, cu pictograme Xbox/PlayStation/Nintendo și override în Controls. Formularele Co-op au tastatură virtuală pentru controller.

Settings include rebinding cu verificarea conflictelor, audio separat de VFX, Quality/Performance prin copie URP temporară, display cu Apply/Keep și revenire automată la 15 secunde, text/motion/simboluri și resetarea progresului cu preferințele păstrate. Pause permite revenire confirmată la Main. Credits rămâne neutru până la alegerea numelui creatorului; licențele disponibile sunt într-o secțiune separată.

Validare: 243/243 teste Unity, 52 randări de panel fără text depășit, navigare live cu tastatură și controller virtual, lobby cu un participant, Retry/Cancel, reset și două fluxuri complete de antrenament. Controller fizic, doi peers și fullscreen în player necesită verificare separată. Nu s-a creat build. Dovezi: `Docs/Validation/MenuFinal/README.md`.


## Paint — verificare și corecții 2026-11-06

Culoarea aleasă la starter este acum echipată și deținută de la confirmare, fără cost. Previziunea și mașina folosită în joc au aceeași aplicare a vopselei. În garaj, doar Buy & Fit cumpără o culoare nouă (100 CR); culorile deținute se remontează gratuit. Ieșirea fără cumpărare revine la finisajul montat, iar saved setups rețin culoarea. Paint nu modifică performanța.

Original/Stock înseamnă culoarea implicită a starterului sau finisajul din fabrică al caroseriei cumpărate. Poate coincide vizual cu una dintre culorile numite pe un starter. Vopseaua schimbă doar zona dedicată din atlas; geamurile, cromul, lemnul, anvelopele și decalurile își păstrează detaliile. Salvările vechi își recuperează o singură dată culoarea globală a starterului dacă nu aveau alt finisaj cumpărat echipat explicit. Banii și progresul sunt păstrați.

Dovezi: `Docs/Validation/PaintAudit` — 266 teste trecute, 164 combinații de aspect, preview/cumpărare/refitare/setups/salvare/reintrare și restaurare după tutorial testate în Play. Prezentarea stării replicate a fost verificată pentru ambele sloturi; doi peers reali rămân de testat. Fără build.
