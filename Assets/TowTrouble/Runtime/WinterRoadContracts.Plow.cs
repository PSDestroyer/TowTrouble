using UnityEngine;

namespace TowTrouble
{
    internal sealed partial class WinterRoadContracts
    {
        private const int DriftCount = 8;
        private const int CellsPerDrift = 4;
        private const int StripCount = 2;
        private const int CompleteStrip = (1 << CellsPerDrift) - 1;
        private const float DriftSpacing = 3f;
        private const float FirstDrift = 6f;
        private const float CellLength = DriftSpacing / CellsPerDrift;
        private const float LaneHalfWidth = 1.5f;
        private const float StripOffset = .72f;
        private const float BladeWidth = 3.15f;
        private const int PrototypeDrift = 3;
        private const int BladeSamples = 12;
        private const float ShoulderOffset = 2.02f;
        private const float ShoulderWidth = 1.05f;
        private const float SnowFlowTime = .29f;
        private const float BladeLift = .48f;
        private static readonly float[] SnowHeights = { .24f, .28f, .48f, .70f, .64f, .51f, .39f, .32f };
        private readonly int[,] plowedCells = new int[DriftCount, StripCount];
        private readonly GameObject[,] drifts = new GameObject[DriftCount, StripCount];
        private readonly GameObject[] piles = new GameObject[DriftCount];
        private readonly Vector3[] driftPoints = new Vector3[DriftCount];
        private readonly GameObject[,] prototypeBanks = new GameObject[StripCount, CellsPerDrift];
        private readonly float[,] prototypeSnow = new float[StripCount, CellsPerDrift];
        private readonly GameObject[,] prototypeShoulderBanks = new GameObject[StripCount, CellsPerDrift];
        private readonly float[,] snowInWave = new float[StripCount, CellsPerDrift];
        private readonly float[,] shoulderSnow = new float[StripCount, CellsPerDrift];
        private readonly GameObject[] snowRolls = new GameObject[StripCount];
        private float shedLeft, shedRight;
        private GameObject bladeSnowWave;
        private Vector3 loweredBladePosition;
        private bool bladeLowered;
        private float bladeTravel, bladeLoad;
        internal bool BladeLowered => PlowFitted && bladeLowered;
        internal float BladeLoad => PlowFitted ? bladeLoad : 0;
        private Vector3 plowStart, plowDirection, plowSide, previousBladePoint;
        private GameObject pinewoodRoad;
        private bool pinewoodCleared;
        private int cleared, nextDrift, nextCell, nextStrip;
        private float nextSpray;

        private float NextSnowAlong => FirstDrift + (nextDrift - .5f) * DriftSpacing + (nextCell + .5f) * CellLength;
        private const float PrototypeCentre = FirstDrift + PrototypeDrift * DriftSpacing;

        private Vector3 NextSnowPoint => nextDrift >= DriftCount ? driftPoints[DriftCount - 1] :
            driftPoints[nextDrift] + plowDirection * ((nextCell + .5f) * CellLength - DriftSpacing * .5f) +
            plowSide * (nextStrip < 0 ? 0 : StripCentre(nextStrip));

        private string PlowStageName
        {
            get
            {
                if (nextDrift >= DriftCount) return "Lane open";
                string section = nextDrift < 2 ? "Shallow approach" : nextDrift < 6 ? "Deep drift" : "Narrow exit";
                string side = nextStrip < 0 ? "" : nextStrip == 0 ? " · left edge" : " · right edge";
                return $"{section}{side} · {cleared}/{DriftCount}";
            }
        }

        internal bool MissedPlowSection(Vector3 front) => PlowFitted && nextDrift < DriftCount &&
            Vector3.Dot(front - plowStart, plowDirection) > NextSnowAlong + 1.6f;

