# Expanding wheel dust and Play Mode checks — 2026-11-10

The user explicitly authorized Play Mode validation and requested a growing, better-looking cloud while protecting Switch frame rate. This supersedes the earlier Edit Mode-only scope for this task. No player build was made.

## Result and changes

- Native Play Mode on the real R20 sand confirmed the previous dust emitted during ordinary rolling but underrepresented stationary wheelspin. With the same physical fixture, the old spinning cloud averaged about 13 live fine particles; the revised desktop effect averages about 25. Ordinary rolling reaches 32 live fines at roughly 2.6 m/s, with all four wheel slips at zero.
- The growth curve starts at 0.32× particle size and grows to 2.8× while opacity fades. Actual live particles read back at 0.32× when young and about 2.50× at mature ages: approximately eightfold diameter growth across those samples. Upward drift and independent puff variation remain bounded. This is expanding billboard presentation, not a volumetric simulation.
- New owned `TowTrouble/Wheel Dust` shader combines three feathered lobes to break the perfect-circle silhouette. It has one transparent unlit pass, no texture/depth/noise samples, lighting, particle collision or additional render target. Only the existing Wheel powder material now references it. Exhaust appearance is unchanged. Authoring uses the same shader and initialization as runtime.
- Actual grounded wheel rate plus a square-root slip response avoids suppressing stalled wheelspin twice. No drive, traction, network or profile data was changed.
- Desktop: 24 fine particles per wheel, ≤14 emitted per second, about 1.5 s life. Switch: 16 fine/16 coarse per wheel, ≤8 fine emissions/s, about 1.25 s life. Switch retains two fine emitters at once: rear tires during normal rolling, or the two highest-slip tires during wheelspin. Already emitted particles fade normally when that pair changes. Coarse grains still cover all contact wheels.
- Emission fades between 30–50 m on Switch, 55–80 m on desktop. Coating/physics continue normally. Maximum allocated capacities are 208 particles per desktop car and 128 per Switch car, including coarse and fine systems; transient fading particles can remain in the other two fine pools. No per-frame object/material creation was added.

## Actual Play checks

Used the existing Editor validation input seam, memory-only `TowTrouble.Validation.DustPlay` profile and in-process offline Solo transport. The player loop advanced across thousands of frames. No real save was loaded for the fixture or written by it.

1. Baseline and final desktop: teleport to the actual sand contact at R20, settle, drive with 40% throttle and no brake. Final speed ~2.60 m/s (9 km/h), slip `(0,0,0,0)`, four actual Sand contacts and 32 live fines at capture. No artificial velocity or wheel telemetry was injected.
2. Stationary wheelspin: same dynamic car/ground, freeze only body X/Z translation and apply full throttle. Suspension and tire solver continue running; actual slips settle near `(0.33,0.50,0.16,0)`, wheel rates near `(6,9,3,0)` rad/s and body speed below 0.001 m/s. This is a controlled physical fixture, not a claim of testing an attached tow load. Final desktop capture has 25 live fines, versus 13 in the baseline.
3. Switch rendering preview: same physical cases with the actual Switch pipeline (.8 render scale, HDR off), mobile sky, scene renderer/culling and 600 m camera budget. Editor Mac quality names only expose PC, so the Switch pipeline was explicitly assigned to the runtime override; the persistent PC quality asset was not edited. Final normal capture has 14 live fines; wheelspin has 17. Both screenshots were inspected. The earlier four-emitter Switch captures/data are retained with `FourWheels-` prefixes.
4. After stopping inputs and applying the brake, zero slips and zero remaining fine particles were verified. All four fine pool limits are 16, material uses the new supported shader and shader errors are false: RestAndBindings.json.
5. Nine direct NUnit methods passed, including no dust at rest/in air/on wood, reverse direction, sustained normal rolling, stationary spin and bounded/selective Switch emission. These were direct method invocations, not a Test Runner run. Final C# compilation has zero errors/warnings.

## Performance evidence and limits

Four alternating pairs of dust renderers OFF/ON were sampled in each configuration, after a three-second warm-up and a half-second settling interval per block. Emission and all other gameplay continued in both halves. Render time and FrameTimingManager GPU readings were sampled alongside real frame counts. The callbacks add identical measurement overhead to each half. `PerformanceSummary.json` contains the reductions; raw rows remain available.

Final optimized Switch preview, **Apple M2 Pro Editor**, .8 render scale:

| Metric | Dust off | Dust on |
|---|---:|---:|
| Samples | 181 | 179 |
| Median frame | 33.388 ms | 33.367 ms |
| Median GPU frame | 22.625 ms | 22.710 ms |
| Median Editor render time | 3.475 ms | 3.504 ms |
| Median draw calls | 419 | 421 |

The sampled frame rate remained at the ~30 FPS cap in both halves. This single interleaved Editor run showed about 0.085 ms difference between aggregate GPU medians and about 0.028 ms between render-time medians; these are observations, not a portable upper bound. The desktop run and the earlier four-emitter preview had variable GPU timing across blocks, so do not treat them as precise causal benchmarks or infer console headroom from them. The optimized Switch fine cloud has roughly 17 live particles during steady stationary spin and two fine draw submissions, compared with roughly 22 and three-to-four before selection. Ordinary rolling fines dropped from 28 to 14 in the captured Switch cases.

**No Nintendo hardware was connected or profiled. Real Switch FPS and worst-case costs still require a device run**, including close camera framing, two nearby moving vehicles, snow and existing expensive environments. The Editor figures do not certify Switch performance. No FPS gain is claimed.

## Preservation and reproducibility

Play stopped; preview flags and validation key cleared; memory-only profile released. Real save.json, backup and previous file hashes match the pre-test baseline. Active scene and original dirty state preserved; PC_RPAsset, LOD 2 and 2300 m camera restored. Protected scenes, package/assembly changes belonging to concurrent localization work, quality and pipeline assets remain hash-identical to this task's baseline. No source scene save, build, commit or push.

Existing Console history was retained. Concurrent localization work produced an initially missing BuildLanguageSettings compiler symbol (resolved by that work before final compilation), and Addressables/locale initialization errors during Play startup. Historical Inspector/AI-account errors also remain. No dust component or new dust shader error was found in the sampled Console; successful dust checks do not imply the entire project's runtime Console is clean.

Key evidence: BaselineDrive.json, FinalDrive.json, SwitchDrive.json; corresponding actual Game screenshots; Cost-PC.json, FourWheels-Cost-SwitchPreview.json, Cost-SwitchPreview.json; Tests.json, Compile.json, RestAndBindings.json, Restore.json, FinalState.json and Preservation.json.
