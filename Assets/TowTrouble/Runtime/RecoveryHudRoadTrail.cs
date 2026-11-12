using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private const float BreadcrumbSpacing = 8f;
        private const float FirstBreadcrumbDistance = 5f;
        private const int EnvironmentLayer = 1 << 8;
        private readonly MeshRenderer[] roadChevrons = new MeshRenderer[5];
        private readonly Mesh[] roadTrailMeshes = new Mesh[5];
        private readonly Vector3[] lastChevronCenters = new Vector3[5], lastChevronDirections = new Vector3[5];
        private readonly bool[] placedChevrons = new bool[5];
        private readonly RaycastHit[] chevronHits = new RaycastHit[16];
        private Vector3[] chevronTemplate, chevronProjection;
        private Texture2D roadTrailPalette;
        private GameObject roadTrailRoot;
        private Material roadTrailMaterial;

        private void BuildRoadTrail()
        {
            if (roadTrailRoot != null) return;
            // This shader is also used by the garage markings in the county scene.
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) return;
            roadTrailPalette = BuildTrailPalette();
            roadTrailMaterial = new Material(shader) { name = "Local route chevrons" };
            roadTrailMaterial.SetTexture("_BaseMap", roadTrailPalette);
            roadTrailMaterial.SetColor("_BaseColor", Color.white);
            roadTrailMaterial.enableInstancing = true;

            roadTrailRoot = new GameObject("Recovery road guidance");
            // Keep road geometry in world coordinates as the HUD canvas moves or resizes.
            roadTrailRoot.transform.SetParent(transform, false);
            roadTrailRoot.SetActive(false);
            for (int i = 0; i < roadChevrons.Length; i++)
            {
                var item = new GameObject("Road chevron", typeof(MeshFilter), typeof(MeshRenderer));
                item.transform.SetParent(roadTrailRoot.transform, false);
                var mesh = BuildChevronMesh();
                if (chevronTemplate == null)
                {
                    chevronTemplate = mesh.vertices;
                    chevronProjection = new Vector3[chevronTemplate.Length];
                }
                mesh.MarkDynamic();
                roadTrailMeshes[i] = mesh;
                item.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = item.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = roadTrailMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                roadChevrons[i] = renderer;
            }
        }

        private static Texture2D BuildTrailPalette()
        {
            // Columns: dark edge, left wing, right wing. The top row picks out the nose.
            var palette = new Texture2D(3, 2, TextureFormat.RGB24, false) {
                name = "Route marker enamel", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            Color32 edge = new Color32(66, 43, 26, 255);
            palette.SetPixels32(new[] {
                edge, new Color32(255, 186, 62, 255), new Color32(235, 151, 42, 255),
                edge, new Color32(255, 232, 156, 255), new Color32(255, 218, 123, 255)
            });
            palette.Apply(false, true);
            return palette;
        }

        private static Mesh BuildChevronMesh()
        {
            // Plan view of a tapered chevron with clipped heels. The outline and
            // enamel share edges in a single surface, including along the center fold.
            var rim = new[] {
                new Vector2(0, .85f), new Vector2(.97f, -.17f), new Vector2(.97f, -.44f), new Vector2(.85f, -.51f),
                new Vector2(0, -.03f), new Vector2(-.85f, -.51f), new Vector2(-.97f, -.44f), new Vector2(-.97f, -.17f)
            };
            var face = new[] {
                new Vector2(0, .69f), new Vector2(.87f, -.21f), new Vector2(.87f, -.37f), new Vector2(.84f, -.39f),
                new Vector2(0, .10f), new Vector2(-.84f, -.39f), new Vector2(-.87f, -.37f), new Vector2(-.87f, -.21f)
            };
            var vertices = new List<Vector3>(66);
            var uvs = new List<Vector2>(66);
            void Triangle(Vector2 a, Vector2 b, Vector2 c, int column)
            {
                var ab = (a + b) * .5f;
                var bc = (b + c) * .5f;
                var ca = (c + a) * .5f;
                Face(a, ab, ca); Face(ab, b, bc); Face(ca, bc, c); Face(ab, bc, ca);
                void Face(Vector2 p, Vector2 q, Vector2 r) { Add(p, column); Add(q, column); Add(r, column); }
            }
            void Add(Vector2 point, int column)
            {
                vertices.Add(new Vector3(point.x, 0, point.y));
                uvs.Add(new Vector2((column + .5f) / 3f, .5f + (point.y - .48f) * .4f));
            }

            for (int i = 0; i < rim.Length; i++)
            {
                int j = (i + 1) % rim.Length;
                Triangle(rim[i], rim[j], face[j], 0);
                Triangle(rim[i], face[j], face[i], 0);
                if (i != 3 && i != 4)
                    Triangle(face[4], face[i], face[j], i < 4 ? 2 : 1);
            }
            var indices = new int[vertices.Count];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            var mesh = new Mesh { name = "Route marker chevron" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(indices, 0);
            return mesh;
        }

        private void ShowRoadTrail(Camera camera, Vector3 driver, List<Vector3> road)
        {
            if (roadTrailRoot == null || camera == null || road == null || road.Count < 2)
            {
                HideRoadTrail();
                return;
            }

            // Project onto the road in 3D so nearby switchbacks at different elevations stay distinct.
            float total = 0f, progress = 0f, bestSquaredDistance = float.PositiveInfinity;
            int nearestSegment = 0;
            float nearestOffset = 0f;
            for (int i = 0; i < road.Count - 1; i++)
            {
                Vector3 delta = road[i + 1] - road[i];
                float length = delta.magnitude;
                if (length < .001f) continue;
                float t = Mathf.Clamp01(Vector3.Dot(driver - road[i], delta) / (length * length));
                float distance = (road[i] + delta * t - driver).sqrMagnitude;
                if (distance < bestSquaredDistance)
                {
                    bestSquaredDistance = distance;
                    nearestSegment = i;
                    nearestOffset = length * t;
                    progress = total + nearestOffset;
                }
                total += length;
            }

            float remaining = total - progress;
            if (remaining < FirstBreadcrumbDistance || bestSquaredDistance > 48f * 48f)
            {
                HideRoadTrail();
                return;
            }

            // Space the marks backward from the fixed exit. They stay put as the driver advances.
            float next = nearestOffset + FirstBreadcrumbDistance
                       + Mathf.Repeat(remaining - FirstBreadcrumbDistance, BreadcrumbSpacing);
            int drawn = 0;
            for (int i = nearestSegment; i < road.Count - 1 && drawn < roadChevrons.Length; i++)
            {
                Vector3 delta = road[i + 1] - road[i];
                float length = delta.magnitude;
                if (length < .001f) continue;
                Vector3 forward = delta / length;
                while (next <= length && drawn < roadChevrons.Length)
                {
                    DrawRoadChevron(drawn++, road[i] + forward * next, forward);
                    next += BreadcrumbSpacing;
                }
                next -= length;
            }
            for (int i = drawn; i < roadChevrons.Length; i++)
                roadChevrons[i].enabled = false;
            roadTrailRoot.SetActive(drawn > 0);
        }

        private void DrawRoadChevron(int index, Vector3 center, Vector3 forward)
        {
            var marker = roadChevrons[index];
            if (placedChevrons[index] && (lastChevronCenters[index] - center).sqrMagnitude < .0001f &&
                Vector3.Dot(lastChevronDirections[index], forward) > .9999f)
            {
                marker.enabled = true;
                return;
            }

            placedChevrons[index] = false;
            marker.enabled = false;
            if (!FindChevronSurface(center, 1.35f, out var surface)) return;
            var tangent = Vector3.ProjectOnPlane(forward, surface.normal).normalized;
            if (tangent.sqrMagnitude < .5f) return;
            var frame = Quaternion.LookRotation(tangent, surface.normal);
            marker.transform.position = surface.point;

            for (int i = 0; i < chevronTemplate.Length; i++)
            {
                var hint = surface.point + frame * chevronTemplate[i];
                if (!FindChevronSurface(hint, .48f, out var contact) ||
                    Vector3.Dot(surface.normal, contact.normal) < .70f) return;
                chevronProjection[i] = marker.transform.InverseTransformPoint(contact.point + contact.normal * .026f);
            }

            var mesh = roadTrailMeshes[index];
            mesh.vertices = chevronProjection;
            mesh.RecalculateBounds();
            lastChevronCenters[index] = center;
            lastChevronDirections[index] = forward;
            placedChevrons[index] = true;
            marker.enabled = true;
        }

        private bool FindChevronSurface(Vector3 hint, float range, out RaycastHit contact)
        {
            contact = default;
            int count = Physics.RaycastNonAlloc(hint + Vector3.up * (range + .03f), Vector3.down,
                chevronHits, range * 2 + .06f, EnvironmentLayer, QueryTriggerInteraction.Ignore);
            float closest = range;
            for (int i = 0; i < count; i++)
            {
                var hit = chevronHits[i];
                if (hit.normal.y < .48f || !IsChevronSurface(hit.collider)) continue;
                float error = Mathf.Abs(hit.point.y - hint.y);
                if (error >= closest) continue;
                closest = error;
                contact = hit;
            }
            return contact.collider != null;
        }

        private static bool IsChevronSurface(Collider collider)
        {
            return collider is TerrainCollider || collider.TryGetComponent<RecoveryGroundSurface>(out _) ||
                collider.TryGetComponent<RecoveryTrackSurface>(out _) || collider.name.StartsWith("Road-") ||
                collider.name == "Bridge drive surface" || collider.name == "Bridge entry ramp";
        }

        private void HideRoadTrail()
        {
            if (roadTrailRoot != null && roadTrailRoot.activeSelf)
                roadTrailRoot.SetActive(false);
        }

        private void DisposeRoadTrail()
        {
            DestroyTrailResource(roadTrailMaterial);
            foreach (var mesh in roadTrailMeshes) DestroyTrailResource(mesh);
            DestroyTrailResource(roadTrailPalette);
        }

        private static void DestroyTrailResource(Object resource)
        {
            if (resource == null) return;
            if (Application.isPlaying) Destroy(resource);
            else DestroyImmediate(resource);
        }
    }
}
