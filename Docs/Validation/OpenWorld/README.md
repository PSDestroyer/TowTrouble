# OpenWorldRecovery — prima versiune, 2026-10-31

Baseline Git a7788dc. Scena este Assets/TowTrouble/Scenes/OpenWorldRecovery.unity, derivată din Mega și din sistemele existente ale tutorialului. Nu s-a pornit Play Mode, nu s-au rulat teste runtime sau Test Runner și nu s-au creat builduri.

## Rezultat

- Teren de 1000 × 1000 m, șase biomuri, 24 recuperări și 8 predări.
- Rețea de 66 noduri și 68 segmente, toate punctele conectate la garaj și la predarea asociată. 16 transporturi regionale și 8 între regiuni.
- Garaj fizic, râu și două traversări, șase suprafețe Living Mud, marcaje și indicatoare. So Stylized și Broken Vector, fără pachete noi.
- Contracte în Tab cu filtre, paginare și 24 de fotografii statice. Fotografii cu mașini pozate, fără simulare; soldul din randările UI este fictiv, nu profilul utilizatorului.
- Nisip/zăpadă au aderență proprie, cu specializarea anvelopelor; gazda FishNet rămâne autoritară. Progresul noii hărți folosește biții 3–26, separat de tutorial.

## Verificări efectuate

SceneChecks.json: 24 poziții cu contact la sol, 8 predări, zero scripturi lipsă și zero drumuri peste pragul de control de 29°. Cel mai mare unghi măsurat pe segmente este aproximativ 17.5°. Acestea sunt măsurători geometrice, nu un test de condus.

RoadClearance.json: 718 eșantioane de drum fără teren deasupra carosabilului sau obstacole în volumul verificat al mașinii. Test discret, nu verificare exhaustivă a tuturor combinațiilor de vehicul/cablu.

EditorReadiness.json: compile și Console fără erori; 24 imagini valide; 333 puncte sigure de resetare pe drum/pod verificate prin raycast. Ambele scene sunt înscrise în EditorBuildSettings numai pentru tranziția prin meniu. Există un avertisment anterior în Console.

DomainChecks.json: 19 aserțiuni pure Edit Mode trecute, invocate direct: economie/migrare/configurații plus extragere în orice direcție, predare la altitudine, progres separat și anvelope specializate. Profilul real nu a fost scris.

BoardChecks.json: toate cele 8 pagini verificate RO/EN la text 130%, fără depășiri de text. Două imagini finale reprezentative sunt păstrate; celelalte PNG-uri de control au fost eliminate, iar rapoartele marchează explicit acest lucru.

Topology.json: toate recuperările și predările sunt conectate. Hashurile celor trei scene sursă .unity (Mega, Desert, CanyonRecovery) corespund stării de dinainte de authoring. Trei amplasamente au fost adaptate față de schemă: C, R19, R23, pentru evitarea suprapunerii drumurilor la înălțimi diferite.

## Limite și fișiere auxiliare

Condusul, comportamentul fizic al fiecărei misiuni, durata/recompensa efectivă, potrivirea dificultăților, persistența în profilul real, performanța și FishNet nu au fost testate în rulare. Utilizatorul testează în Editor, începând cu R01 → A, apoi R03 / R17 / R13 cu echipamentul potrivit.

La generare au existat deja modificări de iluminare ale tutorialului în Git; acestea au fost păstrate. Fonturile TMP dinamice s-au extins pentru etichetele noi. Cache-ul de iluminare copiat în noua hartă a fost detașat prin ClearLightingDataAsset, iar copia nefolosită a fost eliminată după verificarea dependențelor. Nu s-a făcut bake sau build la comandă.
