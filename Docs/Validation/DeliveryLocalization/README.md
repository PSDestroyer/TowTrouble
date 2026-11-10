# Delivery progress localization — 2026-11-11

The waypoint uses `Delivering…`, which was absent from the source/catalog. The mission status uses `Delivering the vehicle…`, which was incorrectly mapped to `Stop to deliver`.

Added direct authored translations of both progress phrases in28 non-English locales and regenerated29 native locale tables/catalog. Removed the incorrect mapping. Canonical English strings and delivery logic unchanged. Added a regression asserting both phrases translate and differ from the stopping instruction in every locale.

Native validation:6 directly invoked localization NUnit tests passed; compilation0errors/0warnings. Actual HUD methods SetWaypointContent/SetMissionStatus exercised across29 locales and5 text scales. Initial check found an existing overlong Indonesian approach instruction at100%/110%; shortened its authored wording while keeping the vehicle-delivery meaning. Final layout report stored alongside this file. No build, external translation service, Play, scene save or career changes.
