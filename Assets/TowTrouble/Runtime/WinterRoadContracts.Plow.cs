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
        private const float SnowStart = FirstDrift - DriftSpacing * .5f - 1.6f;
        private const float SnowEnd = FirstDrift + (DriftCount - .5f) * DriftSpacing + 2.4f;
        private const float SnowCoverHalfWidth = 5.7f;
        private const float StripOffset = .72f;
        private const float BladeWidth = 3.15f;
        private const int BladeSamples = 12;
        private const float ShoulderOffset = 2.02f;
        private const float SnowFlowTime = .29f;
        private const float BladeLift = .48f;
        private const float DeepestDrift = 1.1f;
        private static readonly float[] SnowHeights = { .45f, .56f, .83f, DeepestDrift, 1.04f, .91f, .76f, .65f };
        private readonly int[,] plowedCells = new int[DriftCount, StripCount];
        private readonly Vector3[] driftPoints = new Vector3[DriftCount];
        private readonly float[,,] snowRemaining = new float[DriftCount, StripCount, CellsPerDrift];
        private readonly float[] snowInWave = new float[StripCount];
        private readonly GameObject[] snowRolls = new GameObject[StripCount];
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

        private float NextSnowAlong => FirstDrift + (nextDrift - .5f) * DriftSpacing + (nextCell + .5f) * CellLength;

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
            if (nextDrift < DriftCount && driver.Speed > 4.5f)
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
            EnsureMaterials();
            Stake(plowStart + plowSide * 2.1f, "Blade station", parent);
            Stake(plowStart - plowSide * 2.1f, "Blade station", parent);
            for (int i = 0; i < DriftCount; i++)
            {
                var point = Ground(plowStart + plowDirection * (FirstDrift + i * DriftSpacing), out _);
                driftPoints[i] = point;
                if (i % 2 == 0 || i == DriftCount - 1)
                {
                    const float width = LaneHalfWidth + .08f;
                    Stake(point + plowSide * width, "Snow pole", parent);
                    Stake(point - plowSide * width, "Snow pole", parent);
                }
            }
            BuildRoadSnow(parent);
            BuildShoulderMeshes(parent);
            SetPinewoodRoad(savedClearance);
        }

        private static float StripCentre(int strip) => strip == 0 ? -StripOffset : StripOffset;

        private void SetPinewoodRoad(bool open)
        {
            System.Array.Clear(snowInWave, 0, snowInWave.Length);
            for (int drift = 0; drift < DriftCount; drift++)
            for (int strip = 0; strip < StripCount; strip++)
            {
                plowedCells[drift, strip] = open ? CompleteStrip : 0;
                for (int cell = 0; cell < CellsPerDrift; cell++)
                    snowRemaining[drift, strip, cell] = open ? 0 : 1;
            }
            RefreshRoadSnow();
            ResetShoulder(open);
            UpdatePlowProgress();
        }

        private const int SnowCellCount = DriftCount * CellsPerDrift;
        private const int SnowSurfaceRows = 201;
        private const int SnowSurfaceColumns = 73;
        private Mesh roadSnowMesh;
        private GameObject roadSnowSurface;
        private Vector3[] roadSnowGround, roadSnowVertices;
        private float[] roadSnowHeight;

        private void BuildRoadSnow(Transform parent)
        {
            roadSnowSurface = CreateSnowSurface("Pinewood snow cover", parent,
                SnowSurfaceRows, SnowSurfaceColumns, out roadSnowMesh, reverse: true);
            roadSnowGround = new Vector3[SnowSurfaceRows * SnowSurfaceColumns];
            roadSnowVertices = new Vector3[roadSnowGround.Length];
            roadSnowHeight = new float[roadSnowGround.Length];
            for (int row = 0; row < SnowSurfaceRows; row++)
            for (int column = 0; column < SnowSurfaceColumns; column++)
            {
                float along = Mathf.Lerp(SnowStart, SnowEnd, row / (float)(SnowSurfaceRows - 1));
                float across = SnowSurfaceAcross(column);
                Vector3 point = plowStart + plowDirection * along + plowSide * across;
                int vertex = row * SnowSurfaceColumns + column;
                roadSnowGround[vertex] =
                    roadSnowSurface.transform.InverseTransformPoint(Ground(point, out _));
                roadSnowHeight[vertex] = UndisturbedSnowHeight(along, across);
            }
        }

        private static float SnowSurfaceAcross(int column) =>
            SnowCoverHalfWidth * (2f * column / (SnowSurfaceColumns - 1) - 1);

        private static float UndisturbedSnowHeight(float along, float across)
        {
            float drift = Mathf.Clamp((along - FirstDrift) / DriftSpacing, 0, DriftCount - 1);
            int lower = Mathf.FloorToInt(drift);
            float depth = Mathf.SmoothStep(SnowHeights[lower],
                SnowHeights[Mathf.Min(lower + 1, DriftCount - 1)], drift - lower);
            float distance = Mathf.Abs(across);
            float sideSeed = across < 0 ? 11f : 26f;
            float width = Mathf.Lerp(3.8f, 5.45f, Mathf.PerlinNoise(along * .16f, sideSeed));
            float shoulder = 1 - .56f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.55f, 2.8f, distance));
            float edge = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(width - 1.8f, width, distance));
            float end = Mathf.SmoothStep(0, 1, Mathf.Min(along - SnowStart, SnowEnd - along) / 2.7f);
            float variation = Mathf.PerlinNoise(along * .39f + 6f, across * .68f + 17f) - .5f;
            return Mathf.Max(0, depth * shoulder + variation * .25f) * edge * end;
        }

        private float RoadSnowRemaining(int cell, int strip) =>
            snowRemaining[cell / CellsPerDrift, strip, cell % CellsPerDrift];

        private void RefreshRoadSnow()
        {
            for (int row = 0; row < SnowSurfaceRows; row++)
            {
                float along = Mathf.Lerp(SnowStart, SnowEnd, row / (float)(SnowSurfaceRows - 1));
                float sample = Mathf.Clamp((along - FirstDrift + DriftSpacing * .5f) / CellLength - .5f, 0, SnowCellCount - 1);
                int first = Mathf.FloorToInt(sample);
                int next = Mathf.Min(first + 1, SnowCellCount - 1);
                float blend = Mathf.SmoothStep(0, 1, sample - first);
                float left = Mathf.Lerp(RoadSnowRemaining(first, 0), RoadSnowRemaining(next, 0), blend);
                float right = Mathf.Lerp(RoadSnowRemaining(first, 1), RoadSnowRemaining(next, 1), blend);
                for (int column = 0; column < SnowSurfaceColumns; column++)
                {
                    float across = SnowSurfaceAcross(column);
                    float sideBlend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.24f, .24f, across));
                    float cut = 1 - SnowbankEdge(across);
                    float remaining = Mathf.Lerp(1, Mathf.Lerp(left, right, sideBlend), cut);
                    int vertex = row * SnowSurfaceColumns + column;
                    roadSnowVertices[vertex] = roadSnowGround[vertex] + Vector3.up * (roadSnowHeight[vertex] * remaining - .015f);
                }
            }
            roadSnowSurface.SetActive(true);
            WriteSnowSurface(roadSnowMesh, roadSnowVertices);
        }

        private static float SnowbankEdge(float across) =>
            Mathf.SmoothStep(0, 1, Mathf.InverseLerp(LaneHalfWidth - .24f, LaneHalfWidth + .24f, Mathf.Abs(across)));

        private float RoadsideSnowAt(Vector3 point)
        {
            Vector3 delta = point - plowStart;
            float across = Vector3.Dot(delta, plowSide);
            if (Mathf.Abs(across) < LaneHalfWidth - .24f || Mathf.Abs(across) >= SnowCoverHalfWidth) return 0;
            float along = Vector3.Dot(delta, plowDirection);
            if (along <= SnowStart || along >= SnowEnd || Mathf.Abs(delta.y) > 2f) return 0;
            return UndisturbedSnowHeight(along, across) * SnowbankEdge(across) / DeepestDrift;
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
            float remaining = snowRemaining[index, strip, cell];
            return remaining * SnowHeights[index] / DeepestDrift *
                (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.18f, LaneHalfWidth, Mathf.Abs(across))));
        }

        internal float SnowAt(Vector3 point) => Mathf.Max(DriftAt(point),
            Mathf.Max(RoadsideSnowAt(point), Mathf.Max(ShoulderSnowAt(point), SupplyDriftAt(point))));

        internal void FixedTick(RecoveryVehicle driver)
        {
            if (!PlowFitted) return;
            if (cleared >= DriftCount) { AdvanceSnowWave(); return; }
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
            // Sample the snow before the swept blade removes it.
            float contactLoad = ApplyBladeResistance(driver, contact, speed);
            float cutAtSpeed = Mathf.Lerp(1, .44f, Mathf.InverseLerp(3.8f, 7f, speed));
            bool changed = false, clearedCell = false;
            for (int index = 0; index < DriftCount; index++)
            {
                float sectionStart = FirstDrift + (index - .5f) * DriftSpacing;
                if (sectionStart > to || sectionStart + DriftSpacing < from ||
                    Mathf.Abs(contact.y - driftPoints[index].y) > 1.4f) continue;

                for (int cell = 0; cell < CellsPerDrift; cell++)
                {
                    float along = sectionStart + (cell + .5f) * CellLength;
                    if (along < from || along > to) continue;
                    float across = Vector3.Dot(Vector3.Lerp(previous, contact, (along - from) / (to - from)) - plowStart, plowSide);
                    int bit = 1 << cell;
                    for (int strip = 0; strip < StripCount; strip++)
                    {
                        if ((plowedCells[index, strip] & bit) != 0) continue;
                        float distance = Mathf.Abs(across - StripCentre(strip));
                        float overlap = Mathf.Clamp01((reach + .65f - distance) / 1.3f);
                        float bite = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.24f, .85f, overlap));
                        if (bite <= 0) continue;

                        float before = snowRemaining[index, strip, cell];
                        float remaining = Mathf.Max(0, before - bite * cutAtSpeed);
                        remaining = remaining > .12f ? remaining : 0;
                        snowRemaining[index, strip, cell] = remaining;
                        snowInWave[strip] += (before - remaining) * SnowHeights[index] / DeepestDrift;
                        changed = true;
                        if (remaining > 0) continue;
                        plowedCells[index, strip] |= bit;
                        clearedCell = true;
                    }
                }
            }
            if (changed) RefreshRoadSnow();
            if (clearedCell) UpdatePlowProgress();
            AdvanceSnowWave(contactLoad);
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
                float depth = DriftAt(point);
                if (depth <= 0) continue;
                driver.Body.AddForceAtPosition(-forward * (forcePerSample * depth), point);
                load += depth / BladeSamples;
            }
            return load;
        }

        private void AdvanceSnowWave(float contactLoad = 0)
        {
            float release = 1 - Mathf.Exp(-Time.fixedDeltaTime / SnowFlowTime);
            float left = DrainSnowStrip(0, release);
            float right = DrainSnowStrip(1, release);
            bladeSnowWave.SetActive(left + right > .03f);
            AdvanceShoulderFlow();
            bladeLoad = Mathf.MoveTowards(bladeLoad, contactLoad, Time.fixedDeltaTime * (contactLoad > bladeLoad ? 11f : 6f));
        }

        private float DrainSnowStrip(int strip, float release)
        {
            float before = snowInWave[strip];
            float released = before < .012f ? before : before * release;
            float moving = snowInWave[strip] = before - released;

            float sign = strip == 0 ? -1 : 1;
            float size = Mathf.Clamp01(moving / 1.6f);
            var roll = snowRolls[strip];
            roll.SetActive(moving > .015f);
            roll.transform.localPosition = new Vector3(sign * (.82f + .26f * (1 - size)), -.15f, .42f);
            roll.transform.localScale = new Vector3(.85f + .78f * size, .18f + .63f * size, .75f + .31f * size);

            if (released > 0) LaunchShoulderSnow(strip, released);
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
            BuildSnowStreams();
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
