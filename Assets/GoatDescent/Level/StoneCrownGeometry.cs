using UnityEngine;

namespace GoatDescent.Levels
{
    public static class StoneCrownGeometry
    {
        public static void Build(LevelGeneratedResources resources, LevelRecipe recipe)
        {
            LevelRoute route = resources.Route;
            var shader = Shader.Find("GoatDescent/Mountain");
            if (!shader) throw new System.InvalidOperationException("Не найден шейдер GoatDescent/Mountain");
            var material = resources.Own(new Material(MountainMaterial.Get()));
            material.name = "Каменная корона — поверхности";
            material.SetFloat("_HeightMin", recipe.ValleyHeight);
            material.SetFloat("_HeightMax", recipe.SummitHeight + 110f);
            material.SetColor("_RockTint", recipe.Biome ? recipe.Biome.RockColor * new Color(1.6f, 1.15f, .8f) : Color.white);
            material.SetColor("_GrassTint", recipe.Biome ? recipe.Biome.FoliageColor * 2f : Color.white);
            material.SetFloat("_SnowStart", 1f);
            material.SetFloat("_GrassEnd", .88f);
            material.SetFloat("_GrassBlend", .62f);
            material.SetFloat("_Tiling", .06f);
            material.SetFloat("_HighlightTrack", .08f);
            material.SetFloat("_RouteSpacing", 1f / (route.Platforms.Length - 1));
            material.SetColor("_AtmoColor", recipe.Biome ? recipe.Biome.FogColor : new Color(.72f, .82f, .88f));
            material.SetFloat("_AtmoDensity", .00045f);
            var mass = new CrownRockMesh();
            mass.Massif(recipe, route);
            Emit(resources, mass.Build("Корона — массив"), material);
            var random = new System.Random(recipe.SeedFor(2));
            for (int start = 0; start < route.Platforms.Length; start += 16)
            {
                var mesh = new CrownRockMesh();
                int end = Mathf.Min(start + 16, route.Platforms.Length);
                for (int i = start; i < end; i++)
                {
                    bool jumpEdge = i > 0 && route.Connections[i - 1] == RouteConnection.Jump
                        || i < route.Connections.Length && route.Connections[i] == RouteConnection.Jump;
                    if (route.Platforms[i].IsCheckpoint || jumpEdge)
                        mesh.Shelf(route.Platforms[i], recipe.ValleyHeight - 15f, random);
                    if (i + 1 < route.Platforms.Length && route.Connections[i] == RouteConnection.Walk)
                    {
                        LevelChallenge challenge = recipe.Episodes[route.Episodes[i]].Challenge;
                        float width = challenge == LevelChallenge.Ledges ? .8f : challenge == LevelChallenge.Climbing ? .9f : 1.6f;
                        mesh.Connection(route.Platforms[i], route.Platforms[i + 1], width);
                    }
                }
                Emit(resources, mesh.Build("Корона — уступы " + start), material);
            }
            var backdrop = new CrownRockMesh();
            backdrop.Backdrop(recipe);
            var scenery = resources.Own(new Material(material));
            scenery.SetFloat("_HighlightTrack", 0f); scenery.SetFloat("_SnowStart", .75f);
            Emit(resources, backdrop.Build("Дальние склоны"), scenery);
            BuildDecor(resources, recipe);
            BuildFinish(resources);
        }

        private static void BuildFinish(LevelGeneratedResources resources)
        {
            var marker = new GameObject("Финиш — два флага");
            marker.transform.SetParent(resources.transform, false);
            marker.transform.localPosition = resources.Route.Finish;
            var wood = resources.Own(new Material(Shader.Find("Standard")) { color = new Color(.32f, .22f, .12f) });
            var cloth = resources.Own(new Material(Shader.Find("Standard")) { color = new Color(1f, .7f, .16f) });
            var flag = new CrownRockMesh();
            Color color = Color.white;
            flag.Triangle(Vector3.zero, Vector3.right * 1.5f, Vector3.down * .8f, color, color, color);
            flag.Triangle(Vector3.zero, Vector3.down * .8f, Vector3.right * 1.5f, color, color, color);
            var mesh = resources.Own(flag.Build("Финишный флаг"));
            for (int i = 0; i < 2; i++)
            {
                Vector3 position = new Vector3(i == 0 ? -3f : 3f, 0f, 1.2f);
                Part(marker.transform, "Древко", DecorMeshLibrary.TrunkMesh(), wood, position, new Vector3(.15f, 4f, .15f));
                Part(marker.transform, "Флаг", mesh, cloth, position + Vector3.up * 3.6f, Vector3.one);
            }
        }

