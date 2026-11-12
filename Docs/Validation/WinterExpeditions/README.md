# Winter Expeditions

Open `OpenWorldRecovery` in Unity and enter Play Mode. Choose a starter on the first launch. **Play Solo** starts classic Tow Trouble. **Winter Expeditions · DLC** starts solo play in the separate winter scene. From the DLC, use **Esc → Main Menu** to return to the classic main menu.

Both modes use the same vehicles, garage builds, credits, career and settings. A previous standalone winter profile is imported once. If both saves exist, the classic profile keeps its vehicle selection, settings and conflicting garage setups. Balances, unlocked equipment and completion records are combined, and the old winter save is retained.

In Unity Play Mode, both main-menu buttons launched their own scene and terrain. The isolated test profile carried two vehicles, an equipped engine and paint, a classic completion and 4,321 CR into winter. Completing **Plow Pinewood Road** added 1,050 CR. Returning through the pause menu showed 5,371 CR and the saved winter completion in the classic profile. All 11 save-store Edit Mode tests passed, including the migration and reset checks.

In the DLC, **Tab** opens and closes the expedition journal. **Winter Jobs** lists the work contracts. In **Field Surveys**, stop at the destination for readings and return to the field station to log the survey. At the field station, **G** opens the garage. Use **F** to attach to a yellow anchor or a job's tow eye, **Q** to reel in, **E** to pay out, and **F** to detach.

## Winter jobs

- **Supplies for Pinewood** loads one crate when you stop inside the orange ring at the workshop. Stop inside the unloading ring at Pinewood.
- **Workshop Meadow rescue** guides you through the meadow entrance to a marked pull point. Back into the ring, attach the rear cable and reel the pickup onto firmer ground. Tow it into the workshop receiving ring and wait for it to settle.
- **Timber on Lakeside Road** marks the opening before the obstacle and the point from which to reel. Pull the entire trunk off the road into the marked lay-by.
- **Plow Pinewood Road** fits a service blade when you stop inside the fitting ring and press **F**. Drive forward between the orange poles, with the blade in the snow, to clear all eight sections. If you miss a section, back up past the waypoint and make another pass.

The winter profile saves each first completion and its reward. An unfinished job can be restarted from its journal card after detaching the cable.

The isolated Play Mode probe used the journal and controlled positions at the objectives. It loaded and unloaded one crate, physically reeled in the pickup and tree, and drove the plow through all eight sections. The four jobs paid a total of 4,550 CR. Reloading the profile restored the four completions, and repeating the supply run paid no second reward.

[Winter jobs in the journal](WinterJobs-Journal.png) · [Plowing Pinewood Road](WinterJobs-Plow.png)

### Full route playtest

Unity 6000.3.19f1 Play Mode, stock Scout and isolated profile. The driver followed the road network and used steering, throttle, brake and the winch from the normal garage spawn. The run covered about 1.15 km with zero resets, cable snaps or runtime errors. The automation timed out on the last approach to the tree and resumed from the stopped vehicle in the same Play Mode session.

| Job | Verified through completion | Reward |
| --- | --- | ---: |
| Plow Pinewood Road | Fitted the blade, passed the drift at an angle, then backed up to the marked remaining snow and cleared all eight sections. | 1,050 CR |
| Supplies for Pinewood | Stopped in the workshop loading ring, drove to Pinewood and unloaded in the receiving ring. | 850 CR |
| Workshop Meadow rescue | Entered the meadow through the marked opening, attached the rear cable, reeled out the pickup and towed it into the workshop receiving ring. | 1,450 CR |
| Timber on Lakeside Road | Drove around the obstacle to the pull point and reeled the entire trunk into the lay-by. | 1,200 CR |

The reloaded test profile contained all four completions and 4,550 CR. The regular profile stayed intact.

[Loading ring](WinterRoute-LoadingBay.png) · [Marked pull point](WinterRoute-PullPoint.png) · [Missed drift cue](WinterRoute-MissedSnow.png)

The winter palette covers the 1 km × 1 km terrain. Snow, packed road surfaces and exposed slate share the existing road geometry. The same road paint feeds tire contact classification. Each suspension contact lowers its support point according to snow depth, wheel load and slip. Existing wheel track meshes show the ruts and the existing wheel emitters throw snow. The terrain collider remains a stable heightfield. Snowfall and day/night run through COZY. The solo demo keeps its expedition progress in a separate career entry.

## Validation

- Unity 6000.3.19f1 compiled the final scripts without errors. Scene inspection found no missing scripts and road routes from base to Pinewood, Lakeshore and High Cabin.
- Isolated Play Mode with a stock Scout. Entered through the production start button, chose Pinewood in the journal, drove out and back with automated controls, logged the survey, and reloaded its saved completion. The first survey paid 850 CR. The run observed 165 active snow particles and up to 122 wheel debris particles, with no recorded runtime errors.
- With controlled vehicle placements, packed snow compressed by 2.5 cm and the powder test by 16.9 cm. With throttle and brake released, the car slid 6.1 m downhill in four simulated seconds on a 17.8° surface. A separate run used F to attach to an anchor and moved the car 4.0 m toward it in five simulated seconds on the winch alone, then detached.
- Checked the two other survey stops and their returns with controlled parking positions. Lakeshore paid 1,050 CR, High Cabin paid 1,500 CR, and revisiting Pinewood did not pay again. Reloading the isolated profile found all three logged surveys.
- Player builds, Switch hardware and physical controllers were outside this test.

[Expedition journal](Journal.png) · [Driving in snow](Driving.png) · [Winching uphill](Winch.png) · [High Cabin](HighCabin.png)

## Persistent snow ruts

The steering probe crossed a prepared 19 cm rut at an angle. The rut added up to 11.4° of front-wheel steering with the input centered, compared with less than 0.1° on fresh snow. The probe recorded three impact events and moved 2.7 m laterally during a three-second powered turn out of the tracks.

In one Unity Play Mode session, a stock Scout made two controlled passes along one line in powder. Depth at the sampled section grew from 8.3 cm to 14.0 cm, and the raised snow at the edge grew from 4.4 cm to 7.4 cm. The test moved the car outside rendering range, then returned and measured the same depth after the nearby meshes rebuilt. At the same position, the chassis settled 13.7 cm lower in the rut than on fresh snow.

A separate three-second wheelspin probe held horizontal movement within 2 mm and dug 19.0 cm into the snow. The Game view showed the compacted floor, raised edges, and the powder and clods kicked up by the tires. The shader rendered in URP, and the probes recorded zero runtime errors. Rut data lasts for the loaded session.

[First pass](SnowRuts-FirstPass.png) · [Second pass](SnowRuts-SecondPass.png) · [Wheelspin](SnowRuts-Wheelspin.png)
