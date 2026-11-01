# Prototip troliu — 2026-11-01

## Comportament

- Stock: 8 kN nominal, 1,0 m/s fără sarcină, inclus. Upgrade existent `winch_2`: 14 kN, 1,3 m/s, 2400 CR. Nu se adaugă treapta profesională în acest prototip.
- Motorul preia cablul liber, încetinește progresiv după 25% sarcină și oprește scurtarea aproape de limita proprie (97%, pentru o oprire stabilă). Pornirea/reluarea accelerează pe 0,25s; reducerea vitezei la creșterea sarcinii este imediată.
- Calculul lungimii verifică bugetul de forță al motorului înainte de a scurta. Viteza de apropiere nu permite acumularea ascunsă a unei întinderi elastice excesive.
- Limita motorului nu plafonează cererea fizică a cablului și nu dă cablu automat. Smuciturile, deplasarea vehiculelor și folosirea unui troliu prea puternic pentru cablu pot produce rupere. Poarta existentă de rupere rămâne înaintea aplicării forței de tractare la ambele capete.
- Eliberarea Q păstrează lungimea; F dă cablu la 1,7 m/s. Lungime 3,5–20m. Q+F simultan țin lungimea, fără comandă contradictorie.
- Cu frâna ținută, roțile în contact pot opune reacția cablului în bugetul existent de aderență. Roțile fără contact nu adaugă sprijin. Pe teren slab, vehiculul poate aluneca. Nu există ancorare artificială sau frânare automată la apăsarea Q.
- HUD RO/EN: preia cablul, trage, limita troliului, dă cablu, ține lungimea și limitele de lungime; sarcină în kN și metri de cablu. Avertizarea cablului rămâne separată. Valoarea poate depăși capacitatea troliului la o sarcină externă.
- Sunet procedural temporar de motor, mai lent sub sarcină, cu ton de efort la blocare. Două surse spațiale reutilizate, un singur clip determinist per lume; respectă master/effects și grupul mixerului motorului dacă există. Se oprește la pauză, deconectare, încheiere, dezactivare și distrugere. Nu este un asset audio final.

## Integrare și compatibilitate

`RecoveryWinch` este regula pură; `RecoveryWorld` o execută doar pe gazdă. `WinchFrame` cu stare/lungime/viteză/sarcină călătorește în snapshotul existent de 15Hz, fără RPC nou. Ambii jucători trebuie să folosească aceeași versiune; multiplayerul live rămâne de verificat.

Catalogul păstrează 382 de piese, ID-urile și prețurile. Câmpul serializat nou `WinchForce` are valoare implicită de 8000; doar cele două înregistrări de troliu își schimbă atributele existente. Profilul v2 continuă să salveze ID-uri: fără migrare, resetare sau modificări PlayerPrefs. `Refresh Winch Prototype` schimbă doar catalogul, este repetabil și păstrează referințele. Generatorul complet folosește aceeași configurație. Nicio scenă, geometrie R01, setare de teren sau rezistență a cablului nu a fost modificată.

## Verificări efectuate

- Compilare în Unity 6000.3.19f1, fără erori. Avertizarea preexistentă de cont/rețea Unity AI Assistant rămâne independentă.
- 68 de verificări directe în Edit Mode: 17 pentru troliu + 51 pentru ruperea cablului, catalog/profil/economie, extracție, scenarii și regulile de tractare. Fiecare test a primit propria instanță și SetUp; Test Runner nu a fost pornit. Rezultate: `Tests.json`.
- Calcule cu geometrie fixă timp de 10s, la pași de 0,01 / 0,02 / 0,04s: stockul se oprește sub 8 kN și nu consumă stres pe cablul de 11 kN; forța disponibilă depășește pragul nominal de 5,6 kN al blocajului R01. Acesta nu demonstrează extracția fizică a mașinii pe teren.
- Verificări: șoc extern, suprasarcină existentă, troliu 14 kN/cablu 11 kN, relaxare prin F, reluare fără salt de viteză, lungime minimă/maximă, comenzi simultane, bugetul comun de aderență al frânei, profil nou și upgrade deținut păstrat după serializare.
- Clonă inactivă de vehicul cu model resetat: 8 kN, 1 m/s, cablu 11 kN. Fără simulare.
- `Preservation.json`: numai stock_Winch și winch_2 au modificări de câmpuri preexistente; catalog repetabil, scena activă curată, Play Mode oprit.
- `HUD-RO130-static.png`, `HUD-EN130-static.png`: fixture-uri statice ale HUD-ului real, cu starea de blocare pozată, text 130%. Textul nou are două rânduri fără overflow; nu reprezintă capturi de gameplay.
- Fără Play Mode, simulare fizică/particule, Test Runner, pornire de rețea, build sau scrieri în profilul real. Sunetul, senzația de tragere și sincronizarea live nu au fost testate.

## Testul utilizatorului pe R01

1. Montează troliul stock și cablul de 11 kN. Acceptă R01 și poziționează vehiculul pe teren tare, orientat spre ieșirea șanțului.
2. Conectează E; ține Space + Q fără accelerație. Urmărește creșterea graduală a sarcinii, încetinirea motorului și deplasarea reală a roții. Stockul trebuie să permită o extragere lentă când unghiul și sprijinul sunt bune; timpul efectiv rămâne de calibrat.
3. Într-un unghi nefavorabil, ține Q: troliul poate ajunge la propria limită fără să rupă singur cablul stock. Repozitionarea trebuie să conteze; verifică și alunecarea pe noroi.
4. Eliberează Q: lungimea rămâne fixă. Ține F: lungimea crește și sarcina scade. Q+F nu trebuie să miște tamburul.
5. La ieșirea roții, motorul își reia viteza progresiv; urmărește dacă apare o smucitură sau o accelerare deranjantă.
6. Compară upgrade-ul de 14 kN cu un cablu de minimum 14 kN. Separat, un cablu de 11 kN cu troliu 14 kN trebuie să poată ceda la suprasarcină. Smucitura cu vehiculul trebuie să păstreze noua rupere rapidă.
7. Verifică sunetul la blocare, Q eliberat, pauză și effects=0; pe controller folosește B+LB, RB pentru dat cablu.

Backup anterior: `/Users/gheorghe/Desktop/TowTrublw/Backups/WinchPrototype-20261101`.
