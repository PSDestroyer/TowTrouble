# Tow Trouble — design aprobat, 2026-10-31

## Direcție
Scena Mega devine ulterior lumea deschisă. Garajul este un loc fizic pentru personalizare. Tab deschide intervențiile; jucătorul acceptă, conduce până la țintă, recuperează, livrează și primește bani. Tutorialul existent rămâne separat. Fizica și rezultatul misiunii sunt ale gazdei FishNet.

## Etapele 1–4 implementate; etapa 5 autorizată la 2026-10-31
1. Documentul și economia.
2. Catalogul real al celor două pachete Hot Rod, cu ID-uri stabile, compatibilități și excluderi explicite.
3. Garaj: previzualizare 3D, cumpărare, montare, vânzare, salvarea configurației și preseturi.
4. Contracte în gameplay prin Tab, obiectiv în lume, recompensă și profil persistent.

Etapa 5 este acum autorizată prin aprobarea hărții cu 24 recuperări și 8 predări. Scena de joc este OpenWorldRecovery, derivată din Mega și sistemele tutorialului. CanyonRecovery și scenele demo originale rămân separate. Explorează încarcă harta nouă; Tutorial / Contracte păstrează accesul la tutorial.

## Biomuri și echipament
| Biom | Provocare | Pregătire |
|---|---|---|
| Pădure | Noroi și poziționare îngustă | Anvelope de noroi, gardă la sol |
| Lac | Mâl și ținte grele | Tracțiune, troliu, poziție pe teren tare |
| Plajă | Afundare în nisip | Anvelope late, masă moderată |
| Munte / zăpadă | Pantă, alunecare, răsturnare | Reductor, echipament de iarnă, stabilitate, cablu |
| Deșert | Dune și șocuri în cablu | Anvelope de nisip, suspensie, cablu rezistent |

Fiecare biom conține dificultăți diferite. Motorul este limitat de aderență; un cablu mai puternic nu produce tracțiune suplimentară. Nisipul și zăpada au acum aderență locală pe colliderul drumului și pe terenul hărții. Anvelopele de nisip cresc aderența pe nisip cu 45%; cele de iarnă cresc aderența pe zăpadă cu 65%. Compromisurile în noroi rămân -10% / -15%. Inventarul actual oferă roți cu crampoane; lanțurile din concept nu au fost confirmate ca model 3D separat.

## Economie
Scout, echipament inițial și zero credite pentru profil nou. Tutorialul plătește ca prima intervenție. Profilurile vechi păstrează progresul și primesc o singură creditare pentru contractele deja finalizate, după medaliile salvate.

| Clasă | Durată țintă cu deplasare | Bază |
|---|---:|---:|
| Local | 6 min | 450 CR |
| Standard | 9 min | 800 CR |
| Specializat | 12 min | 1300 CR |
| Greu | 15 min | 1900 CR |
| Expert | 18 min | 2700 CR |

Bonusuri: +10% fără resetări, +10% fără cablu rupt, +5% la prima finalizare; maximum +25%. Fără bonus de viteză. Cele trei misiuni existente sunt Local / Standard / Specializat; duratele lor scurte de tutorial nu confirmă ritmul viitoarei lumi deschise. Greu și Expert sunt valori de design, nu misiuni construite.

| Echipament | Preț |
|---|---:|
| Cablu stock 11 kN | Inclus |
| Cablu ușor 14 kN | 200 CR |
| Cablu standard 18 kN | 450 CR |
| Set universal îmbunătățit / noroi / nisip | 1200 CR fiecare |
| Set de iarnă | 1600 CR |
| Cablu 26 kN | 1400 CR |
| Suspensie | 1800 CR |
| Reductor | 2200 CR |
| Troliu îmbunătățit | 2400 CR |
| Motor 2 | 2800 CR |
| Motor 3 | 8800 CR total; schimbul motorului 2 costă încă 6000 CR |
| Cablu 36 kN | 3400 CR |
| Vopsea | 100 CR |

Motoare: +20% / +35% față de baza vehiculului. Reductor: +25% forță și -20% viteză maximă. La ultima cerere a utilizatorului din 2026-11-01, cablul stock este 11 kN în explorare și tutorial; 14 kN devine primul upgrade la 200 CR, urmat de 18 kN la 450 CR. Progresie: 11 → 14 → 18 → 26 → 36 kN. Piesele cosmetice nu primesc bonus de motor. Montarea pieselor deținute și încărcarea presetului sunt gratuite. Vânzare la 70%; piesele montate sau referite de un preset se demontează înainte de vânzare. Fără taxe obligatorii de combustibil/reparații/cablu și fără amendă la abandon. Piesa cumpărată rămâne în inventar.

