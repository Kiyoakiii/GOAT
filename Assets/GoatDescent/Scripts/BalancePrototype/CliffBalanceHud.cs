using UnityEngine;

namespace GoatDescent
{
    /// <summary>Development HUD. The goat's posture and hoof colours remain the primary feedback.</summary>
    public sealed class CliffBalanceHud : MonoBehaviour
    {
        private CliffBalancePrototypeBootstrap level;
        private GUIStyle title, body, big, small;
        private Texture2D white;
        public void Initialize(CliffBalancePrototypeBootstrap owner) => level = owner;

        private void OnGUI()
        {
            if (!level || !level.Goat) return;
            EnsureStyles();
            var goat = level.Goat;
            float width = Mathf.Min(560f, Screen.width - 30f);
            DrawBox(new Rect(12f, 12f, width, 172f), new Color(.06f, .075f, .08f, .77f));
            GUI.Label(new Rect(25f, 20f, width - 26f, 30f), "УРОВЕНЬ 1 — СКАЛА", title);
            string timer = level.Started ? $"{level.Remaining:00.0} с" : "90.0 с";
            GUI.Label(new Rect(25f, 51f, width - 26f, 22f),
                $"До охоты: {timer}     {(level.PredatorActive ? "PredatorActive" : "Пещера внизу")}", body);
            GUI.Label(new Rect(25f, 78f, width - 26f, 20f),
                "W/S — вперёд/назад   A/D — влево/вправо", small);
            GUI.Label(new Rect(25f, 99f, width - 26f, 20f),
                "Мышь — наклон   Space — прыжок   R — заново", small);
            GUI.Label(new Rect(25f, 125f, width - 26f, 24f), goat.Posture, body);
            GUI.Label(new Rect(25f, 151f, width - 26f, 21f), $"Высота {goat.Height:0.0} м   Срывы {goat.Falls}", small);

            float barWidth = Mathf.Min(305f, Screen.width * .36f);
            var bar = new Rect(Screen.width - barWidth - 18f, 23f, barWidth, 18f);
            DrawBox(new Rect(bar.x - 10f, 12f, barWidth + 20f, 267f), new Color(.06f, .075f, .08f, .77f));
            GUI.Label(new Rect(bar.x, 47f, barWidth, 20f), "3D баланс / проекция веса", small);
            DrawBox(bar, new Color(.20f, .22f, .23f));
            DrawBox(new Rect(bar.x + 2f, bar.y + 2f, (barWidth - 4f) * goat.Balance01, 14f),
                Color.Lerp(new Color(.96f, .25f, .13f), new Color(.36f, .82f, .45f), goat.Balance01));
            string[] names = { "ПЛ", "ПП", "ЗЛ", "ЗП" };
            for (int i = 0; i < 4; i++)
            {
                var state = goat.GetHoofState(i);
                GUI.color = state == HoofState.Anchored ? new Color(.55f, 1f, .55f)
                    : state == HoofState.Sliding ? new Color(1f, .45f, .27f)
                    : state == HoofState.Seeking ? new Color(1f, .83f, .42f) : Color.grey;
                GUI.Label(new Rect(bar.x + i * barWidth / 4f, 72f, barWidth / 4f, 20f), names[i] + " " + Abbr(state), small);
            }
            GUI.color = Color.white;
            DrawBox(new Rect(bar.x, 104f, barWidth, 5f), new Color(.22f, .23f, .24f));
            float depthMarker = Mathf.Clamp01(.5f + goat.DepthOffset / .70f);
            DrawBox(new Rect(bar.x + depthMarker * (barWidth - 7f), 99f, 7f, 15f), Color.white);
            GUI.Label(new Rect(bar.x, 116f, barWidth, 20f), "Мышь вправо — от скалы, влево — к скале", small);
            float padX = bar.x + (barWidth - 112f) * .5f;
            DrawBox(new Rect(padX, 147f, 112f, 100f), new Color(.20f, .22f, .23f));
            DrawBox(new Rect(padX + 55f, 151f, 2f, 92f), new Color(.44f, .47f, .48f));
            DrawBox(new Rect(padX + 4f, 196f, 104f, 2f), new Color(.44f, .47f, .48f));
            Vector2 mouse = goat.MouseBalance;
            DrawBox(new Rect(padX + 52f + mouse.x * 45f, 193f - mouse.y * 40f, 8f, 8f),
                Color.Lerp(new Color(.43f, .91f, .49f), new Color(1f, .37f, .22f), mouse.magnitude));
            GUI.Label(new Rect(bar.x, 249f, barWidth, 20f), "Положение веса под мышью", small);

            if (!string.IsNullOrEmpty(level.Message))
            {
                float messageWidth = Mathf.Min(700f, Screen.width - 30f);
                var panel = new Rect((Screen.width - messageWidth) / 2f, Screen.height - 92f, messageWidth, 65f);
                DrawBox(panel, new Color(.05f, .06f, .07f, .83f));
                GUI.Label(new Rect(panel.x + 13f, panel.y + 14f, panel.width - 26f, 44f), level.Message, big);
            }
        }

        private static string Abbr(HoofState state) => state == HoofState.Anchored ? "держит"
            : state == HoofState.Seeking ? "ищет" : state == HoofState.Sliding ? "скользит" : "свободна";

        private void EnsureStyles()
        {
            if (title != null) return;
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
            title = Style(23, FontStyle.Bold);
            body = Style(16, FontStyle.Bold);
            big = Style(22, FontStyle.Bold);
            big.alignment = TextAnchor.MiddleCenter;
            small = Style(14, FontStyle.Normal);
        }

        private static GUIStyle Style(int size, FontStyle weight) => new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = weight,
            normal = { textColor = Color.white }
        };

        private void DrawBox(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, white);
            GUI.color = previous;
        }
    }
}