        private string PlowInstruction(RecoveryVehicle driver, bool near)
        {
            if (!PlowFitted) return near ? "Stop inside the fitting bay" : "Drive to the plow fitting point";
            if (!BladeLowered) return "Lower the blade to start clearing";
            if (MissedPlowSection(driver.FrontHitch))
                return "Snow left behind · back up past the marker and make another pass";
            if (Vector3.Dot(driver.transform.forward, plowDirection) < .7f)
                return "Turn the blade toward the exit poles";
            if (Mathf.Abs(Vector3.Dot(blade.transform.position - NextSnowPoint, plowSide)) > .86f)
                return nextStrip < 0 ? "Center the blade between the poles" : "Line up the blade with the marked edge";
            if (nextDrift == PrototypeDrift && driver.Speed > 4.5f)
                return "Slow down · a fast pass leaves packed snow behind";
            if (nextDrift >= 2 && nextDrift < 6)
                return "Ease through the deep drift · use another pass if an edge remains";
            return "Drive forward between the poles";
        }

        private void BuildPinewoodRoad(bool savedClearance)
        {
            pinewoodCleared = savedClearance;
            plowStart = Ground(new Vector3(-30.5f, 22f, 10.45f), out _);
            plowDirection = new Vector3(-.987f, 0, .16f).normalized;
            plowSide = Vector3.Cross(Vector3.up, plowDirection);
            pinewoodRoad = new GameObject("Pinewood road snow");
            pinewoodRoad.transform.SetParent(owner, false);
            var parent = pinewoodRoad.transform;
            var rotation = Quaternion.LookRotation(plowDirection);
            EnsureMaterials();
            Stake(plowStart + plowSide * 2.1f, "Blade station", parent);
            Stake(plowStart - plowSide * 2.1f, "Blade station", parent);
            for (int i = 0; i < DriftCount; i++)
            {
                var point = Ground(plowStart + plowDirection * (FirstDrift + i * DriftSpacing), out _);
                driftPoints[i] = point;
                for (int strip = 0; strip < StripCount; strip++)
                {
                    var center = Ground(point + plowSide * StripCentre(strip), out _);
                    if (i == PrototypeDrift)
                    {
                        for (int cell = 0; cell < CellsPerDrift; cell++)
                        {
                            Vector3 patch = Ground(center + plowDirection * ((cell + .5f) * CellLength - DriftSpacing * .5f), out _);
                            prototypeBanks[strip, cell] = Snowbank("Prototype drift · snow patch", patch + Vector3.up * .035f,
                                new Vector3(1.6f, SnowHeights[i], 1.04f), rotation, parent);
                            float sign = strip == 0 ? -1 : 1;
                            Vector3 shoulder = Ground(patch + plowSide * (sign * ShoulderOffset - StripCentre(strip)), out _);
                            prototypeShoulderBanks[strip, cell] = Snowbank("Snow deposited on shoulder", shoulder + Vector3.up * .045f,
                                new Vector3(ShoulderWidth, .01f, 1.07f), rotation, parent);
                        }
                    }
                    else drifts[i, strip] = Snowbank("Snow in lane", center + Vector3.up * .04f,
                        new Vector3(1.7f, SnowHeights[i], 3.45f), rotation, parent);
                }
                if (i != PrototypeDrift)
                {
                    var edge = Ground(point + plowSide * 2.03f, out _);
                    piles[i] = Snowbank("Snow pushed to shoulder", edge + Vector3.up * .02f,
                        new Vector3(.93f, .1f, 3.45f), rotation, parent);
                }
                if (i % 2 == 0 || i == DriftCount - 1)
                {
                    float width = i >= 6 ? 1.83f : 2.24f;
                    Stake(point + plowSide * width, "Snow pole", parent);
                    Stake(point - plowSide * width, "Snow pole", parent);
                }
            }
            SetPinewoodRoad(savedClearance);
        }

        private static float StripCentre(int strip) => strip == 0 ? -StripOffset : StripOffset;

