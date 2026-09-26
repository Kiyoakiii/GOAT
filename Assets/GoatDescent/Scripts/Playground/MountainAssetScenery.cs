using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    // Authored Kenney models, arranged deterministically around the playable heightfield.
    public static class MountainAssetScenery
    {
        public static void Build(Transform mountain, MountainArt art)
        {
            var root = new GameObject("KENNEY — alpine forest and crags").transform;
            root.SetParent(mountain, false);
            var trees = Load("tree_pineTallA_detailed", "tree_pineTallB_detailed",
                "tree_pineTallC_detailed", "tree_pineTallD_detailed", "tree_pineSmallA");
            var rocks = Load("rock_largeA", "rock_largeB", "rock_largeC", "rock_largeD",
                "rock_tallA", "rock_tallC", "rock_tallF", "stone_smallFlatA");
            var needles = art.Mat(new Color(.14f, .29f, .27f));
            var bark = art.Mat(new Color(.27f, .22f, .19f));
            var snow = art.Mat(new Color(.57f, .66f, .69f));
            var stone = art.Mat(new Color(.31f, .36f, .39f));
            var random = new System.Random(7319);

            // Keep the central descent and branching gullies clear. Groups are deliberately
            // uneven, with gaps, rather than two repeating rows lining an artificial track.
            for (int i = 0; i < 110; i++)
            {
                float z = Range(random, -77f, 325f);
                float side = random.Next(2) == 0 ? -1f : 1f;
                float x = SteepMountain.Center(z) + side * Range(random, 43f, 78f);
                if (Mathf.Abs(x) > 94f) continue;
                bool tree = z > 165f ? random.NextDouble() < .8 : random.NextDouble() < .34;
                float height = tree ? Range(random, 6f, 13f) : Range(random, 5f, 16f);
                Place(tree ? trees : rocks, x, z, height, tree, random, root, needles, bark, snow, stone);
                if (!tree && i % 3 == 0)
                    Place(rocks, x + side * 5f, z + 3f, height * .45f, false,
                        random, root, needles, bark, snow, stone);
            }

            // A composed summit immediately shows the new art from the starting camera.
            foreach (float side in new[] { -1f, 1f })
            {
                Place(trees, SteepMountain.Center(-66f) + side * 16f, -72f, 10f, true,
                    random, root, needles, bark, snow, stone);
                Place(rocks, SteepMountain.Center(-55f) + side * 26f, -55f, 12f, false,
                    random, root, needles, bark, snow, stone);
                Place(trees, SteepMountain.Center(-55f) + side * 30f, -51f, 8f, true,
                    random, root, needles, bark, snow, stone);
            }
        }

        private static List<GameObject> Load(params string[] names)
        {
            var result = new List<GameObject>();
            foreach (string name in names)
            {
                var model = Resources.Load<GameObject>("KenneyNature/" + name);
                if (model) result.Add(model);
                else Debug.LogWarning("Missing Kenney scenery model: " + name);
            }
            return result;
        }

        private static float Range(System.Random random, float min, float max)
            => Mathf.Lerp(min, max, (float)random.NextDouble());

        private static void Place(List<GameObject> models, float x, float z, float height,
            bool tree, System.Random random, Transform parent, Material needles, Material bark,
            Material snow, Material stone)
        {
            if (models.Count == 0) return;
            var instance = Object.Instantiate(models[random.Next(models.Count)], parent);
            instance.name = (tree ? "Alpine pine — " : "Snowy crag — ") + instance.name.Replace("(Clone)", "");
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, Range(random, 0f, 360f), 0f);
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Object.Destroy(instance); return; }
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = height / Mathf.Max(.01f, bounds.size.y);
            instance.transform.localScale *= scale;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string name = materials[i] ? materials[i].name.ToLowerInvariant() : "";
                    materials[i] = tree ? (name.Contains("wood") ? bark : needles)
                        : (name.Contains("grass") ? snow : stone);
                }
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            // Bury the foot slightly so imported pivots never leave floating trees or stones.
            instance.transform.position += new Vector3(x - bounds.center.x,
                SteepMountain.HeightAt(x, z) - bounds.min.y - height * (tree ? .025f : .18f),
                z - bounds.center.z);
            foreach (var collider in instance.GetComponentsInChildren<Collider>())
                collider.enabled = false;
        }
    }
}
