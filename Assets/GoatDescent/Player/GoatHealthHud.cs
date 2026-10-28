using UnityEngine;
using UnityEngine.UIElements;

namespace GoatDescent
{
    public sealed class GoatHealthHud : MonoBehaviour
    {
        private UIDocument document;
        private PanelSettings panel;
        private VisualElement fill,trail,card;
        private Label value,owner,beer,herdStatus,coatStatus;
        private VisualElement rockfallAlert;
        private Label rockfallTitle,rockfallHint,rockfallProtection;
        private float shown=1f,previous=1f;
        private GoatController target;
        private void Start()
        {
            panel=ScriptableObject.CreateInstance<PanelSettings>();panel.name="Goat health HUD";
            panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(1280,720);
            panel.match=.5f;panel.sortingOrder=500;
            var node=new GameObject("Goat health overlay");node.transform.SetParent(transform,false);
            document=node.AddComponent<UIDocument>();document.panelSettings=panel;
            document.visualTreeAsset=Resources.Load<VisualTreeAsset>("GoatHealthHud");
            var root=document.rootVisualElement;
            root.styleSheets.Add(Resources.Load<StyleSheet>("GoatHealthHud"));
            root.AddToClassList("health-document");
            foreach(var child in root.Children()) if(child is TemplateContainer) child.AddToClassList("health-template");
            root.style.unityFontDefinition=FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            root.Query<VisualElement>().ForEach(element=>element.pickingMode=PickingMode.Ignore);
            root.pickingMode=PickingMode.Ignore;
            card=root.Q("healthCard");fill=root.Q("healthFill");trail=root.Q("healthTrail");
            value=root.Q<Label>("healthValue");owner=root.Q<Label>("healthOwner");beer=root.Q<Label>("beerStatus");
            herdStatus=root.Q<Label>("herdStatus");coatStatus=root.Q<Label>("coatStatus");
            rockfallAlert=root.Q("rockfallAlert");rockfallTitle=root.Q<Label>("rockfallTitle");
            rockfallHint=root.Q<Label>("rockfallHint");rockfallProtection=root.Q<Label>("rockfallProtection");
        }
        private void Update()
        {
            if(!document) return;
            var goat=LocalGoatPair.Instance?.Active;
            UpdateRockfall(goat);
            if(!goat) {card.AddToClassList("hidden");return;}
            card.RemoveFromClassList("hidden");
            var health=goat.GetComponent<GoatHealth>();if(!health) return;
            float fraction=health.Fraction;
            if(target!=goat) {target=goat;shown=previous=fraction;}
            shown=Mathf.MoveTowards(shown,fraction,Time.unscaledDeltaTime*1.8f);
            previous=Mathf.MoveTowards(previous,fraction,Time.unscaledDeltaTime*.5f);
            fill.style.width=Length.Percent(shown*100f);trail.style.width=Length.Percent(previous*100f);
            value.text=Mathf.CeilToInt(health.Current)+" / 100";
            owner.text="КОЗЁЛ "+(goat.GetComponent<GoatLocalControl>()?.Label??"A")+"  ·  ЗДОРОВЬЕ";
            card.EnableInClassList("hurt",fraction<.35f);
            float remaining=goat.GetComponent<GoatBeerEffect>()?.Remaining??0f;
            beer.EnableInClassList("hidden",remaining<=0f);
            beer.text="ПИВНАЯ ВАННА  ·  "+Mathf.CeilToInt(remaining)+" С";
            var nerves=goat.GetComponent<GoatHerdBehaviour>();
            bool electric=nerves && nerves.Charge>.025f;
            herdStatus.EnableInClassList("hidden",!electric && (!nerves || !nerves.Isolated));
            herdStatus.text=nerves && nerves.Grounding ? "ЗАЗЕМЛЕНИЕ · "+Mathf.CeilToInt(nerves.Charge*100)+"%"
                : (nerves && nerves.Isolated ? "ОДИНОЧЕСТВО" : "СТАТИКА")+(electric ? " · ЗАРЯД "+Mathf.CeilToInt(nerves.Charge*100)+"%" : "");
            float coat=goat.GetComponent<GoatFurEffect>()?.Remaining??0;
            coatStatus.EnableInClassList("hidden",coat<=0);
            int seconds=Mathf.CeilToInt(coat);
            coatStatus.text="ШЕРСТЬ ОТРАСТАЕТ · "+(seconds/60)+":"+(seconds%60).ToString("00");
        }
        private void UpdateRockfall(GoatController goat)
        {
            if(rockfallAlert==null) return;
            var fall=RockfallEpisode.Current;
            bool show=goat && fall && (fall.IsDangerous || fall.Phase==RockfallPhase.Complete && fall.SecondsLeft>0)
                && fall.InArea(goat.transform.position,35);
            rockfallAlert.EnableInClassList("hidden",!show);
            if(!show) return;
            rockfallTitle.text=fall.Phase==RockfallPhase.Warning ? "КАМНЕПАД ЧЕРЕЗ "+Mathf.CeilToInt(fall.SecondsLeft)+" С"
                : fall.Phase==RockfallPhase.Falling ? "КАМНЕПАД — К СКАЛЕ!"
                : fall.Phase==RockfallPhase.Settling ? "ОСТОРОЖНО: ПОСЛЕДНИЕ КАМНИ" : "КАМНЕПАД ЗАКОНЧИЛСЯ";
            rockfallHint.text=fall.Phase==RockfallPhase.Complete ? "Можно продолжать спуск."
                : "У скалы остановись и держи Ctrl. Повернись к напарнику и удерживай F — сцепить рога.";
            var shelter=goat.GetComponent<GoatRockfallShelter>();
            rockfallProtection.text=fall.Phase==RockfallPhase.Complete || !shelter ? ""
                : !shelter.NearWall ? "Подойди ближе к скале"
                : !shelter.Still ? "Не двигайся — закрепись на выступе"
                : !shelter.Bracing ? "Зажми Ctrl, чтобы сильнее упереться"
                : goat.GetComponent<GoatInteraction>()?.IsLinked!=true ? "Держи F — сцепись с напарником"
                : "ДЕРЖИТЕСЬ ВМЕСТЕ · ЗАЩИТА "+Mathf.RoundToInt(shelter.Protection*100)+"%";
        }
        private void OnDestroy() {if(panel) Destroy(panel);}
    }
}
