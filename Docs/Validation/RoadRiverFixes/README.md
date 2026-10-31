# Corecții drumuri, recuperări, râu și urme — 2026-10-31

Scena: `Assets/TowTrouble/Scenes/OpenWorldRecovery.unity`.

## Modificări

- Am netezit racordurile dintre carosabil, acostamente și malul lacului. Taluzurile abrupte primesc material de stâncă, nu textura drumului. TerrainCollider este acum suprafața efectivă a drumurilor pictate; cele 68 de coliziuni plane vechi sunt dezactivate pentru a evita suprafețe ascunse și praguri la intersecții. Podurile au coliziuni proprii.
- Toate cele 24 de mașini de recuperat sunt în retrageri laterale, la 19–27 m de nodurile originale. Centrele lor au cel puțin 14,59 m până la marginea carosabilului; indicatoarele au minimum 8,23 m. Accesul și spațiul de tractare sunt eliberate de decor. Identificatorii, nivelurile contractelor și destinațiile rămân aceleași.
- Râul are profil continuu descendent, albie sub apă, cascadă, maluri cu bolovani și vegetație joasă. Copacii au fost eliminați din culoarul râului. Gura râului ajunge la cota lacului; fețele de apă suprapuse sunt decupate pentru a elimina capătul dreptunghiular vizibil.
- Patru traversări reale drum/râu au poduri cu grinzi, pile până la albie, scânduri și rampe. Racordurile terenului la rampe sunt netezite. 363 de puncte de resetare sunt proiectate pe contactul real teren/punte.
- 678 de obiecte suplimentare formează aflorimente și grohotiș în munți, folosind prefaburi So Stylized. Amplasarea respectă culoarele drumurilor și recuperărilor.
- `RecoveryWheelTracks` generează urme din contactele reale ale celor 12 roți ale celor două vehicule și ale mașinii recuperate. Pământul, nisipul și zăpada au relief vizual discret, margini estompate și textură de rulare. Living Mud continuă să gestioneze separat noroiul și murdărirea anvelopelor.
- Am corectat și salvarea mesh-urilor generate: actualizarea prin `Mesh` API înlocuiește explicit bufferele grafice, păstrând GUID-urile. `CopySerialized` singur lăsa unele suprafețe afișate cu geometria veche, deși interogările CPU vedeau deja datele noi.
- Fotografiile statice ale celor 24 de contracte sunt actualizate la pozițiile noi.

## Dovezi și limite

| Verificare | Rezultat |
|---|---|
| Compilare în Unity 6000.3.19f1 / URP 17.3 | 0 erori, 0 avertizări de compilare |
| Culoare de rulare | 18.213 poziții eșantionate; 0 obstacole, 0 puncte fără suport |
| Pantă maximă eșantionată | 22,05° |
| Puncte de resetare | 363, toate sprijinite pe teren/pod |
| Râu | 369 centre, fără albie deasupra apei sau segmente ascendente; lac la 10,4 m |
| Poduri | 4, toate deasupra râului; racord exterior aproximativ 0,10 m |
| Copaci în culoarul verificat al râului | 0 |
| Scripturi lipsă / erori shader inspectate | 0 / 0 |
| Verificări pure ale urmelor și hărții de suprafețe | 5 trecute prin invocare directă |
| Previzualizare geometrică a urmelor | 60 segmente statice, coordonate finite |

`SceneChecks.json`, `WheelChecks.json` și `EditorReady.json` conțin rezultatele. `ValidateScene.cs` și `ValidateWheelGeometry.cs` sunt scripturi pentru `unity command script eval_file`, în Edit Mode. Imaginile sunt randări statice; `StaticWheelImpressions.png` arată geometrie de probă, eliminată după captură, nu o sesiune de condus. `Preservation.json` confirmă că tutorialul, terenul lui și scenele Mega/Desert originale coincid cu versiunile Git HEAD.

Urmele noi sunt relief **vizual local**, cu buze de aproximativ 2,4 cm pe pământ, 3,5 cm pe nisip și 4,8 cm pe zăpadă. Nu sapă permanent TerrainData și nu modifică fizica sau autoritatea FishNet. Pool-ul este limitat la 36 de mesh-uri / 4.608 segmente; cele mai vechi urme sunt reciclate. Fiecare client le reconstruiește din contactele mașinilor pe care le vede; nu se trimit RPC-uri suplimentare pentru urme.

Nu au fost pornite Play Mode, Test Runner, simulări de fizică, conexiuni multiplayer sau builduri. Nu există măsurători FPS și nici validare a condusului cu cablu. Utilizatorul verifică în Editor:

1. Solo → Explorează, viraje pe pământ și mers înapoi: urmele trebuie să rămână sub traseul roților; resetarea nu trebuie să traseze o linie între poziții.
2. R20 și un contract de lac: mașina și indicatorul trebuie să fie în afara drumului, cu acces liber pentru tractare.
3. Cele patru poduri în ambele sensuri: acces fără agățarea șasiului, apă sub punte și fără urme de pământ pe scânduri.
4. Nisip, zăpadă și noroi: diferențe vizuale corecte, fără dublarea urmelor Living Mud.
5. Serpentinele montane și resetarea vehiculului. Pantele și tractarea grea rămân parte din progresia echipării.

## Reaplicare și backup

Uneltele noi sunt `OpenWorldHydrology`, `OpenWorldRecoveryPullouts`, `OpenWorldRiverDressing` și `OpenWorldWheelTracks`, integrate în `ApplyCompleteArtPass`. Generarea completă înlocuiește decorul generat; nu se rulează peste ajustări manuale nesalvate separat. După modificări de teren se refac suprafețele/aderența, pozițiile, verificările și fotografiile.

Nu folosi vechile `RefineRoads`, `FinishLandmarks` sau `AlignRecoverySigns` pe scena finisată.

Backup anterior corecțiilor: `/Users/gheorghe/Desktop/TowTrublw/Backups/TowCounty-before-road-river-fixes-20261031.tar.gz`. Capturile temporare de diagnostic sunt în `Backups/RoadRiver-diagnostic-probes-20261031`. Nu s-au făcut commit sau push.
