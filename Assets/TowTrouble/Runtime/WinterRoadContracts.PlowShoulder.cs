using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    internal sealed partial class WinterRoadContracts
    {
        private const int RidgeRows = 215;
        private const int RidgeColumns = 16;
        private const int StreamRows = 10;
        private const int StreamColumns = 5;
        private const float RidgeStart = FirstDrift - 2.2f;
        private const float RidgeEnd = FirstDrift + (DriftCount - 1) * DriftSpacing + 4.2f;
        private const float RidgeInnerEdge = 1.44f;
        private const float RidgeWidth = 2.62f;
        private const float RidgeStep = (RidgeEnd - RidgeStart) / (RidgeRows - 1);
        private const float LandingTime = .25f;
        private readonly float[,] ridgeSnow = new float[StripCount, RidgeRows];
        private readonly float[,] ridgeAcrossSum = new float[StripCount, RidgeRows];
        private readonly Mesh[] shoulderMeshes = new Mesh[StripCount];
        private readonly Mesh[] streamMeshes = new Mesh[StripCount];
        private readonly GameObject[] shoulderObjects = new GameObject[StripCount];
        private readonly GameObject[] streamObjects = new GameObject[StripCount];
        private readonly Vector3[][] shoulderBase = new Vector3[StripCount][];
        private readonly Vector3[][] shoulderVertices = new Vector3[StripCount][];
        private readonly Vector3[] streamVertices = new Vector3[StreamRows * StreamColumns];
        private readonly float[] streamSpray = new float[StripCount];
        private readonly List<ShoulderFlight> snowFlights = new(36);

        private struct ShoulderFlight
        {
            internal int Side;
            internal Vector3 Start, End;
            internal float Amount, Age;
        }

        private void BuildShoulderMeshes(Transform parent)
        {
            for (int strip = 0; strip < StripCount; strip++)
            {
                var ridge = CreateSnowSurface("Continuous plowed shoulder", parent, RidgeRows, RidgeColumns,
                    out shoulderMeshes[strip], reverse: strip == 1);
                shoulderObjects[strip] = ridge;
                var ground = shoulderBase[strip] = new Vector3[RidgeRows * RidgeColumns];
                shoulderVertices[strip] = new Vector3[ground.Length];
                float sign = strip == 0 ? -1 : 1;
                for (int row = 0; row < RidgeRows; row++)
                for (int column = 0; column < RidgeColumns; column++)
                {
                    float across = RidgeInnerEdge + RidgeWidth * column / (RidgeColumns - 1);
                    Vector3 point = plowStart + plowDirection * (RidgeStart + row * RidgeStep) + plowSide * (across * sign);
                    ground[row * RidgeColumns + column] = ridge.transform.InverseTransformPoint(Ground(point, out _));
                }
            }
        }

        private GameObject CreateSnowSurface(string name, Transform parent, int rows, int columns, out Mesh mesh, bool reverse = false)
        {
            var surface = new GameObject(name);
            surface.transform.SetParent(parent, false);
            mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            mesh.vertices = new Vector3[rows * columns];
            var triangles = new int[(rows - 1) * (columns - 1) * 6];
            int cursor = 0;
            for (int row = 0; row < rows - 1; row++)
            for (int column = 0; column < columns - 1; column++)
            {
                int a = row * columns + column;
                int b = a + 1, c = a + columns, d = c + 1;
                triangles[cursor++] = a;
                triangles[cursor++] = reverse ? c : b;
                triangles[cursor++] = reverse ? b : c;
                triangles[cursor++] = b;
                triangles[cursor++] = reverse ? c : d;
                triangles[cursor++] = reverse ? d : c;
            }
            mesh.triangles = triangles;
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            surface.AddComponent<MeshRenderer>().sharedMaterial = snow;
            surface.SetActive(false);
            return surface;
        }

        private void ResetShoulder(bool open)
        {
            snowFlights.Clear();
            Array.Clear(ridgeSnow, 0, ridgeSnow.Length);
            Array.Clear(ridgeAcrossSum, 0, ridgeAcrossSum.Length);
            for (int strip = 0; strip < StripCount; strip++)
            {
                if (open)
                    for (int drift = 0; drift < DriftCount; drift++)
                    for (int cell = 0; cell < CellsPerDrift; cell++)
                        DepositShoulder(strip, FirstDrift + drift * DriftSpacing + (cell - 1.5f) * CellLength + .28f,
                            ShoulderOffset, SnowHeights[drift] / DeepestDrift);
                ShowShoulderRidge(strip);
            }
        }

        private void DepositShoulder(int strip, float along, float across, float amount)
        {
            float centre = Mathf.Clamp((along - RidgeStart) / RidgeStep, 5, RidgeRows - 6);
            int start = Mathf.Max(1, Mathf.FloorToInt(centre - 5));
            int end = Mathf.Min(RidgeRows - 2, Mathf.CeilToInt(centre + 5));
            float sum = 0;
            for (int row = start; row <= end; row++) sum += LandingWeight(row - centre);
            for (int row = start; row <= end; row++)
            {
                float deposited = amount * LandingWeight(row - centre) / sum;
                ridgeSnow[strip, row] += deposited;
                ridgeAcrossSum[strip, row] += across * deposited;
            }
        }

        private static float LandingWeight(float distance)
        {
            float weight = Mathf.Clamp01(1 - Mathf.Abs(distance) / 5f);
            return weight * weight * (3 - 2 * weight);
        }

        private static float RidgeProfile(float distance)
        {
            return distance < 0 ? Mathf.SmoothStep(0, 1, 1 + distance / .4f) :
                Mathf.Pow(Mathf.Clamp01(1 - distance / .86f), 1.45f);
        }

        private void ShowShoulderRidge(int strip)
        {
            bool visible = false;
            var vertices = shoulderVertices[strip];
            for (int row = 0; row < RidgeRows; row++)
            {
                float thickness = ridgeSnow[strip, row];
                visible |= thickness > .006f;
                float crest = thickness > .0001f ? ridgeAcrossSum[strip, row] / thickness : ShoulderOffset;
                if (thickness <= .0001f)
                {
                    int start = row * RidgeColumns;
                    for (int column = 0; column < RidgeColumns; column++)
                        vertices[start + column] = shoulderBase[strip][start + column] - Vector3.up * .012f;
                    continue;
                }
                for (int column = 0; column < RidgeColumns; column++)
                {
                    int index = row * RidgeColumns + column;
                    float across = RidgeInnerEdge + RidgeWidth * column / (RidgeColumns - 1);
                    float signedAcross = strip == 0 ? -across : across;
                    float along = RidgeStart + row * RidgeStep;
                    float baseHeight = UndisturbedSnowHeight(along, signedAcross) * SnowbankEdge(across);
                    float profile = RidgeProfile(across - crest);
                    float irregularity = 1 + .09f * Mathf.PerlinNoise(along * .9f, across * 2.8f + strip);
                    vertices[index] = shoulderBase[strip][index] + Vector3.up *
                        (baseHeight + Mathf.Min(.53f, thickness * 2.55f) * profile * irregularity - .017f);
                }
            }
            shoulderObjects[strip].SetActive(visible);
            if (visible) WriteSnowSurface(shoulderMeshes[strip], vertices);
        }

        private float ShoulderSnowAt(Vector3 point)
        {
            var delta = point - plowStart;
            float along = Vector3.Dot(delta, plowDirection);
            float across = Vector3.Dot(delta, plowSide);
            int drift = Mathf.Clamp(Mathf.RoundToInt((along - FirstDrift) / DriftSpacing), 0, DriftCount - 1);
            float distance = Mathf.Abs(across);
            if (along <= RidgeStart || along >= RidgeEnd || distance <= RidgeInnerEdge || distance >= RidgeInnerEdge + RidgeWidth ||
                Mathf.Abs(point.y - driftPoints[drift].y) > 1.4f) return 0;
            float sample = (along - RidgeStart) / RidgeStep;
            int row = Mathf.Min(RidgeRows - 2, Mathf.FloorToInt(sample));
            int strip = across < 0 ? 0 : 1;
            float amount = Mathf.Lerp(ridgeSnow[strip, row], ridgeSnow[strip, row + 1], sample - row);
            if (amount <= .0001f) return 0;
            float centre = Mathf.Lerp(ridgeAcrossSum[strip, row], ridgeAcrossSum[strip, row + 1], sample - row) / amount;
            return Mathf.Clamp01(amount * 3.7f) * RidgeProfile(distance - centre);
        }

        private void BuildSnowStreams()
        {
            Array.Clear(streamSpray, 0, streamSpray.Length);
            for (int strip = 0; strip < StripCount; strip++)
                streamObjects[strip] = CreateSnowSurface("Snow stream from blade", blade.transform,
                    StreamRows, StreamColumns, out streamMeshes[strip], reverse: strip == 0);
        }

        private void DisposeSnowStreams()
        {
            for (int strip = 0; strip < StripCount; strip++)
            {
                if (streamMeshes[strip] != null) UnityEngine.Object.Destroy(streamMeshes[strip]);
                streamMeshes[strip] = null;
            }
        }

        private void LaunchShoulderSnow(int strip, float amount)
        {
            float sign = strip == 0 ? -1 : 1;
            Vector3 tip = blade.transform.TransformPoint(new Vector3(sign * 1.42f, -.08f, .39f));
            float along = Vector3.Dot(tip - plowStart, plowDirection);
            float landing = Mathf.Clamp(along + .36f, RidgeStart + .64f, RidgeEnd - .64f);
            float across = Mathf.Abs(Vector3.Dot(tip - plowStart, plowSide));
            float shoulder = Mathf.Clamp(across + .48f, ShoulderOffset, RidgeInnerEdge + RidgeWidth - .74f);
            Vector3 end = Ground(plowStart + plowDirection * landing + plowSide * (sign * shoulder), out _) + Vector3.up * .045f;
            snowFlights.Add(new ShoulderFlight { Side = strip, Start = tip, End = end, Amount = amount });
        }

        private bool SnowStillMoving()
        {
            if (snowFlights.Count > 0) return true;
            foreach (float amount in snowInWave)
                if (amount > 0) return true;
            return false;
        }

        private void AdvanceShoulderFlow()
        {
            bool leftLanded = false, rightLanded = false;
            for (int index = snowFlights.Count - 1; index >= 0; index--)
            {
                var flight = snowFlights[index];
                flight.Age += Time.fixedDeltaTime;
                if (flight.Age < LandingTime)
                {
                    snowFlights[index] = flight;
                    continue;
                }

                Vector3 landing = flight.End - plowStart;
                DepositShoulder(flight.Side, Vector3.Dot(landing, plowDirection),
                    Mathf.Abs(Vector3.Dot(landing, plowSide)), flight.Amount);
                if (flight.Side == 0) leftLanded = true;
                else rightLanded = true;
                snowFlights.RemoveAt(index);
            }
            if (leftLanded) ShowShoulderRidge(0);
            if (rightLanded) ShowShoulderRidge(1);
            for (int strip = 0; strip < StripCount; strip++) ShowSnowStream(strip);
        }

        private void ShowSnowStream(int strip)
        {
            float amount = 0;
            ShoulderFlight newest = default;
            ShoulderFlight oldest = default;
            foreach (var flight in snowFlights)
            {
                if (flight.Side != strip) continue;
                if (amount == 0) oldest = flight;
                newest = flight;
                amount += flight.Amount;
            }

            var surface = streamObjects[strip];
            surface.SetActive(amount > .022f);
            if (!surface.activeSelf) return;
            var basis = surface.transform;
            float sign = strip == 0 ? -1 : 1;
            float thickness = Mathf.InverseLerp(.022f, .65f, amount);
            Vector3 tip = blade.transform.TransformPoint(new Vector3(sign * 1.43f, -.08f, .39f));
            Vector3 landing = Vector3.Lerp(newest.End, oldest.End, .57f);
            float length = Mathf.Clamp01(oldest.Age / LandingTime);
            float retract = Mathf.Clamp01(newest.Age / LandingTime - .18f);
            Vector3 streamStart = Vector3.Lerp(tip, newest.Start, retract);

            for (int row = 0; row < StreamRows; row++)
            {
                float t = Mathf.Lerp(retract, Mathf.Max(retract, length), row / (float)(StreamRows - 1));
                Vector3 centre = Vector3.Lerp(streamStart, landing, t);
                centre.y += Mathf.Sin(t * Mathf.PI) * .12f;
                float width = Mathf.Lerp(.19f, .67f, thickness) * Mathf.Lerp(.68f, 1.12f, t);
                for (int column = 0; column < StreamColumns; column++)
                {
                    float u = column / (float)(StreamColumns - 1) * 2 - 1;
                    float crown = (1 - u * u) * Mathf.Sin(t * Mathf.PI) * (.025f + thickness * .13f);
                    float ripple = .027f * Mathf.Sin(t * 29 - Time.time * 31 + u * 3) * (1 - Mathf.Abs(u));
                    streamVertices[row * StreamColumns + column] =
                        basis.InverseTransformPoint(centre + plowDirection * (u * width) + Vector3.up * (crown + ripple));
                }
            }
            WriteSnowSurface(streamMeshes[strip], streamVertices);
            EmitFallingSnow(strip, amount, tip, newest.End);
        }

        private void EmitFallingSnow(int strip, float amount, Vector3 tip, Vector3 landing)
        {
            streamSpray[strip] += amount * 4.6f;
            int count = Mathf.FloorToInt(streamSpray[strip]);
            streamSpray[strip] -= count;
            Vector3 velocity = (landing - tip) / LandingTime + Vector3.up * .92f;
            for (int i = 0; i < count; i++)
            {
                bool clump = i % 3 != 2;
                plowSnow.Emit(new ParticleSystem.EmitParams
                {
                    position = tip + plowDirection * UnityEngine.Random.Range(-.24f, .22f),
                    velocity = velocity + UnityEngine.Random.insideUnitSphere * (clump ? .52f : 1.65f),
                    startSize = clump ? UnityEngine.Random.Range(.085f, .18f) : .055f,
                    startLifetime = clump ? .29f : .42f,
                    rotation3D = UnityEngine.Random.rotation.eulerAngles
                }, 1);
            }
        }

        private static void WriteSnowSurface(Mesh mesh, Vector3[] vertices)
        {
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
