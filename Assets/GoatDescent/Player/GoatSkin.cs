using System.Collections.Generic;
using UnityEngine;
namespace GoatDescent
{
    [DefaultExecutionOrder(200)]
    public sealed class GoatSkin : MonoBehaviour
    {
        public const int DefaultNumber = 11;
        private const string ChoiceKey = "GoatSkin.WithIbex.";
        public static readonly int[] Numbers = { 0, 11, 12, 7, 4 };
        public static readonly int[] VariantNumbers = { 7, 4 };
        public int Number { get; private set; } = -1;
        public GoatSkinDefinition HeadAttachments => activeDefinition && activeDefinition.useCustomHeadAttachments
            ? activeDefinition : null;
        public GoatSkinDefinition BodyAttachments => activeDefinition && activeDefinition.useCustomBodyAttachments
            ? activeDefinition : null;
        [SerializeField] private Renderer[] originalRenderers;
        [SerializeField] private bool[] originalEnabled;
        private SkinnedMeshRenderer skin;
        private Transform rig;
        private Dictionary<string, Transform> bones;
        private bool ready;
        private GoatSkinDefinition activeDefinition;
        private Transform tongue;
        private GameObject tongueSource;
        public static bool IsValid(int number) => System.Array.IndexOf(Numbers, number)>=0;
        public static GoatSkinDefinition Definition(int number) => Resources.Load<GoatSkinDefinition>($"GoatSkins/Skin{number:00}");
        private void Start()
        {
            if(Number>=0) return;
            string label=GetComponent<GoatLocalControl>()?.Label??"A";
            int fallback=DefaultNumber;
            // The new default is applied once per player. Later menu choices,
            // including the original goat, persist normally under this version.
            int saved=PlayerPrefs.GetInt(ChoiceKey+label,fallback);
            Set(IsValid(saved) ? saved : fallback,true);
        }
        public void Set(int number, bool save = false)
        {
            if(!IsValid(number)) return;
            var visual=transform.Find("VisualRoot — replaceable goat model");
            if(!visual) return;
            if(originalRenderers==null || originalRenderers.Length==0)
            {
                originalRenderers=visual.GetComponentsInChildren<Renderer>(true);
                originalEnabled=new bool[originalRenderers.Length];
                for(int i=0;i<originalRenderers.Length;i++) originalEnabled[i]=originalRenderers[i].enabled;
            }
            if(number==0)
            {
                BindAnimations(visual.GetComponentInChildren<Animation>(),null);
                activeDefinition=null;
                if(tongue) tongue.gameObject.SetActive(false);
                if(skin) skin.enabled=false;
                for(int i=0;i<originalRenderers.Length;i++)
                    if(originalRenderers[i]) originalRenderers[i].enabled=originalEnabled[i];
                Number=0;
                if(save) SaveChoice(number);
                return;
            }
            if(Number==number && ready && bones!=null) {RefreshTongue();return;}
            var definition=Definition(number);
            if(!definition || !definition.mesh) return;
            BindAnimations(visual.GetComponentInChildren<Animation>(),definition);
            if(!ready)
            {
                foreach(var t in visual.GetComponentsInChildren<Transform>(true)) if(t.name=="GoatRig_A") {rig=t;break;}
                if(!rig) return;
                bones=new Dictionary<string,Transform>();
                foreach(var t in rig.GetComponentsInChildren<Transform>(true)) bones.TryAdd(t.name,t);
                var node=new GameObject("Selected goat skin"); node.transform.SetParent(rig,false);
                skin=node.AddComponent<SkinnedMeshRenderer>(); skin.updateWhenOffscreen=true;
                // Torso and joint seams blend up to four physical bones. A low
                // graphics preset must not turn those transitions into rigid pieces.
                skin.quality=SkinQuality.Bone4;
                ready=true;
            }
            // Dictionaries are not restored by Unity's Play Mode script reload.
            if(bones==null)
            {
                bones=new Dictionary<string,Transform>();
                foreach(var t in rig.GetComponentsInChildren<Transform>(true)) bones.TryAdd(t.name,t);
            }
            var mapped=new Transform[definition.boneNames.Length];
            for(int i=0;i<mapped.Length;i++)
                if(!bones.TryGetValue(definition.boneNames[i],out mapped[i]))
                { Debug.LogError("Goat skin bone missing: "+definition.boneNames[i]); return; }
            skin.sharedMesh=definition.mesh;
            skin.sharedMaterials=definition.materials!=null && definition.materials.Length>0
                ? definition.materials : new[]{definition.material};
            foreach(var renderer in originalRenderers) if(renderer) renderer.enabled=false;
            skin.enabled=true;
            skin.bones=mapped; skin.rootBone=rig;
            skin.localBounds=definition.mesh.bounds;
            Number=number;
            activeDefinition=definition;
            RefreshTongue();
            if(save) SaveChoice(number);
        }
        private void LateUpdate()
        {
            // Existing goats also gain the accessory when its asset is imported
            // during Play Mode; parenting keeps it on the physically animated head.
            if(activeDefinition && activeDefinition.tonguePrefab && !tongue) RefreshTongue();
        }
        private void RefreshTongue()
        {
            if(!activeDefinition || !activeDefinition.tonguePrefab)
            {
                if(tongue) tongue.gameObject.SetActive(false);
                return;
            }
            var head=FindBone("Head");if(!head) return;
            if(tongue && tongueSource!=activeDefinition.tonguePrefab)
            {Destroy(tongue.gameObject);tongue=null;}
            if(!tongue)
            {
                tongue=Instantiate(activeDefinition.tonguePrefab,head,false).transform;
                tongue.name="Small pink tongue";
                tongueSource=activeDefinition.tonguePrefab;
            }
            // Imported bones have scale 200, while accessory dimensions are metres.
            Vector3 scale=head.lossyScale;
            Vector3 inverseScale=new Vector3(1f/scale.x,1f/scale.y,1f/scale.z);
            tongue.localPosition=Vector3.Scale(activeDefinition.tongueOffset,inverseScale);
            tongue.localRotation=activeDefinition.tongueRotation;
            tongue.localScale=inverseScale;
            tongue.gameObject.SetActive(true);
        }
        private void SaveChoice(int number)
        {
            PlayerPrefs.SetInt(ChoiceKey+(GetComponent<GoatLocalControl>()?.Label??"A"),number);
            PlayerPrefs.Save();
        }
        private static void BindAnimations(Animation animation,GoatSkinDefinition definition)
        {
            if(!animation) return;
            var clips=definition ? definition.animations : null;
            if(clips==null || clips.Length==0)
            {
                var reference=Resources.Load<GameObject>("GoatDuoRefined")?.GetComponentInChildren<Animation>();
                if(!reference) return;
                var shared=new List<AnimationClip>();
                foreach(AnimationState state in reference) shared.Add(state.clip);
                clips=shared.ToArray();
            }
            foreach(var clip in clips)
            {
                if(!clip || !clip.legacy || animation.GetClip(clip.name)==clip) continue;
                animation.AddClip(clip,clip.name);
                animation[clip.name].wrapMode=clip.wrapMode;
            }
        }
        public bool TryGetAttachment(string boneName,Vector3 offset,out Vector3 position)
        {
            // Offsets are in metre-sized physical bone space, whereas the
            // imported armature transforms retain the original FBX scale.
            var bone=rig ? FindBone(boneName) : null;
            position=bone ? bone.position+bone.rotation*offset : transform.position;
            return bone;
        }
        private Transform FindBone(string name)
        {
            if(bones!=null && bones.TryGetValue(name,out var bone)) return bone;
            foreach(var candidate in rig.GetComponentsInChildren<Transform>(true))
                if(candidate.name==name) return candidate;
            return null;
        }
    }
}
