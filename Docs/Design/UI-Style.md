# Tow Trouble — direcție UI

Actualizat 2026-10-31. Stilul pornește de la logoul utilizatorului: auriu spre portocaliu, crem, contur închis, volum discret și forme rotunjite. HUD-ul păstrează compoziția referințelor: obiectiv sus-stânga, cablu jos-stânga, viteză jos-dreapta.

## Componente

Interfață uGUI/TMP nativă, referință 1600 × 900. RecoveryPlateGraphic desenează panouri cu colțuri tăiate, margine, umbră, gradient și mici detalii de metal. RecoveryIconGraphic desenează pictograme vectoriale pentru navigare și categorii. Nu sunt necesare imagini generate pentru butoane sau pachete noi.

Acțiunea principală și selecția folosesc auriu; panourile secundare sunt cărbune; textul este crem. Focusul și apăsarea folosesc timp nescalat. Comenzile indisponibile sunt estompate. Reconstruirea aceleiași pagini păstrează controlul selectat când acesta mai există.

Garaj: categorii Stil / Tehnic / Tractare, mașină centrală, listă de piese cu prețuri distincte, statistici și trei configurații salvate. Contracte: miniaturi, teren, recompensă, recomandare și stare activă. Regulile economiei, cumpărării și contractelor sunt cele existente.

## Tipografie și imagini

Tow Display pentru titluri și butoane, Tow Text pentru descrieri; fonturi derivate cu licențele și proveniența în Assets/TowTrouble/Art/UI/Theme/Fonts/Provenance.md. Au diacritice românești și fallback TMP. Textul încearcă dimensiunea preferată, apoi se ajustează în limitele spațiului pentru a evita tăierea.

Logoul existent este păstrat. Cele trei fotografii de contract sunt randări statice din geometria tutorialului, cu vehicule și cablu pozate în scene temporare; nu sunt capturi ale unei misiuni jucate.

## Authoring și limite

UiSkinAuthoring.Apply leagă fonturile și fotografiile de CanyonRecovery și salvează scena prin API-ul Editorului. BindExisting păstrează tema la regenerarea demo-ului. Previzualizarea garajului pornește din fața atelierului și verifică obstacolele pentru a evita camera în perete.

Scena tutorialului conține doar referințe UI actualizate. Mega/Desert și asseturile furnizorilor nu sunt modificate. Nu s-a pornit Play Mode și nu s-au creat builduri. Dovezi: Docs/Validation/UI-Finish.
