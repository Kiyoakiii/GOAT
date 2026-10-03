using UnityEngine;

namespace GoatDescent
{
    /// <summary>Small cartoon pose animation driven by the goat's real movement.</summary>
    public sealed class GoatVisualController : MonoBehaviour
    {
        private const string RootName = "VisualRoot — replaceable goat model";
        private readonly Transform[] legPivots = new Transform[4];
        private readonly Transform[] ears = new Transform[2];

        private Rigidbody body;
        private GoatGroundDetector ground;
        private Transform visual;
        private Transform torso;
        private Transform headPivot;
        private Transform tailPivot;
        private Transform tongue;
        private Animation importedAnimation;
        private float walkCycle;
        private float gaitWeight;
        private float squash;
        private float tumble;
        private GameObject looseHorn;
        private GameObject looseScarf;
        private bool hornFlying;
        private Vector3 previousVelocity;
        private bool hasPreviousVelocity;
        private float torsoPitch, torsoPitchSpeed;
        private float torsoRoll, torsoRollSpeed;
        private float headPitch, headPitchSpeed;
        private float headRoll, headRollSpeed;
        private float earFlop, earFlopSpeed;
        private float tailSwing, tailSwingSpeed;
        private float impactJolt;
        private bool preparingJump;

        public void SetJumpPreparation(bool value) => preparingJump = value;

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
            visual = transform.Find(RootName);
            if (!visual) BuildVisual();
            CacheRig();
            AttachRefinedGoat();
        }

        private void BuildVisual()
        {
            visual = new GameObject(RootName).transform;
            visual.SetParent(transform, false);

            torso = NewPivot("Torso Motion", visual, Vector3.zero);
            headPivot = NewPivot("Head Motion", torso, new Vector3(0f, 1.18f, .62f));
            tailPivot = NewPivot("Tail Motion", torso, new Vector3(0f, .87f, -.61f));

            var coat = MakeMaterial(new Color(.77f, .68f, .51f));
            var cream = MakeMaterial(new Color(.91f, .84f, .68f));
            var muzzle = MakeMaterial(new Color(.96f, .89f, .76f));
            var hoof = MakeMaterial(new Color(.24f, .21f, .19f));
            var horn = MakeMaterial(new Color(.73f, .64f, .48f));
            var eyeWhite = MakeMaterial(new Color(.98f, .96f, .87f));
            var scarf = MakeMaterial(new Color(.22f, .43f, .47f));
            var tonguePink = MakeMaterial(new Color(.97f, .40f, .57f));

            Part(PrimitiveType.Sphere, "Rounded body", new Vector3(0f, .75f, -.08f), new Vector3(.84f, .62f, 1.15f), coat, torso);
            Part(PrimitiveType.Sphere, "Chest", new Vector3(0f, .83f, .35f), new Vector3(.71f, .67f, .67f), cream, torso);
            var neck = Part(PrimitiveType.Capsule, "Neck", new Vector3(0f, 1.12f, .51f), new Vector3(.34f, .45f, .34f), cream, torso);
            neck.transform.localRotation = Quaternion.Euler(23f, 0f, 0f);
            looseScarf = Part(PrimitiveType.Sphere, "Neckerchief", new Vector3(0f, 1.04f, .53f), new Vector3(.44f, .10f, .43f), scarf, torso);

            HeadPart(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.35f, .73f), new Vector3(.48f, .43f, .53f), cream);
            HeadPart(PrimitiveType.Sphere, "Soft muzzle", new Vector3(0f, 1.23f, 1.05f), new Vector3(.36f, .25f, .40f), muzzle);
            HeadPart(PrimitiveType.Sphere, "Nose", new Vector3(0f, 1.28f, 1.25f), new Vector3(.16f, .08f, .09f), hoof);
            HeadPart(PrimitiveType.Sphere, "Small beard", new Vector3(0f, 1.04f, 1.02f), new Vector3(.14f, .24f, .15f), cream);
            tongue = HeadPart(PrimitiveType.Capsule, "Silly tongue", new Vector3(0f, 1.10f, 1.31f), new Vector3(.09f, .16f, .08f), tonguePink).transform;
            Part(PrimitiveType.Sphere, "Short tail", new Vector3(0f, 0f, -.12f), new Vector3(.19f, .18f, .27f), cream, tailPivot);

