# Tow Trouble — demo conform documentului de proiect

Sursa de cerințe este `Docs/Design/TOW_TROUBLE-source.md`, copia documentului utilizatorului. Selecția anterioară Low_Poly_Environment_World a fost greșită: folderul și referințele sale au fost eliminate. Materialul derivat și cele două prefabricate procedurale vechi au fost eliminate.

## Asseturi integrate

- **Stylized Nature & Environment MEGA PACK — So Stylized**: terenul, vegetația, stâncile, apa și cerul. Scena demonstrativă originală este adaptată în copie; hărțile de înălțime, texturi, detalii și arbori sunt rotite împreună, iar culoarul de recuperare este nivelat. Asseturile sursă nu sunt modificate.
- **Low Poly Road Pack — Broken Vector**: accesul rutier spre atelier, cu material armonizat.
- **Hot Rod Constructor: Modular Battle Cars — Slava Z.**, produs 303248: Scout, Hauler, Buddy și ținta de recuperat, construite din prefabricatele Historic_Taxi, Historic_Villager și Historic_Bulldog. Fără arme.
- **Hot Rod Constructor: Modular Off-Road Cars — Slava Z.**, produs 346948: Ranger, din Goblin_Offroad. Ambele pachete au fost descărcate prin Package Manager din achizițiile contului Unity conectat.
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
| Conectare / desprindere | E | A |
| Scurtare / alungire cablu | Q / F | LB / RB |
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
