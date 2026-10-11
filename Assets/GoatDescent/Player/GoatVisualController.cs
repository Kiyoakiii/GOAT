using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    /// <summary>
    /// Lightweight, replaceable hero-goat model. The stylized meshes are combined
    /// by material so this runtime prototype stays to a small number of draw calls.
    /// </summary>
    public sealed class GoatVisualController : MonoBehaviour
    {
        private const string VisualName = "VisualRoot — replaceable goat model";

        private static Material coatMaterial;
        private static Material muzzleMaterial;
        private static Material hornMaterial;
        private static Material hoofMaterial;
        private static Material mouthMaterial;
        private static Material pinkMaterial;
        private static Material eyeWhiteMaterial;
        private static Material eyeDarkMaterial;
        private static Material sparkleMaterial;

        private Rigidbody body;
        private GoatGroundDetector ground;
        private Transform visual;
        private Animation importedAnimation;
        private string manualActionClip;
        private float manualActionUntil;
        private string activeAutomaticClip = "Goat_Idle";
        private bool surpriseEarMaskReady;
        private GUIStyle animationHelpStyle;
        private Vector3 interactionFacing;
        private string networkClip;
        private Vector3 networkFacing;
        private float faceUntil;
        public Vector3 Facing => visual ? visual.forward : transform.forward;
        public string CurrentClip => manualActionClip ?? activeAutomaticClip;
        public void SetJumpPreparation(bool value)
        {
            var motor = GetComponent<GoatController>();
            if (motor) motor.IsBracingForJump = value;
        }
        public void PlayTakeoff(float amount = .14f)
        {
            if (!importedAnimation || importedAnimation["Goat_Jump"] == null) return;
            var jump = importedAnimation["Goat_Jump"];
            jump.time = 0f;
            jump.wrapMode = WrapMode.ClampForever;
            importedAnimation.CrossFade("Goat_Jump", .08f, PlayMode.StopSameLayer);
            activeAutomaticClip = "Goat_Jump";
        }
        public void PlayLanding(float impact)
        {
            if (impact >= 7f) PlaySurpriseEarOverlay();
        }
        public void ApplyNetworkPose(string clip, Vector3 facing)
        {
            networkClip = clip;
            networkFacing = facing;
        }
        public void SetFacing(Vector3 direction)
        {
            interactionFacing = direction; faceUntil = Time.time + .1f;
            if (visual) visual.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
        public void FaceInteraction(Vector3 direction, float duration)
        { interactionFacing = direction; faceUntil = Time.time + duration; }
        public void PlayInteraction(string clip, float duration, Vector3 direction)
        {
            FaceInteraction(direction, duration);
            if (!importedAnimation || importedAnimation[clip] == null) return;
            importedAnimation.Stop("Goat_Surprise");
            var state = importedAnimation[clip];
            state.time = 0f; state.speed = 1f; state.wrapMode = WrapMode.Once;
            manualActionClip = clip; manualActionUntil = Time.time + duration;
            activeAutomaticClip = clip;
            importedAnimation.CrossFade(clip, .09f, PlayMode.StopSameLayer);
        }

        public void Configure(Rigidbody targetBody, GoatGroundDetector targetGround)
        {
            body = targetBody;
            ground = targetGround;
        }

        private void Awake()
        {
            body ??= GetComponent<Rigidbody>();
            ground ??= GetComponent<GoatGroundDetector>();
        }

        private void Start()
        {
            visual = transform.Find(VisualName);
            if (!visual)
                BuildVisual();

            if (visual)
                importedAnimation ??= visual.GetComponentInChildren<Animation>();
            if (importedAnimation)
                visual.localPosition = new Vector3(0f, -0.08f, 0f);
            if (visual && GetComponent<GoatLocalControl>()?.Label == "B")
            {
                var tint = new MaterialPropertyBlock();
                tint.SetColor("_Color", new Color(.72f, .46f, .26f, 1f));
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                        if (materials[i] && materials[i].name == "hideWhite") renderer.SetPropertyBlock(tint, i);
                }
            }

            if (importedAnimation != null)
            {
                foreach (AnimationState state in importedAnimation)
                    state.wrapMode = IsLoop(state.name) ? WrapMode.Loop : state.name == "Goat_Jump" ? WrapMode.ClampForever : WrapMode.Once;
                ConfigureSurpriseEarMask();
                if (!importedAnimation.isPlaying)
                    importedAnimation.Play("Goat_Idle");
            }
            if (interactionFacing.sqrMagnitude > .01f) SetFacing(interactionFacing);
        }
        private static bool IsLoop(string clip) => clip == "Goat_Idle" || clip == "Goat_Walk"
            || clip == "Goat_GrabHold" || clip == "Goat_GrabbedHold" || clip == "Goat_Pull"
            || clip == "Goat_Hang"
            || clip == "Goat_RescueBrace" || clip == "Goat_GripStrain"
            || clip == "Goat_FreeFall" || clip == "Goat_EagleCarry";

        private void BuildVisual()
        {
            var refined = Resources.Load<GameObject>("GoatDuoRefined");
            if (refined != null)
            {
                visual = Instantiate(refined, transform, false).transform;
                visual.name = VisualName;
                importedAnimation = visual.GetComponentInChildren<Animation>();
                return;
            }
            EnsureMaterials();
            visual = new GameObject(VisualName).transform;
            visual.SetParent(transform, false);
            visual.localPosition = new Vector3(0f, 0.31f, 0f);

            // A round, fleece-covered body and raised chest give the goat a clear
            // friendly silhouette even when viewed from the follow camera.
            Part(PrimitiveType.Sphere, "Soft wool body", new Vector3(0f, 0.25f, -0.08f), new Vector3(0.91f, 0.60f, 1.18f), coatMaterial);
            Part(PrimitiveType.Sphere, "Chest ruff", new Vector3(0f, 0.45f, 0.48f), new Vector3(0.75f, 0.72f, 0.67f), coatMaterial);

            // Small overlapping fleece locks break up the smooth primitive base.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    float z = 0.48f - i * 0.27f;
                    float y = 0.47f + (i % 2) * 0.11f;
                    Part(PrimitiveType.Sphere, "Fleece lock", new Vector3(side * 0.38f, y, z), new Vector3(0.25f, 0.23f, 0.28f), coatMaterial);
                }
            }

            // Head, rounded muzzle, tiny nose and a readable smile/tongue.
            Part(PrimitiveType.Sphere, "Bright white head", new Vector3(0f, 0.80f, 0.73f), new Vector3(0.71f, 0.67f, 0.68f), coatMaterial);
            Part(PrimitiveType.Sphere, "Velvety muzzle", new Vector3(0f, 0.59f, 1.08f), new Vector3(0.42f, 0.30f, 0.35f), muzzleMaterial);
            Part(PrimitiveType.Sphere, "Rose nose", new Vector3(0f, 0.73f, 1.385f), new Vector3(0.23f, 0.145f, 0.12f), pinkMaterial);
            Part(PrimitiveType.Sphere, "Nostril", new Vector3(-0.068f, 0.745f, 1.483f), new Vector3(0.032f, 0.025f, 0.018f), mouthMaterial);
            Part(PrimitiveType.Sphere, "Nostril", new Vector3(0.068f, 0.745f, 1.483f), new Vector3(0.032f, 0.025f, 0.018f), mouthMaterial);

            // Curved smile built from short rounded links so it reads from the front.
            TubeBetween("Smile", new Vector3(-0.19f, 0.535f, 1.385f), new Vector3(-0.095f, 0.49f, 1.405f), 0.018f, mouthMaterial);
            TubeBetween("Smile", new Vector3(-0.095f, 0.49f, 1.405f), new Vector3(0f, 0.48f, 1.41f), 0.018f, mouthMaterial);
            TubeBetween("Smile", new Vector3(0f, 0.48f, 1.41f), new Vector3(0.095f, 0.49f, 1.405f), 0.018f, mouthMaterial);
            TubeBetween("Smile", new Vector3(0.095f, 0.49f, 1.405f), new Vector3(0.19f, 0.535f, 1.385f), 0.018f, mouthMaterial);
            Part(PrimitiveType.Sphere, "Playful pink tongue", new Vector3(0f, 0.405f, 1.43f), new Vector3(0.14f, 0.19f, 0.095f), pinkMaterial);
            TubeBetween("Tongue crease", new Vector3(0f, 0.44f, 1.51f), new Vector3(0f, 0.38f, 1.515f), 0.009f, mouthMaterial);

            for (int side = -1; side <= 1; side += 2)
            {
                // Oversized bright eyes with a glint make the face legible at game scale.
                float eyeX = side * 0.265f;
                Part(PrimitiveType.Sphere, "Eye white", new Vector3(eyeX, 0.91f, 1.255f), new Vector3(0.14f, 0.17f, 0.09f), eyeWhiteMaterial);
                Part(PrimitiveType.Sphere, "Warm brown eye", new Vector3(eyeX, 0.90f, 1.333f), new Vector3(0.077f, 0.105f, 0.045f), eyeDarkMaterial);
                Part(PrimitiveType.Sphere, "Eye sparkle", new Vector3(eyeX - 0.02f, 0.95f, 1.369f), new Vector3(0.027f, 0.033f, 0.018f), sparkleMaterial);
                Part(PrimitiveType.Sphere, "Soft floppy ear", new Vector3(side * 0.50f, 0.91f, 0.61f), new Vector3(0.31f, 0.13f, 0.23f), coatMaterial)
                    .transform.localRotation = Quaternion.Euler(0f, side * 18f, side * -12f);
                Part(PrimitiveType.Sphere, "Pink ear inset", new Vector3(side * 0.58f, 0.925f, 0.66f), new Vector3(0.21f, 0.055f, 0.145f), pinkMaterial)
                    .transform.localRotation = Quaternion.Euler(0f, side * 18f, side * -12f);

                // Gently swept horns instead of the old blunt cylinders.
                float hornSide = side * 0.29f;
                TubeBetween("Horn base", new Vector3(hornSide, 1.12f, 0.76f), new Vector3(side * 0.34f, 1.34f, 0.72f), 0.085f, hornMaterial);
                TubeBetween("Horn curl", new Vector3(side * 0.34f, 1.34f, 0.72f), new Vector3(side * 0.36f, 1.47f, 0.59f), 0.062f, hornMaterial);
                TubeBetween("Horn tip", new Vector3(side * 0.36f, 1.47f, 0.59f), new Vector3(side * 0.31f, 1.50f, 0.48f), 0.034f, hornMaterial);
            }

            // Four short legs and split visual hooves; the gameplay collider stays
            // on the root and is never duplicated by the art.
            for (int side = -1; side <= 1; side += 2)
            for (int row = 0; row <= 1; row++)
            {
                float z = row == 0 ? 0.46f : -0.55f;
                float x = side * 0.29f;
                TubeBetween("White lower leg", new Vector3(x, 0.15f, z), new Vector3(x, -0.28f, z), 0.10f, coatMaterial);
                Part(PrimitiveType.Cube, "Charcoal cloven hoof", new Vector3(x, -0.37f, z + 0.025f), new Vector3(0.19f, 0.15f, 0.23f), hoofMaterial);
                Part(PrimitiveType.Cube, "Hoof split", new Vector3(x, -0.372f, z + 0.142f), new Vector3(0.014f, 0.10f, 0.012f), mouthMaterial);
            }

            Part(PrimitiveType.Sphere, "Little tail", new Vector3(0f, 0.40f, -0.70f), new Vector3(0.19f, 0.22f, 0.25f), coatMaterial);
            Part(PrimitiveType.Sphere, "Tail puff", new Vector3(0f, 0.46f, -0.82f), new Vector3(0.22f, 0.20f, 0.20f), coatMaterial);

            CombineByMaterial();
        }

        private void EnsureMaterials()
        {
            Shader shader = Shader.Find("Standard");
            coatMaterial ??= MakeMaterial("Goat Snow-White Fleece", new Color(0.94f, 0.925f, 0.86f), shader, 0.22f);
            muzzleMaterial ??= MakeMaterial("Goat Cream Muzzle", new Color(0.98f, 0.83f, 0.70f), shader, 0.24f);
            hornMaterial ??= MakeMaterial("Goat Warm Ivory Horn", new Color(0.72f, 0.55f, 0.34f), shader, 0.2f);
            hoofMaterial ??= MakeMaterial("Goat Slate Hoof", new Color(0.16f, 0.18f, 0.18f), shader, 0.18f);
            mouthMaterial ??= MakeMaterial("Goat Deep Cocoa Details", new Color(0.20f, 0.105f, 0.085f), shader, 0.18f);
            pinkMaterial ??= MakeMaterial("Goat Rose Nose and Tongue", new Color(0.93f, 0.31f, 0.39f), shader, 0.27f);
            eyeWhiteMaterial ??= MakeMaterial("Goat Eye White", new Color(1f, 0.98f, 0.91f), shader, 0.16f);
            eyeDarkMaterial ??= MakeMaterial("Goat Warm Eyes", new Color(0.22f, 0.105f, 0.055f), shader, 0.12f);
            sparkleMaterial ??= MakeMaterial("Goat Eye Sparkle", Color.white, shader, 0.06f);
        }

        private static Material MakeMaterial(string name, Color color, Shader shader, float smoothness)
        {
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);
            return material;
        }

        private GameObject Part(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(visual, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(part.GetComponent<Collider>());
            return part;
        }

        private void TubeBetween(string name, Vector3 start, Vector3 end, float radius, Material material)
        {
            Vector3 direction = end - start;
            GameObject tube = Part(PrimitiveType.Capsule, name, (start + end) * 0.5f,
                new Vector3(radius, direction.magnitude * 0.5f + radius, radius), material);
            tube.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        }

        private void CombineByMaterial()
        {
            var batches = new Dictionary<Material, List<CombineInstance>>();
            var sourceParts = new List<GameObject>();
            foreach (Transform child in visual)
            {
                MeshFilter filter = child.GetComponent<MeshFilter>();
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                if (!filter || !renderer || !filter.sharedMesh)
                    continue;

                if (!batches.TryGetValue(renderer.sharedMaterial, out List<CombineInstance> batch))
                    batches.Add(renderer.sharedMaterial, batch = new List<CombineInstance>());
                batch.Add(new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    transform = visual.worldToLocalMatrix * child.localToWorldMatrix
                });
                sourceParts.Add(child.gameObject);
            }

            foreach (KeyValuePair<Material, List<CombineInstance>> batch in batches)
            {
                var mesh = new Mesh { name = $"Goat {batch.Key.name} Combined" };
                mesh.CombineMeshes(batch.Value.ToArray(), true, true, false);
                var combined = new GameObject($"Goat {batch.Key.name} Mesh");
                combined.transform.SetParent(visual, false);
                combined.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = combined.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = batch.Key;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            foreach (GameObject source in sourceParts)
                Destroy(source);
        }

        private void Update()
        {
            body ??= GetComponent<Rigidbody>();
            ground ??= GetComponent<GoatGroundDetector>();
            visual ??= transform.Find(VisualName);
            if (!visual || !body)
                return;

            if (importedAnimation != null && !surpriseEarMaskReady)
                ConfigureSurpriseEarMask();

            if (!MountainAuthority.IsHost)
            {
                if (importedAnimation && !string.IsNullOrEmpty(networkClip)
                    && importedAnimation[networkClip] != null && activeAutomaticClip != networkClip)
                {
                    importedAnimation[networkClip].wrapMode = IsLoop(networkClip) ? WrapMode.Loop : WrapMode.Once;
                    importedAnimation.CrossFade(networkClip, .12f, PlayMode.StopSameLayer);
                    activeAutomaticClip = networkClip;
                }
                if (networkFacing.sqrMagnitude > .01f)
                    visual.rotation = Quaternion.Slerp(visual.rotation,
                        Quaternion.LookRotation(networkFacing, Vector3.up), 1f - Mathf.Exp(-15f * Time.deltaTime));
                return;
            }

            if (importedAnimation != null)
            {
                if (GoatLocalControl.AllowsInput(this) && !(GetComponent<GoatInteraction>()?.IsBusy ?? false)) ReadAnimationHotkeys();
                if (!string.IsNullOrEmpty(manualActionClip) && Time.time >= manualActionUntil)
                    manualActionClip = null;

                var goat = GetComponent<GoatController>();
                if (goat && (goat.IsPredatorCarried || (ground && !ground.IsGrounded && body.linearVelocity.y < -3f)))
                    manualActionClip = null;

                if (string.IsNullOrEmpty(manualActionClip))
                {
                    Vector3 velocity = body.linearVelocity;
                    velocity.y = 0f;
                    string clip = goat && goat.IsPredatorCarried ? "Goat_Idle"
                        : ground && !ground.IsGrounded
                            ? "Goat_Jump"
                            : velocity.sqrMagnitude > .2f ? "Goat_Walk" : "Goat_Idle";
                    var pair = GetComponent<GoatInteraction>();
                    if (pair && pair.IsLinked && !(goat && goat.IsPredatorCarried))
                    {
                        if (pair.IsHolding)
                        {
                            clip = pair.IsPulling ? "Goat_Pull"
                                : pair.Partner && !pair.Partner.GetComponent<GoatGroundDetector>().IsGrounded
                                    ? "Goat_RescueBrace" : "Goat_GrabHold";
                        }
                        else
                            clip = ground && ground.IsGrounded ? "Goat_GrabbedHold" : "Goat_GripStrain";
                    }
                    if (importedAnimation[clip] == null) clip = "Goat_Idle";
                    if (!importedAnimation.IsPlaying(clip))
                    {
                        importedAnimation[clip].wrapMode = IsLoop(clip) ? WrapMode.Loop : WrapMode.Once;
                        importedAnimation.CrossFade(clip, .15f);
                        if (activeAutomaticClip != clip)
                        {
                            activeAutomaticClip = clip;
                            if (!(pair && pair.IsLinked)) ApplySurpriseEarsForAction(clip);
                        }
                    }
                }
            }

            if (GetComponent<GoatPhysicalBody>()?.RootIsTumbling == true)
            {
                visual.rotation = body.rotation;
                return;
            }

            Vector3 horizontal = body.linearVelocity;
            horizontal.y = 0f;
            if (Time.time < faceUntil && interactionFacing.sqrMagnitude > .01f)
            {
                visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.LookRotation(interactionFacing, Vector3.up), 1f - Mathf.Exp(-16f * Time.deltaTime));
            }
            else if (horizontal.sqrMagnitude > 0.2f)
            {
                Vector3 up = ground && ground.IsGrounded ? ground.GroundNormal : Vector3.up;
                Quaternion target = Quaternion.LookRotation(horizontal.normalized, up);
                visual.rotation = Quaternion.Slerp(visual.rotation, target, Time.deltaTime * 9f);
            }

            visual.localScale = Vector3.one;
        }

        public void PlayNetworkAction(byte action)
        {
            if (!MountainAuthority.IsHost || action == 0) return;
            switch (action)
            {
                case 1: PlayManualAction("Goat_EatGrass"); break;
                case 2: PlayManualAction("Goat_Pee"); break;
                case 3: PlayManualAction("Goat_Poop"); break;
                case 4: PlayManualAction("Goat_Sequence"); break;
                case 5: PlayManualAction("GoatA_Duo_Performance"); break;
                case 6: PlayManualAction("GoatB_Duo_Performance"); break;
                case 7: TriggerSurprise(); break;
            }
        }

        private void ReadAnimationHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Q)) PlayManualAction("Goat_EatGrass");
            else if (Input.GetKeyDown(KeyCode.E)) PlayManualAction("Goat_Pee");
            else if (Input.GetKeyDown(KeyCode.C)) PlayManualAction("Goat_Poop");
            else if (Input.GetKeyDown(KeyCode.V)) PlayManualAction("Goat_Sequence");
            else if (Input.GetKeyDown(KeyCode.Z)) PlayManualAction("GoatA_Duo_Performance");
            else if (Input.GetKeyDown(KeyCode.X)) PlayManualAction("GoatB_Duo_Performance");
            else if (Input.GetKeyDown(KeyCode.T)) TriggerSurprise();
        }

        /// <summary>Plays the startled ear-flick reaction; gameplay events can call this directly.</summary>
        public void TriggerSurprise() => PlayManualAction("Goat_Surprise");

        private void PlayManualAction(string clipName)
        {
            AnimationState state = importedAnimation[clipName];
            if (state == null)
            {
                Debug.LogWarning($"Goat animation clip '{clipName}' is missing from GoatDuoRefined.");
                return;
            }

            if (clipName == "Goat_Surprise")
            {
                PlaySurpriseEarOverlay();
                return;
            }

            state.wrapMode = WrapMode.Once;
            state.time = 0f;
            state.speed = 1f;
            manualActionClip = clipName;
            manualActionUntil = Time.time + Mathf.Max(state.length, 0.25f);
            activeAutomaticClip = clipName;
            importedAnimation.CrossFade(clipName, 0.2f, PlayMode.StopSameLayer);
            ApplySurpriseEarsForAction(clipName);
        }

        private void ApplySurpriseEarsForAction(string clipName)
        {
            if (ShouldAutoTriggerSurpriseEars(clipName))
                PlaySurpriseEarOverlay();
            else if (importedAnimation)
                importedAnimation.Stop("Goat_Surprise");
        }

        private void ConfigureSurpriseEarMask()
        {
            AnimationState state = importedAnimation?["Goat_Surprise"];
            if (state == null || !visual) return;

            Transform leftEar = FindBone(visual, "Ear.L");
            Transform rightEar = FindBone(visual, "Ear.R");
            if (!leftEar || !rightEar)
            {
                Debug.LogWarning("Goat surprise ear animation is missing Ear.L or Ear.R in the imported rig.");
                return;
            }

            state.layer = 1;
            state.blendMode = AnimationBlendMode.Blend;
            state.AddMixingTransform(leftEar);
            state.AddMixingTransform(rightEar);
            surpriseEarMaskReady = true;
        }

        private void PlaySurpriseEarOverlay()
        {
            if (!importedAnimation) return;
            if (!surpriseEarMaskReady) ConfigureSurpriseEarMask();

            AnimationState state = importedAnimation["Goat_Surprise"];
            if (state == null || !surpriseEarMaskReady) return;
            state.wrapMode = WrapMode.Once;
            state.time = 0f;
            state.speed = 1f;
            state.weight = 1f;
            importedAnimation.CrossFade("Goat_Surprise", 0.15f, PlayMode.StopSameLayer);
        }

        private static bool ShouldAutoTriggerSurpriseEars(string clipName)
        {
            return clipName != "Goat_Idle"
                && clipName != "Goat_Walk"
                && clipName != "Goat_EatGrass"
                && clipName != "Goat_Surprise";
        }

        private static Transform FindBone(Transform root, string boneName)
        {
            foreach (Transform child in root)
            {
                if (child.name == boneName) return child;
                Transform nested = FindBone(child, boneName);
                if (nested) return nested;
            }
            return null;
        }

        private void OnGUI()
        {
            if (importedAnimation == null || !GoatLocalControl.AllowsInput(this))
                return;

            animationHelpStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = new Color(1f, 1f, 1f, 0.94f) }
            };
            GUI.Label(new Rect(25, 108, 960, 24),
                "Анимации: Q — трава   E — пописать   C — покакать   T — удивление   V — весь ролик   Z/X — роли дуэта",
                animationHelpStyle);
        }
    }
}