## Configurație și compatibilitate
Ruperea cablului depinde de mărimea suprasarcinii, nu de o fereastră fixă de0,8s. Se verifică cererea de forță înainte de netezire și înainte de aplicarea impulsului: la≥165% din rezistența cablului se rupe în același pas de fizică; depășirile mai mici consumă un buget de stres în aproximativ0,04–0,18s. Smuciturile apropiate acumulează stres; acesta se relaxează gradual fără sarcină și nu se șterge instant prin deconectare. Nu este uzură permanentă. Galben peste60%, portocaliu peste82%, roșu la limita de100%. Gazda decide ruperea; ambele capete pierd forța de tractare în pasul ruperii.

Caroserii complete sanitizate, cu rig și socketuri compatibile, din ambele pachete. Alte sloturi: roți, capotă, bare, aripi, accesorii de plafon/benă, admisie, evacuare, șasiu, motor, transmisie, suspensie, cablu, troliu și vopsea. Rigurile și materialele originale nu se modifică. Schimbarea caroseriei demontează în configurația nouă piesele incompatibile, dar le păstrează în inventar. Compatibilitatea automată verifică familia și socketul; potrivirea vizuală trebuie inspectată.

Preseturi per vehicul: Pădure, Munte, Nisip. Sunt salvate doar configurații cu piese deținute. Previzualizarea unei piese nu cumpără și nu salvează nimic. Ieșirea fără montare restaurează configurația instalată.

## Contracte și garaj
În explorare nu există țintă activă. Tab afișează distanța, terenul, dificultatea, recompensa și recomandarea. Un contract activ pentru echipă. Acceptarea activează ținta și marcajul fără să teleporteze mașinile jucătorilor. Abandonarea cere confirmare și nu plătește. Finalizarea permite continuarea explorării la poziția curentă.

Garajul se deschide cu G când mașina este oprită în zona atelierului, fără cablu și fără intervenție activă. Controller: apăsare stick stânga pentru contracte, X pentru garaj. În solo, panourile opresc timpul; în cooperare meniul este local și comenzile proprii sunt frânate.

## Persistență și rețea
Profil v2 migrează profilul v1, păstrează setări/progres/culori și adaugă bani, inventar, configurații și preseturi. ID de intervenție emis de gazdă pentru plată o singură dată pe profil. Banii locali nu sunt o economie competitivă cu backend. Gazda validează slotul solicitantului, starea, accesul la garaj și ID-urile catalogului; nu acceptă coeficienți fizici trimiși de client. Configurațiile se transmit la schimbare, nu în fiecare snapshot. Ambii jucători au nevoie de aceeași versiune.

## Validare
Fără builduri și fără pornirea Play Mode de către agent. Compilare, verificări Edit Mode pentru economie/migrare/catalog și previzualizare statică. Condusul, navigarea reală a meniurilor și multiplayerul se testează de utilizator. Etapa 5 este construită în scena separată OpenWorldRecovery; condusul și multiplayerul rămân pentru testul utilizatorului.


## Harta construită — etapa 5

Scenă: Assets/TowTrouble/Scenes/OpenWorldRecovery.unity. Teren 1000 × 1000 m derivat dintr-o copie a TerrainData Mega, relieful și vopsirea adaptate planului. So Stylized pentru natură și deșert; Broken Vector la accesul atelierului, drumuri de pământ conforme terenului pentru restul rețelei. Originalele Mega, Desert și CanyonRecovery nu sunt rescrise.

24 recuperări, câte 4 în Pădure, Lac, Munte, Zăpadă, Plajă și Deșert; 8 predări A–H. 16 transporturi regionale și 8 între regiuni. JSON-ul de authoring cu ID-uri și legături este Assets/TowTrouble/Editor/OpenWorldPlan.json. C, R19 și R23 au fost repoziționate ușor față de schiță pentru a evita suprapunerea acceselor la înălțimi diferite.

Garaj central G, râu cu două traversări, serpentine, zone de Living Mud pentru 6 recuperări din pădure/lac. Panoul Tab are filtre de biom și 8 pagini, cu fotografii statice pozate în scenă. O singură țintă activă, activată de gazdă la acceptarea contractului; nu se simulează 24 de mașini simultan.

Extragerea este măsurată prin distanță față de țintă, nu prin Z global. Predarea verifică distanța orizontală și diferența de altitudine. Progresul tutorialului folosește în continuare biții 0–2; harta folosește 3–26, fără migrarea formatului v2. Resetarea folosește puncte sigure de pe suprafața drumului, inclusiv deasupra podurilor. Apa adâncă a lacului resetează mașina la ultimul punct sigur.

Aceasta este prima versiune a hărții. Nu sunt validate încă senzația de condus, durata economică efectivă, potrivirea dificultății fiecărui contract sau multiplayerul pe hartă. Dovezi: Docs/Validation/OpenWorld.
