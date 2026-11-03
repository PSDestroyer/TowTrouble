using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private const float BreadcrumbSpacing = 8f;
        private const float FirstBreadcrumbDistance = 5f;
        private readonly MeshRenderer[] roadChevrons = new MeshRenderer[5];
        private Mesh roadTrailMesh;
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
            roadTrailMesh = BuildChevronMesh();
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
                item.GetComponent<MeshFilter>().sharedMesh = roadTrailMesh;
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
                Add(a, column); Add(b, column); Add(c, column);
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
            mesh.UploadMeshData(true);
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
                    DrawRoadChevron(roadChevrons[drawn++], road[i] + forward * next, forward);
                    next += BreadcrumbSpacing;
                }
                next -= length;
            }
            for (int i = drawn; i < roadChevrons.Length; i++)
                roadChevrons[i].enabled = false;
            roadTrailRoot.SetActive(drawn > 0);
        }

        private static void DrawRoadChevron(MeshRenderer marker, Vector3 center, Vector3 forward)
        {
            // Marker dimensions are in meters regardless of the scene owner's scale.
            var scale = marker.transform.parent.lossyScale;
            marker.transform.localScale = new Vector3(1f / Mathf.Max(.00001f, scale.x),
                1f / Mathf.Max(.00001f, scale.y), 1f / Mathf.Max(.00001f, scale.z));
            marker.transform.SetPositionAndRotation(center + Vector3.up * .16f, Quaternion.LookRotation(forward, Vector3.up));
            marker.enabled = true;
        }

        private void HideRoadTrail()
        {
            if (roadTrailRoot != null && roadTrailRoot.activeSelf)
                roadTrailRoot.SetActive(false);
        }

        private void DisposeRoadTrail()
        {
            DestroyTrailResource(roadTrailMaterial);
            DestroyTrailResource(roadTrailMesh);
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
