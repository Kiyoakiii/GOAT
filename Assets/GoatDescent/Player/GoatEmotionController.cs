using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Cosmetic reactions derived from gameplay state, including replicated actions.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(250)]
    public sealed class GoatEmotionController : MonoBehaviour
    {
        public enum Expression { Calm, Relief, Straining, Frightened, Fierce, Hurt, Dazed, PlayingDead }
        public Expression Current { get; private set; }
        public float MouthOpen { get; private set; }
        public Vector3 HeadEuler { get; private set; }

        private GoatController motor;
        private GoatVisualController visual;
        private GoatPhysicalBody physical;
        private GoatToiletAction toilet;
        private GoatComedyActions comedy;
        private GoatHealth health;
        private GoatSkin skin;
        private GoatHerdBehaviour herd;
        private RespawnController respawn;
        private Transform visualRoot, tongue, headBone;
        private readonly EarPose[] accents = { new EarPose("Ear.L"), new EarPose("Ear.R"), new EarPose("Tail") };
        private SkinnedMeshRenderer[] faces;
        private SkinnedMeshRenderer ibexRenderer;
        private Mesh ibexSource, ibexExpressions;
        private int boundSkin = int.MinValue;
        private float lastHealth, hitAt = -100f, hitStrength, reliefUntil, airborneTime;
        private float squint, alarm, smile, anger, earFold, tailWag, tongueOut;
        private float previousToiletProgress;
        private bool wasToilet;
        private Vector3 tongueScale;
        private Quaternion tongueRotation;

        private sealed class EarPose
        {
            public readonly string name;
            public Transform bone;
            public Quaternion original, applied;
            public Vector3 originalPosition, appliedPosition;
            public Vector3? anchorLocal;
            public bool changed;
            public EarPose(string name) { this.name = name; }
            public void Restore()
            {
                // A clip may already have written a fresh pose. Never undo that pose.
                if (changed && bone && Quaternion.Angle(bone.localRotation, applied) < .01f)
                    bone.localRotation = original;
                if (changed && bone && (bone.localPosition - appliedPosition).sqrMagnitude < 1e-12f)
                    bone.localPosition = originalPosition;
                changed = false;
            }
            public void Apply(Vector3 euler, Vector3? attachedRoot = null)
            {
                if (!bone) return;
                original = bone.localRotation;
                originalPosition = bone.localPosition;
                Vector3 pivot = attachedRoot ?? (anchorLocal.HasValue ? bone.TransformPoint(anchorLocal.Value) : bone.position);
                applied = original * Quaternion.Euler(euler);
                bone.localRotation = applied;
                if (anchorLocal.HasValue) bone.position += pivot - bone.TransformPoint(anchorLocal.Value);
                appliedPosition = bone.localPosition;
                changed = true;
            }
        }

        private void Start()
        {
            motor = GetComponent<GoatController>(); visual = GetComponent<GoatVisualController>();
            physical = GetComponent<GoatPhysicalBody>(); toilet = GetComponent<GoatToiletAction>();
            comedy = GetComponent<GoatComedyActions>(); health = GetComponent<GoatHealth>();
            skin = GetComponent<GoatSkin>(); herd = GetComponent<GoatHerdBehaviour>();
            respawn = GetComponent<RespawnController>();
            lastHealth = health ? health.Current : GoatHealth.Maximum;
        }

        public void ReactToImpact(float strength)
        {
            // Multiple contact points in one landing produce one reaction.
            if (Time.time - hitAt < .3f) return;
            hitAt = Time.time;
            hitStrength = Mathf.Clamp(strength, .4f, 1f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > 4.5f)
                ReactToImpact(collision.relativeVelocity.magnitude / 12f);
        }

        private void Update()
        {
            foreach (var accent in accents) accent.Restore();
            if (!motor) return;
            if (health)
            {
                if (health.Current < lastHealth - .01f) ReactToImpact((lastHealth - health.Current) / 25f);
                if (health.Current > lastHealth + 25f) { hitAt = -100f; reliefUntil = 0f; }
                lastHealth = health.Current;
            }
            bool activeToilet = toilet && toilet.Active;
            if (wasToilet && !activeToilet && previousToiletProgress > .88f && motor.Grounded)
                reliefUntil = Time.time + 1.6f;
            wasToilet = activeToilet;
            previousToiletProgress = activeToilet ? toilet.Progress : 0f;
            airborneTime = motor.Grounded ? 0f : airborneTime + Time.deltaTime;
            string clip = visual ? visual.CurrentClip : "";
            float hitAge = Time.time - hitAt;
            Current = Expression.Calm;
            if (respawn && respawn.IsDead) Current = Expression.PlayingDead;
            else if (hitAge < .27f || (herd && herd.ShockPose > .2f)) Current = Expression.Hurt;
            else if ((comedy && comedy.PlayingDead) || clip == "Goat_PlayDeadHold") Current = Expression.PlayingDead;
            else if (motor.IsPredatorCarried || (physical && physical.IsEagleStrained)
                || (airborneTime > .12f && motor.Velocity.y < -3.2f)) Current = Expression.Frightened;
            else if ((comedy && comedy.Recovering) || clip == "Goat_GetUp" || hitAge < 1.05f) Current = Expression.Dazed;
            else if (activeToilet) Current = toilet.Kind == GoatToiletKind.Poop ? Expression.Straining : Expression.Relief;
            else if (clip == "Goat_EagleRoundhouse" || clip == "Goat_RightPunch") Current = Expression.Fierce;
            else if (Time.time < reliefUntil || clip == "Goat_BackRide") Current = Expression.Relief;

            float t = Time.time;
            float effort = activeToilet ? Mathf.Sin(toilet.Progress * Mathf.PI) : 0f;
            float pulse = .5f + .5f * Mathf.Sin((activeToilet ? toilet.Elapsed : t) * 12f);
            float targetSquint = 0, targetAlarm = 0, targetSmile = 0, targetAnger = 0;
            float targetMouth = 0, targetEar = 0, targetTail = 0, targetTongue = 0;
            Vector3 head = Vector3.zero;
            switch (Current)
            {
                case Expression.Relief:
                    targetSquint = .65f; targetSmile = .85f; targetEar = -.3f; targetTail = .8f;
                    head = new Vector3(-4f, 0, Mathf.Sin(t * 2.6f) * 3f); break;
                case Expression.Straining:
                    targetSquint = .7f + .28f * pulse * effort; targetAnger = .7f;
                    targetMouth = .12f; targetEar = .5f; targetTail = .2f; targetTongue = .25f;
                    head = new Vector3(5f * effort, 0, Mathf.Sin(t * 18f) * effort * 1.8f); break;
                case Expression.Frightened:
                    targetAlarm = 1f; targetMouth = 1f; targetEar = .95f; targetTongue = .8f;
                    head = new Vector3(-7f, 0, Mathf.Sin(t * 15f) * 2f); break;
                case Expression.Fierce:
                    targetSquint = .3f; targetAnger = 1f; targetEar = .75f; targetMouth = .2f;
                    head = new Vector3(5f, 0, -4f); break;
                case Expression.Hurt:
                    targetSquint = 1f; targetMouth = .65f; targetEar = 1f; targetTongue = .7f;
                    head = new Vector3(-8f, 0, 9f) * Mathf.Max(.6f, hitStrength); break;
                case Expression.Dazed:
                    targetSquint = .45f; targetAlarm = .2f; targetEar = .3f; targetTongue = 1f;
                    head = new Vector3(Mathf.Sin(t * 9f) * 3f, 0, Mathf.Sin(t * 7f) * 6f); break;
                case Expression.PlayingDead:
                    targetSquint = 1f; targetEar = .7f; targetTongue = 1f; break;
            }
            // Quiet blinks keep the resting face alive without random gameplay effects.
            float blinkPhase = Mathf.Repeat(t + (GetComponent<GoatLocalControl>()?.Label == "B" ? 1.7f : 0f), 4.3f);
            if (Current == Expression.Calm || Current == Expression.Relief)
                targetSquint = Mathf.Max(targetSquint, Mathf.Clamp01(1f - Mathf.Abs(blinkPhase - .12f) / .1f));
            float blend = 1f - Mathf.Exp(-Time.deltaTime * (Current == Expression.Hurt ? 30f : 13f));
            squint = Mathf.Lerp(squint, targetSquint, blend); alarm = Mathf.Lerp(alarm, targetAlarm, blend);
            smile = Mathf.Lerp(smile, targetSmile, blend); anger = Mathf.Lerp(anger, targetAnger, blend);
            MouthOpen = Mathf.Lerp(MouthOpen, targetMouth, blend);
            // Head-local Y follows the muzzle, Z is vertical: nod/roll without
            // adding yaw to the player's mouse-driven facing.
            HeadEuler = Vector3.Lerp(HeadEuler, new Vector3(head.x, head.z, 0f), blend);
            earFold = Mathf.Lerp(earFold, targetEar, blend); tailWag = Mathf.Lerp(tailWag, targetTail, blend);
            tongueOut = Mathf.Lerp(tongueOut, targetTongue, blend);
        }

        private void LateUpdate()
        {
            if (!visualRoot)
            {
                visualRoot = transform.Find("VisualRoot — replaceable goat model");
                if (!visualRoot) return;
                foreach (var bone in visualRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (bone.name == "Head") headBone = bone;
                    foreach (var accent in accents) if (bone.name == accent.name) accent.bone = bone;
                }
            }
            if (faces == null || (skin ? skin.Number : 0) != boundSkin) BindFace();
            foreach (var face in faces)
            {
                if (!face || !face.enabled || !face.sharedMesh) continue;
                SetShape(face, "EmotionSquint", squint);
                SetShape(face, "EmotionAlarm", alarm);
                SetShape(face, "EmotionSmile", smile);
                SetShape(face, "EmotionFierce", anger);
                SetShape(face, "Raise", alarm * .95f + smile * .3f);
                SetShape(face, "Furrow", anger);
            }
            float flutter = Current == Expression.Straining ? Mathf.Sin(Time.time * 26f) * 5f
                : Current == Expression.Frightened ? Mathf.Sin(Time.time * 20f) * 6f : 0f;
            // Authored clips also rotate the old low ear pivots. Anchor the
            // visible ibex ear bases to the skull throughout those clips.
            Vector3? leftRoot = headBone && accents[0].anchorLocal.HasValue
                ? headBone.position + headBone.rotation * new Vector3(.105f, -.09f, .58f) : (Vector3?)null;
            Vector3? rightRoot = headBone && accents[1].anchorLocal.HasValue
                ? headBone.position + headBone.rotation * new Vector3(-.105f, -.09f, .58f) : (Vector3?)null;
            accents[0].Apply(new Vector3(earFold * 22f + flutter, earFold * -32f, -earFold * 25f), leftRoot);
            accents[1].Apply(new Vector3(earFold * 22f - flutter, earFold * 32f, earFold * 25f), rightRoot);
            accents[2].Apply(new Vector3(tailWag * -15f, Mathf.Sin(Time.time * 12f) * tailWag * 22f, 0));
            if (tongue)
            {
                tongue.localScale = Vector3.Scale(tongueScale, new Vector3(1f, 1f, 1f + tongueOut * 1.4f));
                tongue.localRotation = tongueRotation * Quaternion.Euler(tongueOut * (12f + Mathf.Sin(Time.time * 13f) * 6f), 0, 0);
            }
        }

        private static void SetShape(SkinnedMeshRenderer face, string name, float amount)
        {
            int index = face.sharedMesh.GetBlendShapeIndex(name);
            if (index >= 0) face.SetBlendShapeWeight(index, Mathf.Clamp01(amount) * 100f);
        }

        private void BindFace()
        {
            ReleaseMesh();
            boundSkin = skin ? skin.Number : 0;
            foreach (var accent in accents) accent.anchorLocal = null;
            faces = visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (boundSkin == 11 || boundSkin == 12)
                foreach (var face in faces)
                    if (face.enabled && face.name == "Selected goat skin")
                    {
                        ibexRenderer = face; ibexSource = face.sharedMesh;
                        var definition = GoatSkin.Definition(boundSkin);
                        ibexExpressions = GoatEmotionMesh.Acquire(ibexSource, definition);
                        face.sharedMesh = ibexExpressions;
                        // Ibex ears sit above the reused skeleton's ear pivots.
                        // Convert their visible roots from bind Head space into
                        // each ear's bind space; this also preserves authored motion.
                        int head = System.Array.IndexOf(definition.boneNames, "Head");
                        var headToMesh = (Matrix4x4.Scale(Vector3.one * 200f) * ibexSource.bindposes[head]).inverse;
                        for (int i = 0; i < 2; i++)
                        {
                            int ear = System.Array.IndexOf(definition.boneNames, accents[i].name);
                            if (ear >= 0) accents[i].anchorLocal = ibexSource.bindposes[ear].MultiplyPoint3x4(
                                headToMesh.MultiplyPoint3x4(new Vector3(i == 0 ? .12f : -.12f, -.19f, .60f)));
                        }
                        break;
                    }
            tongue = null;
            foreach (var node in visualRoot.GetComponentsInChildren<Transform>(true))
                if (node.name == "Small pink tongue" && node.gameObject.activeSelf)
                { tongue = node; tongueScale = node.localScale; tongueRotation = node.localRotation; break; }
        }

        private void ReleaseMesh()
        {
            if (ibexRenderer && ibexRenderer.sharedMesh == ibexExpressions) ibexRenderer.sharedMesh = ibexSource;
            if (ibexExpressions) GoatEmotionMesh.Release(ibexSource);
            ibexRenderer = null; ibexSource = null; ibexExpressions = null;
        }
        private void OnDisable()
        {
            foreach (var accent in accents) accent.Restore();
            if (tongue) { tongue.localScale = tongueScale; tongue.localRotation = tongueRotation; }
            if (faces != null)
                foreach (var face in faces)
                    if (face && face.sharedMesh)
                        foreach (string name in new[] { "EmotionSquint", "EmotionAlarm", "EmotionSmile", "EmotionFierce", "Raise", "Furrow" }) SetShape(face, name, 0);
            ReleaseMesh(); faces = null;
            HeadEuler = Vector3.zero; MouthOpen = 0;
        }
    }

    /// <summary>Shared runtime face morphs and groomed ibex coats; no per-frame mesh edits.</summary>
    internal static class GoatEmotionMesh
    {
        private sealed class Entry { public Mesh mesh; public int users; }
        private static readonly Dictionary<Mesh, Entry> meshes = new Dictionary<Mesh, Entry>();
        public static Mesh Acquire(Mesh source, GoatSkinDefinition definition)
        {
            if (meshes.TryGetValue(source, out var entry)) { entry.users++; return entry.mesh; }
            int head = System.Array.IndexOf(definition.boneNames, "Head");
            if (head < 0 || !source.isReadable) return source;
            var mesh = Object.Instantiate(source); mesh.name = source.name + " · expressions";
            // Share one runtime copy for facial morphs and coat grooming.
            GoatFurGroom.Apply(mesh);
            // Imported rig uses scale 200. Work in metre-sized Head coordinates:
            // X across the face, Y towards the nose, Z towards the crown.
            var toHead = Matrix4x4.Scale(Vector3.one * 200f) * source.bindposes[head];
            var toMesh = toHead.inverse;
            var vertices = mesh.vertices;
            var squint = new Vector3[vertices.Length]; var alarm = new Vector3[vertices.Length];
            var smile = new Vector3[vertices.Length]; var fierce = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = toHead.MultiplyPoint3x4(vertices[i]);
                float side = Mathf.Sign(p.x);
                Vector3 eye = new Vector3(side * .124f, .05f, .584f);
                Vector3 e = p - eye;
                float region = 1f - Mathf.Clamp01(new Vector3(e.x / .065f, e.y / .075f, e.z / .085f).magnitude);
                region = Mathf.SmoothStep(0f, 1f, region);
                squint[i] = toMesh.MultiplyVector(new Vector3(0, 0, -e.z * .96f) * Mathf.Clamp01(region * 2.5f));
                alarm[i] = toMesh.MultiplyVector(new Vector3(e.x * .8f + side * .012f, e.y * 1.1f, e.z * 1.6f + .018f) * region);
                float brow = region * Mathf.Clamp01((e.z + .005f) / .035f);
                fierce[i] = toMesh.MultiplyVector(new Vector3(-side * .007f, .008f, -.025f) * brow);
                float corner = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Abs(p.x) - .063f) / .065f)
                    * Mathf.Clamp01(1f - Mathf.Abs(p.y - .29f) / .12f)
                    * Mathf.Clamp01(1f - Mathf.Abs(p.z - .36f) / .055f);
                smile[i] = toMesh.MultiplyVector(new Vector3(side * .008f, -.008f, .025f) * corner);
            }
            mesh.AddBlendShapeFrame("EmotionSquint", 100, squint, null, null);
            mesh.AddBlendShapeFrame("EmotionAlarm", 100, alarm, null, null);
            mesh.AddBlendShapeFrame("EmotionSmile", 100, smile, null, null);
            mesh.AddBlendShapeFrame("EmotionFierce", 100, fierce, null, null);
            meshes[source] = new Entry { mesh = mesh, users = 1 };
            return mesh;
        }
        public static void Release(Mesh source)
        {
            if (!source || !meshes.TryGetValue(source, out var entry) || --entry.users > 0) return;
            Object.Destroy(entry.mesh); meshes.Remove(source);
        }
    }
}
