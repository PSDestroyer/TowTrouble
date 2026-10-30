# TOW TROUBLE — document de proiect

> Versiune 0.1 · 30 octombrie 2026 · Design propus pentru implementare.
> Acest document reunește conceptul, asseturile identificate, misiunile și meniurile. Imaginile sunt concepte generate, nu capturi dintr-un build. Sistemele și meniurile descrise mai jos nu sunt încă implementate.

![TOW TROUBLE — banner](Organization/Tow-Trouble-Concepts/2026-10-30/tow-trouble-banner-v1.png)

## 1. Conceptul

Un joc de recuperări auto cu mașini retro stilizate: ajungi la un vehicul blocat, alegi poziția de tractare, prinzi cablul și îl scoți pe teren sigur. Distracția vine din combinația dintre teren, aderență, greutatea mașinilor și coordonarea dintre jucători.

- **Experiență:** recuperări scurte și satisfăcătoare, cu momente amuzante produse de fizică.
- **Cameră:** persoana a treia, ușor ridicată, cu rotire și resetare în spatele mașinii.
- **Moduri propuse:** solo și cooperare online în doi; multiplayerul se validează într-un prototip separat înainte de extinderea conținutului.
- **Structură:** trei zone compacte, cu contracte separate, construite din aceeași familie vizuală.
- **Platformă de dezvoltare inițială:** PC cu mouse/tastatură și controller. Steam este ținta inițială; Nintendo și PlayStation rămân obiective ulterioare, cu validare și integrare dedicate.
- **Identitate:** natură luminoasă, mașini Hot Rod cu forme clare, cabluri lizibile, interfață discretă.

### Bucla unei misiuni

**Alegi contractul → alegi mașina → conduci la țintă → examinezi terenul → atașezi cablul → recuperezi vehiculul → îl livrezi în zona sigură → primești evaluarea.**

Prima versiune evită mersul pe jos: selectarea cârligului și conectarea cablului se fac din mașină, la distanță mică, printr-o interacțiune clară pe ecran.

## 2. Ce există și ce urmează să construim

| Element | Starea la redactare |
| --- | --- |
| Direcție vizuală și comparație între pachete | Documentate în auditul din 28 octombrie. |
| Nouă imagini de gameplay conceptual | Salvate local, inclusiv un cadru de cooperare în doi. |
| Banner orizontal și vertical | Salvate local, generate cu imagegen. |
| Pachete de natură, drumuri, controllere, UI și efecte | Locațiile confirmate sunt în secțiunea 3. Existența fișierului nu confirmă compatibilitatea sau integrarea. |
| Hot Rod Constructor | Referință vizuală selectată; nu a fost găsit în biblioteca locală sau în cache-ul verificat. Descărcarea din cont rămâne de verificat. |
| Proiect Unity TOW TROUBLE, scene, misiuni și multiplayer funcțional | Nu au fost create/validate în cadrul acestei etape. |

## 3. Asseturile proiectului și locațiile lor

Inventarul de mai jos acoperă pachetele relevante pentru TOW TROUBLE, nu toate produsele din biblioteca generală. Sunt separate sursele de bază, alternativele și opțiunile care pot fi adăugate ulterior.

**Locații verificate:**

- Biblioteca de pe Mac: `/Users/gheorghe/UnityAssets`.
- Cache Unity Asset Store: `/Users/gheorghe/Library/Unity/Asset Store-5.x`.
- Biblioteca arhivată este documentată la `/Volumes/SEAGATE/UnityAssets`, dar discul **nu era montat** la această verificare. Nu tratăm locațiile de pe HDD ca verificate acum.
- Codurile `UA-…` provin din [registrul bibliotecii](Organization/Asset%20Catalog/asset_registry.json). Pachetele din cache pot să nu aibă cod în acel registru.

### 3.1. Baza vizuală