            int legIndex = 0;
            for (int side = -1; side <= 1; side += 2)
            {
                foreach (float z in new[] { -.43f, .40f })
                {
                    Transform leg = NewPivot($"Leg Pivot {legIndex}", visual, new Vector3(.27f * side, .54f, z));
                    Part(PrimitiveType.Capsule, "Leg", new Vector3(0f, -.22f, 0f), new Vector3(.17f, .27f, .18f), coat, leg);
                    Part(PrimitiveType.Sphere, "Hoof", new Vector3(0f, -.44f, .04f), new Vector3(.22f, .15f, .25f), hoof, leg);
                    legPivots[legIndex++] = leg;
                }

                var ear = HeadPart(PrimitiveType.Sphere, side < 0 ? "Left ear" : "Right ear", new Vector3(.36f * side, 1.43f, .69f), new Vector3(.29f, .13f, .22f), coat);
                ear.transform.localRotation = Quaternion.Euler(0f, 0f, -20f * side);
                ears[side < 0 ? 0 : 1] = ear.transform;
                var hornBase = HeadPart(PrimitiveType.Capsule, "Horn base", new Vector3(.16f * side, 1.63f, .55f), new Vector3(.12f, .24f, .12f), horn);
                hornBase.transform.localRotation = Quaternion.Euler(-15f, 0f, -20f * side);
                var hornTip = HeadPart(PrimitiveType.Capsule, "Horn tip", new Vector3(.24f * side, 1.82f, .49f), new Vector3(.07f, .16f, .07f), horn);
                hornTip.transform.localRotation = Quaternion.Euler(-25f, 0f, -35f * side);
                if (side == 1) looseHorn = hornTip;
                HeadPart(PrimitiveType.Sphere, "Eye white", new Vector3(.22f * side, 1.39f, .93f), new Vector3(.12f, .10f, .07f), eyeWhite);
                HeadPart(PrimitiveType.Sphere, "Pupil", new Vector3(.25f * side, 1.39f, .99f), new Vector3(.055f, .06f, .045f), hoof);
            }
        }

        private void CacheRig()
        {
            torso = visual ? visual.Find("Torso Motion") : null;
            headPivot = torso ? torso.Find("Head Motion") : null;
            tailPivot = torso ? torso.Find("Tail Motion") : null;
            looseHorn = headPivot ? headPivot.Find("Horn tip")?.gameObject : null;
            looseScarf = torso ? torso.Find("Neckerchief")?.gameObject : null;
            tongue = headPivot ? headPivot.Find("Silly tongue") : null;
            ears[0] = headPivot ? headPivot.Find("Left ear") : null;
            ears[1] = headPivot ? headPivot.Find("Right ear") : null;
            for (int i = 0; i < legPivots.Length; i++)
                legPivots[i] = visual ? visual.Find($"Leg Pivot {i}") : null;
            importedAnimation = torso ? torso.Find("Refined FBX goat")?.GetComponentInChildren<Animation>() : null;
        }

        private void AttachRefinedGoat()
        {
            if (!torso) return;
            var model = torso.Find("Refined FBX goat");
            if (!model)
            {
                var prefab = Resources.Load<GameObject>("GoatDuoRefined");
                if (!prefab) return;
                model = Instantiate(prefab, torso, false).transform;
                model.name = "Refined FBX goat";
                model.localPosition = Vector3.zero;
                model.localRotation = Quaternion.identity;
                model.localScale = Vector3.one * .78f;
            }
            // The old stylized parts remain as fragments for the comic crash effect.
            // The old legs are siblings of Torso Motion, so hide the entire old rig.
            foreach (var renderer in visual.GetComponentsInChildren<MeshRenderer>())
                if (!renderer.transform.IsChildOf(model)) renderer.enabled = false;
            importedAnimation = model.GetComponentInChildren<Animation>();
            if (importedAnimation && importedAnimation["Goat_Idle"] != null)
            {
                importedAnimation["Goat_Idle"].wrapMode = WrapMode.Loop;
                if (importedAnimation["Goat_Walk"] != null)
                    importedAnimation["Goat_Walk"].wrapMode = WrapMode.Loop;
                importedAnimation.Play("Goat_Idle");
            }
        }

        private static Transform NewPivot(string name, Transform parent, Vector3 position)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            return pivot;
        }

        private GameObject HeadPart(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            return Part(type, name, position - headPivot.localPosition, scale, material, headPivot);
        }

        private static GameObject Part(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            Mesh sculpted = GoatShapeMesh.ForPart(name);
            var part = sculpted ? new GameObject(name) : GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            if (sculpted) part.AddComponent<MeshFilter>().sharedMesh = sculpted;
            if (sculpted) part.AddComponent<MeshRenderer>();
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            if (collider) Destroy(collider);
            return part;
        }

        private static Material MakeMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard")) { color = color };
            material.SetFloat("_Glossiness", .12f);
            return material;
        }

        public void PlayTakeoff(float amount = .14f)
        {
            squash = -Mathf.Clamp(amount, .08f, .28f);
            impactJolt = -12f;
        }

        public void PlayLanding(float impact)
        {
            squash = Mathf.Clamp(.10f + impact * .018f, .10f, .32f);
            impactJolt = Mathf.Clamp(impact * 3f, 0f, 35f);
            if (impact > 6.5f && !hornFlying && looseHorn) StartCoroutine(LaunchHorn());
        }

        private System.Collections.IEnumerator LaunchHorn()
        {
            hornFlying = true;
            Toss(looseHorn, "Oops! Flying horn");
            if (looseScarf) Toss(looseScarf, "Oops! Flying scarf");
            yield return new WaitForSeconds(2.2f);
            if (looseHorn) looseHorn.SetActive(true);
            if (looseScarf) looseScarf.SetActive(true);
            hornFlying = false;
        }

        private static void Toss(GameObject original, string name)
        {
            var flying = Instantiate(original);
            flying.name = name;
            flying.transform.SetParent(null, true);
            flying.AddComponent<SphereCollider>().radius = .5f;
            var flyingBody = flying.AddComponent<Rigidbody>();
            flyingBody.mass = .15f;
            flyingBody.AddForce(Vector3.up * 3f + Random.insideUnitSphere * 2f, ForceMode.Impulse);
            flyingBody.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            original.SetActive(false);
            Destroy(flying, 2.6f);
        }

        private void Update()
        {
            body ??= GetComponent<Rigidbody>();
            ground ??= GetComponent<GoatGroundDetector>();
            visual ??= transform.Find(RootName);
            if (!visual || !body) return;
            if (!torso)
            {
                if (!visual.Find("Torso Motion"))
                {
                    Destroy(visual.gameObject);
                    BuildVisual();
                }
                CacheRig();
                AttachRefinedGoat();
            }

            Vector3 horizontal = body.linearVelocity;
            horizontal.y = 0f;
            float speed = horizontal.magnitude;
            bool grounded = ground && ground.IsGrounded;
            if (speed > .45f)
            {
                Quaternion facing = Quaternion.LookRotation(horizontal.normalized, grounded ? ground.GroundNormal : Vector3.up);
                visual.rotation = Quaternion.Slerp(visual.rotation, facing, 1f - Mathf.Exp(-10f * Time.deltaTime));
            }

            Vector3 acceleration = hasPreviousVelocity && Time.deltaTime > .0001f
                ? Vector3.ClampMagnitude((body.linearVelocity - previousVelocity) / Time.deltaTime, 24f)
                : Vector3.zero;
            previousVelocity = body.linearVelocity;
            hasPreviousVelocity = true;
            Vector3 localAcceleration = visual.InverseTransformDirection(acceleration);
            float step = Mathf.Min(Time.deltaTime, .04f);
            float pitchTarget = Mathf.Clamp(-localAcceleration.z * 1.5f - body.linearVelocity.y * 1.2f, -26f, 26f);
            float rollTarget = Mathf.Clamp(localAcceleration.x * 2.2f, -24f, 24f);
            Spring(ref torsoPitch, ref torsoPitchSpeed, pitchTarget + impactJolt, 11f, .48f, step);
            var balance = GetComponent<GoatSlopeBalance>();
            Spring(ref torsoRoll, ref torsoRollSpeed, rollTarget + (balance ? balance.LeanDegrees : 0f), 10f, .5f, step);
            Spring(ref headPitch, ref headPitchSpeed, -torsoPitch * .55f + impactJolt * .7f, 13f, .38f, step);
            Spring(ref headRoll, ref headRollSpeed, -torsoRoll * .65f, 12f, .45f, step);
            Spring(ref earFlop, ref earFlopSpeed, Mathf.Clamp(speed * 1.3f + Mathf.Abs(localAcceleration.z) * .8f + impactJolt, 0f, 38f), 14f, .3f, step);
            Spring(ref tailSwing, ref tailSwingSpeed, Mathf.Clamp(-localAcceleration.x * 3f, -35f, 35f), 9f, .35f, step);
            impactJolt = Mathf.MoveTowards(impactJolt, 0f, Time.deltaTime * 65f);

            float moveAmount = Mathf.Clamp01(speed / 4f);
            gaitWeight = Mathf.MoveTowards(gaitWeight, grounded ? moveAmount : 0f, Time.deltaTime * 5f);
            walkCycle += speed * Time.deltaTime * 7f;
            for (int i = 0; i < legPivots.Length; i++)
            {
                if (!legPivots[i]) continue;
                float phase = i == 0 || i == 3 ? 0f : Mathf.PI;
                float swing = grounded ? Mathf.Sin(walkCycle + phase) * gaitWeight * 24f : (i == 1 || i == 3 ? -20f : 18f);
                legPivots[i].localRotation = Quaternion.Euler(swing, 0f, 0f);
            }

            float bob = grounded ? Mathf.Abs(Mathf.Sin(walkCycle)) * gaitWeight * .045f : 0f;
            if (torso)
            {
                if (!grounded && body.linearVelocity.y < -3f)
                    tumble += Time.deltaTime * Mathf.Min(150f, -body.linearVelocity.y * 14f);
                else tumble = Mathf.MoveTowardsAngle(tumble, 0f, Time.deltaTime * 400f);
                torso.localPosition = new Vector3(0f, bob + Mathf.Sin(Time.time * 2.3f) * .008f
                    - (preparingJump && grounded ? .13f : 0f), 0f);
                torso.localRotation = Quaternion.Euler((grounded ? Mathf.Sin(walkCycle) * gaitWeight * 3f : -5f)
                    + tumble + torsoPitch + (preparingJump && grounded ? 7f : 0f), 0f,
                    (grounded ? Mathf.Cos(walkCycle) * gaitWeight * 2f : 0f) + torsoRoll);
            }
            if (headPivot) headPivot.localRotation = Quaternion.Euler(headPitch + Mathf.Sin(Time.time * 2.5f) * 2f, 0f, headRoll);
            if (tailPivot) tailPivot.localRotation = Quaternion.Euler(0f, tailSwing + Mathf.Sin(Time.time * 7f) * (6f + moveAmount * 13f), 0f);
            for (int i = 0; i < ears.Length; i++)
                if (ears[i]) ears[i].localRotation = Quaternion.Euler(-earFlop * .55f, 0f, (i == 0 ? 1f : -1f) * (20f + earFlop));
            if (tongue)
            {
                float tongueOut = Mathf.Clamp01((speed - 5f) / 7f + (!grounded ? .45f : 0f));
                tongue.localScale = new Vector3(.09f, Mathf.Lerp(.01f, .20f, tongueOut), .08f);
                tongue.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 16f) * tongueOut * 18f, 0f, 0f);
            }

            squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 1.5f);
            visual.localScale = new Vector3(1f + squash * .35f, 1f - squash, 1f + squash * .35f);
            if (importedAnimation)
            {
                string action = !grounded ? "Goat_Jump" : speed > .85f ? "Goat_Walk" : "Goat_Idle";
                if (importedAnimation[action] != null && !importedAnimation.IsPlaying(action))
                    importedAnimation.CrossFade(action, .13f);
            }
        }

        private static void Spring(ref float value, ref float velocity, float target, float frequency, float damping, float dt)
        {
            velocity += ((target - value) * frequency * frequency - 2f * damping * frequency * velocity) * dt;
            value += velocity * dt;
        }
    }

}
