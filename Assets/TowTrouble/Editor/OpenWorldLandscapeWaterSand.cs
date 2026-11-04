using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private sealed class WaterSandCove
        {
            public Vector3 Center, Outward;
            public int Kind;
        }

        private sealed class WaterSandCrossing
        {
            public Vector3 Center, Forward;
            public float HalfLength, HalfWidth;
            public int Kind;
            public Transform Source;
            public Vector3 Side => Vector3.Cross(Vector3.up, Forward);
            public Vector3 Pullout => Center + Forward * (HalfLength + 10f) + Side * (HalfWidth + 11f);
        }

        private sealed class WaterSandBank
        {
            public Vector3 Center, Outward, Along;
            public float Level, Length, Width;
            public int Kind;
        }

        // Each ridge has a long windward slope and a shorter lee face. Fixed anchors
        // keep repeat authoring deterministic and leave the incident locations alone.
        private static readonly Vector4[] waterSandDunes = {
            new Vector4(282f, 108f, 51f, 5.4f), new Vector4(403f, 76f, 48f, 6.5f),
            new Vector4(341f, -23f, 58f, 6.8f), new Vector4(433f, -65f, 43f, 5.7f),
            new Vector4(278f, -159f, 54f, 5.8f), new Vector4(414f, -246f, 52f, 7f),
            new Vector4(322f, -302f, 59f, 6.3f), new Vector4(383f, -374f, 47f, 5.2f)
        };
        private static readonly Vector2[] waterSandWash = {
            new Vector2(388f, 128f), new Vector2(365f, 75f), new Vector2(390f, 17f),
            new Vector2(349f, -47f), new Vector2(357f, -119f), new Vector2(329f, -181f),
            new Vector2(360f, -244f), new Vector2(312f, -319f), new Vector2(300f, -395f)
        };
        private static readonly Vector2[] waterSandWashCurve = WaterSandSmoothWash();
        private static readonly Vector2 waterSandWind = new Vector2(.47f, -.883f);
        private static readonly Vector2 waterSandRidge = new Vector2(.883f, .47f);
        private static readonly List<WaterSandCove> waterSandCoves = new List<WaterSandCove>();
        private static readonly List<WaterSandCrossing> waterSandCrossings = new List<WaterSandCrossing>();
        private static readonly List<WaterSandCrossing> waterSandAllCrossings = new List<WaterSandCrossing>();
        private static readonly List<WaterSandBank> waterSandBanks = new List<WaterSandBank>();
        private static Vector3[] waterSandLakePolygon = Array.Empty<Vector3>();
        private static float waterSandLakeLevel = 10.4f;
        private static int waterSandProps;
        private const int WaterSandPropBudget = 460;

        private static void ShapeLandscapeWaterAndSand(float[,] heights, float[,] original)
        {
            CacheWaterSandCoves(original);
            CacheWaterSandCrossings();
            CacheWaterSandBanks(original);
            var size = terrain.terrainData.size;
            var origin = terrain.transform.position;
            int rows = heights.GetLength(0), columns = heights.GetLength(1);
            WaterSandRange(new Vector2(315f, -133f), new Vector2(145f, 285f), columns, rows,
                out int x0, out int x1, out int z0, out int z1);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    var p = origin + new Vector3(x / (float)(columns - 1) * size.x,
                        original[z, x] * size.y, z / (float)(rows - 1) * size.z);
                    float protection = CompositionTerrainWeight(p) * WaterSandDesertWeight(p);
                    if (protection < .001f || p.y < 13f) continue;
                    float wash = WaterSandWashDistance(p, out _, out _);
                    float dune = WaterSandDuneHeight(p) * Smooth01(7f, 21f, wash);
                    float cut = 1.6f * (1f - Smooth01(1.2f, 7.5f, wash));
                    heights[z, x] = Mathf.Clamp01(heights[z, x] +
                        Mathf.Clamp(dune - cut, -1.6f, 7f) * protection / size.y);
                }

            // Only lower existing shallow shores covered by the existing lake mesh.
            // This creates small connected inlets, not isolated circular water holes.
            foreach (var cove in waterSandCoves)
            {
                WaterSandRange(new Vector2(cove.Center.x, cove.Center.z), Vector2.one * 30f,
                    columns, rows, out x0, out x1, out z0, out z1);
                var side = Vector3.Cross(Vector3.up, cove.Outward);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        var p = origin + new Vector3(x / (float)(columns - 1) * size.x,
                            original[z, x] * size.y, z / (float)(rows - 1) * size.z);
                        if (!InsideNaturalLake(p, waterSandLakePolygon)) continue;
                        var d = Flat(p - cove.Center);
                        float u = Vector3.Dot(d, side) / (cove.Kind == 0 ? 27f : 22f);
                        float v = Vector3.Dot(d, cove.Outward) / (cove.Kind == 0 ? 16f : 12f) -
                            .24f * Mathf.Sin(u * 3.4f + cove.Kind);
                        float mask = 1f - Smooth01(.08f, 1f, u * u + v * v);
                        float level = Smooth01(waterSandLakeLevel - 1f, waterSandLakeLevel - .35f, p.y) *
                            (1f - Smooth01(waterSandLakeLevel + 1.1f, waterSandLakeLevel + 2.4f, p.y));
                        float cut = Mathf.Min(1.05f * mask * level, Mathf.Max(0f, p.y - waterSandLakeLevel + .65f));
                        heights[z, x] -= cut * CompositionTerrainWeight(p) / size.y;
                    }
            }

            // Read the real river surface, then shape only its dry shoulders. Existing
            // submerged samples, bridge ramps, graded roads and recovery pads are fixed.
            foreach (var bank in waterSandBanks)
            {
                WaterSandRange(new Vector2(bank.Center.x, bank.Center.z), Vector2.one * (bank.Length + bank.Width),
                    columns, rows, out x0, out x1, out z0, out z1);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        var p = origin + new Vector3(x / (float)(columns - 1) * size.x,
                            original[z, x] * size.y, z / (float)(rows - 1) * size.z);
                        float mask = WaterSandBankMask(bank, p, out float u, out float v);
                        if (mask < .001f || p.y <= bank.Level + .15f || p.y > bank.Level + 6f) continue;
                        mask *= WaterSandBankProtection(p) * Smooth01(bank.Level + .15f, bank.Level + .8f, p.y);
                        if (mask < .001f) continue;
                        float crest = Mathf.Exp(-Mathf.Pow((v - .35f) * 2.4f, 2f));
                        float hollow = Mathf.Exp(-Mathf.Pow((v + .17f) * 3f, 2f));
                        float offset = (bank.Kind == 0 ? .42f * crest - .72f * hollow :
                            bank.Kind == 1 ? 1.15f * crest - .22f * hollow : .8f * crest - .5f * hollow);
                        offset *= mask * (.82f + .18f * Mathf.Sin(u * 4.4f + bank.Kind));
                        float value = Mathf.Max(bank.Level + .15f, p.y + offset);
                        heights[z, x] += (value - p.y) / size.y;
                    }
            }
        }

        private static void PaintLandscapeWaterAndSand(float[,,] paint)
        {
            int rows = paint.GetLength(0), columns = paint.GetLength(1);
            if (paint.GetLength(2) < 8) throw new InvalidOperationException("Landscape composition requires the county terrain layers.");
            WaterSandRange(new Vector2(315f, -133f), new Vector2(145f, 285f), columns, rows,
                out int x0, out int x1, out int z0, out int z1);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    var p = WaterSandPaintPoint(x, z, columns, rows);
                    float weight = CompositionTerrainWeight(p) * WaterSandDesertWeight(p) *
                        Smooth01(.15f, .7f, paint[z, x, 5]);
                    if (weight < .001f) continue;
                    float wash = WaterSandWashDistance(p, out _, out _);
                    float grain = Mathf.PerlinNoise(p.x * .061f + 8f, p.z * .061f + 9f);
                    float dune = WaterSandDuneHeight(p) / 7f;
                    BlendLayer(paint, z, x, 2, dune * (.14f + grain * .13f) * weight);
                    float channel = (1f - Smooth01(2f, 10f + grain * 3f, wash)) * weight;
                    BlendLayer(paint, z, x, 1, channel * .48f);
                    BlendLayer(paint, z, x, 3, channel * (.10f + grain * .12f));
                }

            foreach (var cove in waterSandCoves)
            {
                WaterSandRange(new Vector2(cove.Center.x, cove.Center.z), Vector2.one * 34f,
                    columns, rows, out x0, out x1, out z0, out z1);
                var side = Vector3.Cross(Vector3.up, cove.Outward);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        var p = WaterSandPaintPoint(x, z, columns, rows);
                        var d = Flat(p - cove.Center);
                        float u = Vector3.Dot(d, side) / 31f, v = Vector3.Dot(d, cove.Outward) / 20f;
                        float grain = Mathf.PerlinNoise(p.x * .085f + 21f, p.z * .085f + 4f);
                        v -= .18f * Mathf.Sin(u * 4f + cove.Kind);
                        float mask = (1f - Smooth01(.12f, 1f, u * u + v * v)) * CompositionTerrainWeight(p);
                        if (mask < .001f || p.y < waterSandLakeLevel - .5f) continue;
                        float wet = 1f - Smooth01(waterSandLakeLevel + .2f, waterSandLakeLevel + 1.5f, p.y);
                        BlendLayer(paint, z, x, cove.Kind == 1 ? 3 : 1, wet * mask * (.23f + .23f * grain));
                        float bank = Smooth01(waterSandLakeLevel + .7f, waterSandLakeLevel + 2f, p.y) *
                            (1f - Smooth01(waterSandLakeLevel + 3f, waterSandLakeLevel + 5f, p.y));
                        BlendLayer(paint, z, x, 0, bank * mask * (.28f + grain * .28f));
                    }
            }

            foreach (var bank in waterSandBanks)
            {
                WaterSandRange(new Vector2(bank.Center.x, bank.Center.z), Vector2.one * (bank.Length + bank.Width),
                    columns, rows, out x0, out x1, out z0, out z1);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        var p = WaterSandPaintPoint(x, z, columns, rows);
                        if (p.y <= bank.Level + .15f || p.y > bank.Level + 6f) continue;
                        float mask = WaterSandBankMask(bank, p, out _, out _) * WaterSandBankProtection(p);
                        if (mask < .001f) continue;
                        float grain = Mathf.PerlinNoise(p.x * .11f + 42f, p.z * .11f + 16f);
                        float wet = 1f - Smooth01(bank.Level + .4f, bank.Level + 2.1f, p.y);
                        BlendLayer(paint, z, x, bank.Kind == 2 ? 3 : 2, mask * (.18f + grain * .3f));
                        BlendLayer(paint, z, x, 1, wet * mask * (.14f + grain * .17f));
                        BlendLayer(paint, z, x, 0, (1f - wet) * mask * (.12f + grain * .24f));
                    }
            }

            // A compact worn shoulder beside selected crossings, painted into terrain.
            foreach (var crossing in waterSandCrossings)
            {
                var center = Ground(crossing.Pullout);
                WaterSandRange(new Vector2(center.x, center.z), Vector2.one * 11f,
                    columns, rows, out x0, out x1, out z0, out z1);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        var p = WaterSandPaintPoint(x, z, columns, rows);
                        var d = Flat(p - center);
                        float u = Vector3.Dot(d, crossing.Forward) / 9f;
                        float v = Vector3.Dot(d, crossing.Side) / 6f;
                        float grain = Mathf.PerlinNoise(p.x * .18f, p.z * .18f);
                        float mask = 1f - Smooth01(.1f, .85f + grain * .2f, u * u + v * v);
                        BlendLayer(paint, z, x, 1, mask * .46f * CompositionTerrainWeight(p));
                    }
            }
            PaintWaterSandRampShoulders(paint);
        }

        private static void PaintWaterSandRampShoulders(float[,,] paint)
        {
            int rows = paint.GetLength(0), columns = paint.GetLength(1);
            foreach (var crossing in waterSandAllCrossings)
                foreach (var ramp in crossing.Source.GetComponentsInChildren<BoxCollider>().Where(c => c.name == "Bridge entry ramp"))
                {
                    var center = ramp.transform.TransformPoint(ramp.center);
                    var forward = Flat(ramp.transform.forward).normalized;
                    var right = Vector3.Cross(Vector3.up, forward);
                    float halfWidth = ramp.transform.TransformVector(Vector3.right * ramp.size.x).magnitude * .5f;
                    float halfLength = ramp.transform.TransformVector(Vector3.forward * ramp.size.z).magnitude * .5f;
                    float extent = halfWidth + halfLength + 4f;
                    WaterSandRange(new Vector2(center.x, center.z), Vector2.one * extent,
                        columns, rows, out int x0, out int x1, out int z0, out int z1);
                    for (int z = z0; z <= z1; z++)
                        for (int x = x0; x <= x1; x++)
                        {
                            var p = WaterSandPaintPoint(x, z, columns, rows);
                            var d = Flat(p - center);
                            float side = Mathf.Abs(Vector3.Dot(d, right)) - halfWidth;
                            float along = Mathf.Abs(Vector3.Dot(d, forward));
                            if (side < .08f || side > 3.6f || along > halfLength + 1.5f ||
                                !PolishClear(new Vector3(p.x, Mathf.Max(11f, p.y), p.z), 1f)) continue;
                            var local = ramp.transform.InverseTransformPoint(p) - ramp.center;
                            // Gravel follows grounded shoulders, never the bank under a
                            // suspended deck and never the middle of the graded road.
                            float separation = Mathf.Abs(local.y - ramp.size.y * .5f) * ramp.transform.lossyScale.y;
                            if (separation > 1.3f) continue;
                            float grain = Mathf.PerlinNoise(p.x * .37f + 19f, p.z * .37f + 26f);
                            float mask = (1f - Smooth01(.2f, 2f + grain * 1.5f, side)) *
                                (1f - Smooth01(halfLength - .4f, halfLength + 1.5f, along));
                            BlendLayer(paint, z, x, 1, mask * .32f);
                            BlendLayer(paint, z, x, 3, mask * (.12f + grain * .17f));
                        }
                }
        }

        private static void ComposeLandscapeWaterAndSand(Transform root)
        {
            waterSandProps = 0;
            var random = new System.Random(812921);
            var group = PolishGroup(root, "Lake coves desert dunes and crossing banks");
            foreach (var cove in waterSandCoves) DressWaterSandCove(cove, group, random);
            DressWaterSandDesert(group, random);
            foreach (var crossing in waterSandCrossings) DressWaterSandCrossing(crossing, group, random);
            foreach (var bank in waterSandBanks) DressWaterSandBank(bank, group, random);
            DressWaterSandRamps(group);
            Debug.Log("[TowTrouble] Water/sand composition: " + waterSandCoves.Count + " coves, " +
                waterSandBanks.Count + " irregular riverbanks, " + waterSandCrossings.Count + " crossing surrounds, " +
                waterSandProps + "/" + WaterSandPropBudget + " native props.");
        }

        private static void CacheWaterSandCoves(float[,] baseline)
        {
            waterSandCoves.Clear();
            waterSandLakePolygon = Array.Empty<Vector3>();
            var lakeFilter = environment.GetComponentsInChildren<MeshFilter>()
                .FirstOrDefault(f => f.name.StartsWith("Lake -", StringComparison.Ordinal) && f.sharedMesh != null);
            if (lakeFilter == null) return;
            waterSandLakePolygon = lakeFilter.sharedMesh.vertices.Select(lakeFilter.transform.TransformPoint).ToArray();
            if (waterSandLakePolygon.Length < 3) return;
            waterSandLakeLevel = waterSandLakePolygon[0].y;
            Vector3[] references = { new Vector3(-337f, 0f, -171f), new Vector3(-208f, 0f, -267f), new Vector3(-359f, 0f, -274f) };
            for (int kind = 0; kind < references.Length; kind++)
            {
                Vector3 best = default, outward = default;
                float bestScore = float.MaxValue;
                for (int z = -36; z <= 36; z += 2)
                    for (int x = -36; x <= 36; x += 2)
                    {
                        var p = references[kind] + new Vector3(x, 0f, z);
                        p.y = WaterSandBaselineHeight(baseline, p);
                        if (p.y < waterSandLakeLevel + .05f || p.y > waterSandLakeLevel + .9f ||
                            CompositionTerrainWeight(p) < .35f || !InsideNaturalLake(p, waterSandLakePolygon)) continue;
                        var slope = new Vector3(
                            WaterSandBaselineHeight(baseline, p + Vector3.right * 2f) - WaterSandBaselineHeight(baseline, p - Vector3.right * 2f), 0f,
                            WaterSandBaselineHeight(baseline, p + Vector3.forward * 2f) - WaterSandBaselineHeight(baseline, p - Vector3.forward * 2f));
                        if (slope.sqrMagnitude < .002f) continue;
                        slope.Normalize();
                        if (WaterSandBaselineHeight(baseline, p - slope * 9f) > waterSandLakeLevel - .15f ||
                            !InsideNaturalLake(p - slope * 9f, waterSandLakePolygon)) continue;
                        float score = x * x + z * z + Mathf.Abs(p.y - waterSandLakeLevel - .35f) * 30f;
                        if (score >= bestScore) continue;
                        best = p; outward = slope; bestScore = score;
                    }
                if (bestScore < float.MaxValue) waterSandCoves.Add(new WaterSandCove { Center = best, Outward = outward, Kind = kind });
            }
        }

        private static void CacheWaterSandCrossings()
        {
            waterSandCrossings.Clear();
            waterSandAllCrossings.Clear();
            var decks = environment.GetComponentsInChildren<BoxCollider>()
                .Where(c => c.name == "Bridge drive surface").OrderBy(c => c.transform.position.z).ToArray();
            if (decks.Length == 0) return;
            foreach (var deck in decks)
                waterSandAllCrossings.Add(new WaterSandCrossing {
                    Center = deck.transform.TransformPoint(deck.center), Forward = Flat(deck.transform.forward).normalized,
                    HalfLength = deck.transform.TransformVector(Vector3.forward * deck.size.z).magnitude * .5f,
                    HalfWidth = deck.transform.TransformVector(Vector3.right * deck.size.x).magnitude * .5f,
                    Source = deck.transform.parent
                });
            var selected = new HashSet<int>();
            for (int kind = 0; kind < Mathf.Min(4, decks.Length); kind++)
            {
                int index = Mathf.RoundToInt(kind * (decks.Length - 1f) / Mathf.Max(1f, Mathf.Min(4, decks.Length) - 1f));
                if (!selected.Add(index)) continue;
                var crossing = waterSandAllCrossings[index];
                crossing.Kind = kind;
                waterSandCrossings.Add(crossing);
            }
        }

        private static void CacheWaterSandBanks(float[,] baseline)
        {
            waterSandBanks.Clear();
            var filter = environment.GetComponentsInChildren<MeshFilter>()
                .FirstOrDefault(f => f.name == "River - continuous channel and cascade" && f.sharedMesh != null);
            if (filter == null) return;
            // This authored ribbon stores paired left/right vertices. Only sample rows
            // referenced by its real surface, excluding waterfall gaps and lake overlap.
            var vertices = filter.sharedMesh.vertices;
            var cascade = environment.GetComponentsInChildren<MeshFilter>()
                .FirstOrDefault(f => f.name == "River - headwater cascade" && f.sharedMesh != null);
            // Lake clipping appends unpaired triangle vertices to the flat mesh. The
            // cascade retains the original ribbon prefix before that clipping step.
            int ribbonCount = Mathf.Min(vertices.Length, cascade != null ? cascade.sharedMesh.vertexCount : RiverPlan().Count * 2);
            ribbonCount -= ribbonCount % 2;
            if (ribbonCount < 6) return;
            var used = new HashSet<int>(filter.sharedMesh.triangles);
            Vector3 previous = filter.transform.TransformPoint((vertices[0] + vertices[1]) * .5f);
            float since = 40f;
            int ordinal = 0;
            for (int i = 2; i < ribbonCount - 2; i += 2)
            {
                var left = filter.transform.TransformPoint(vertices[i]);
                var right = filter.transform.TransformPoint(vertices[i + 1]);
                var center = (left + right) * .5f;
                since += Flat(center - previous).magnitude;
                previous = center;
                if (since < 39f || !used.Contains(i) || !used.Contains(i + 1) || center.z > 175f ||
                    InsideNaturalLake(center, waterSandLakePolygon)) continue;
                since = 0f;
                var along = Flat(filter.transform.TransformPoint((vertices[i + 2] + vertices[i + 3]) * .5f) - center).normalized;
                if (along.sqrMagnitude < .5f) continue;
                float halfWidth = Flat(right - left).magnitude * .5f;
                // One bank is composed at each station; the opposite bank stays open.
                var outward = Flat(right - left).normalized * (ordinal % 2 == 0 ? 1f : -1f);
                float bestScore = float.MaxValue;
                Vector3 best = default;
                for (float d = Mathf.Max(3f, halfWidth * .65f); d <= halfWidth + 13f; d += 1f)
                {
                    var p = center + outward * d;
                    p.y = WaterSandBaselineHeight(baseline, p);
                    if (p.y < center.y + .3f || p.y > center.y + 3f || WaterSandBankProtection(p) < .7f) continue;
                    float score = Mathf.Abs(p.y - center.y - 1f) + Mathf.Abs(d - halfWidth - 2f) * .05f;
                    if (score >= bestScore) continue;
                    best = p; bestScore = score;
                }
                if (bestScore < float.MaxValue)
                    waterSandBanks.Add(new WaterSandBank { Center = best, Outward = outward, Along = along,
                        Level = center.y, Length = 21f + ordinal % 3 * 3f, Width = 9f + ordinal % 2 * 2f, Kind = ordinal % 3 });
                ordinal++;
            }
        }

        private static float WaterSandBankMask(WaterSandBank bank, Vector3 p, out float along, out float across)
        {
            var d = Flat(p - bank.Center);
            along = Vector3.Dot(d, bank.Along) / bank.Length;
            across = Vector3.Dot(d, bank.Outward) / bank.Width - .18f * Mathf.Sin(along * 3.7f + bank.Kind);
            return 1f - Smooth01(.08f, 1f, along * along + across * across);
        }

        private static float WaterSandBankProtection(Vector3 p)
        {
            // Unlike the broad composition mask, this excludes only the river's wet
            // surface, not a 30 m band of dry land around it. Keep all play-space margins.
            float clearance = Flat(p - compositionGarage).magnitude - 62f;
            clearance = Mathf.Min(clearance, Flat(p - AlpineShelter).magnitude - 19f,
                Flat(p - MeadowShelter).magnitude - 19f);
            foreach (var site in polishMap.Sites)
            {
                var d = Flat(p - site.Position);
                clearance = Mathf.Min(clearance, d.magnitude - (site.Id == "R01" ? 39f : 23f));
                float exit = Vector3.Dot(d, site.ExitDirection);
                if (exit >= -7f && exit <= 48f)
                    clearance = Mathf.Min(clearance, Mathf.Abs(Vector3.Dot(d, Vector3.Cross(Vector3.up, site.ExitDirection))) - 10f);
            }
            foreach (var bay in polishMap.Deliveries) clearance = Mathf.Min(clearance, Flat(p - bay.Position).magnitude - 25f);
            foreach (var bridge in waterSandAllCrossings)
            {
                var d = Flat(p - bridge.Center);
                float beyond = Mathf.Abs(Vector3.Dot(d, bridge.Forward)) - bridge.HalfLength - 11f;
                float lateral = Mathf.Abs(Vector3.Dot(d, bridge.Side)) - bridge.HalfWidth - 5f;
                clearance = Mathf.Min(clearance, Mathf.Max(beyond, lateral));
            }
            var key = new Vector2Int(Mathf.FloorToInt(p.x / 16f), Mathf.FloorToInt(p.z / 16f));
            for (int z = -2; z <= 2; z++)
                for (int x = -2; x <= 2; x++)
                    if (polishRoads.TryGetValue(key + new Vector2Int(x, z), out var cell))
                        foreach (var q in cell) clearance = Mathf.Min(clearance, Flat(q - p).magnitude - 10f);
            return Smooth01(0f, 7f, clearance);
        }

        private static void DressWaterSandCove(WaterSandCove cove, Transform root, System.Random random)
        {
            var group = PolishGroup(root, new[] { "Reed inlet", "Pebble cove", "Driftwood bank" }[cove.Kind]);
            var side = Vector3.Cross(Vector3.up, cove.Outward);
            // Three unequal pockets leave visible bare sand and water between them.
            // Every pocket has a dense centre and a few isolated fringe stems.
            Vector2[] pockets = { new Vector2(-17f, 2f), new Vector2(-7f, 5f), new Vector2(18f, 1.5f) };
            for (int i = 0; i < 42; i++)
            {
                int pocket = i < 21 ? 0 : i < 34 ? 1 : 2;
                float spread = pocket == 0 ? 6.5f : pocket == 1 ? 3.8f : 3f;
                float along = pockets[pocket].x + WaterSandPick(random, -spread, spread);
                float bank = pockets[pocket].y + WaterSandPick(random, -1.8f, 2.8f) + .8f * Mathf.Sin(along * .19f + cove.Kind);
                var p = Ground(cove.Center + side * along + cove.Outward * bank);
                if (p.y < waterSandLakeLevel - .25f || p.y > waterSandLakeLevel + 1.9f) continue;
                string reed = i % 7 == 0 ? "P_Marshtail01" : i % 4 == 0 ? "P_Marshtail04" : "P_Marshtail03";
                WaterSandProp(reed, p, WaterSandPick(random, .72f, pocket == 0 ? 1.85f : 1.3f),
                    WaterSandPick(random, 0f, 360f), group, false, 185f, .08f);
            }
            for (int i = 0; i < 10; i++)
            {
                var p = Ground(cove.Center + side * WaterSandPick(random, -26f, 26f) +
                    cove.Outward * WaterSandPick(random, 7f, 15f));
                if (p.y < waterSandLakeLevel + .4f) continue;
                WaterSandProp(i % 3 == 0 ? "P_Bush1_Light" : "P_Foxtails1", p,
                    WaterSandPick(random, .65f, 1.3f), WaterSandPick(random, 0f, 360f), group, false, 195f, .05f);
            }
            for (int i = 0; i < (cove.Kind == 1 ? 8 : 4); i++)
            {
                float angle = WaterSandPick(random, 0f, Mathf.PI * 2f);
                float radius = i == 0 ? 0f : WaterSandPick(random, 1.5f, 5.8f);
                var p = Ground(cove.Center + side * (21f + Mathf.Cos(angle) * radius) +
                    cove.Outward * (8f + Mathf.Sin(angle) * radius));
                if (p.y < waterSandLakeLevel + .2f) continue;
                WaterSandProp(i == 0 ? "P_RockClassic8" : i % 2 == 0 ? "P_RockClassic3" : "P_RockClassic5", p,
                    i == 0 ? 2.3f : WaterSandPick(random, .35f, 1f), WaterSandPick(random, 0f, 360f),
                    group, i == 0, 310f, i == 0 ? .6f : .2f);
            }
            if (cove.Kind != 1)
                WaterSandProp("P_FirFallen1", Ground(cove.Center + side * 14f + cove.Outward * 11f),
                    .65f, Quaternion.LookRotation(side).eulerAngles.y + 13f, group, false, 265f, .1f);
        }

        private static void DressWaterSandDesert(Transform root, System.Random random)
        {
            var group = PolishGroup(root, "Wind-shaped dune toes and dry wash");
            for (int i = 1; i < waterSandWash.Length - 1; i++)
            {
                var center = new Vector3(waterSandWash[i].x, 0f, waterSandWash[i].y);
                WaterSandWashDistance(center, out var nearest, out var direction);
                center = new Vector3(nearest.x, 0f, nearest.y);
                var side = new Vector3(-direction.y, 0f, direction.x);
                var pocket = PolishGroup(group, "Dry wash bank " + i);
                for (int k = 0; k < 11; k++)
                {
                    float sign = k < 7 ? 1f : -1f;
                    var p = Ground(center + side * sign * WaterSandPick(random, 8f, 17f) +
                        new Vector3(direction.x, 0f, direction.y) * WaterSandPick(random, -12f, 12f));
                    string prefab = k < 3 ? "P_RockDesert_Clump09" : k < 5 ? "P_CactusPricklyPear02" :
                        k % 2 == 0 ? "P_GrassDesert1_Paintable" : "P_DesertBush02";
                    float height = k < 3 ? WaterSandPick(random, .55f, 1.3f) : k < 5 ? WaterSandPick(random, 1.4f, 2.4f) : WaterSandPick(random, .5f, 1.1f);
                    WaterSandProp(prefab, p, height, WaterSandPick(random, 0f, 360f), pocket, k == 0, k < 5 ? 285f : 170f, k < 3 ? .18f : .07f);
                }
            }
            for (int i = 0; i < waterSandDunes.Length; i++)
            {
                var dune = waterSandDunes[i];
                var anchor = new Vector3(dune.x + waterSandWind.x * 19f, 0f, dune.y + waterSandWind.y * 19f);
                for (int k = 0; k < 6; k++)
                {
                    var p = Ground(anchor + new Vector3(WaterSandPick(random, -12f, 12f), 0f, WaterSandPick(random, -9f, 9f)));
                    WaterSandProp(k < 2 ? "P_Cactus05" : k == 2 ? "P_DesertTwigRoots02" : "P_DesertBush01", p,
                        k < 2 ? WaterSandPick(random, 2.6f, 4.2f) : WaterSandPick(random, .55f, 1.25f),
                        WaterSandPick(random, 0f, 360f), group, k < 2, k < 2 ? 340f : 170f, .1f);
                }
            }
        }

        private static void DressWaterSandCrossing(WaterSandCrossing crossing, Transform root, System.Random random)
        {
            string[] names = { "Reed-bank crossing", "Logging crossing", "Stone-bank crossing", "Gorge approach crossing" };
            var group = PolishGroup(root, names[crossing.Kind]);
            for (int end = -1; end <= 1; end += 2)
                for (int i = 0; i < (end < 0 ? 4 : 2); i++)
                {
                    float angle = WaterSandPick(random, 0f, Mathf.PI * 2f);
                    float radius = i == 0 ? 0f : WaterSandPick(random, 1.8f, 4.7f);
                    float side = end < 0 ? 1f : -1f;
                    var p = Ground(crossing.Center + crossing.Forward * (end * (crossing.HalfLength + 9f) + Mathf.Cos(angle) * radius) +
                        crossing.Side * side * (crossing.HalfWidth + 10f + Mathf.Sin(angle) * radius));
                    float height = i == 0 ? (crossing.Kind < 2 ? 1f : 2.5f) : WaterSandPick(random, .35f, .9f);
                    float embed = height * (i == 0 && crossing.Kind < 2 ? .35f : .24f);
                    WaterSandProp(i == 0 ? "P_RockClassic8" : i % 2 == 0 ? "P_RockClassic3" : "P_RockClassic5", p,
                        height, WaterSandPick(random, 0f, 360f), group, i == 0, 320f, embed);
                }
            for (int i = 0; i < 12; i++)
            {
                var p = Ground(crossing.Center + crossing.Forward * (crossing.HalfLength + WaterSandPick(random, 4f, 19f)) +
                    crossing.Side * (crossing.HalfWidth + WaterSandPick(random, 8f, 18f)));
                string name = crossing.Kind == 0 ? "P_Marshtail03" : crossing.Kind == 1 ? "P_Fern2" :
                    i % 3 == 0 ? "P_BirchSapling1" : "P_Bush1_Light";
                WaterSandProp(name, p, name.Contains("Sapling") ? 3.4f : WaterSandPick(random, .75f, 1.45f),
                    WaterSandPick(random, 0f, 360f), group, false, name.Contains("Sapling") ? 290f : 180f, .07f);
            }
            if (crossing.Kind == 1)
                for (int i = 0; i < 3; i++)
                    WaterSandProp("P_FirFallen1", Ground(crossing.Pullout + crossing.Side * (5f + i * .8f)),
                        .55f + i * .08f, Quaternion.LookRotation(crossing.Forward).eulerAngles.y + 4f,
                        group, false, 280f, .08f);
            else if (crossing.Kind == 0)
                WaterSandProp("P_OakFallen1", Ground(crossing.Pullout + crossing.Side * 6f), .8f,
                    Quaternion.LookRotation(crossing.Forward).eulerAngles.y - 18f, group, false, 280f, .12f);
        }

        private static void DressWaterSandBank(WaterSandBank bank, Transform root, System.Random random)
        {
            var group = PolishGroup(root, new[] { "Low sandy river shelf", "Rooted river shoulder", "Broken gravel riverbank" }[bank.Kind]);
            for (int i = 0; i < 13; i++)
            {
                bool primary = i < 9;
                float along = (primary ? -6f : 11f) + WaterSandPick(random, primary ? -5f : -2.5f, primary ? 5f : 2.5f);
                float outwards = (primary ? 1.5f : 6f) + WaterSandPick(random, -1.8f, 3f);
                var p = Ground(bank.Center + bank.Along * along + bank.Outward * outwards);
                if (p.y < bank.Level + .1f || p.y > bank.Level + 4f) continue;
                bool reed = p.y < bank.Level + 1.5f;
                string prefab = reed ? (i % 3 == 0 ? "P_Marshtail04" : "P_Marshtail02") :
                    i % 4 == 0 ? "P_Bush1_Light" : "P_Foxtails1";
                WaterSandProp(prefab, p, WaterSandPick(random, .65f, reed ? 1.55f : 1.1f),
                    WaterSandPick(random, 0f, 360f), group, false, 185f, .08f);
            }
            if (bank.Kind == 2)
                for (int i = 0; i < 5; i++)
                {
                    var p = Ground(bank.Center + bank.Along * (10f + WaterSandPick(random, -4f, 4f)) +
                        bank.Outward * WaterSandPick(random, 2f, 7f));
                    if (p.y < bank.Level + .25f) continue;
                    WaterSandProp(i == 0 ? "P_RockClassic8" : "P_RockClassic3", p,
                        i == 0 ? 1.6f : WaterSandPick(random, .35f, .75f), WaterSandPick(random, 0f, 360f),
                        group, i == 0, 280f, i == 0 ? .42f : .14f);
                }
        }

        private static void DressWaterSandRamps(Transform root)
        {
            const string textureRoot = "Assets/Suntail Village/Textures/Building Modules/";
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + "Wood_2_AlbedoSmoothness.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + "Wood_2_Normal.png");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (albedo == null || normal == null || shader == null)
                throw new InvalidOperationException("Bridge approach dressing requires the imported Suntail timber textures and URP Lit.");
            Color[] tones = { new Color(.50f, .40f, .29f), new Color(.44f, .35f, .25f), new Color(.56f, .46f, .34f) };
            var materials = new Material[tones.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                var material = new Material(shader) { name = "Weathered approach boards " + i, enableInstancing = true };
                material.SetTexture("_BaseMap", albedo); material.SetColor("_BaseColor", tones[i]);
                material.SetTexture("_BumpMap", normal); material.SetFloat("_BumpScale", .7f);
                material.EnableKeyword("_NORMALMAP"); material.SetFloat("_Smoothness", .12f);
                materials[i] = VisualAsset(material, "LandscapeComposition/" + material.name + ".mat");
            }
            var group = PolishGroup(root, "Bridge approach timber surfaces");
            int index = 0;
            foreach (var crossing in waterSandAllCrossings)
                foreach (var ramp in crossing.Source.GetComponentsInChildren<BoxCollider>().Where(c => c.name == "Bridge entry ramp"))
                {
                    float width = ramp.transform.TransformVector(Vector3.right * ramp.size.x).magnitude;
                    float height = ramp.transform.TransformVector(Vector3.up * ramp.size.y).magnitude;
                    float length = ramp.transform.TransformVector(Vector3.forward * ramp.size.z).magnitude;
                    int boards = Mathf.Max(3, Mathf.CeilToInt(length / .44f));
                    float step = length / boards;
                    var vertices = new List<Vector3>(); var uv = new List<Vector2>();
                    var triangles = new[] { new List<int>(), new List<int>(), new List<int>() };
                    for (int board = 0; board < boards; board++)
                    {
                        // A thin visible skin only: original continuous box stays the
                        // sole driving collider. Boards and end joints do not bump tires.
                        float z = -length * .5f + (board + .5f) * step;
                        float trim = .012f + (board % 3) * .009f;
                        var min = new Vector3(-width * .5f + trim, height * .5f + .002f, z - step * .5f + .012f);
                        var max = new Vector3(width * .5f - trim, height * .5f + .024f, z + step * .5f - .012f);
                        AddWaterSandPlank(vertices, uv, triangles[(board * 7 + index) % 3], min, max, board * .217f);
                    }
                    var mesh = new Mesh { name = "Bridge approach planks " + index++ };
                    mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.subMeshCount = triangles.Length;
                    for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
                    mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
                    var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(group, false);
                    go.transform.SetPositionAndRotation(ramp.transform.TransformPoint(ramp.center), ramp.transform.rotation);
                    go.GetComponent<MeshFilter>().sharedMesh = VisualAsset(mesh, "LandscapeComposition/" + mesh.name + ".asset");
                    go.GetComponent<MeshRenderer>().sharedMaterials = materials;
                    go.isStatic = true; go.layer = ramp.gameObject.layer;
                    ConfigureDetailLod(go, 230f);
                }
        }

        private static void AddWaterSandPlank(List<Vector3> vertices, List<Vector2> uv, List<int> triangles,
            Vector3 min, Vector3 max, float offset)
        {
            // Separate vertices on the five visible faces retain crisp plank edges.
            Vector3[] corners = {
                new Vector3(min.x,max.y,min.z), new Vector3(min.x,max.y,max.z), new Vector3(max.x,max.y,max.z), new Vector3(max.x,max.y,min.z),
                new Vector3(min.x,min.y,min.z), new Vector3(min.x,min.y,max.z), new Vector3(max.x,min.y,max.z), new Vector3(max.x,min.y,min.z)
            };
            int[] faces = { 0,1,2,3, 4,0,3,7, 7,3,2,6, 6,2,1,5, 5,1,0,4 };
            for (int face = 0; face < faces.Length; face += 4)
            {
                int first = vertices.Count;
                for (int j = 0; j < 4; j++)
                {
                    var p = corners[faces[face + j]];
                    vertices.Add(p);
                    uv.Add(face == 0 ? new Vector2((p.z - min.z) * .18f + offset, p.x * .14f + offset) :
                        new Vector2((p.y - min.y) * .18f + offset,
                            (face == 4 || face == 12 ? p.x : p.z) * .14f + offset));
                }
                triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }
        }

        private static GameObject WaterSandProp(string prefab, Vector3 p, float height, float yaw,
            Transform parent, bool solid, float distance, float embed)
        {
            bool standingTree = MountainStandingTree(prefab);
            if (waterSandProps >= WaterSandPropBudget || !CompositionClear(p, Mathf.Max(.6f, height * .55f))) return null;
            var go = ScenicPrefab(prefab, p, height, yaw, parent, false);
            if (go == null) throw new InvalidOperationException("Missing landscape prefab " + prefab);
            foreach (var script in go.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
            go.transform.position -= Vector3.up * embed;
            var bounds = ScenicBounds(go);
            // Native fallen timber can be much wider than its height. Check the
            // complete visible footprint before adding collision or keeping it.
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            if (!CompositionClear(new Vector3(bounds.center.x, p.y, bounds.center.z), radius + .4f))
            { UnityEngine.Object.DestroyImmediate(go); return null; }
            if (standingTree)
            {
                AddMountainTrunk(go, p, height);
                var trunk = go.GetComponent<CapsuleCollider>();
                var scale = trunk.transform.lossyScale;
                float trunkRadius = trunk.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                var trunkCenter = trunk.transform.TransformPoint(trunk.center);
                if (!CompositionClear(trunkCenter, trunkRadius + 1f))
                { UnityEngine.Object.DestroyImmediate(go); return null; }
            }
            else if (solid)
            {
                var filter = go.GetComponentsInChildren<MeshFilter>().FirstOrDefault(f => f.sharedMesh != null);
                if (filter != null) filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = solid || standingTree ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }
            foreach (var lod in go.GetComponentsInChildren<LODGroup>())
                if (!lod.GetLODs().Any(l => l.renderers != null && l.renderers.Length > 0))
                    lod.SetLODs(new[] { new LOD(.01f, lod.GetComponentsInChildren<Renderer>()) });
            ConfigureDetailLod(go, distance);
            if (solid) go.isStatic = true;
            waterSandProps++;
            return go;
        }

        private static float WaterSandDuneHeight(Vector3 p)
        {
            float height = 0f;
            foreach (var dune in waterSandDunes)
            {
                var delta = new Vector2(p.x - dune.x, p.z - dune.y);
                float along = Vector2.Dot(delta, waterSandRidge) / dune.z;
                if (Mathf.Abs(along) >= 1f) continue;
                float across = Vector2.Dot(delta, waterSandWind) / 19f - .28f * along * along;
                if (Mathf.Abs(across) >= 1.7f) continue;
                float ends = 1f - Smooth01(.4f, 1f, Mathf.Abs(along));
                float shoulder = 1f - Smooth01(1f, 1.7f, Mathf.Abs(across));
                float profile = Mathf.Exp(-across * across * (across < 0f ? 1.45f : 4.1f));
                height = Mathf.Max(height, dune.w * ends * shoulder * profile);
            }
            return height;
        }

        private static float WaterSandWashDistance(Vector3 p, out Vector2 point, out Vector2 direction)
        {
            float best = float.MaxValue;
            point = default; direction = Vector2.up;
            var flat = new Vector2(p.x, p.z);
            for (int i = 1; i < waterSandWashCurve.Length; i++)
            {
                float d = Distance(flat, waterSandWashCurve[i - 1], waterSandWashCurve[i], out float t);
                if (d >= best) continue;
                best = d; point = Vector2.Lerp(waterSandWashCurve[i - 1], waterSandWashCurve[i], t);
                direction = (waterSandWashCurve[i] - waterSandWashCurve[i - 1]).normalized;
            }
            return best;
        }

        private static Vector2[] WaterSandSmoothWash()
        {
            var points = new List<Vector2>(waterSandWash);
            for (int pass = 0; pass < 2; pass++)
            {
                var smooth = new List<Vector2> { points[0] };
                for (int i = 1; i < points.Count; i++)
                {
                    smooth.Add(Vector2.Lerp(points[i - 1], points[i], .25f));
                    smooth.Add(Vector2.Lerp(points[i - 1], points[i], .75f));
                }
                smooth.Add(points[points.Count - 1]);
                points = smooth;
            }
            return points.ToArray();
        }

        private static float WaterSandDesertWeight(Vector3 p)
        {
            return Smooth01(170f, 211f, p.x) * (1f - Smooth01(426f, 459f, p.x)) *
                Smooth01(-420f, -392f, p.z) * (1f - Smooth01(110f, 150f, p.z));
        }

        private static Vector3 WaterSandPaintPoint(int x, int z, int columns, int rows)
        {
            var data = terrain.terrainData;
            float u = x / (float)(columns - 1), v = z / (float)(rows - 1);
            return terrain.transform.position + new Vector3(u * data.size.x, data.GetInterpolatedHeight(u, v), v * data.size.z);
        }

        private static float WaterSandBaselineHeight(float[,] heights, Vector3 p)
        {
            var local = p - terrain.transform.position;
            var size = terrain.terrainData.size;
            float x = Mathf.Clamp01(local.x / size.x) * (heights.GetLength(1) - 1);
            float z = Mathf.Clamp01(local.z / size.z) * (heights.GetLength(0) - 1);
            int x0 = Mathf.FloorToInt(x), z0 = Mathf.FloorToInt(z);
            int x1 = Mathf.Min(x0 + 1, heights.GetLength(1) - 1), z1 = Mathf.Min(z0 + 1, heights.GetLength(0) - 1);
            float a = Mathf.Lerp(heights[z0, x0], heights[z0, x1], x - x0);
            float b = Mathf.Lerp(heights[z1, x0], heights[z1, x1], x - x0);
            return Mathf.Lerp(a, b, z - z0) * size.y + terrain.transform.position.y;
        }

        private static void WaterSandRange(Vector2 center, Vector2 extent, int columns, int rows,
            out int x0, out int x1, out int z0, out int z1)
        {
            var origin = terrain.transform.position;
            var size = terrain.terrainData.size;
            x0 = Mathf.Clamp(Mathf.FloorToInt((center.x - extent.x - origin.x) / size.x * (columns - 1)), 0, columns - 1);
            x1 = Mathf.Clamp(Mathf.CeilToInt((center.x + extent.x - origin.x) / size.x * (columns - 1)), 0, columns - 1);
            z0 = Mathf.Clamp(Mathf.FloorToInt((center.y - extent.y - origin.z) / size.z * (rows - 1)), 0, rows - 1);
            z1 = Mathf.Clamp(Mathf.CeilToInt((center.y + extent.y - origin.z) / size.z * (rows - 1)), 0, rows - 1);
        }

        private static float WaterSandPick(System.Random random, float min, float max) =>
            Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}