| Asset | Ce folosim | Locație / sursă | Decizie |
| --- | --- | --- | --- |
| **Hot Rod Constructor: Modular Battle Cars — Slava Z** | Caroserii, șasiuri, roți, variante de cabină și benă. Mașini jucabile și vehicule de recuperat, fără arme. | [Pagina produsului](https://assetstore.unity.com/packages/3d/vehicles/land/hot-rod-constructor-modular-battle-cars-303248). **Fișier local negăsit** în cele două rădăcini verificate. | Familia principală de vehicule. Trebuie descărcată și verificată lista reală de prefabs; nu inventăm o cale locală. |
| **Stylized Nature & Environment MEGA PACK — So Stylized** | Stânci, arbori, vegetație și elemente de mediu pentru pădure, canion și mal de lac. | [Pachet în cache](</Users/gheorghe/Library/Unity/Asset Store-5.x/So Stylized/3D ModelsEnvironments/Stylized Nature Environment MEGA PACK.unitypackage>) · [Asset Store](https://assetstore.unity.com/packages/3d/environments/stylized-nature-environment-mega-pack-326646). | Baza mediului. Locație confirmată; versiunea și conținutul exact se verifică la import. |
| **Low Poly Road Pack — Broken Vector** | Numai segmentele și elementele de drum potrivite pentru trasee; materialele se armonizează cu natura. | [Pachet în cache](</Users/gheorghe/Library/Unity/Asset Store-5.x/Broken Vector/3D ModelsEnvironmentsRace Tracks Roadways/Low Poly Road Pack.unitypackage>) · [Asset Store](https://assetstore.unity.com/packages/3d/environments/roadways/low-poly-road-pack-67288). | Opțional la construcția traseelor; drumurile de pământ pot fi realizate direct pe teren. |

**Important pentru niveluri:** serpentinele, locurile de împotmolire, malurile și trecerile din imaginile conceptuale sunt compoziții propuse. Nu sunt hărți gata făcute incluse în pachete. Construim aceste trasee din piesele reale și adaptăm unde geometria diferă.

### 3.2. Conducere, cablu și integrare

| Asset / sistem | Locație exactă | Utilizare și limită |
| --- | --- | --- |
| **Realistic Car Controller Pro** | [Pachet în cache](</Users/gheorghe/Library/Unity/Asset Store-5.x/BoneCracker Games/ScriptingPhysics/Realistic Car Controller Pro.unitypackage>) | Primul candidat de evaluat pentru conducere. Reglajele trebuie să favorizeze controlul la viteză mică și tractarea. Versiunea, dependențele și funcționarea multiplayer nu sunt verificate. |
| **Universal Vehicle Controller 1.4.03** · UA-0167 / UA-0168 | [Unitypackage](</Users/gheorghe/UnityAssets/Controllers/Vehicles/Universal Vehicle Controller (Without dependencies) v1.4.03.unitypackage>) · [Arhivă ZIP](</Users/gheorghe/UnityAssets/Controllers/Vehicles/Universal Vehicle Controller 1.4.03.zip>) | Alternativă pentru testul de conducere; alegem un singur controller după comparație. |
| **Ash Vehicle Physics 2.0.1** · UA-0152 | [Arhivă RAR](</Users/gheorghe/UnityAssets/Controllers/Vehicles/Ash Vehicle Physics v2.0.1.rar>) | Rezervă; nu este o dependență obligatorie. |
| **RCC.unitypackage** · UA-0157 | [Pachet local](</Users/gheorghe/UnityAssets/Controllers/Vehicles/RCC.unitypackage>) | Copie cu nume scurt; versiune neconfirmată. Nu o confundăm cu RCC Pro din cache. |
| **Cable System Physics** | Menționat anterior ca posibil instrument; **nicio arhivă locală identificată** și posesia nu este reconfirmată în această verificare. | Candidat, nu dependență confirmată. Nu presupunem că oferă remorcare auto sau sincronizare online gata făcute. |
| **Sistem propriu de tractare** | De creat în viitorul proiect Unity; nu există încă o cale de cod. | Puncte de prindere, lungime, tensiune, conectare/deconectare și reguli de recuperare. Inițial cablu vizual simplu, cu forțe fizice controlabile. |
| **Steamworks.NET 2025.161.0** · UA-0466 | [Pachet local](</Users/gheorghe/UnityAssets/Tools/Integrations and SDKs/Steamworks.NET_2025.161.0.unitypackage>) | Candidat pentru integrarea cu Steam într-o etapă ulterioară. Prezența sa nu înseamnă că lobby-ul sau multiplayerul sunt rezolvate. |

Nu importăm simultan toate controllerele. Testul inițial compară manevrabilitatea, reacția la masa remorcată, stabilitatea pe pantă și posibilitatea de integrare cu sistemul de cablu.

### 3.3. Interfață și meniuri

| Asset | Locație exactă | Ce preluăm |
| --- | --- | --- |
| **Racing Game UI Pack 1.0** · UA-0513 / UA-0514 | [Pachet extras](</Users/gheorghe/UnityAssets/UI/Kits/Racing Game UI Pack 1.0/Racing UI-pack v1.0.unitypackage>) · [ZIP](</Users/gheorghe/UnityAssets/UI/Kits/Racing Game UI Pack 1.0.zip>) | Candidat pentru componente de HUD și selecție auto. Aspectul se adaptează; nu este validat ca meniu final. |
| **Kenney input prompts** · UA-0508 | [Arhivă cu pictograme](</Users/gheorghe/UnityAssets/UI/Icons and Portraits/kenney_input-prompts icons.zip>) | Indicatoare de taste și controller, după verificarea setului efectiv de simboluri. |
| **720+ Simple Vector Icons 2.2** · UA-0506 | [Arhivă ZIP](</Users/gheorghe/UnityAssets/UI/Icons and Portraits/720+ Simple Vector Icons 2.2.zip>) | Pictograme simple pentru setări, contracte, vehicule și rezultate, dacă sunt potrivite vizual. |
| **Modern UI Pack 5.4.10** · UA-0511 / UA-0512 | [Unitypackage extras](</Users/gheorghe/UnityAssets/UI/Kits/Modern UI Pack 5.4.10/Modern_UI_Pack_v5.4.10.unitypackage>) · [ZIP](</Users/gheorghe/UnityAssets/UI/Kits/Modern UI Pack 5.4.10.zip>) | Alternativă pentru componente standard; nu îl amestecăm automat cu Racing UI Pack. |
| **MenuTemplate** · UA-0517 | [Unitypackage](</Users/gheorghe/UnityAssets/UI/Menu Templates/MenuTemplate/MenuTemplate.unitypackage>) · [ZIP](</Users/gheorghe/UnityAssets/UI/Menu Templates/MenuTemplate/MenuTemplate.zip>) | Rezervă pentru structură/navigare, după inspectarea conținutului. |

Aspectul final al interfeței este cel descris în secțiunea 7. Alegerea unui pachet de UI nu înlocuiește construirea ecranelor și a navigării cu controllerul.

### 3.4. Efecte, noroi și atmosferă

| Asset | Locație exactă | Statut |
| --- | --- | --- |
| **Epic Toon FX 1.81** · UA-0540 | [Unitypackage](</Users/gheorghe/UnityAssets/VFX/Epic Toon FX v1.81.unitypackage>) | Opțional: efecte mici de praf, impact sau stropi, numai dacă există efecte potrivite după inspectare. |
| **Living Mud URP 1.0** · UA-0548 | [Arhivă RAR](</Users/gheorghe/UnityAssets/VFX/Mud/Living Mud URP 1.0.rar>) | Candidat pentru aspectul noroiului. Rezistența fizică a terenului se implementează separat; nu presupunem că acest pachet simulează împotmolirea. |
| **COZY Stylized Weather 2, arhivă v2.0.9** · UA-0560 | [Arhivă RAR](</Users/gheorghe/UnityAssets/VFX/Weather and Atmosphere/RS1099_COZY Stylized Weather 2 v2.0.9.rar>) | Opțional după prototip. Prima versiune poate folosi cer și lumină simple, fixe pentru fiecare misiune. |

**Apa:** verificăm întâi ce oferă pachetul principal de natură. Dacă nu există o soluție potrivită, construim un material simplu pentru râu/lac. Nu presupunem că apa din concepte este un shader confirmat din bibliotecă.

### 3.5. Multiplayer și sunet: ce rămâne de ales

- În cache există [PUN 2 - FREE](</Users/gheorghe/Library/Unity/Asset Store-5.x/Photon Engine/ScriptingNetwork/PUN 2 - FREE.unitypackage>). Este inventariat aici ca opțiune disponibilă, **nu ca soluție selectată sau validată** pentru acest joc.
- Există și [BR200 - Battle Royale Multiplayer - Photon Fusion](</Users/gheorghe/Library/Unity/Asset Store-5.x/Photon Engine/Complete ProjectsPacks/BR200 - Battle Royale Multiplayer - Photon Fusion.unitypackage>). Este un proiect de referință din alt gen; nu intră în baza rapidă TOW TROUBLE și nu confirmă accesul la o configurație multiplayer potrivită.
- Soluția de rețea se alege după un test cu două mașini și un obiect remorcat. Sunt necesare autoritate fizică, sincronizarea prinderilor și comportament controlat la deconectare.
- Nu a fost selectat și verificat un pachet adecvat de sunete de motor, cauciucuri, troliu, cablu, impact și natură. Muzica și efectele audio sunt asseturi încă de completat; pachetele de arme/zombie din bibliotecă nu sunt baza sonoră a jocului.

### 3.6. Resurse care trebuie construite pentru joc

| Resursă | Ce trebuie realizat |
| --- | --- |
| Traseele și cele trei zone | Teren, drumuri, obstacole și zone sigure, asamblate din asseturi. |
| Prefaburile vehiculelor | Alegerea pieselor Hot Rod, coliziuni, roți, masă, controller și puncte de tractare. |
| Cârligele și cablul | Puncte vizibile, geometrie simplă unde lipsește din pachet, afișarea tensiunii. |
| Trecerea din lemn | Asamblare din piese existente dacă sunt disponibile; altfel model simplu nou. Nu e confirmată ca prefab din pachet. |
| Indicatoare și zona de livrare | Marcaje discrete, iconiță de obiectiv, confirmare de recuperare. |
| UI final și ecrane | Panouri, butoane, stări, focus, pictograme și adaptare la controller. |
| Logică de misiuni și salvare | Contracte, obiective, progres, evaluări, checkpointuri și setări. |
| Grafică de promovare finală | Bannerurile actuale sunt concepte; logo-ul și materialele de producție se definitivează separat. |

## 4. Reguli vizuale

### Mașinile propuse

| Vehicul | Rol de gameplay propus | Aspect urmărit |
| --- | --- | --- |
| **Scout** | Mașina de început, ușoară și ușor de manevrat. | Sedan retro crem, cabină înaltă, aripi separate. |
| **Hauler** | Tracțiune stabilă pentru sarcini mai grele, viteză mică. | Pickup verde măsliniu, cabină scurtă și benă cu laterale de lemn. |
| **Buddy** | Mașină compactă, agilă, potrivită pentru poziționare și cooperare. | Coupé galben muștar, plafon rotunjit și caroserie scurtă. |
| **Vehicule de recuperat** | Ținte cu mase și poziții diferite. | Variante ruginii/maro ale aceleiași familii, fără arme. |

Numele și caracteristicile sunt propuneri de design. Formele exacte și combinațiile posibile se confirmă după importul Hot Rod Constructor; imaginile generate nu dovedesc existența unor prefabs identice.

### Mediul

Folosim stânci mari cu forme simplificate, vegetație în grupuri clare și drumuri suficient de late pentru cameră și cablu. Păstrăm suprafețele curate și paleta caldă. Nu amestecăm aceste mașini cu garaje și medii industriale fotorealiste.

Prima versiune folosește suprafețe cu proprietăți simple: pietriș, nisip, noroi și apă puțin adâncă. Urmele și deformarea vizuală sunt secundare; controlul și reacția mașinii trebuie să funcționeze și fără deformarea terenului.

## 5. Mecanici și condiții de misiune

### Conducere și recuperare

- Viteză mică și control precis; frână, marșarier și frână de mână.
- Punctele disponibile de tractare se evidențiază numai la apropiere sau la selectarea țintei.
- Un cablu activ per jucător în prima versiune; două cabluri pot trage aceeași țintă în cooperare.
- Cablu scurtabil/alungibil în limite controlate, cu indicator vizibil de tensiune.
- Tensiune verde: sigură; galben: aproape de limită; roșu: redu accelerația sau schimbă unghiul. Forma și mesajul scurt însoțesc culoarea.
- Dacă limita este depășită suficient timp, cablul se desprinde controlat; jucătorul îl poate reconecta.
- Ținta este recuperată când ajunge în zona sigură și rămâne stabilă pentru scurt timp; pragurile exacte se stabilesc prin testare.

### Evaluare și reluare

Fiecare contract are o medalie de finalizare și două obiective opționale, de exemplu fără resetare și fără supratensionare. Timpul este afișat la rezultate; nu toate misiunile trebuie transformate în curse contra cronometru.

Resetarea readuce mașina la ultimul punct sigur și desprinde cablul. Răsturnarea sau căderea nu șterg progresul campaniei. Nu există distrugere ireversibilă a mașinii în prima versiune.

### Cooperare în doi

Ambii jucători au câte o mașină și propriul cablu. Pot lucra pe aceeași țintă, pot lua poziții diferite și pot folosi un semnal simplu de tip „gata / trage / oprește”. Numele și culorile distincte identifică mașinile. Coechipierul nu trebuie să fie permanent în același cadru.

Contractele obișnuite rămân realizabile solo. Contractele marcate **Duo** folosesc o configurație mai grea și cer doi jucători; sunt suplimentare, astfel încât progresul solo să poată fi încheiat independent.

## 6. Misiunile

**Plan de conținut propus:** 12 contracte, împărțite în trei zone. Numărul este o țintă de design, nu conținut deja implementat. Misiunile 01–03 formează primul prototip; restul se construiesc după validarea fizicii.

| ID | Contract și zonă | Ce faci | Provocare / regulă distinctă | Mod | Referință vizuală |
| --- | --- | --- | --- | --- | --- |
| 01 | **Primul cârlig — Valea Verde** | Conectezi cablul și muți o mașină de pe iarbă în zona sigură. | Tutorial scurt pentru apropiere, conectare, accelerație și desprindere. | Solo / 2 | Direcția primului cadru. |
| 02 | **La marginea drumului — Valea Verde** | Tragi un sedan din șanțul exterior al unei curbe. | Alegi un unghi care îl ridică spre drum, fără să împingi propria mașină spre margine. | Solo / 2 | [01 — drum montan](Organization/Tow-Trouble-Concepts/2026-10-30/01-mountain-recovery.png). |
| 03 | **Roți în noroi — Valea Verde** | Scoți vehiculul din două făgașe și îl livrezi pe pietriș. | Pornești gradual; accelerația brutală produce patinare și tensiune excesivă. | Solo / 2 | [04 — noroi în pădure](Organization/Tow-Trouble-Concepts/2026-10-30/04-forest-mud.png). |
| 04 | **Pârâul — Lacul Albastru** | Recuperezi o mașină cu roțile din spate în apă. | Aliniezi ținta cu o trecere scurtă și eviți bolovanii de lângă mal. | Solo / 2 | [03 — trecere peste râu](Organization/Tow-Trouble-Concepts/2026-10-30/03-forest-stream-recovery.png). |
| 05 | **Un pas prea sus — Valea Verde** | Tragi o mașină peste o treaptă joasă de stâncă. | Poziționarea și o tragere continuă sunt mai utile decât elanul. | Solo / 2 | [06 — urcare printre bolovani](Organization/Tow-Trouble-Concepts/2026-10-30/06-boulder-climb.png). |
| 06 | **Malul moale — Lacul Albastru** | Scoți sedanul din mâlul de la marginea lacului. | Găsești punct de sprijin pe pietriș și tragi spre uscat. | Solo / 2 | [07 — mal de lac](Organization/Tow-Trouble-Concepts/2026-10-30/07-lakeshore-recovery.png). |
| 07 | **Drumul lung — Lacul Albastru** | După recuperare, remorchezi ținta până într-o poiană sigură. | Două curbe strânse cer oprire, scurtarea cablului și repoziționare. | Solo / 2 | Nivel nou, construit din aceleași piese. |
| 08 | **Înghițit de nisip — Canionul Roșu** | Tragi un vehicul dintr-o mică adâncitură nisipoasă. | Alegi o porțiune tare înainte de a pune cablul în sarcină. | Solo / 2 | [02 — nisip în canion](Organization/Tow-Trouble-Concepts/2026-10-30/02-canyon-recovery.png). |
| 09 | **Curba suspendată — Canionul Roșu** | Recuperezi ținta blocată lângă marginea unei serpentine. | Spațiu de manevră redus; tragi spre interiorul curbei. | Solo / 2 | [05 — serpentină](Organization/Tow-Trouble-Concepts/2026-10-30/05-canyon-hairpin.png). |
| 10 | **Ultima tragere — Canionul Roșu** | Scoți o mașină din pasajul nisipos, apoi urci până la marcajul sigur. | Combini unghiul, aderența și dozarea tensiunii; lumină caldă de după-amiază. | Solo / 2 | [08 — pasaj la apus](Organization/Tow-Trouble-Concepts/2026-10-30/08-sunset-sand-pass.png). |
| 11 | **Tragem împreună — Valea Verde** | Două mașini recuperează simultan aceeași țintă grea. | Fiecare cablu are tensiune separată; porniți și opriți coordonat. | Duo | [09 — cooperare](Organization/Tow-Trouble-Concepts/2026-10-30/09-multiplayer-team-recovery.png). |
| 12 | **Două unghiuri — Canionul Roșu** | Recuperați o țintă grea dintr-o curbă și o livrați după un prag stâncos. | Un jucător menține direcția, celălalt asigură tracțiunea; rolurile se pot schimba. | Duo | Configurație nouă din mecanicile deja validate. |

### Dimensiunea fiecărei zone

Fiecare zonă este compactă, cu trasee reutilizate în configurații diferite, puncte clare de pornire și locuri de resetare. Nu construim o lume uriașă pentru prima versiune. Durata și dificultatea fiecărui contract se calibrează după teste cu jucători.

### Deblocări

Începi cu Scout și tutorialul. Contractele terminate deschid următoarele trasee și variante de vehicul. Hauler și Buddy adaugă moduri diferite de rezolvare, nu obligă la repetarea artificială a misiunilor. Culorile pot fi recompense cosmetice. Upgrade-urile complexe și economia extinsă nu sunt necesare pentru prototip.

## 7. Cum arată meniurile

Toate ecranele de mai jos sunt **specificații propuse**, nu capturi ale unor meniuri existente.

### Limbaj vizual comun

- Fundaluri din lumea 3D a jocului, cu o cameră calmă și mișcare discretă.
- Logo crem/galben, contur întunecat și accent portocaliu, pornind de la banner.
- Panouri cărbune cu transparență moderată, text crem și butoane mari, simple, cu colțuri ușor rotunjite.
- Selecția activă are contur și mică deplasare, nu doar o schimbare de culoare.
- Albastru pentru jucătorul 1, portocaliu pentru jucătorul 2; verde/galben/roșu pentru tensiune.
- Text lizibil pe televizor; navigare completă cu controllerul și indicarea consecventă a acțiunilor Confirmă / Înapoi.

### 7.1. Ecran de titlu

Scenă 3D cu mașinile parcate lângă lac, logo mare și un singur îndemn de continuare. La prima pornire se aleg limba, luminozitatea și dimensiunea textului. Îndemnul se adaptează la tastatură sau controller.

### 7.2. Meniul principal

Logo în stânga sus, listă verticală de acțiuni sub el. În dreapta vedem mașina selectată pe o porțiune de drum, cu canionul sau lacul în fundal. Fără un interior de garaj nou, costisitor de construit.

```text
TOW TROUBLE                         [mașina selectată în peisaj]

Continuă
Joacă solo
Joacă în doi
Vehicule
Setări
Credite
Ieșire                              [doar unde platforma o permite]
```

„Continuă” apare numai când există progres salvat și deschide ultimul contract sau ultimul punct sigur, conform tipului de salvare implementat.

### 7.3. Selectarea contractului

Trei file sus: **Valea Verde / Lacul Albastru / Canionul Roșu**. În stânga sunt cardurile contractelor, iar în dreapta o imagine a traseului, obiectivul, dificultatea și recomandarea de vehicul. Contractele Duo au simbol cu doi jucători. Jos sunt medaliile obținute și butonul **Pornește**.

```text
[Valea Verde]  [Lacul Albastru]  [Canionul Roșu]

01 Primul cârlig       |  IMAGINEA CONTRACTULUI
02 La marginea...      |  Obiectiv: recuperează sedanul
03 Roți în noroi       |  Teren: noroi / pietriș
05 Un pas prea sus     |  Recomandare: Scout / Hauler
11 Tragem împreună     |  [Schimbă mașina]  [Pornește]
```

Selectarea unui contract Duo fără coechipier duce la lobby și explică cerința de doi jucători.

### 7.4. Vehicule

Mașina ocupă centrul ecranului pe o platformă simplă din lumea jocului. În lateral sunt trei caracteristici ușor de înțeles: **Tracțiune / Manevrabilitate / Stabilitate**. Jos sunt cardurile Scout, Hauler și Buddy, culorile disponibile și butonul **Selectează**.

Statisticile sunt comparative și se bazează pe reglajele efective; nu afișăm valori de performanță inventate. Rotația modelului este disponibilă din mouse sau stick. Nu există montare de arme.

### 7.5. Lobby cooperativ

Două locuri de parcare în aceeași scenă, câte unul pentru fiecare jucător. Deasupra fiecărei mașini: numele, culoarea, vehiculul și starea **Pregătit**. În lateral: contractul, dificultatea și vizibilitatea sesiunii, dacă soluția de rețea aleasă o suportă.

```text
JOACĂ ÎN DOI                         Contract: Tragem împreună

[PLAYER 1 — pickup verde]            [PLAYER 2 — coupé galben]
[Schimbă mașina]                     [Schimbă mașina]
[Pregătit]                           [Pregătit]

[Invită / Cod sesiune*]              [Pornește contractul]
```

`*` Metoda exactă de invitație depinde de integrarea aleasă. Nu promitem toate metodele simultan. Gazda pornește contractul când amândoi sunt pregătiți. Dacă cineva pleacă, ecranul explică ce se întâmplă și oferă revenire sigură în lobby.

### 7.6. HUD în timpul jocului

Obiectivul stă în stânga sus, discret. Viteza este în dreapta jos. Tensiunea cablului și acțiunile disponibile sunt în stânga jos. Punctul de livrare are un indicator simplu în lume; nu încărcăm ecranul cu o hartă mare.

```text
RECUPEREAZĂ MAȘINA                              CO-OP 2/2

                   [numele coechipierului]
                         [lumea jocului]

CABLU  [████████░░]                              3 km/h
[Conectează] [Scurtează] [Desprinde]              [Frână]
```

În solo nu apare indicatorul CO-OP. În cooperare, tensiunea proprie este principală, iar cea a colegului apare compact când trage aceeași țintă. Indicatoarele de conectare apar contextual; cablul liber nu produce bare goale permanente.

### 7.7. Pauză

Panou compact peste scena estompată: **Continuă / Revino la punct sigur / Reîncepe contractul / Setări / Înapoi la contracte**. Ieșirea și reluarea misiunii cer confirmare când se pierde progresul curent. În online meniul local nu oprește fizica pentru ceilalți; acest comportament trebuie explicat și implementat.

### 7.8. Rezultate

Camera arată vehiculul recuperat în zona sigură. Un card central prezintă **Contract încheiat**, medaliile, timpul, resetările și obiectivele opționale. În doi apar ambele nume și contribuțiile simple disponibile, fără clasament care să penalizeze rolul de sprijin.

Butoane: **Următorul contract / Rejoacă / Vehicule / Contracte**. În online gazda alege continuarea, iar coechipierul vede starea de pregătire.

### 7.9. Setări

| Filă | Opțiuni propuse |
| --- | --- |
| Audio | Volum general, motoare/efecte, muzică, interfață. |
| Imagine | Rezoluție și mod fereastră pe PC, preseturi de calitate, luminozitate; opțiunile disponibile se adaptează platformei. |
| Comenzi | Reatribuirea comenzilor, sensibilitatea camerei, inversarea axelor, vibrații. |
| Accesibilitate | Dimensiune text/HUD, mișcare cameră redusă, ajutoare de culoare și simboluri pentru tensiune. |
| Joc | Limba, indicatoare tutorial, asistențe de conducere dacă sunt implementate. |

## 8. Prima versiune de construit

Ordinea de mai jos urmărește validarea nucleului înainte de producerea tuturor misiunilor.

1. **Test vizual în Unity:** o mașină Hot Rod și un petic de mediu So Stylized, la aceeași scară și lumină; confirmăm aspectul cu asseturi reale.
2. **Prototip solo:** o mașină jucabilă, o țintă, un cablu, teren simplu și zonă de livrare. Trebuie să rămână stabil după opriri, întoarceri și resetări.
3. **Trei contracte inițiale:** tutorialul, șanțul și noroiul, cu HUD, rezultate și progres salvat.
4. **Test cooperativ:** două instanțe conectate, două mașini și o țintă comună; verificăm cablurile, resetarea, deconectarea și recuperarea stării.
5. **Meniurile complete și vehiculele alternative:** contracte, vehicule, setări și lobby, cu navigare din controller.
6. **Extindere spre cele 12 contracte:** numai după ce controlul, camera și remorcarea sunt convingătoare.

Nu intră în primul prototip: lume deschisă mare, patru jucători, trafic complex, pietoni, interior de garaj detaliat, distrugere extinsă și simulare volumetrică a noroiului. Acestea nu sunt necesare pentru a valida ideea.

## 9. Referințele vizuale existente

Toate fișierele sunt în:

`/Users/gheorghe/UnityAssets/Organization/Tow-Trouble-Concepts/2026-10-30/`

| Fișier | Utilizare |
| --- | --- |
| [01-mountain-recovery.png](Organization/Tow-Trouble-Concepts/2026-10-30/01-mountain-recovery.png) | Drum montan și recuperare din șanț. |
| [02-canyon-recovery.png](Organization/Tow-Trouble-Concepts/2026-10-30/02-canyon-recovery.png) | Recuperare din nisip. |
| [03-forest-stream-recovery.png](Organization/Tow-Trouble-Concepts/2026-10-30/03-forest-stream-recovery.png) | Pârâu și trecere scurtă din lemn. |
| [04-forest-mud.png](Organization/Tow-Trouble-Concepts/2026-10-30/04-forest-mud.png) | Noroi în pădure. |
| [05-canyon-hairpin.png](Organization/Tow-Trouble-Concepts/2026-10-30/05-canyon-hairpin.png) | Serpentină lângă marginea canionului. |
| [06-boulder-climb.png](Organization/Tow-Trouble-Concepts/2026-10-30/06-boulder-climb.png) | Prag de stâncă și urcare. |
| [07-lakeshore-recovery.png](Organization/Tow-Trouble-Concepts/2026-10-30/07-lakeshore-recovery.png) | Mal de lac. |
| [08-sunset-sand-pass.png](Organization/Tow-Trouble-Concepts/2026-10-30/08-sunset-sand-pass.png) | Canion în lumină caldă. |
| [09-multiplayer-team-recovery.png](Organization/Tow-Trouble-Concepts/2026-10-30/09-multiplayer-team-recovery.png) | Recuperare în doi, pickup verde și coupé galben. |
| [tow-trouble-banner-v1.png](Organization/Tow-Trouble-Concepts/2026-10-30/tow-trouble-banner-v1.png) | Banner orizontal, 1942 × 809. |
| [tow-trouble-banner-vertical-v1.png](Organization/Tow-Trouble-Concepts/2026-10-30/tow-trouble-banner-vertical-v1.png) | Banner vertical, 1024 × 1536. |

Prompturi și proveniență: [primele trei cadre](Organization/Tow-Trouble-Concepts/2026-10-30/prompts.json), [următoarele cinci](Organization/Tow-Trouble-Concepts/2026-10-30/prompts-batch-2.json), [multiplayer](Organization/Tow-Trouble-Concepts/2026-10-30/prompt-multiplayer.json), [banner](Organization/Tow-Trouble-Concepts/2026-10-30/prompt-banner-v1.json), [banner vertical](Organization/Tow-Trouble-Concepts/2026-10-30/prompt-banner-vertical-v1.json).

Aceste imagini stabilesc direcția de artă și compoziție. Nu confirmă geometrie identică cu asseturile originale, niveluri funcționale sau sisteme implementate.

## 10. Surse și actualizarea documentului

- [Audit vizual al conceptelor — 28 octombrie](Organization/visual-game-concepts-audit-2026-10-28.txt).
- [Registru de asseturi](Organization/Asset%20Catalog/asset_registry.json).
- Verificare directă a existenței fișierelor din biblioteca Mac și din cache Unity la 30 octombrie 2026.
- Deciziile de design din acest document extind conceptele discutate; detaliile noi sunt propuneri și se ajustează după prototip.

Înainte de implementare se completează calea Hot Rod Constructor, versiunile pachetelor importate, controllerul selectat și soluția de rețea. După fiecare etapă, se marchează separat ce a fost efectiv construit și testat.
