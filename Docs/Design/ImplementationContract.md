# Contract de corectare — TOW TROUBLE

Sursă: `/Users/gheorghe/Desktop/TowTrublw/TOW_TROUBLE.md`, plus imaginile și instrucțiunile explicite din chat.

- Înlocuire completă Low_Poly_Environment_World; ștergere folder și materiale derivate, după eliminarea referințelor.
- Mediu: Stylized Nature & Environment MEGA PACK, So Stylized, materiale URP originale. Drumuri: Low Poly Road Pack, Broken Vector, adaptate cromatic.
- Vehicule: cele două produse Hot Rod Constructor cumpărate; descărcare din contul Unity și confirmarea conținutului real. Fără arme.
- Primele trei contracte din MD: Primul cârlig, La marginea drumului, Roți în noroi. Secțiunea 8 definește aceste trei contracte ca prima versiune, iar celelalte nouă urmează după validarea controlului și remorcării.
- Flux: titlu → meniu vertical → contracte → vehicul/lobby → misiune → rezultate → progres salvat.
- Ecrane: contracte cu trei zone, vehicule cu statistici bazate pe reglaje, lobby cu două mașini și stare pregătit, setări, credite, pauză cu confirmări, rezultate cu medalii.
- HUD ca în concepte: obiectiv sus stânga, CO-OP numai online sus dreapta, viteză jos dreapta, cablu contextual jos stânga. Nume/culori P1 albastru și P2 portocaliu. Tensiune verde/galben/roșu cu simbol/text.
- Mecanici: cablu propriu/jucător, două cabluri pe aceeași țintă, scurtare/alungire, desprindere la suprasarcină, reset la punct sigur, cameră rotativă cu recentrare, semnale co-op.
- Rețea: FishNet, autoritate pe server, validare două instanțe, reset/deconectare. Instrucțiunea FishNet înlocuiește opțiunile de rețea nehotărâte din MD.
- Input: tastatură și controller, focus vizibil și comenzi adaptate dispozitivului. Setări persistente de sunet/imagine/comenzi/accesibilitate/joc.

Starea de implementare și dovezile se raportează separat; existența unui buton sau a unui fișier nu este dovadă de validare.

## Constrângere de execuție
Utilizatorul a cerut explicit să nu se creeze builduri. Nu se mai rulează BuildPipeline sau validare nativă; verificarea continuă în Unity Editor / Play Mode. Build-ul care era deja în curs s-a încheiat înainte de anulare și nu este folosit drept livrabil.
