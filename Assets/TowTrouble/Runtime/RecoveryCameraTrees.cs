using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryCamera
    {
        private const float TreeCheckInterval = .06f;
        private const float TreeRestoreDelay = .3f;
        private const float MaxTreeSightDistance = 56f;
        private readonly List<RecoveryOccludingTree> cameraTrees = new List<RecoveryOccludingTree>();
        private readonly List<RecoveryOccludingTree> fadingTrees = new List<RecoveryOccludingTree>();
        private readonly List<RecoveryTerrainTrees> terrainTrees = new List<RecoveryTerrainTrees>();
        private readonly HashSet<Collider> treeColliders = new HashSet<Collider>();
        private readonly Vector3[] sightPoints = new Vector3[15]; // Five samples per car, up to two drivers and one casualty.
        private RaycastHit[] cameraHits = new RaycastHit[16];
        private Camera treeCamera;
        private float nextTreeCheck;

        private void IndexCameraTrees()
        {
            ClearCameraTrees();
            treeCamera = GetComponent<Camera>();
            var colliders = new List<Collider>();
            foreach (var group in FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
            {
                if (group.gameObject.scene != gameObject.scene || !RecoveryOccludingTree.IsStandingTree(group.name)) continue;
                cameraTrees.Add(new RecoveryOccludingTree(group));
                group.GetComponentsInChildren(true, colliders);
                foreach (var collider in colliders) treeColliders.Add(collider);
            }
            foreach (var terrain in Terrain.activeTerrains)
            {
                if (terrain.gameObject.scene != gameObject.scene) continue;
                var trees = RecoveryTerrainTrees.TryCreate(terrain);
                if (trees != null) terrainTrees.Add(trees);
            }
            nextTreeCheck = 0f;
        }

        // A tree can disappear for the camera; terrain, cliffs and buildings still shorten the camera arm.
        private bool CameraObstruction(Vector3 from, Vector3 to, out RaycastHit hit, QueryTriggerInteraction triggers)
        {
            const int environment = 1 << 8;
            if (!Physics.Linecast(from, to, out hit, environment, triggers)) return false;
            if (!treeColliders.Contains(hit.collider)) return true;

            var offset = to - from;
            float distance = offset.magnitude;
            int count;
            while ((count = Physics.RaycastNonAlloc(from, offset / distance, cameraHits, distance, environment, triggers)) == cameraHits.Length)
                Array.Resize(ref cameraHits, cameraHits.Length * 2);
            float closest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var other = cameraHits[i];
                if (other.distance >= closest || treeColliders.Contains(other.collider)) continue;
                hit = other;
                closest = other.distance;
            }
            return closest != float.PositiveInfinity;
        }

        private void UpdateTreeVisibility(bool lobby)
        {
            float now = Time.unscaledTime;
            if (treeCamera == null || (cameraTrees.Count == 0 && terrainTrees.Count == 0)) return;
            if (now >= nextTreeCheck) CheckTreeSightlines(lobby, now);
            // A hitch or returning to the Game view should not skip the entire transition.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);
            for (int i = fadingTrees.Count - 1; i >= 0; i--)
                if (!fadingTrees[i].AdvanceFade(dt)) fadingTrees.RemoveAt(i);
            foreach (var trees in terrainTrees) trees.AdvanceFades(dt);
        }

        private void CheckTreeSightlines(bool lobby, float now)
        {
            nextTreeCheck = now + TreeCheckInterval;
            var world = session.World;
            int slot = Mathf.Max(0, session.LocalSlot);
            bool preview = session.GarageOpen || session.View == "Vehicles";
            int count = 0;
            if (!lobby || preview) AddCarSightPoints(world.Drivers[slot], ref count);
            if (!lobby && !preview)
            {
                if (world.Cooperative && world.OccupiedSlot(1 - slot)) AddCarSightPoints(world.Drivers[1 - slot], ref count);
                if (world.HasContract || world.Phase == RecoveryPhase.Complete) AddCarSightPoints(world.Casualty, ref count);
            }

            Vector3 eye = transform.position;
            var sightBounds = new Bounds(eye, Vector3.zero);
            for (int i = 0; i < count; i++) sightBounds.Encapsulate(sightPoints[i]);
            foreach (var tree in cameraTrees)
            {
                if (count > 0 && tree.Bounds.Intersects(sightBounds))
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (!tree.Blocks(eye, sightPoints[i])) continue;
                        tree.RestoreAt = now + TreeRestoreDelay;
                        if (!tree.Hidden) StartTreeFade(tree, true);
                        break;
                    }
                }
                if (tree.Hidden && now >= tree.RestoreAt) StartTreeFade(tree, false);
            }
            foreach (var trees in terrainTrees)
                trees.UpdateVisibility(eye, sightPoints, count, sightBounds, now, TreeRestoreDelay);
        }

        private void StartTreeFade(RecoveryOccludingTree tree, bool hide)
        {
            tree.SetHidden(hide);
            if (!fadingTrees.Contains(tree)) fadingTrees.Add(tree);
        }

        private void AddCarSightPoints(RecoveryVehicle car, ref int count)
        {
            if (car == null || !car.gameObject.activeInHierarchy || !car.TryGetComponent<BoxCollider>(out var box)) return;
            var t = car.transform;
            Vector3 center = t.TransformPoint(box.center);
            if ((center - transform.position).sqrMagnitude > MaxTreeSightDistance * MaxTreeSightDistance) return;
            Vector3 viewport = treeCamera.WorldToViewportPoint(center);
            if (viewport.z <= 0f || viewport.x < -.04f || viewport.x > 1.04f || viewport.y < -.04f || viewport.y > 1.04f) return;

            Vector3 side = t.TransformVector(Vector3.right * (box.size.x * .38f));
            Vector3 front = t.TransformVector(Vector3.forward * (box.size.z * .38f));
            sightPoints[count++] = center + t.TransformVector(Vector3.up * (box.size.y * .3f));
            sightPoints[count++] = center + front + side;
            sightPoints[count++] = center + front - side;
            sightPoints[count++] = center - front + side;
            sightPoints[count++] = center - front - side;
        }

        private void ClearCameraTrees()
        {
            foreach (var tree in cameraTrees) tree.Restore();
            foreach (var trees in terrainTrees) trees.Dispose();
            cameraTrees.Clear();
            fadingTrees.Clear();
            terrainTrees.Clear();
            treeColliders.Clear();
        }

        private void OnEnable()
        {
            if (session != null) IndexCameraTrees();
        }

        private void OnDisable() { DisableTitleLighting(); ClearCameraTrees(); }
    }
}
