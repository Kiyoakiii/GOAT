using UnityEngine;

namespace GoatDescent
{
    /// <summary>Editable, rounded eagle silhouette with individually articulated feather fans.</summary>
    public static class CartoonEagleVisual
    {
        private static readonly string[] Variants =
            { "01_Captain", "02_Dumpling", "03_Punk", "04_Frost", "05_Bandit" };

        public static void Build(Transform root, Material[] palette, int index, out Transform left, out Transform right)
        {
            string variant = Variants[Mathf.Abs(index) % Variants.Length];
            var prefab = Resources.Load<GameObject>("Eagles/Eagle_" + variant);
            if (prefab)
            {
                var model = Object.Instantiate(prefab, root, false);
                model.name = "Blender eagle " + variant;
                foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                    renderer.sharedMaterial = palette[6];
                left = WingPivot(root, model.transform, "Left wing");
                right = WingPivot(root, model.transform, "Right wing");
                return;
            }
            Debug.LogWarning("Missing Blender eagle resource: " + variant);
            Part(root, "Round chest", new Vector3(0, 0, -.1f), new Vector3(.85f, .72f, 1.5f), palette[0]);
            Part(root, "Cream chest bib", new Vector3(0, -.12f, .39f), new Vector3(.63f, .58f, .65f), palette[3]);
            Part(root, "Ivory head", new Vector3(0, .35f, .67f), new Vector3(.78f, .7f, .76f), palette[3]);
            Part(root, "Golden beak", new Vector3(0, .24f, 1.12f), new Vector3(.36f, .29f, .5f), palette[2]);
            Part(root, "Hooked beak tip", new Vector3(0, .08f, 1.3f), new Vector3(.23f, .35f, .2f), palette[2]);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(root, "Eye white", new Vector3(side * .31f, .41f, .89f), new Vector3(.26f, .29f, .22f), palette[4]);
                Part(root, "Amber iris", new Vector3(side * .34f, .4f, .982f), new Vector3(.14f, .18f, .09f), palette[2]);
                Part(root, "Pupil", new Vector3(side * .34f, .4f, 1.02f), new Vector3(.07f, .11f, .045f), palette[1]);
                Part(root, "Eye glint", new Vector3(side * .33f, .44f, 1.047f), Vector3.one * .038f, palette[4]);
                var brow = Part(root, "Determined brow", new Vector3(side * .28f, .54f, .96f), new Vector3(.36f, .09f, .14f), palette[0]);
                brow.localRotation = Quaternion.Euler(0, 0, side * 16f);
                var wing = new GameObject(side < 0 ? "Left wing" : "Right wing").transform;
                wing.SetParent(root, false); wing.localPosition = new Vector3(side * .33f, .14f, 0);
                Part(wing, "Wing shoulder", new Vector3(side * .6f, 0, -.08f), new Vector3(1.4f, .25f, .83f), palette[0]);
                for (int i = 0; i < 7; i++)
                {
                    var feather = Part(wing, "Primary feather " + i,
                        new Vector3(side * (1.02f + i * .18f), -.025f, -.32f - i * .035f),
                        new Vector3(.3f, .09f, 1.05f - Mathf.Abs(i - 3) * .06f), i % 2 == 0 ? palette[1] : palette[5]);
                    feather.localRotation = Quaternion.Euler(0, side * (12 + i * 6), side * -4);
                }
                Part(root, "Tucked leg", new Vector3(side * .23f, -.39f, .12f), new Vector3(.19f, .36f, .19f), palette[2]);
                for (int toe = 0; toe < 3; toe++)
                {
                    Part(root, "Curled talon", new Vector3(side * .23f + (toe - 1) * .08f, -.58f, .22f), new Vector3(.055f, .1f, .3f), palette[2]);
                    Part(root, "Talon tip", new Vector3(side * .23f + (toe - 1) * .08f, -.62f, .35f), new Vector3(.045f, .12f, .065f), palette[1]);
                }
            }
            for (int i = -2; i <= 2; i++)
            {
                var tail = Part(root, "Tail feather", new Vector3(i * .13f, -.02f, -.99f), new Vector3(.25f, .09f, .8f), palette[3]);
                tail.localRotation = Quaternion.Euler(-10, i * 9, 0);
            }
            left = root.Find("Left wing"); right = root.Find("Right wing");
        }

        private static Transform WingPivot(Transform root, Transform model, string name)
        {
            Transform imported = null;
            foreach (var child in model.GetComponentsInChildren<Transform>())
                if (child.name == name) { imported = child; break; }
            if (!imported) throw new System.InvalidOperationException("Missing eagle wing: " + name);
            // FBX basis conversion can rotate the empty's local axes. A canonical pivot
            // lets the existing Z-axis wing animation lift the wings in Unity space.
            var pivot = new GameObject(name + " flight pivot").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = root.InverseTransformPoint(imported.position);
            imported.SetParent(pivot, true);
            return pivot;
        }

        private static Transform Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            var collider = part.GetComponent<Collider>(); collider.enabled = false;
            Object.Destroy(collider);
            return part.transform;
        }

        public static void ShedFeathers(Transform bird, Material material)
        {
            var cloud = new GameObject("Eagle impact feathers");
            cloud.transform.SetPositionAndRotation(bird.position + bird.forward * .65f, bird.rotation);
            var particles = cloud.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false; main.duration = .1f; main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(.13f, .28f);
            main.gravityModifier = .3f; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startRotationY = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            var emission = particles.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 22) });
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .6f;
            var rotation = particles.rotationOverLifetime; rotation.enabled = true; rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-4, 4); rotation.z = new ParticleSystem.MinMaxCurve(-3, 3);
            var noise = particles.noise; noise.enabled = true; noise.strength = .35f; noise.frequency = .9f;
            var fade = particles.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] {new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1)},
                new[] {new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .65f), new GradientAlphaKey(0, 1)});
            fade.color = gradient;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.sharedMaterial = material;
            // A pointed feather vane and thin stem; double-sided for tumbling in flight.
            var mesh = new Mesh { name = "Falling eagle feather" };
            mesh.vertices = new[] {new Vector3(0,0,-.7f), new Vector3(-.24f,0,-.25f),
                new Vector3(-.18f,0,.2f), new Vector3(0,0,.7f), new Vector3(.18f,0,.2f), new Vector3(.24f,0,-.25f)};
            mesh.triangles = new[] {0,1,5,1,2,5,2,4,5,2,3,4,5,1,0,5,2,1,5,4,2,4,3,2};
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); renderer.mesh = mesh;
            particles.Play(); Object.Destroy(mesh, 3.2f); Object.Destroy(cloud, 3.2f);
        }
    }
}
