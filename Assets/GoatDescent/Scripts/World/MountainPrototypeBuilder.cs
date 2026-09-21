using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public sealed class MountainPrototypeBuilder : MonoBehaviour
    {
        private Transform mountain, ledges, vegetation, scenery;
        private Material rockLight, rockMid, rockDark, ledgeTop, grass, trunk, needles, cloud, skyGradient, trailMarker;
        private Vector3 finishPosition;
        // Summit-to-basin height is roughly 136 m; the playable descent itself is ~132 m.
        public static readonly Vector3 SpawnPoint = new Vector3(0f, 133.5f, 0f);

        private void Awake()
        {
            CreateMaterials();
            mountain = new GameObject("MountainMass").transform; mountain.SetParent(transform);
            ledges = new GameObject("Ledges — guaranteed descent route").transform; ledges.SetParent(transform);
            vegetation = new GameObject("Vegetation").transform; vegetation.SetParent(transform);
            scenery = new GameObject("Atmosphere and distant ranges").transform; scenery.SetParent(transform);
            BuildMountain();
            BuildRoute();
            BuildVegetation();
            BuildScenery();
            BuildFinish();
            CreatePlayer();
            CreateLighting();
        }

        private void CreateMaterials()
        {
            rockLight = MakeRockMaterial("Sunlit granite", new Color(.18f, .28f, .34f), new Color(.52f, .62f, .62f));
            rockMid = MakeRockMaterial("Blue granite", new Color(.07f, .18f, .27f), new Color(.30f, .47f, .57f));
            rockDark = MakeRockMaterial("Shadow granite", new Color(.035f, .10f, .16f), new Color(.14f, .29f, .38f));
            ledgeTop = MakeRockMaterial("Sunlit route stone", new Color(.16f, .29f, .12f), new Color(.48f, .63f, .26f));
            grass = MakeRockMaterial("Alpine grass", new Color(.11f, .20f, .09f), new Color(.36f, .49f, .20f), false);
            trunk = MakeMaterial("Pine trunk", new Color(.18f, .10f, .06f));
            needles = MakeMaterial("Pine needles", new Color(.08f, .22f, .15f));
            cloud = MakeMaterial("Sunlit cloud", new Color(.91f, .96f, .95f));
            trailMarker = MakeMaterial("Warm trail marker", new Color(.96f, .31f, .07f));
            var skyShader = Shader.Find("GoatDescent/Stylized Sky");
            if (skyShader != null)
            {
                skyGradient = new Material(skyShader) { name = "Alpine gradient sky" };
                skyGradient.SetColor("_HorizonColor", new Color(.31f, .63f, .84f));
                skyGradient.SetColor("_ZenithColor", new Color(.035f, .15f, .38f));
                skyGradient.SetColor("_SunColor", new Color(1f, .68f, .30f));
                skyGradient.SetVector("_SunDirection", new Vector4(-.42f, .62f, .45f, 0f));
            }
        }

        private Material MakeMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Glossiness", .12f);
            return material;
        }

        private Material MakeRockMaterial(string name, Color shadow, Color sunlit, bool receivesSnow = true)
        {
            var shader = Shader.Find("GoatDescent/Stylized Rock");
            if (shader == null) return MakeMaterial(name, sunlit);
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", shadow);
            material.SetColor("_AccentColor", sunlit);
            material.SetColor("_SnowColor", new Color(.88f, .95f, .97f));
            material.SetFloat("_SnowHeight", receivesSnow ? 108f : 10000f);
            material.SetFloat("_SnowBlend", 11f);
            return material;
        }

        private void BuildMountain()
        {
            // A single terraced shell reads as a climbable mountain, not a collection of floating primitive cones.
            CreateMountainShell();
            CreateLandingRock("Summit", new Vector3(0, 131.4f, 0), new Vector3(6f, 1f, 5.5f), ledgeTop, 8f);
            CreateFlatRock("Base basin", new Vector3(0, -3f, 0), new Vector3(155f, 3f, 145f), grass, 0f);
        }

        private void CreateMountainShell()
        {
            var rings = new[]
            {
                new Vector2(-2f, 82f), new Vector2(20f, 65f), new Vector2(48f, 44f), new Vector2(75f, 31f),
                new Vector2(98f, 22f), new Vector2(114f, 10f), new Vector2(124f, 6f), new Vector2(130f, 3f)
            };
            const int sides = 14;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int ring = 0; ring < rings.Length; ring++)
            {
                for (int side = 0; side < sides; side++)
                {
                    float a = (side * 360f / sides + ring * 7f) * Mathf.Deg2Rad;
                    float noise = .86f + Mathf.PerlinNoise(side * .41f, ring * .73f) * .25f;
                    float yJitter = ring == 0 || ring == rings.Length - 1 ? 0f : (Mathf.PerlinNoise(side * .29f + 7f, ring * .53f) - .5f) * 5f;
                    vertices.Add(new Vector3(Mathf.Cos(a) * rings[ring].y * noise, rings[ring].x + yJitter, Mathf.Sin(a) * rings[ring].y * noise));
                }
            }
            for (int ring = 0; ring < rings.Length - 1; ring++)
            {
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    int a = ring * sides + side, b = ring * sides + next, c = (ring + 1) * sides + side, d = (ring + 1) * sides + next;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }
            int summit = vertices.Count;
            vertices.Add(new Vector3(0f, 131.6f, 0f));
            int topRing = (rings.Length - 1) * sides;
            for (int side = 0; side < sides; side++) { int next = (side + 1) % sides; triangles.Add(topRing + side); triangles.Add(summit); triangles.Add(topRing + next); }
            var mesh = new Mesh { name = "Faceted alpine mountain mesh" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            var mountainObject = new GameObject("Faceted alpine mountain shell"); mountainObject.transform.SetParent(mountain);
            mountainObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            mountainObject.AddComponent<MeshRenderer>().sharedMaterial = rockMid;
            mountainObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private void BuildRoute()
        {
            // A gently widening spiral follows the surface of the shell. Consecutive landings stay inside the air-control envelope.
            var route = new Vector3[19];
            for (int i = 0; i < route.Length; i++)
            {
                float a = (117f + i * 8f) * Mathf.Deg2Rad;
                float radius = 9f + i * 3.8f;
                route[i] = new Vector3(Mathf.Cos(a) * radius, 126f - i * 7f, Mathf.Sin(a) * radius);
            }
            finishPosition = route[route.Length - 1];
            for (int i = 0; i < route.Length; i++)
            {
                float width = i == 3 || i == 7 ? 2.5f : 3.6f;
                CreateLedge("Route ledge " + (i + 1), route[i], new Vector3(width, .9f, width * .8f), i % 3 == 0 ? ledgeTop : rockLight, i * 23f);
                if (i > 0 && i % 3 == 0) CreateTrailMarker(route[i], i * 23f);
                if (i < route.Length - 2 && i % 2 == 0)
                {
                    Vector3 optional = Vector3.Lerp(route[i], route[i + 2], .58f) + new Vector3(i % 4 == 0 ? 5f : -5f, 1f, -3f);
                    CreateLedge("Risk shortcut " + (i + 1), optional, new Vector3(2.1f, .7f, 1.8f), rockLight, i * 37f);
                }
            }
        }

        private void BuildVegetation()
        {
            for (int i = 0; i < 24; i++)
            {
                float a = i * 53f * Mathf.Deg2Rad; float r = 38f + (i % 5) * 8f;
                CreatePine(new Vector3(Mathf.Cos(a) * r, -1.5f, Mathf.Sin(a) * r), 4f + (i % 3) * 1.7f);
            }
            for (int i = 0; i < 48; i++)
            {
                float a = i * 71f * Mathf.Deg2Rad;
                float r = 18f + (i % 7) * 11f;
                CreateAlpineTuft(new Vector3(Mathf.Cos(a) * r, -.6f, Mathf.Sin(a) * r), .45f + (i % 4) * .13f, i * 19f);
            }
        }

        private void BuildScenery()
        {
            CreateSkyDome();
            // Cloud banks stay above the descent line and leave the navigation view uncluttered.
            var cloudPositions = new[]
            {
                new Vector3(5f, 160f, 48f), new Vector3(-38f, 164f, 92f), new Vector3(58f, 152f, 92f),
                new Vector3(-108f, 145f, -18f), new Vector3(30f, 174f, 165f)
            };
            for (int i = 0; i < cloudPositions.Length; i++) CreateCloudBank(cloudPositions[i], 1f + (i % 3) * .18f, i * 29f);
        }

        private void CreateSkyDome()
        {
            if (skyGradient == null) return;
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "Stylized alpine sky dome";
            dome.transform.SetParent(scenery);
            dome.transform.localPosition = new Vector3(0f, 70f, 0f);
            dome.transform.localScale = Vector3.one * 620f;
            dome.GetComponent<Renderer>().sharedMaterial = skyGradient;
            Destroy(dome.GetComponent<Collider>());
        }

        private void CreateCloudBank(Vector3 center, float scale, float rotation)
        {
            var offsets = new[] { new Vector3(-5f, 0f, 0f), new Vector3(0f, 1.4f, 1f), new Vector3(5f, -.3f, -.5f) };
            for (int i = 0; i < offsets.Length; i++)
            {
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "Sunlit cloud puff";
                puff.transform.SetParent(scenery);
                puff.transform.SetPositionAndRotation(center + offsets[i] * scale, Quaternion.Euler(0f, rotation + i * 17f, 0f));
                puff.transform.localScale = new Vector3(11f, 5f, 7f) * scale;
                puff.GetComponent<Renderer>().sharedMaterial = cloud;
                Destroy(puff.GetComponent<Collider>());
            }
        }

        private void BuildFinish()
        {
            var finish = CreateLandingRock("Finish meadow", finishPosition + new Vector3(0f, -.8f, 0f), new Vector3(12, .5f, 10), grass, 12);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder); marker.name = "Finish cairn"; marker.transform.SetPositionAndRotation(finishPosition + Vector3.up * .8f, Quaternion.identity); marker.transform.localScale = new Vector3(.45f, 1.8f, .45f); marker.GetComponent<Renderer>().sharedMaterial = rockLight;
            finish.transform.SetParent(ledges);
        }

        private void CreatePlayer()
        {
            var goat = new GameObject("Mountain Goat"); goat.transform.position = SpawnPoint;
            int playerLayer = LayerMask.NameToLayer("Player"); if (playerLayer >= 0) goat.layer = playerLayer;
            var body = goat.AddComponent<Rigidbody>(); body.mass = 72f; body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.constraints = RigidbodyConstraints.FreezeRotation;
            var capsule = goat.AddComponent<CapsuleCollider>(); capsule.radius = .48f; capsule.height = 1.15f; capsule.center = new Vector3(0, .58f, 0);
            var detector = goat.AddComponent<GoatGroundDetector>();
            var controller = goat.AddComponent<GoatController>(); controller.Configure(body, detector);
            goat.AddComponent<GoatJumpController>().Configure(controller, detector);
            goat.AddComponent<GoatLandingAssist>().Configure(body, detector);
            goat.AddComponent<GoatVisualController>().Configure(body, detector);
            goat.AddComponent<RespawnController>().Configure(body, SpawnPoint);
            var cam = new GameObject("Third Person Goat Camera"); cam.tag = "MainCamera";
            var camera = cam.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.29f, .58f, .82f);
            cam.AddComponent<ThirdPersonGoatCamera>().Configure(goat.transform);
        }

        private void CreateLighting()
        {
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetColor("_SkyTint", new Color(.38f, .61f, .82f));
                sky.SetColor("_GroundColor", new Color(.31f, .35f, .38f));
                sky.SetFloat("_AtmosphereThickness", .72f);
                sky.SetFloat("_Exposure", 1.15f);
                RenderSettings.skybox = sky;
            }
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.54f, .66f, .72f);
            RenderSettings.fogDensity = .0045f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.45f, .62f, .78f);
            RenderSettings.ambientEquatorColor = new Color(.38f, .43f, .38f);
            RenderSettings.ambientGroundColor = new Color(.15f, .17f, .19f);
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 95f;
            QualitySettings.shadowResolution = ShadowResolution.High;
            var sun = new GameObject("Mountain Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.2f; sun.color = new Color(1f, .86f, .64f); sun.shadows = LightShadows.Soft; sun.shadowStrength = .72f; sun.shadowBias = .04f; sun.shadowNormalBias = .3f; sun.transform.rotation = Quaternion.Euler(48, -35, 0);
        }

        private GameObject CreateFlatRock(string name, Vector3 position, Vector3 scale, Material mat, float yRotation)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube); rock.name = name; rock.transform.SetParent(ledges); rock.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yRotation, 0)); rock.transform.localScale = scale; rock.GetComponent<Renderer>().sharedMaterial = mat; return rock;
        }

        private void CreateLedge(string name, Vector3 p, Vector3 s, Material mat, float rotation)
        {
            Vector3 radial = Vector3.ProjectOnPlane(p, Vector3.up).normalized;
            if (radial.sqrMagnitude > .01f)
            {
                // An inset slab overlaps the shell and the landing, reading as a carved terrace rather than a floating block.
                float radialYaw = 90f - Mathf.Atan2(radial.z, radial.x) * Mathf.Rad2Deg;
                var bridge = CreateFlatRock(name + " carved terrace", p - radial * 1.9f - Vector3.up * .12f, new Vector3(s.x * 1.25f, .62f, 5.1f), rockMid, radialYaw);
                bridge.transform.SetParent(ledges);
            }
            var ledge = CreateLandingRock(name, p, s, mat, rotation); ledge.transform.SetParent(ledges);
            // A broad top and downward taper visually anchor the landing to the mountain instead of creating an upward spike.
            var support = CreateRock(name + " attached rock shelf", p + new Vector3(0, -3.4f, 0), new Vector3(s.x * 1.45f, 6.8f, s.z * 1.45f), rockDark, 5, rotation);
            support.transform.SetParent(ledges);
            support.transform.localRotation = Quaternion.Euler(180f, rotation, 0f);
        }

        private GameObject CreateRock(string name, Vector3 position, Vector3 size, Material material, int sides, float rotation, bool createsCollider = true)
        {
            var obj = new GameObject(name); obj.transform.SetParent(mountain); obj.transform.SetPositionAndRotation(position, Quaternion.Euler(0, rotation, 0));
            var mesh = new Mesh { name = name + " mesh" }; var vertices = new List<Vector3>(); var triangles = new List<int>();
            float bottom = -size.y * .5f, top = size.y * .5f;
            vertices.Add(new Vector3(0, top, 0)); vertices.Add(new Vector3(0, bottom, 0));
            for (int i = 0; i < sides; i++) { float a = i * Mathf.PI * 2f / sides; float wobble = .78f + ((i * 17) % 5) * .055f; vertices.Add(new Vector3(Mathf.Cos(a) * size.x * .5f * wobble, bottom, Mathf.Sin(a) * size.z * .5f * wobble)); }
            for (int i = 0; i < sides; i++) { int n = i == sides - 1 ? 0 : i + 1; triangles.Add(0); triangles.Add(2 + i); triangles.Add(2 + n); triangles.Add(1); triangles.Add(2 + i); triangles.Add(2 + n); }
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (createsCollider) obj.AddComponent<MeshCollider>().sharedMesh = mesh;
            return obj;
        }

        private GameObject CreateLandingRock(string name, Vector3 position, Vector3 size, Material material, float yRotation)
        {
            const int sides = 7;
            var vertices = new List<Vector3> { new Vector3(0f, size.y * .5f, 0f), new Vector3(0f, -size.y * .5f, 0f) };
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                float wobble = .86f + ((i * 19 + name.Length * 7) % 7) * .035f;
                float x = Mathf.Cos(angle) * size.x * .5f * wobble;
                float z = Mathf.Sin(angle) * size.z * .5f * wobble;
                vertices.Add(new Vector3(x, size.y * .5f, z));
                vertices.Add(new Vector3(x * 1.12f, -size.y * .5f, z * 1.12f));
            }
            var triangles = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                int top = 2 + i * 2, bottom = top + 1, nextTop = 2 + next * 2, nextBottom = nextTop + 1;
                triangles.Add(0); triangles.Add(nextTop); triangles.Add(top);
                triangles.Add(top); triangles.Add(nextTop); triangles.Add(bottom);
                triangles.Add(bottom); triangles.Add(nextTop); triangles.Add(nextBottom);
                triangles.Add(1); triangles.Add(bottom); triangles.Add(nextBottom);
            }
            var mesh = new Mesh { name = name + " low-poly landing mesh" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            var rock = new GameObject(name); rock.transform.SetParent(ledges); rock.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yRotation, 0f));
            rock.AddComponent<MeshFilter>().sharedMesh = mesh;
            rock.AddComponent<MeshRenderer>().sharedMaterial = material;
            rock.AddComponent<MeshCollider>().sharedMesh = mesh;
            return rock;
        }

        private void CreatePine(Vector3 p, float h)
        {
            var root = new GameObject("Alpine pine"); root.transform.SetParent(vegetation); root.transform.position = p;
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder); stem.transform.SetParent(root.transform); stem.transform.localPosition = Vector3.up * h * .22f; stem.transform.localScale = new Vector3(.22f, h * .22f, .22f); stem.GetComponent<Renderer>().sharedMaterial = trunk; Destroy(stem.GetComponent<Collider>());
            for (int i = 0; i < 3; i++) { var crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder); crown.transform.SetParent(root.transform); crown.transform.localPosition = Vector3.up * (h * (.32f + i * .19f)); crown.transform.localScale = new Vector3(h * (.28f - i * .06f), h * .26f, h * (.28f - i * .06f)); crown.GetComponent<Renderer>().sharedMaterial = needles; Destroy(crown.GetComponent<Collider>()); }
        }

        private void CreateAlpineTuft(Vector3 position, float size, float rotation)
        {
            var tuft = new GameObject("Alpine grass tuft"); tuft.transform.SetParent(vegetation); tuft.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation, 0f));
            for (int i = 0; i < 3; i++)
            {
                var blade = GameObject.CreatePrimitive(PrimitiveType.Cube); blade.transform.SetParent(tuft.transform);
                blade.transform.localPosition = new Vector3((i - 1) * size * .22f, size * .35f, (i % 2 == 0 ? .05f : -.08f));
                blade.transform.localRotation = Quaternion.Euler((i - 1) * 13f, i * 29f, (i - 1) * 16f);
                blade.transform.localScale = new Vector3(size * .08f, size * .7f, size * .05f);
                blade.GetComponent<Renderer>().sharedMaterial = grass;
                Destroy(blade.GetComponent<Collider>());
            }
        }

        private void CreateTrailMarker(Vector3 position, float rotation)
        {
            var marker = new GameObject("Warm route marker"); marker.transform.SetParent(ledges); marker.transform.SetPositionAndRotation(position + Vector3.up * .55f, Quaternion.Euler(0f, rotation, 0f));
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder); pole.transform.SetParent(marker.transform); pole.transform.localPosition = Vector3.up * .38f; pole.transform.localScale = new Vector3(.045f, .42f, .045f); pole.GetComponent<Renderer>().sharedMaterial = trailMarker; Destroy(pole.GetComponent<Collider>());
            var pennant = GameObject.CreatePrimitive(PrimitiveType.Cube); pennant.transform.SetParent(marker.transform); pennant.transform.localPosition = new Vector3(.23f, .62f, 0f); pennant.transform.localScale = new Vector3(.45f, .24f, .03f); pennant.GetComponent<Renderer>().sharedMaterial = trailMarker; Destroy(pennant.GetComponent<Collider>());
        }
    }
}