        private void SetPinewoodRoad(bool open)
        {
            for (int strip = 0; strip < StripCount; strip++)
            for (int cell = 0; cell < CellsPerDrift; cell++)
            {
                prototypeSnow[strip, cell] = open ? 0 : 1;
                snowInWave[strip, cell] = 0;
                shoulderSnow[strip, cell] = open ? 1 : 0;
                ShowShoulderSnow(strip, cell);
            }

            for (int i = 0; i < DriftCount; i++)
            {
                for (int strip = 0; strip < StripCount; strip++)
                    plowedCells[i, strip] = open ? CompleteStrip : 0;
                ShowPlowSection(i);
            }
            UpdatePlowProgress();
        }

        private void ShowPlowSection(int index)
        {
            float totalRemoved = 0;
            for (int strip = 0; strip < StripCount; strip++)
            {
                if (index == PrototypeDrift)
                {
                    for (int cell = 0; cell < CellsPerDrift; cell++)
                    {
                        float remaining = prototypeSnow[strip, cell];
                        var patch = prototypeBanks[strip, cell];
                        patch.SetActive(remaining > 0);
                        var patchScale = patch.transform.localScale;
                        patchScale.y = SnowHeights[index] * remaining;
                        patch.transform.localScale = patchScale;
                    }
                    continue;
                }
                var bank = drifts[index, strip];
                int removed = 0;
                for (int cell = 0; cell < CellsPerDrift; cell++)
                    if ((plowedCells[index, strip] & (1 << cell)) != 0) removed++;
                totalRemoved += removed;
                bank.SetActive(removed < CellsPerDrift);
                var scale = bank.transform.localScale;
                scale.y = SnowHeights[index] * (1 - .7f * removed / CellsPerDrift);
                bank.transform.localScale = scale;
            }
            if (index == PrototypeDrift) return;
            var pile = piles[index];
            pile.SetActive(totalRemoved > 0);
            var pileScale = pile.transform.localScale;
            pileScale.y = SnowHeights[index] * totalRemoved / (CellsPerDrift * StripCount) * .77f;
            pile.transform.localScale = pileScale;
        }

        private void UpdatePlowProgress()
        {
            cleared = 0;
            nextDrift = DriftCount;
            for (int i = 0; i < DriftCount; i++)
            {
                if ((plowedCells[i, 0] & plowedCells[i, 1]) == CompleteStrip)
                {
                    cleared++;
                    continue;
                }
                if (nextDrift != DriftCount) continue;
                nextDrift = i;
                for (int cell = 0; cell < CellsPerDrift; cell++)
                {
                    bool left = (plowedCells[i, 0] & (1 << cell)) == 0;
                    bool right = (plowedCells[i, 1] & (1 << cell)) == 0;
                    if (!left && !right) continue;
                    nextCell = cell;
                    nextStrip = left && right ? -1 : left ? 0 : 1;
                    break;
                }
            }
        }

