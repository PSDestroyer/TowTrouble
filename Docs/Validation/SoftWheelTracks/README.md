# Soft wheel-track transitions — 2026-10-31

The supplied screenshot showed hard grass/dirt color boundaries and dark rectangular strip ends. Visual surface weights now interpolate neighboring authored map cells per tread vertex. Adjacent materials overlap with graded opacity instead of breaking the strip at each categorical boundary. This also blends across the tread when a tire straddles the road edge.

The shader feathers the sides and beginning of each continuous trail. Only the latest segment has a faded end; extending a trail opens that end. Chunk generations protect recycled geometry from stale tail references. Grass opacity is reduced from .72 to .38 and its compaction shade raised from .62 to .86; soft-ground opacity is .54. Authoring defaults match the saved materials.

Traction, terrain geometry/paint, Living Mud, vehicles, lights and networking are unchanged. Hard/wood overrides and unsupported-contact/teleport breaks are retained. No additional packages or map rebake are required.

Validation: Unity compilation zero errors/warnings, no shader errors;11 direct Edit Mode checks passed. GrassDirtBlend.png is a static preview using native terrain layers and the production contact/mesh code. Static pool audit fed6000 contacts:36 chunks maximum,14 reused, valid alpha and cap data. No Play Mode, physics simulation, Test Runner, networking startup or builds. Runtime driving/performance remains for the user.

Restart Play and make a fresh pass from road to grass, then return and make a tight turn. Check the smoother color boundary and tapered ends; existing runtime tracks are session-local and will be rebuilt on a fresh session.

Pre-change code copies: /Users/gheorghe/Desktop/TowTrublw/Backups/SoftTracks-20261031. Earlier uncommitted tracks and vehicle-effects work was preserved.
