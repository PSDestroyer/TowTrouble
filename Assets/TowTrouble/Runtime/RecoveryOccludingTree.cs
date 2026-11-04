using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    internal sealed class RecoveryOccludingTree
    {
        private static readonly string[] StandingFamilies = {
            "P_BirchTree", "P_BirchSapling", "P_FirTree", "P_FirSapling", "P_OakTree", "P_OakSapling",
            "P_Pine", "P_ChinaPine", "P_RedMaple", "P_KnotwoodTree", "P_Bamboo", "P_BananaTree",
            "P_PalmTree", "P_CoconutPalm", "P_DesertPalm", "P_DesertCoconutPalm", "P_TreeWindswept", "P_YuccaTree"
        };
        private static readonly string[] GroundProps = { "Fallen", "Stump", "Roots", "Branch", "Bush", "Sprout" };

        internal static bool IsStandingTree(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (string family in StandingFamilies)
            {
                if (!name.StartsWith(family, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (string prop in GroundProps)
                    if (name.IndexOf(prop, StringComparison.OrdinalIgnoreCase) >= 0) return false;
                return true;
            }
            return false;
        }

        private readonly Renderer[] renderers;
        private RecoveryTreeFade fade;
        private float progress;
        private readonly Shape shape;
        internal Bounds Bounds { get; }
        internal bool Hidden { get; private set; }
        internal float RestoreAt { get; set; }

        internal RecoveryOccludingTree(LODGroup group)
        {
            var unique = new List<Renderer>();
            var bounds = new Bounds(group.transform.position, Vector3.zero);
            foreach (var lod in group.GetLODs())
                foreach (var renderer in lod.renderers)
                {
                    if (renderer == null || unique.Contains(renderer)) continue;
                    if (unique.Count == 0) bounds = renderer.bounds;
                    else bounds.Encapsulate(renderer.bounds);
                    unique.Add(renderer);
                }
            renderers = unique.ToArray();
            Bounds = bounds;
            shape = new Shape(group.name, bounds, group.transform.position);
        }

        internal bool Blocks(Vector3 camera, Vector3 target) => shape.Blocks(camera, target);

        internal readonly struct Shape
        {
            private readonly Ellipsoid trunk, crown, tip;
            private readonly bool valid, conifer;

            internal Shape(string name, Bounds bounds, Vector3 basePoint)
            {
                this = default;
                var extents = bounds.extents;
                if (extents.x <= 0f || extents.y <= 0f || extents.z <= 0f) return;
                valid = true;
                float height = bounds.size.y;
                basePoint.x = Mathf.Clamp(basePoint.x, bounds.min.x, bounds.max.x);
                basePoint.z = Mathf.Clamp(basePoint.z, bounds.min.z, bounds.max.z);
                basePoint.y = bounds.min.y;
                float radius = Mathf.Max(.10f, height * .023f);
                trunk = new Ellipsoid(basePoint + Vector3.up * (height * .31f), new Vector3(radius, height * .39f, radius));

                conifer = name != null && (name.StartsWith("P_Fir", StringComparison.OrdinalIgnoreCase) || name.StartsWith("P_Pine", StringComparison.OrdinalIgnoreCase));
                var center = bounds.center;
                // Firs carry their broadest branches low, with a second narrower crown tapering to the tip.
                center.y = basePoint.y + height * (conifer ? .46f : .66f);
                crown = new Ellipsoid(center, new Vector3(extents.x, height * (conifer ? .30f : .34f), extents.z));
                if (conifer)
                {
                    center.y = basePoint.y + height * .75f;
                    tip = new Ellipsoid(center, new Vector3(extents.x * .66f, height * .25f, extents.z * .66f));
                }
            }

            internal bool Blocks(Vector3 camera, Vector3 target)
            {
                if (!valid) return false;
                var delta = target - camera;
                return crown.Intersects(camera, delta) || (conifer && tip.Intersects(camera, delta)) || trunk.Intersects(camera, delta);
            }
        }

        private readonly struct Ellipsoid
        {
            private readonly Vector3 center, inverseRadius;
            internal Ellipsoid(Vector3 center, Vector3 radius)
            {
                this.center = center;
                inverseRadius = new Vector3(1f / Mathf.Max(.01f, radius.x), 1f / Mathf.Max(.01f, radius.y), 1f / Mathf.Max(.01f, radius.z));
            }
            internal bool Intersects(Vector3 camera, Vector3 delta)
            {
                // In unit-sphere space the nearest point on the finite segment also handles either endpoint inside.
                Vector3 start = Vector3.Scale(camera - center, inverseRadius);
                Vector3 step = Vector3.Scale(delta, inverseRadius);
                float lengthSquared = step.sqrMagnitude;
                float t = lengthSquared > 0f ? Mathf.Clamp01(-Vector3.Dot(start, step) / lengthSquared) : 0f;
                return (start + step * t).sqrMagnitude <= 1f;
            }
        }

        internal void SetHidden(bool hidden) => Hidden = hidden;

        internal bool AdvanceFade(float dt)
        {
            progress = RecoveryTreeFade.Advance(progress, Hidden, dt);
            if (progress == 0f) fade?.Restore();
            else (fade ??= new RecoveryTreeFade(renderers)).Apply(progress);
            return progress != (Hidden ? 1f : 0f);
        }

        internal void Restore()
        {
            Hidden = false;
            progress = 0f;
            fade?.Restore();
        }
    }
}