        internal float DriftAt(Vector3 point)
        {
            if (pinewoodCleared && Active != WinterJob.Plow) return 0;
            var delta = point - plowStart;
            float across = Vector3.Dot(delta, plowSide);
            if (Mathf.Abs(across) >= LaneHalfWidth) return 0;
            float metres = Vector3.Dot(delta, plowDirection) - FirstDrift + DriftSpacing * .5f;
            int index = Mathf.FloorToInt(metres / DriftSpacing);
            if (index < 0 || index >= DriftCount || Mathf.Abs(point.y - driftPoints[index].y) > 1.4f) return 0;
            int cell = Mathf.Clamp(Mathf.FloorToInt((metres - index * DriftSpacing) / CellLength), 0, CellsPerDrift - 1);
            int strip = across < 0 ? 0 : 1;
            if ((plowedCells[index, strip] & (1 << cell)) != 0) return 0;
            float remaining = index == PrototypeDrift ? prototypeSnow[strip, cell] : 1;
            return remaining * SnowHeights[index] / .70f *
                (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.18f, LaneHalfWidth, Mathf.Abs(across))));
        }

        internal float SnowAt(Vector3 point) => Mathf.Max(DriftAt(point), Mathf.Max(ShoulderSnowAt(point), SupplyDriftAt(point)));

        internal void FixedTick(RecoveryVehicle driver)
        {
            if (!PlowFitted || cleared >= DriftCount) return;
            bladeTravel = Mathf.MoveTowards(bladeTravel, bladeLowered ? 0 : 1, 4.5f * Time.fixedDeltaTime);
            blade.transform.localPosition = loweredBladePosition + Vector3.up * (bladeTravel * BladeLift);
            Vector3 contact = blade.transform.position;
            Vector3 previous = previousBladePoint;
            previousBladePoint = contact;
            Vector3 forward = driver.transform.forward;
            float speed = Vector3.Dot(driver.Body.linearVelocity, forward);
            bool cutting = bladeLowered && bladeTravel < .15f && speed >= .15f &&
                Vector3.Dot(forward, plowDirection) >= .7f &&
                (driver.TryGetWheelContact(0, out _) || driver.TryGetWheelContact(1, out _));
            if (!cutting) { AdvanceSnowWave(); return; }
            float from = Vector3.Dot(previous - plowStart, plowDirection);
            float to = Vector3.Dot(contact - plowStart, plowDirection);
            if (to <= from || to - from > 2f) { AdvanceSnowWave(); return; }

            float reach = BladeWidth * .5f * Mathf.Abs(Vector3.Dot(driver.transform.right, plowSide)) - .07f;
            float resistance = DriftAt(contact);
            bool inPrototype = Mathf.Abs(to - PrototypeCentre) <= DriftSpacing * .5f;
            // Read the undisturbed snow before the swept blade removes it.
            float contactLoad = inPrototype ? ApplyBladeResistance(driver, contact, speed) : 0;
            bool changed = false;
            for (int index = 0; index < DriftCount; index++)
            {
                float sectionStart = FirstDrift + (index - .5f) * DriftSpacing;
                if (sectionStart > to || sectionStart + DriftSpacing < from ||
                    Mathf.Abs(contact.y - driftPoints[index].y) > 1.4f) continue;

                bool changedSection = false;
                for (int cell = 0; cell < CellsPerDrift; cell++)
                {
                    float along = sectionStart + (cell + .5f) * CellLength;
                    if (along < from || along > to) continue;
                    float across = Vector3.Dot(Vector3.Lerp(previous, contact, (along - from) / (to - from)) - plowStart, plowSide);
                    int bit = 1 << cell;
                    for (int strip = 0; strip < StripCount; strip++)
                    {
                        if ((plowedCells[index, strip] & bit) != 0) continue;
                        if (index == PrototypeDrift)
                        {
                            float distance = Mathf.Abs(across - StripCentre(strip));
                            float overlap = Mathf.Clamp01((reach + .65f - distance) / 1.3f);
                            float bite = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.24f, .85f, overlap));
                            if (bite <= 0) continue;
                            float cut = bite * Mathf.Lerp(1, .44f, Mathf.InverseLerp(3.8f, 7f, speed));
                            float before = prototypeSnow[strip, cell];
                            float remaining = Mathf.Max(0, before - cut);
                            prototypeSnow[strip, cell] = remaining > .12f ? remaining : 0;
                            snowInWave[strip, cell] += before - prototypeSnow[strip, cell];
                            if (prototypeSnow[strip, cell] > 0)
                            {
                                changedSection = true;
                                continue;
                            }
                        }
                        else if (Mathf.Abs(across - StripCentre(strip)) > reach) continue;
                        plowedCells[index, strip] |= bit;
                        changedSection = true;
                    }
                }
                if (!changedSection) continue;
                changed = true;
                ShowPlowSection(index);
            }
            if (changed) UpdatePlowProgress();
            AdvanceSnowWave(contactLoad);
            if (inPrototype) return;
            if (resistance > 0)
                driver.Body.AddForceAtPosition(-forward * resistance * (240 + 190 * speed), contact);
            if (resistance <= 0) return;
            if (Time.time < nextSpray) return;
            nextSpray = Time.time + .065f;
            for (int j = 0; j < 5; j++)
            {
                float sign = j == 0 ? -1 : 1;
                plowSnow.Emit(new ParticleSystem.EmitParams
                {
                    position = contact + forward * .22f + plowSide * (sign * Random.Range(.45f, 1.35f)),
                    velocity = plowSide * (sign * Random.Range(1.6f, 3.7f)) +
                        Vector3.up * Random.Range(1.2f, 2.7f) + forward * Random.Range(.2f, 1.4f),
                    rotation3D = Random.rotation.eulerAngles
                }, 1);
            }
        }

        private float ApplyBladeResistance(RecoveryVehicle driver, Vector3 contact, float speed)
        {
            Vector3 forward = driver.transform.forward;
            Vector3 right = driver.transform.right;
            Vector3 cuttingEdge = contact - driver.transform.up * .28f + forward * .09f;
            float forcePerSample = (940 + 490 * speed) / BladeSamples;
            float load = 0;
            for (int i = 0; i < BladeSamples; i++)
            {
                float offset = ((i + .5f) / BladeSamples - .5f) * BladeWidth;
                Vector3 point = cuttingEdge + right * offset;
                float along = Vector3.Dot(point - plowStart, plowDirection);
                if (Mathf.Abs(along - PrototypeCentre) >= DriftSpacing * .5f) continue;
                float depth = DriftAt(point);
                if (depth <= 0) continue;
                driver.Body.AddForceAtPosition(-forward * (forcePerSample * depth), point);
                load += depth / BladeSamples;
            }
            return load;
        }

        private void ShowShoulderSnow(int strip, int cell)
        {
            float snowAmount = shoulderSnow[strip, cell];
            var bank = prototypeShoulderBanks[strip, cell];
            bank.SetActive(snowAmount > .015f);
            bank.transform.localScale = new Vector3(ShoulderWidth, snowAmount * .92f, 1.07f);
        }

        private float ShoulderSnowAt(Vector3 point)
        {
            Vector3 delta = point - driftPoints[PrototypeDrift];
            float along = Vector3.Dot(delta, plowDirection);
            float across = Vector3.Dot(delta, plowSide);
            float distance = Mathf.Abs(Mathf.Abs(across) - ShoulderOffset);
            if (Mathf.Abs(along) >= DriftSpacing * .5f || distance >= ShoulderWidth * .5f || Mathf.Abs(delta.y) > 1.4f)
                return 0;
            int cell = Mathf.Clamp(Mathf.FloorToInt((along + DriftSpacing * .5f) / CellLength), 0, CellsPerDrift - 1);
            return shoulderSnow[across < 0 ? 0 : 1, cell] * .7f *
                (1 - Mathf.SmoothStep(0, 1, distance / (ShoulderWidth * .5f)));
        }

        private void AdvanceSnowWave(float contactLoad = 0)
        {
            float release = 1 - Mathf.Exp(-Time.fixedDeltaTime / SnowFlowTime);
            float left = DrainSnowStrip(0, release, ref shedLeft);
            float right = DrainSnowStrip(1, release, ref shedRight);
            bladeSnowWave.SetActive(left + right > .03f);
            bladeLoad = Mathf.MoveTowards(bladeLoad, contactLoad, Time.fixedDeltaTime * (contactLoad > bladeLoad ? 11f : 6f));
        }

        private float DrainSnowStrip(int strip, float release, ref float spray)
        {
            float moving = 0, released = 0;
            for (int cell = 0; cell < CellsPerDrift; cell++)
            {
                float before = snowInWave[strip, cell];
                if (before <= 0) continue;
                float shed = before < .012f ? before : before * release;
                snowInWave[strip, cell] -= shed;
                shoulderSnow[strip, cell] += shed;
                moving += snowInWave[strip, cell];
                released += shed;
                ShowShoulderSnow(strip, cell);
            }

            float sign = strip == 0 ? -1 : 1;
            float size = Mathf.Clamp01(moving / 1.6f);
            var roll = snowRolls[strip];
            roll.SetActive(moving > .015f);
            roll.transform.localPosition = new Vector3(sign * (.82f + .26f * (1 - size)), -.15f, .42f);
            roll.transform.localScale = new Vector3(.85f + .78f * size, .18f + .63f * size, .75f + .31f * size);

            spray += released * 42f;
            int chunks = Mathf.FloorToInt(spray);
            spray -= chunks;
            for (int i = 0; i < chunks; i++)
            {
                plowSnow.Emit(new ParticleSystem.EmitParams
                {
                    position = blade.transform.TransformPoint(new Vector3(sign * Random.Range(1.15f, 1.55f), -.08f, .43f)),
                    velocity = plowSide * (sign * Random.Range(2.3f, 4.4f)) +
                        Vector3.up * Random.Range(.85f, 2f) + plowDirection * Random.Range(.1f, .8f),
                    rotation3D = Random.rotation.eulerAngles
                }, 1);
            }
            return moving;
        }

        internal void ToggleBlade() => bladeLowered = !bladeLowered;

        internal void FitPlow(RecoveryVehicle driver)
        {
            if (!AtPlowStart(driver.transform.position) || driver.Speed > .65f) return;
            currentMarker.SetActive(false);
            blade = new GameObject("Service plow blade");
            blade.transform.SetParent(driver.transform, false);
            loweredBladePosition = driver.transform.InverseTransformPoint(driver.FrontHitch) + new Vector3(0, -.30f, .52f);
            bladeLowered = false;
            bladeTravel = 1;
            bladeLoad = 0;
            blade.transform.localPosition = loweredBladePosition + Vector3.up * BladeLift;
            Piece(blade.transform, "Moldboard", PrimitiveType.Cube, Vector3.zero, new Vector3(BladeWidth, .60f, .16f), orange, Quaternion.Euler(-18, 0, 0));
            var edge = Piece(blade.transform, "Cutting edge", PrimitiveType.Cube, new Vector3(0, -.28f, .09f), new Vector3(BladeWidth, .13f, .19f), steel);
            Piece(blade.transform, "Mount", PrimitiveType.Cube, new Vector3(0, .03f, -.34f), new Vector3(1.1f, .28f, .65f), steel);
            plowSnow = blade.AddComponent<ParticleSystem>();
            plowSnow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = plowSnow.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .8f);
            main.startSize = new ParticleSystem.MinMaxCurve(.09f, .25f);
            main.startSpeed = 0;
            main.gravityModifier = .8f;
            main.maxParticles = 220;
            var emission = plowSnow.emission;
            emission.enabled = false;
            var shape = plowSnow.shape;
            shape.enabled = false;
            var renderer = plowSnow.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = edge.GetComponent<MeshFilter>().sharedMesh;
            renderer.sharedMaterial = particles;
            bladeSnowWave = new GameObject("Snow rolling off blade");
            bladeSnowWave.transform.SetParent(blade.transform, false);
            for (int strip = 0; strip < StripCount; strip++)
                snowRolls[strip] = Snowbank("Snow roll along cutting edge", blade.transform.position, Vector3.one,
                    blade.transform.rotation, bladeSnowWave.transform);
            shedLeft = shedRight = 0;
            bladeSnowWave.SetActive(false);
            plowSnow.Play();
            previousBladePoint = blade.transform.position;
        }

        private GameObject Snowbank(string name, Vector3 point, Vector3 scale, Quaternion rotation, Transform parent)
        {
            if (driftMesh == null) driftMesh = BuildDriftMesh();
            var bank = new GameObject(name);
            bank.transform.SetParent(parent, false);
            bank.transform.SetPositionAndRotation(point, rotation);
            bank.transform.localScale = scale;
            bank.AddComponent<MeshFilter>().sharedMesh = driftMesh;
            bank.AddComponent<MeshRenderer>().sharedMaterial = snow;
            return bank;
        }
    }
}
