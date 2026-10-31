# Integrarea îmbunătățirilor scenei — 2026-10-31

Scena modificată: `Assets/TowTrouble/Scenes/OpenWorldRecovery.unity`.

## Integrat

- Relief continuu calculat în jurul cotelor fixe ale drumurilor și misiunilor; racorduri locale netezite la atelier. Teren 1025², splatmap 1024², opt straturi native cu materiale proprii.
- Curbe mai ample, acostamente neregulate și urme vizuale de roți pe toate cele 68 de segmente. Coliziunea de rulare rămâne separată de stratul vizual; 363 puncte de resetare actualizate.
- Contururi organice pentru cele șase suprafețe Living Mud, UV-uri plane păstrate pentru stampilele anvelopelor, material propriu fără modificarea materialului tutorialului. Maluri variate, stuf, pietre și lemn căzut.
- Compoziție deterministă pe biomi: pâlcuri de mesteceni și brazi, pini înzăpeziți, cactuși, formațiuni stâncoase îngropate în teren și munți nativi în fundal. Vegetație joasă instanțiată prin Terrain; prefaburile și materialele derivate sunt în folderul proiectului.
- Decor în jurul celor 24 de recuperări, cu spațiu liber pentru acces și tractare; indicatoare de recuperare și predare repoziționate după verificarea coliziunilor.
- Garaj cu ferestre, grinzi, fundație, corpuri de iluminat, bancuri, rafturi, piese Hot Rod, recipiente și semnalizare. Iluminare direcțională și ambient ajustate.
- RecoveryGroundMap leagă clasificarea nisip/zăpadă de pictarea terenului; terrain și drumurile folosesc același asset, fără citiri de texturi sau alocări în bucla roților. Scenele fără această hartă păstrează comportamentul anterior.
- Cele 24 de fotografii ale contractelor au fost recapturate static. UI-ul, economia, identificatorii și regulile misiunilor nu au fost reproiectate.

## Dovezi

- `SceneChecks.json`: 68 drumuri, pantă maximă 17.29°, 1.050 eșantioane de carosabil, fără obstacole în volumul de verificare sau teren deasupra drumului; toate cele 363 puncte de resetare au sprijin. 24 recuperări / 8 predări, zero scripturi lipsă, zero erori de shader în materialele proprii inspectate.
- `EditorChecks.json`: șapte verificări pure Edit Mode invocate direct, fără Test Runner. MeshColliders: 1.912 → 600. Grupuri LOD cu MeshColliders distincte duplicate: 402 → 0. Aceste numere nu reprezintă măsurători de FPS.
- `Preservation.json`: tutorialul, Mega și Desert originale au aceleași hash-uri; secțiunile serializate cu recuperările și predările sunt neschimbate.
- `Before-*.png` provin din capturile anterioare acestei integrări. `After-*.png` și `Workshop-Detail.png` sunt randări statice ale scenei. Previzualizarea atelierului include o copie vizuală temporară a mașinii, eliminată după captură.
- Compilare în Unity 6000.3.19f1 / URP 17.3 fără erori C#.

Nu au fost pornite Play Mode, simulări de fizică, Test Runner, conexiuni multiplayer sau builduri. Condusul, comportamentul cablului, tranzițiile LOD în mișcare și performanța necesită testarea utilizatorului în Editor. Verificarea statică a culoarului nu demonstrează manevrabilitatea completă cu două vehicule și cablu.

## Reaplicare și recuperare

Uneltele sunt în `Assets/TowTrouble/Editor/OpenWorldArt*.cs`. Pentru reaplicarea întregii compoziții există `Tow Trouble > Art Integration > Apply Complete Art Pass`, care cere scena corectă salvată și Edit Mode. Comanda regenerează decorul; nu se folosește peste ajustări manuale fără a le păstra separat. Etapele individuale sunt pentru ajustări punctuale; după modificarea terenului se reface harta de aderență și se recapturează fotografiile.

Nu folosi vechile `RefineRoads`, `FinishLandmarks`, `AlignRecoverySigns` sau primul `Visuals/PolishScenery` peste această versiune: acelea generează structura vizuală anterioară.

Backup-ul de dinaintea integrării (scenă, OpenWorld, unelte Editor și RecoveryGroundSurface) este în afara repository-ului:
`/Users/gheorghe/Desktop/TowTrublw/Backups/TowCounty-before-art-integration-20261031.tar.gz`.

Nu s-au făcut commit sau push; modificările deja existente în repository au fost păstrate.
