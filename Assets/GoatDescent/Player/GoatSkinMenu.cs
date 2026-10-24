using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
namespace GoatDescent
{
    [DefaultExecutionOrder(-200)]
    public sealed class GoatSkinMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private UIDocument document;
        private PanelSettings settings;
        private VisualElement overlay,cards,players;
        private Label selection;
        private GoatController target;
        private CursorLockMode previousLock;
        private bool previousVisible;
        private bool restoreCursor;
        private void Start()
        {
            settings=ScriptableObject.CreateInstance<PanelSettings>(); settings.name="Goat skin menu";
            settings.scaleMode=PanelScaleMode.ScaleWithScreenSize; settings.referenceResolution=new Vector2Int(1280,720);
            settings.match= .5f; settings.sortingOrder=1000;
            var node=new GameObject("Goat skin selection overlay");
            node.transform.SetParent(transform,false);
            document=node.AddComponent<UIDocument>(); document.panelSettings=settings;
            document.visualTreeAsset=Resources.Load<VisualTreeAsset>("GoatSkinMenu");
            var root=document.rootVisualElement;
            root.style.position=Position.Absolute;
            root.style.left=0; root.style.right=0; root.style.top=0; root.style.bottom=0;
            root.style.width=Length.Percent(100); root.style.height=Length.Percent(100);
            foreach(var child in root.Children())
                if(child is TemplateContainer) { child.style.flexGrow=1; child.style.width=Length.Percent(100); child.style.height=Length.Percent(100); }
            root.style.unityFontDefinition=FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            root.pickingMode=PickingMode.Ignore;
            overlay=root.Q("overlay"); cards=root.Q("cards"); players=root.Q("players"); selection=root.Q<Label>("selection");
            root.Q("open").style.display=DisplayStyle.None;
            root.Q<Button>("close").clicked+=Close;
            root.Query<Button>().ForEach(button=>{button.focusable=false;button.tabIndex=-1;});
            root.RegisterCallback<KeyDownEvent>(evt=>
            {
                if(evt.keyCode==KeyCode.Space) {evt.PreventDefault();evt.StopImmediatePropagation();}
            },TrickleDown.TrickleDown);
            overlay.style.display=DisplayStyle.None;
        }
        private void Update()
        {
            if(!document || GoatSettingsMenu.BlocksInput) return;
            if(Input.GetKeyDown(KeyCode.M)) { if(IsOpen) Close(); else Open(); }
            else if(IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }
        public void Open()
        {
            var pair=LocalGoatPair.Instance;
            if(!pair || !pair.Active || !document || GoatSettingsMenu.IsOpen) return;
            if(!IsOpen) { previousLock=Cursor.lockState; previousVisible=Cursor.visible; }
            IsOpen=true; Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            overlay.style.display=DisplayStyle.Flex; document.rootVisualElement.Q("open").style.display=DisplayStyle.None;
            target=pair.Active; Refresh();
        }
        public void Close()
        {
            IsOpen=false;
            if(document) overlay.style.display=DisplayStyle.None;
            Cursor.lockState=previousLock; Cursor.visible=previousVisible;
            restoreCursor=true;
        }
        private void LateUpdate()
        {
            if(IsOpen) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
            else if(restoreCursor) { Cursor.lockState=previousLock; Cursor.visible=previousVisible; restoreCursor=false; }
        }
        private void Refresh()
        {
            var pair=LocalGoatPair.Instance; players.Clear(); cards.Clear();
            foreach(var goat in new[]{pair.Primary,pair.Secondary})
            {
                if(!goat) continue;
                var button=new Button(()=>{target=goat;Refresh();}) {text="Козёл "+goat.GetComponent<GoatLocalControl>().Label,focusable=false,tabIndex=-1};
                if(target==goat) button.AddToClassList("active");
                button.SetEnabled(!pair.NetworkMode || goat==pair.Active); players.Add(button);
            }
            var skin=target.GetComponent<GoatSkin>();
            foreach(int number in GoatSkin.Numbers)
            {
                var definition=GoatSkin.Definition(number);
                if(number!=0 && !definition) continue;
                var card=new Button(()=>{skin.Set(number,true);Refresh();}) {focusable=false,tabIndex=-1}; card.AddToClassList("skin-card");
                if(skin.Number==number) card.AddToClassList("selected");
                var picture=new VisualElement(); picture.AddToClassList("skin-picture");
                picture.style.backgroundImage=new StyleBackground(number==0 ? Resources.Load<Texture2D>("GoatSkins/Preview00") : definition.preview); picture.pickingMode=PickingMode.Ignore;
                card.Add(picture);
                var label=new Label(number==0 ? "Исходный козёл" : $"{number:00} · {definition.displayName}"); label.AddToClassList("skin-name"); label.pickingMode=PickingMode.Ignore; card.Add(label);
                cards.Add(card);
            }
            var current=GoatSkin.Definition(skin.Number);
            selection.text=skin.Number==0 ? "Выбран: исходный козёл · сохранено" : current ? "Выбран: "+current.displayName+" · сохранено" : "Выбери скин";
        }
        private void OnDestroy() { IsOpen=false; if(settings) Destroy(settings); }
    }
}
