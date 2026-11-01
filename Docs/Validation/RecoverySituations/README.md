# Intervenții vizibile — 2026-11-01

## Livrat

Cele 24 de puncte sunt clasificate și descrise individual în RO/EN. R01 păstrează terenul local, poziția, poza și setările mecanice validate anterior; primește doar clasificarea Ditch.

| Situație | Puncte |
|---|---|
| Roți într-un șanț | R01, R04, R14, R18 |
| Făgașe și șasiu pe noroi | R02, R03, R06 |
| Mașină în apa lacului | R05, R08 |
| O latură alunecată pe taluz | R07, R11 |
| Motor defect / supraîncălzit | R09, R16, R24 |
| Piatră sub șasiu / culoar între bolovani | R10, R12, R23 |
| Zăpadă acumulată în jurul roților și sub șasiu | R13, R15 |
| Punte îngropată / creastă de dună / nisip umed | R17, R19, R20, R22 |
| Albie uscată cu rampă de ieșire | R21 |

Geometria TerrainData este modelată local, cu rampe spre ieșire. Poziția și înclinarea țintei se potrivesc după înălțimile roților și razele reale de suspensie, inclusiv triunghiurile diagonale ale hărții de înălțime. Șanțurile coboară o latură a mașinii; făgașele, marginile de pământ dislocat, zăpada și nisipul sunt vizibile lângă roți. Materialele și vegetația folosesc sursele existente; geometria și materialele noi sunt derivate deținute de proiect.

R05 și R08 sunt mutate în interiorul suprafeței lacului existent, cu fund puțin adânc și rampă conectată la mal. Nu sunt adăugate suprafețe de apă false. Poziția șasiului rămâne peste limita existentă de resetare de 9,5m. Nu s-au adăugat plutire, înec sau deteriorare nouă a motorului în apă.

Pentru motor defect, ținta este pe teren liber, cu pată de ulei și triunghi de avertizare. Rezistența artificială de blocaj este zero la aceste trei puncte. Un singur ParticleSystem de fum este atașat țintei, urmărește remorcarea, are maximum48 particule, rată8/s la effects100%, pornire doar în Play Mode pentru contractul potrivit și oprire/curățare la dezactivare, abandon, finalizare sau effects0. Pauza oprește efectul. Nu se pornesc efecte în Editor pentru verificare. Shaderul propriu folosește amestec transparent și culoarea particulelor; materialul de previzualizare statică dezactivează culoarea particulelor.

Camera de recuperare favorizează și la șanțurile/taluzurile noi partea cu roțile coborâte, păstrând orbitarea manuală și încadrarea normală în deplasare. Fotografiile panoului sunt actualizate pentru23 puncte; R01 rămâne identic. Fumul din fotografii este o pozare statică de carduri, fără simularea particulelor.

## Integrare

- `OpenWorldRecoverySituations.cs`: authoring repetabil, baseline doar pentru peticele locale, modelarea terenului, îmbrăcarea punctelor, potrivirea noroiului și configurarea fumului. Meniu: Tow Trouble / Recovery / Dress Distinct Recovery Situations.
- `RecoveryMap.RecoverySite.Kind` se propagă în `RecoveryContract.SituationKind`. Profilul și completările rămân neschimbate. Clasificarea face parte din scena comună; ambii jucători trebuie să aibă versiunea actualizată.
- `RecoveryDistressVisuals` este doar prezentare, derivată din contractul activ pe fiecare peer. Fizica și progresul rămân ale gazdei; nu există RPC nou.
- Decorațiunile generice vechi sunt dezactivate numai în grupurile R02–R24, păstrând copacii tractabili activi și referințele existente. R01 rămâne intact. Peticele vechi de noroi R05/R08 sunt dezactivate deoarece țintele sunt acum în lac; celelalte patru se potrivesc cu făgașele actuale.
- Nu rula generatoarele vechi de scenarii/drumuri peste această compoziție. După alte modificări intenționate ale terenului în aceleași zone, revizuiește baseline-ul înainte de reautorizare. Instrumentul reface doar peticele proprii, nu reîncarcă întregul relief dintr-un snapshot vechi.

## Dovezi și limite

- Compilare reușită în Unity6000.3.19f1; fără erori. Avertizarea intermitentă de cont/rețea Unity AI Assistant este preexistentă. Numele mesh-urilor salvate au fost aliniate cu numele fișierelor.
- `Tests.json`:72 verificări directe Edit Mode trecute (4 situații/fum/metadate +68 troliu/cablu/garaj/extracție/scenarii). SetUp separat per instanță; fără Test Runner.
- `SceneValidation.json`, `WheelContacts.json`:96/96 roți au suport la razele statice de suspensie; zero scripturi lipsă;5 copaci tractabili activi la indicii1,3,5,13,17. Aceasta nu demonstrează stabilitatea dinamică a vehiculelor sau finalizarea misiunilor.
- R01: setări identice în afară de Kind, întreg peticul local de21m identic, fotografie identică la nivel de octeți. Identificatorii, numele, clasele, recompensele, destinațiile, punctele de drum și setările Extraction ale tuturor punctelor sunt păstrate.
-4541 eșantioane de înălțime schimbate; diferența maximă3,6425m este la zonele inundate. Cel mai apropiat eșantion modificat este la27,94m de un checkpoint de drum.
- `Idempotence.json`:reaplicarea păstrează exact hash-ul înălțimilor și datele hărții; un singur grup de scenarii. Scena finală este salvată și curată.
- `Representative.png`, `All-sites-1.png`, `All-sites-2.png`:previzualizări statice, nu capturi de gameplay. Au fost inspectate toate cele24 de imagini.
- Fără Play Mode, simulare fizică/particule, Test Runner, startup de rețea, build sau scrieri PlayerPrefs. Nu sunt validate încă extragerea efectivă, timpul și dificultatea fiecărui punct, efectul de fum animat, performanța sau multiplayerul live.

## Testul utilizatorului

Începe cu R04 (o latură în șanț), R03 (noroi), R05/R08 (apă), R16 (motor și fum), R12 (bolovani), R20 (creasta dunei). Verifică dacă motivul blocajului este evident la apropiere, mașina se așază fără salturi, cârligul este accesibil, există un unghi util de tragere și livrarea poate fi încheiată. La fum: urmează ținta în timpul remorcării și dispare la abandon/finalizare; effects0 îl oprește. Verifică ulterior și intervențiile cu copaci în două etape.

Backup complet anterior: `/Users/gheorghe/Desktop/TowTrublw/Backups/RecoverySituations-20261101`.
