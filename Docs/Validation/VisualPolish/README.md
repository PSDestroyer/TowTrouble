# OpenWorldRecovery — finisare vizuală, 2026-10-31

Scena lucrată: `Assets/TowTrouble/Scenes/OpenWorldRecovery.unity`. Verificare exclusiv în Edit Mode prin randări Camera.Render, inspecție de componente și geometrie. Fără Play Mode, Test Runner, simulare fizică, sesiuni FishNet sau builduri. Profilul utilizatorului nu a fost scris.

## Modificări

- Teren de 1000 × 1000 m păstrat la aceeași scară, rezoluție 1025, netezire separabilă și umeri de drum gradați. Unity modifică automat dimensiunea XZ când se schimbă rezoluția; unealta fixează explicit dimensiunea după această operație.
- 68 de trasee cu racorduri Bézier și variație discretă a curburii. Texturi/normale So Stylized pictate direct în TerrainLayers, cu margini amestecate în sol; coliziuni separate cu două coloane, folosite la validarea pantelor. Meshurile vizuale cu șase coloane sunt păstrate cu Renderer oprit pentru a evita cusăturile coplanare ale suprapunerii. Profilul geometric pentru condus păstrează altitudinile planului.
- Opt TerrainLayers proprii: iarbă, pământ, nisip, stâncă, zăpadă, deșert, sol de pădure și zăpadă compactată pentru traseu. Tranziții după pantă/altitudine/țărm, cu materiale nemetalice și texturi la scară mai mică.
- Atelier cu acoperiș în două ape, copertină, grinzi, siding, bancuri, scule, dulap, raft cu anvelope Hot Rod și logo-ul real. Accesul Broken Vector rămâne integrat, nivelat în curte.
- Vegetație și plante de margine, stânci reîngropate/redimensionate, repere separate pentru biomuri și fundal exterior vizual fără coliziuni. Nu este o extindere a suprafeței jucabile.
- Mal curbat triangulat pentru lac, râu sinuos cu albie locală și două punți de lemn. Materialul apei este derivat din cel nativ.
- Profil Volume separat, ACES moderat, lumină caldă, ceață de distanță, SMAA și nori nativi din MEGA PACK. Fără depth of field sau motion blur pentru gameplay. Materialele de vegetație din scenă au variante cromatice proprii; asseturile cumpărate nu sunt modificate.
- Aceleași 24 de recuperări / 8 predări. Punctele sigure urmăresc curbele: 363. Fotografii statice ale contractelor actualizate cu noua iluminare.

## Dovezi

`SceneChecks.json`: 68 drumuri, pantă maximă aproximativ 17,4°, 1.050 eșantioane fără obstacole sau teren peste carosabil, 363 puncte sigure cu sprijin, 24 recuperări cu sol, zero scripturi lipsă și zero erori de shader. Verifică pante, volum de control liber pe drumuri, contact la sol al recuperărilor, puncte sigure și compatibilitatea shaderelor. Verificarea drumurilor folosește MeshCollider, nu ordinea vertexurilor din noul mesh vizual.

`MissionPreservation.json`: pozițiile și datele serializate ale celor 24 de recuperări și 8 predări sunt identice cu copia de siguranță de dinaintea finisării.

`SourcePreservation.json`: hashurile Mega, Desert și CanyonRecovery.unity sunt identice cu cele înregistrate înainte de crearea hărții. Modificările preexistente ale lightmapurilor tutorialului sunt păstrate.

`Before-*.png` și `After-*.png`: aceleași camere pentru cinci zone. `Workshop-Detail.png`: randare statică suplimentară cu un Hot Rod pozat, fără simulare. Randările nu sunt dovezi de performanță sau gameplay.

## Authoring

Varianta cu overlay a fost comparată static cu drumul pictat în Terrain și înlocuită din cauza muchiei vizibile. Shaderul propriu rămâne folosit pentru accesul Broken Vector.

Uneltele sunt în `Assets/TowTrouble/Editor/OpenWorldVisuals.cs`, `OpenWorldScenery.cs`, `OpenWorldWaterVisuals.cs`. Meniul Tow Trouble → Visuals: 1 (teren/drumuri), apoi 2 (decor/apă/iluminare), apoi 4 (nori) și 5 (integrare în sol și curățarea vegetației din platforme); Capture Open World Contract Photos actualizează fotografiile. Pasul 3 permite reîmprospătarea materialelor și suprafeței vizuale fără remodelarea terenului.

Copiile Terrain before visual pass și Road graded terrain snapshot sunt surse locale pentru repetarea deterministă a operațiilor. Carvarea râului pornește din snapshot, nu adâncește cumulativ albia. Uneltele vechi Refine Open World Road Grades / Finish Open World Landmarks / Align Recovery Signs sunt pași ai primei generări și ar înlocui părți ale finisării; nu se rulează după finisare. Nu se rulează Create Open World peste scena existentă.

Condusul pe curbe, tracțiunea, cablurile, performanța pe calculatorul utilizatorului și multiplayerul rămân pentru testare de către utilizator în Editor.
