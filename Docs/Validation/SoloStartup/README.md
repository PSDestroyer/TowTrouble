# Corecție pornire solo — 2026-10-31

Logul utilizatorului: NullReferenceException în RecoveryVehicle.ResetVehicle, apelat prin SetSpawn din RecoveryWorld.ResetRecovery în OnStartServer / ConfigureSession / StartRecovery.

Reproducere izolată înainte de fix: instanță temporară a StrandedClassic, dezactivată înainte de Awake. Rigidbody exista pe obiect, dar câmpul body nu era inițializat. Apelul SetSpawn a aruncat aceeași NullReferenceException la ResetVehicle. Scena utilizatorului și profilul nu au fost modificate de reproducere.

Corecție: EnsureInitialized comun pentru Awake, SetSpawn, ResetVehicle și SetSimulation. Inițializarea se execută o dată; Rigidbody, modelul, suspensia, sunetul și efectele sunt pregătite inclusiv pe o țintă inactivă. SetSpawn inițializează înainte de a salva poziția solicitată. Awake ulterior păstrează configurația deja pregătită. Modificarea nu activează automat ținta.

După fix: același apel izolat reușește. Cele cinci verificări din InitializationChecks.json au trecut, inclusiv Awake întârziat și resetarea la poziția salvată. PrefabChecks.json confirmă inițializarea pe Scout, Hauler și StrandedClassic. Verificările au fost invocate direct în Edit Mode, fără Test Runner sau simularea fizicii. Pentru copiile inactive din preview se verifică poziția Transform, nu o poziție nativă de corp fizic simulat.

Compilare fără erori. Nu s-a pornit Play Mode, nu s-au pornit server/client și nu s-au creat builduri. Pornirea completă și condusul rămân pentru retestarea utilizatorului. Erorile istorice din Console nu au fost șterse.

Retest: Play în OpenWorldRecovery → Explorează. Solo este deja legat la Host(true,true), cu pregătire automată, fără necesitatea unui al doilea jucător. Apoi Tab → R01 → Acceptă, pentru verificarea activării întârziate a țintei. Ieșirea și o nouă pornire trebuie să funcționeze la fel.
