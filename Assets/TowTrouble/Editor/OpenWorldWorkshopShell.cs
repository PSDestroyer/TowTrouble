using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        // Exterior only. Front service doors and the sign are authored separately;
        // the closed side walls and gables remove every view into an empty shell.
        private static void BuildPolishedWorkshopShell(Transform facade)
        {
            var shell = PolishGroup(facade, "Finished workshop exterior shell");
            var walls = PolishGroup(shell, "Terracotta walls and timber structure");
            var wall = WorkshopFinish("Wall");
            var wallLight = WorkshopFinish("WallLight");
            var timber = WorkshopFinish("Timber");
            var trim = WorkshopFinish("Trim");
            var foundation = WorkshopFinish("Foundation");
            using (var shapes = new GarageFacadeShapes())
            {
                shapes.BevelBox(foundation, new Vector3(0, .15f, 22), new Vector3(18.35f, .30f, 14.3f), .07f);
                foreach (float side in new[] { -1f, 1f })
                {
                    shapes.Box(wall, new Vector3(side * 8.85f, 2.84f, 22), new Vector3(.30f, 5.12f, 14));
                    shapes.Box(foundation, new Vector3(side * 9.02f, .49f, 22), new Vector3(.13f, .43f, 14));
                    shapes.Box(timber, new Vector3(side * 8.99f, 5.22f, 22), new Vector3(.24f, .36f, 14.15f));
                    foreach (float z in new[] { 15.14f, 21.65f, 28.85f })
                        shapes.BevelBox(timber, new Vector3(side * 9.015f, 2.87f, z), new Vector3(.22f, 5.10f, .22f), .025f);
                    // Broad tonal courses retain the stylised timber character
                    // without high-frequency dark pinstripes at driving distance.
                    foreach (float y in new[] { 1.15f, 2.45f, 3.75f })
                        shapes.Box(wallLight, new Vector3(side * 9.005f, y, 22), new Vector3(.024f, .065f, 13.8f));
                }
                shapes.Box(wall, new Vector3(0, 2.84f, 28.85f), new Vector3(18, 5.12f, .30f));
                shapes.Box(foundation, new Vector3(0, .49f, 29.025f), new Vector3(18.15f, .43f, .13f));
                shapes.Box(timber, new Vector3(0, 5.22f, 29.015f), new Vector3(18.18f, .36f, .24f));
                foreach (float x in new[] { -4.5f, 0, 4.5f })
                    shapes.Box(timber, new Vector3(x, 2.87f, 29.015f), new Vector3(.18f, 5.10f, .22f));
                foreach (float y in new[] { 1.15f, 2.45f, 3.75f })
                    shapes.Box(wallLight, new Vector3(0, y, 29.005f), new Vector3(17.8f, .065f, .024f));
                shapes.Finish(walls, "Workshop closed wall shell");
            }
            BuildWorkshopClosedGables(walls, wallLight);
            WorkshopShellCollider(walls, "Workshop foundation", new Vector3(0, .15f, 22), new Vector3(18.35f, .30f, 14.3f), Quaternion.identity);
            foreach (float side in new[] { -1f, 1f })
                WorkshopShellCollider(walls, "Closed side wall", new Vector3(side * 8.85f, 2.84f, 22), new Vector3(.30f, 5.12f, 14), Quaternion.identity);
            WorkshopShellCollider(walls, "Closed rear wall", new Vector3(0, 2.84f, 28.85f), new Vector3(18, 5.12f, .30f), Quaternion.identity);
            ConfigureDetailLod(walls.gameObject, 1200);

            BuildWorkshopRoof(shell);
            BuildWorkshopWallCanopy(shell);
            BuildWorkshopSideOpenings(shell, timber, trim);
            foreach (var child in shell.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = 8;
                child.gameObject.isStatic = true;
            }
        }

        private static void BuildWorkshopClosedGables(Transform parent, Material material)
        {
            var vertices = new List<Vector3>(48);
            var triangles = new List<int>(48);
            void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int index = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
            }
            foreach (float z in new[] { 15.30f, 28.72f })
            {
                var a = new Vector3(-9, 5.40f, z);
                var b = new Vector3(9, 5.40f, z);
                var c = new Vector3(0, 8.15f, z);
                var depth = Vector3.forward * .30f;
                Triangle(a, c, b); Triangle(a + depth, b + depth, c + depth);
                Triangle(a, b, b + depth); Triangle(a, b + depth, a + depth);
                Triangle(b, c, c + depth); Triangle(b, c + depth, b + depth);
                Triangle(c, a, a + depth); Triangle(c, a + depth, c + depth);
            }
            var mesh = new Mesh { name = "Workshop closed triangular gables" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(parent, false);
            var savedMesh = VisualAsset(mesh, "GarageServiceBase/Workshop closed triangular gables.asset");
            go.GetComponent<MeshFilter>().sharedMesh = savedMesh;
            go.GetComponent<MeshCollider>().sharedMesh = savedMesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void BuildWorkshopRoof(Transform shell)
        {
            var roof = PolishGroup(shell, "Slate roof fascia and rainwater fittings");
            var roofFinish = WorkshopFinish("Roof");
            var edge = WorkshopFinish("RoofEdge");
            var timber = WorkshopFinish("Timber");
            var metal = WorkshopFinish("Metal");
            const float halfSpan = 9.45f, ridge = 8.09f, eave = 5.34f, depth = 15.25f;
            float rise = ridge - eave;
            float slope = Mathf.Atan2(rise, halfSpan) * Mathf.Rad2Deg;
            float slopeLength = Mathf.Sqrt(halfSpan * halfSpan + rise * rise);
            using (var shapes = new GarageFacadeShapes())
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    var rotation = Quaternion.Euler(0, 0, -side * slope);
                    var center = new Vector3(side * halfSpan * .5f, (ridge + eave) * .5f, 22.025f);
                    shapes.Box(roofFinish, center, new Vector3(slopeLength, .16f, depth), rotation);
                    WorkshopShellCollider(roof, "Pitched roof plane", center, new Vector3(slopeLength, .18f, depth), rotation);
                    foreach (float z in new[] { 14.39f, 29.66f })
                    {
                        shapes.Beam(timber, new Vector3(0, ridge - .065f, z), new Vector3(side * halfSpan, eave - .065f, z), .23f);
                        shapes.Beam(edge, new Vector3(0, ridge + .08f, z), new Vector3(side * halfSpan, eave + .08f, z), .10f);
                    }
                    shapes.Box(timber, new Vector3(side * 9.43f, 5.22f, 22.025f), new Vector3(.20f, .27f, depth));
                    float gutterX = side * 9.57f;
                    shapes.Box(metal, new Vector3(gutterX, 5.16f, 22.025f), new Vector3(.24f, .05f, depth + .10f));
                    shapes.Box(edge, new Vector3(side * 9.69f, 5.23f, 22.025f), new Vector3(.045f, .17f, depth + .10f));
                    shapes.Box(edge, new Vector3(side * 9.45f, 5.23f, 22.025f), new Vector3(.045f, .17f, depth + .10f));
                    foreach (float z in new[] { 14.38f, 29.67f })
                        shapes.Box(edge, new Vector3(gutterX, 5.23f, z), new Vector3(.25f, .17f, .045f));
                    // Connected elbows lead from each rear gutter to its downpipe.
                    var pipeTop = new Vector3(side * 9.22f, 4.72f, 28.57f);
                    shapes.Beam(metal, new Vector3(gutterX, 5.17f, 28.57f), pipeTop, .12f);
                    shapes.Beam(metal, pipeTop, new Vector3(side * 9.22f, .52f, 28.57f), .12f);
                    shapes.Beam(metal, new Vector3(side * 9.22f, .52f, 28.57f), new Vector3(side * 9.57f, .27f, 28.57f), .12f);
                    foreach (float y in new[] { 1.18f, 3.45f })
                        shapes.Box(edge, new Vector3(side * 9.17f, y, 28.57f), new Vector3(.23f, .065f, .19f));
                }
                shapes.BevelBox(edge, new Vector3(0, 8.18f, 22.025f), new Vector3(.42f, .18f, 15.46f), .07f);
                shapes.Finish(roof, "Workshop slate roof and drainage");
            }
            ConfigureDetailLod(roof.gameObject, 1200);

            // Thin surface seams have their own near-detail renderer. They do
            // not cast unstable patterned shadows onto the solid roof planes.
            var seams = PolishGroup(shell, "Slate roof raised seam detail");
            using (var shapes = new GarageFacadeShapes())
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    var rotation = Quaternion.Euler(0, 0, -side * slope);
                    var center = new Vector3(side * halfSpan * .5f, (ridge + eave) * .5f, 22.025f);
                    foreach (float z in new[] { 16.8f, 19.4f, 22f, 24.6f, 27.2f })
                        shapes.Box(roofFinish, new Vector3(center.x, center.y, z) + rotation * Vector3.up * .1125f,
                            new Vector3(slopeLength - .12f, .045f, .09f), rotation);
                }
                shapes.Finish(seams, "Workshop raised slate roof seams");
            }
            foreach (var renderer in seams.GetComponentsInChildren<MeshRenderer>())
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            ConfigureDetailLod(seams.gameObject, 150);
        }

        private static void BuildWorkshopWallCanopy(Transform shell)
        {
            var canopy = PolishGroup(shell, "Shallow wall supported service canopy");
            var roof = WorkshopFinish("Roof");
            var edge = WorkshopFinish("RoofEdge");
            var timber = WorkshopFinish("Timber");
            var metal = WorkshopFinish("Metal");
            const float frontZ = 13.2f, backZ = 15.35f, frontY = 4.95f, backY = 5.24f;
            float slope = Mathf.Atan2(backY - frontY, backZ - frontZ) * Mathf.Rad2Deg;
            float length = new Vector2(backZ - frontZ, backY - frontY).magnitude;
            var rotation = Quaternion.Euler(-slope, 0, 0);
            var center = new Vector3(0, (frontY + backY) * .5f, (frontZ + backZ) * .5f);
            using (var shapes = new GarageFacadeShapes())
            {
                shapes.Box(roof, center, new Vector3(18.65f, .11f, length), rotation);
                shapes.Box(edge, new Vector3(0, frontY - .025f, frontZ), new Vector3(18.75f, .18f, .16f));
                shapes.Box(timber, new Vector3(0, 5.06f, 15.15f), new Vector3(18.2f, .20f, .22f));
                foreach (float x in new[] { -9.25f, 9.25f })
                    shapes.Beam(timber, new Vector3(x, frontY - .08f, frontZ), new Vector3(x, backY - .08f, backZ), .17f);
                foreach (float x in new[] { -8.65f, 0, 8.65f })
                {
                    shapes.Box(timber, new Vector3(x, 4.82f, 15.08f), new Vector3(.18f, .72f, .20f));
                    shapes.Beam(timber, new Vector3(x, 5.11f, 15.08f), new Vector3(x, 4.87f, 13.52f), .16f);
                    shapes.Beam(timber, new Vector3(x, 4.49f, 15.08f), new Vector3(x, 4.89f, 13.63f), .13f);
                    foreach (float y in new[] { 4.61f, 5.05f })
                        shapes.Box(metal, new Vector3(x, y, 14.963f), new Vector3(.25f, .065f, .04f));
                }
                shapes.Finish(canopy, "Workshop bracketed canopy");
            }
            WorkshopShellCollider(canopy, "Shallow canopy roof", center, new Vector3(18.65f, .13f, length), rotation);
            ConfigureDetailLod(canopy.gameObject, 1000);
        }

        private static void BuildWorkshopSideOpenings(Transform shell, Material timber, Material trim)
        {
            var openings = PolishGroup(shell, "Opaque side windows and service door");
            var glass = WorkshopFinish("Glass");
            var metal = WorkshopFinish("Metal");
            var door = WorkshopFinish("Door");
            var doorLight = WorkshopFinish("DoorLight");
            using (var shapes = new GarageFacadeShapes())
            {
                foreach (float side in new[] { -1f, 1f })
                    foreach (float z in side < 0 ? new[] { 23.2f, 26f } : new[] { 18.7f, 23.3f })
                    {
                        const float y = 3.23f;
                        shapes.Box(timber, new Vector3(side * 9.06f, y, z), new Vector3(.14f, 1.86f, 2.58f));
                        shapes.Box(glass, new Vector3(side * 9.15f, y, z), new Vector3(.045f, 1.55f, 2.25f));
                        foreach (float dz in new[] { -1.20f, 0, 1.20f })
                            shapes.Box(trim, new Vector3(side * 9.19f, y, z + dz), new Vector3(.075f, 1.72f, .095f));
                        foreach (float dy in new[] { -.83f, .83f })
                            shapes.Box(trim, new Vector3(side * 9.19f, y + dy, z), new Vector3(.075f, .10f, 2.49f));
                        shapes.Box(trim, new Vector3(side * 9.19f, y + .08f, z), new Vector3(.075f, .07f, 2.4f));
                        shapes.BevelBox(timber, new Vector3(side * 9.22f, y - .92f, z), new Vector3(.40f, .13f, 2.72f), .025f);
                    }

                const float serviceZ = 26.65f;
                shapes.Box(timber, new Vector3(9.075f, 1.78f, serviceZ), new Vector3(.15f, 3.03f, 1.85f));
                shapes.BevelBox(door, new Vector3(9.18f, 1.78f, serviceZ), new Vector3(.12f, 2.85f, 1.61f), .025f);
                foreach (float z in new[] { serviceZ - .88f, serviceZ + .88f })
                    shapes.Box(trim, new Vector3(9.205f, 1.82f, z), new Vector3(.19f, 3.04f, .13f));
                shapes.Box(trim, new Vector3(9.205f, 3.31f, serviceZ), new Vector3(.19f, .13f, 1.89f));
                foreach (float y in new[] { .75f, 1.53f, 2.31f })
                    shapes.BevelBox(doorLight, new Vector3(9.255f, y, serviceZ), new Vector3(.04f, .61f, 1.29f), .015f);
                shapes.Box(metal, new Vector3(9.29f, 1.54f, serviceZ - .55f), new Vector3(.055f, .27f, .08f));
                shapes.Box(trim, new Vector3(9.335f, 1.54f, serviceZ - .46f), new Vector3(.07f, .065f, .25f));
                shapes.BevelBox(WorkshopFinish("Foundation"), new Vector3(9.28f, .19f, serviceZ), new Vector3(.70f, .20f, 2.05f), .04f);
                shapes.Finish(openings, "Workshop side window frames and service door");
            }
            ConfigureDetailLod(openings.gameObject, 650);
        }

        private static void WorkshopShellCollider(Transform parent, string name, Vector3 center, Vector3 size, Quaternion rotation)
        {
            var go = new GameObject(name, typeof(BoxCollider));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center; go.transform.localRotation = rotation;
            go.GetComponent<BoxCollider>().size = size;
        }
    }
}