        public static Material RouteMaterial(LevelGeneratedResources resources)
        {
            return resources.GetComponentInChildren<MeshRenderer>().sharedMaterial;
        }

        private static void Emit(LevelGeneratedResources resources, Mesh mesh, Material material)
        {
            resources.Own(mesh);
            var part = new GameObject(mesh.name);
            part.transform.SetParent(resources.transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
            part.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void BuildDecor(LevelGeneratedResources resources, LevelRecipe recipe)
        {
            var random = new System.Random(recipe.SeedFor(3));
            float density = recipe.Biome ? recipe.Biome.TreeDensity : .65f;
            var foliage = resources.Own(new Material(Shader.Find("Standard")));
            foliage.color = recipe.Biome ? recipe.Biome.FoliageColor : new Color(.22f, .38f, .14f);
            var trunk = resources.Own(new Material(Shader.Find("Standard")) { color = new Color(.3f, .22f, .12f) });
            var root = new GameObject("Сосны на уступах");
            root.transform.SetParent(resources.transform, false);
            TerraceTrees(resources, recipe, root.transform, foliage, trunk);
            for (int i = 0; i < resources.Route.Platforms.Length; i++)
            {
                var point = resources.Route.Platforms[i];
                if (point.Radius < 2.5f || random.NextDouble() > density || i < 3 || i >= resources.Route.Platforms.Length - 2) continue;
                Vector3 radial = -Vector3.forward;
                Vector3 position = point.Center - radial * point.Radius * .78f;
                if (!ClearOfConnections(resources.Route, position, 1.2f)) continue;
                float height = Mathf.Lerp(3.5f, 6.5f, (float)random.NextDouble());
                var tree = new GameObject("Сосна " + i);
                tree.transform.SetParent(root.transform, false); tree.transform.localPosition = position;
                Part(tree.transform, "Ствол", DecorMeshLibrary.TrunkMesh(), trunk, Vector3.zero, new Vector3(.3f, height, .3f));
                for (int tier = 0; tier < 3; tier++)
                    Part(tree.transform, "Хвоя", DecorMeshLibrary.PineMesh(), foliage,
                        Vector3.up * height * (.25f + tier * .2f), new Vector3(height * (.8f - tier * .16f), height * .55f, height * (.8f - tier * .16f)));
            }
        }
        private static void TerraceTrees(LevelGeneratedResources resources, LevelRecipe recipe, Transform root, Material foliage, Material trunk)
        {
            var random = new System.Random(recipe.SeedFor(9));
            var surface = resources.GetComponentInChildren<MeshCollider>();
            float density = recipe.Biome ? recipe.Biome.TreeDensity : .65f;
            for (int i = 0; i < 500; i++)
            {
                if (random.NextDouble() > density) continue;
                float x = Mathf.Lerp(-recipe.Radius * 1.15f, recipe.Radius * 1.15f, (float)random.NextDouble());
                float z = Mathf.Lerp(-recipe.Radius, recipe.Radius, (float)random.NextDouble());
                var ray = new Ray(new Vector3(x, recipe.SummitHeight * 2f, z), Vector3.down);
                if (!surface.Raycast(ray, out var hit, recipe.SummitHeight * 3f) || hit.normal.y < .94f
                    || hit.point.y < recipe.ValleyHeight + 20f || !ClearOfConnections(resources.Route, hit.point, 4f)) continue;
                float height = Mathf.Lerp(5f, 10f, (float)random.NextDouble());
                var tree = new GameObject("Сосна на скальной террасе " + i);
                tree.transform.SetParent(root, false); tree.transform.localPosition = hit.point;
                Part(tree.transform, "Ствол", DecorMeshLibrary.TrunkMesh(), trunk, Vector3.zero, new Vector3(.35f, height, .35f));
                for (int tier = 0; tier < 3; tier++)
                    Part(tree.transform, "Хвоя", DecorMeshLibrary.PineMesh(), foliage,
                        Vector3.up * height * (.25f + tier * .2f), new Vector3(height * (.62f - tier * .12f), height * .5f, height * (.62f - tier * .12f)));
            }
        }

        private static bool ClearOfConnections(LevelRoute route, Vector3 position, float radius)
        {
            for (int i = 0; i + 1 < route.Platforms.Length; i++)
            {
                Vector3 a = route.Platforms[i].Center, b = route.Platforms[i + 1].Center;
                Vector3 delta = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(position - a, delta) / delta.sqrMagnitude);
                if ((position - (a + delta * t)).sqrMagnitude < radius * radius) return false;
            }
            return true;
        }

        private static void Part(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale)
        {
            var part = new GameObject(name); part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
