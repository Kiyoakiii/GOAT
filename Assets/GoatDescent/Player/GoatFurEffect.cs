using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoatDescent
{
    [System.Serializable]
    public struct GoatCoatState { public float remaining, duration, shedAge; public int shedSerial; }

    [DisallowMultipleComponent, DefaultExecutionOrder(220)]
    public sealed class GoatFurEffect : MonoBehaviour
    {
        private sealed class Surface
        {
            public SkinnedMeshRenderer Renderer;
            public Material[] Original, Temporary;
        }
        private readonly List<Surface> surfaces = new List<Surface>();
        private readonly List<Material> ownedMaterials = new List<Material>();
        private readonly List<SkinnedMeshRenderer> renderers = new List<SkinnedMeshRenderer>();
        private MaterialPropertyBlock properties;
        private ParticleSystem shedding;
        private Material particleMaterial;
        private float regrowAt, shedAt = -100f, nextRendererScan;
        private float regrowDuration = 600f;
        private int shedSerial, lastSkin = -100;
        private bool bareMaterials;
        public float Remaining => Mathf.Max(0, regrowAt - Time.time);
        public float Growth => Mathf.Clamp01(1f - Remaining / Mathf.Max(1, regrowDuration));

        public void ShedCoat()
        {
            if (!MountainAuthority.IsHost) return;
            regrowDuration = Mathf.Max(1, GoatHerdSettings.Get().furRegrowthSeconds);
            regrowAt = Time.time + regrowDuration;
            shedAt = Time.time; shedSerial++; EmitCoat();
            GetComponent<GoatJumpController>()?.AnnounceTrick("ШЕРСТЬ СЛЕЗЛА!");
        }
        private void ScanRenderers()
        {
            renderers.Clear();
            foreach (var candidate in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!candidate.enabled || candidate.name.StartsWith("First person") || !candidate.sharedMesh) continue;
                foreach (var bone in candidate.bones)
                    if (bone && bone.name == "Head") { renderers.Add(candidate); break; }
            }
            lastSkin = GetComponent<GoatSkin>()?.Number ?? -1;
            nextRendererScan = Time.time + 1f;
        }
        private static bool IsCoat(Material material)
        {
            if (!material) return false;
            string name = material.name.ToLowerInvariant();
            if (name.Contains("horn") || name.Contains("hoof") || name.Contains("eye") || name.Contains("pupil")
                || name.Contains("brow") || name.Contains("tongue") || name.Contains("teeth")
                || name.StartsWith("face") || name.Contains("goat face")) return false;
            return name.Contains("surface") || name.Contains("hide") || name.Contains("body") || name.Contains("coat")
                || name.Contains("fur") || name.Contains("wool") || name.Contains("mane") || name.Contains("beard")
                || name.Contains("default-material");
        }
        private void InstallBareMaterials()
        {
            var shader = Resources.Load<Shader>("GoatCoatSurface");
            if (!shader) return;
            foreach (var renderer in renderers)
            {
                var original = renderer.sharedMaterials;
                var temporary = (Material[])original.Clone();
                for (int i = 0; i < temporary.Length; i++)
                {
                    if (!IsCoat(original[i]) || original[i].shader.name == "GoatDescent/Ibex Fibers") continue;
                    var material = new Material(shader); material.CopyPropertiesFromMaterial(original[i]);
                    material.name = original[i].name + " · coat regrowth";
                    material.SetFloat("_CoatSurface", 1);
                    if (lastSkin == 11 || lastSkin == 12)
                    {
                        var mask = Resources.Load<Texture2D>("GoatCoatMasks/IbexCoatMask");
                        if (mask) material.SetTexture("_CoatMask", mask);
                    }
                    ownedMaterials.Add(material); temporary[i] = material;
                }
                renderer.sharedMaterials = temporary;
                surfaces.Add(new Surface { Renderer = renderer, Original = original, Temporary = temporary });
            }
            bareMaterials = true; RefreshFirstPerson();
        }
        private void RestoreMaterials()
        {
            foreach (var surface in surfaces)
            {
                if (!surface.Renderer) continue;
                var current = surface.Renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < current.Length && i < surface.Temporary.Length; i++)
                    if (current[i] == surface.Temporary[i]) { current[i] = surface.Original[i]; changed = true; }
                if (changed) surface.Renderer.sharedMaterials = current;
            }
            surfaces.Clear(); bareMaterials = false;
            RefreshFirstPerson();
            foreach (var material in ownedMaterials) if (material) Destroy(material);
            ownedMaterials.Clear();
        }
        private void RefreshFirstPerson()
        {
            if (Camera.main) Camera.main.GetComponent<GoatFirstPersonView>()?.RefreshAppearance(GetComponent<GoatController>());
        }
        private void LateUpdate()
        {
            int skin = GetComponent<GoatSkin>()?.Number ?? -1;
            if (skin != lastSkin || renderers.Count == 0 || Time.time >= nextRendererScan && !bareMaterials)
            {
                if (bareMaterials) RestoreMaterials();
                ScanRenderers();
            }
            if (Remaining > 0 && !bareMaterials) InstallBareMaterials();
            else if (Remaining <= 0 && bareMaterials) { RestoreMaterials(); ScanRenderers(); }
            properties ??= new MaterialPropertyBlock();
            float charge = GetComponent<GoatHerdBehaviour>()?.Charge ?? 0;
            foreach (var renderer in renderers)
            {
                if (!renderer) continue;
                renderer.GetPropertyBlock(properties);
                properties.SetFloat("_FurGrowth", Growth);
                properties.SetFloat("_StaticCharge", charge);
                Vector3 scale = renderer.transform.lossyScale;
                properties.SetFloat("_StaticFurOffset", .018f * charge
                    / Mathf.Max(1f, Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                properties.SetColor("_SkinColor", GoatHerdSettings.Get().bareSkinColor);
                renderer.SetPropertyBlock(properties);
            }
        }
        private void EmitCoat()
        {
            if (!shedding)
            {
                var node = new GameObject("Falling coat clumps"); node.transform.SetParent(transform, false);
                shedding = node.AddComponent<ParticleSystem>();
                shedding.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = shedding.main; main.playOnAwake = false; main.loop = false;
                main.duration = 1.5f; main.startLifetime = new ParticleSystem.MinMaxCurve(1.3f, 2.4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(.4f, 1.4f);
                main.startSize = new ParticleSystem.MinMaxCurve(.035f, .09f);
                main.gravityModifier = .35f; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 160;
                var emission = shedding.emission; emission.enabled = false;
                var shape = shedding.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .48f;
                var size = shedding.sizeOverLifetime; size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
                particleMaterial = new Material(Resources.Load<Shader>("GoatCoatClump"));
                var renderer = shedding.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = particleMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                shedding.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            int skin = GetComponent<GoatSkin>()?.Number ?? 11;
            var config = shedding.main;
            config.startColor = skin == 12 ? new Color(.8f, .8f, .73f) : new Color(.19f, .16f, .13f);
            shedding.transform.position = GetComponent<GoatPhysicalBody>()?.HarnessChest ?? transform.position + Vector3.up;
            shedding.Play(); shedding.Emit(90);
        }
        public GoatCoatState Capture() => new GoatCoatState { remaining = Remaining, duration = regrowDuration, shedSerial = shedSerial, shedAge = Time.time - shedAt };
        public void Apply(GoatCoatState state)
        {
            if (MountainAuthority.IsHost) return;
            regrowAt = state.remaining > 0 ? Time.time + state.remaining : 0;
            regrowDuration = Mathf.Max(1, state.duration);
            if (shedSerial != state.shedSerial && state.shedAge < .35f) EmitCoat();
            shedSerial = state.shedSerial; shedAt = Time.time - state.shedAge;
        }
        private void OnDestroy() { RestoreMaterials(); if (particleMaterial) Destroy(particleMaterial); }
    }
}
