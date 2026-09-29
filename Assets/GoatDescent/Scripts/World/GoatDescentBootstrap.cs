using UnityEngine;

namespace GoatDescent
{
    public static class GoatDescentBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreatePrototype()
        {
            if (Object.FindFirstObjectByType<ProceduralWorldGameplayBootstrap>() != null ||
                Object.FindFirstObjectByType<CliffBalancePrototypeBootstrap>() != null) return;
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            Application.targetFrameRate = 120;
            Application.runInBackground = true;

            var world = new GameObject("Goat Descent Procedural World");
            var bootstrap = world.AddComponent<ProceduralWorldGameplayBootstrap>();
            world.AddComponent<PrototypeHud>();
            bootstrap.Begin();
        }
    }

    public sealed class PrototypeHud : MonoBehaviour
    {
        private GUIStyle title;
        private GUIStyle copy;
        private GUIStyle warning;
        private void OnGUI()
        {
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            copy ??= new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(1f, 1f, 1f, .9f) } };
            warning ??= new GUIStyle(copy) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, .63f, .3f) } };
            GUI.Box(new Rect(16f, 12f, 920f, 96f), "");
            GUI.Label(new Rect(24, 22, 430, 32), "УРОВЕНЬ 1 — СПУСК", title);
            GUI.Label(new Rect(25, 56, 870, 25), "WASD — ходьба   Мышь — камера   Колесо — масштаб   R — к точке сохранения   Backspace — вершина", copy);
            var goat = LocalGoatPair.Instance ? LocalGoatPair.Instance.Active : Object.FindFirstObjectByType<GoatController>();
            string held = "";
            if (Input.GetKey(KeyCode.W)) held += "W "; if (Input.GetKey(KeyCode.A)) held += "A ";
            if (Input.GetKey(KeyCode.S)) held += "S "; if (Input.GetKey(KeyCode.D)) held += "D ";
            if (Input.GetKey(KeyCode.Space)) held += "SPACE ";
            float speed = goat ? goat.Velocity.magnitude : 0f;
            GUI.Label(new Rect(25, 81, 700, 25), $"Клавиши: {(string.IsNullOrEmpty(held) ? "— (нажми на окно игры, если управление не работает)" : held)}   Опора: {(goat && goat.Grounded ? "да" : "нет")}   Скорость: {speed:0.0} м/с", copy);
            var life = goat ? goat.GetComponent<RespawnController>() : null;
            var finish = Object.FindFirstObjectByType<PillarDescentLevel>();
            var jump = goat ? goat.GetComponent<GoatJumpController>() : null;
            var grip = goat ? goat.GetComponent<GoatCliffGrip>() : null;
            var vault = goat ? goat.GetComponent<GoatHornVault>() : null;
            float panelY = Screen.height - 145f;
            GUI.Box(new Rect(20f, panelY, 960f, 138f), "");
            GUI.Label(new Rect(30f, panelY + 7f, 880f, 22f), "Space: зажать/отпустить — дальний прыжок    Shift у скалы — зацеп    W/S — вверх/вниз    A/D — вдоль    Space на стене — отскок", copy);
            string state = vault && vault.IsVaulting ? "РОГОВОЙ РЫВОК" : grip && grip.IsGripping ? "ДЕРЖИТСЯ" : jump && jump.IsCharging ? "ЗАРЯЖАЕТ" : "ГОТОВ";
            GUI.Label(new Rect(30f, panelY + 31f, 930f, 22f), $"Заряд: {(jump ? jump.Charge01 * 100f : 0f):0}%    Зацеп: {(grip ? grip.Stamina01 * 100f : 0f):0}%    {state}", copy);
            if (finish)
            {
                float seconds = finish.ElapsedSeconds;
                GUI.Label(new Rect(30f, panelY + 55f, 930f, 22f),
                    $"Выступ: {Mathf.Max(0, finish.ProgressIndex + 1)}/{finish.LedgeCount}    Колокольчики: {finish.BellsCollected}/{finish.BellCount}    Рывки: {finish.VaultsUsed}/3    Время: {Mathf.FloorToInt(seconds / 60f):00}:{Mathf.FloorToInt(seconds % 60f):00}    Падения: {finish.Falls}", copy);
            }
            if (life && life.IsDead) GUI.Label(new Rect(30f, panelY + 78f, 900f, 32f), "Козёл разбился! Возвращение к точке сохранения...", title);
            else if (finish && finish.Completed) GUI.Label(new Rect(30f, panelY + 78f, 900f, 32f), "Долина достигнута!", title);
            else if (vault && vault.IsVaulting)
                GUI.Label(new Rect(30f, panelY + 80f, 900f, 24f), "РОГОВОЙ ПЕРЕЛЁТ — приземлись на голубой выступ!", warning);
            else if (finish && finish.CrumbleSecondsLeft > 0f)
                GUI.Label(new Rect(30f, panelY + 80f, 900f, 24f), $"КАМЕНЬ ОСЫПАЕТСЯ — прыгай{(vault && vault.NearbyStone ? " или жми G у фиолетового камня" : "")}! Осталось {finish.CrumbleSecondsLeft:0.0} с", warning);
            else if (vault && vault.NearbyStone)
                GUI.Label(new Rect(30f, panelY + 80f, 900f, 24f), $"G — ударь рогами по фиолетовому камню и пропусти {vault.NearbyStone.SkippedLedges} выступа", warning);
            else if (finish && finish.NextLedgePosition != Vector3.zero && goat)
            {
                Vector3 delta = finish.NextLedgePosition - goat.transform.position;
                Vector3 flat = Vector3.ProjectOnPlane(delta, Vector3.up);
                var view = Camera.main ? Camera.main.transform : null;
                float side = view ? Vector3.Dot(flat.normalized, view.right) : 0f;
                float forward = view ? Vector3.Dot(flat.normalized, Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized) : 1f;
                string direction = forward < -.25f ? "сзади" : side < -.3f ? "левее" : side > .3f ? "правее" : "впереди";
                GUI.Label(new Rect(30f, panelY + 80f, 900f, 24f), $"Следующий оранжевый маяк: {flat.magnitude:0} м, {direction} и ниже", copy);
            }
            else GUI.Label(new Rect(30f, panelY + 80f, 900f, 24f), "Иди к каменному указателю, затем спускайся по выступам.", copy);
            if (finish && !string.IsNullOrEmpty(finish.RecentEvent))
                GUI.Label(new Rect(30f, panelY + 108f, 900f, 22f), finish.RecentEvent, warning);
            else GUI.Label(new Rect(30f, panelY + 108f, 930f, 22f), "Оранжевый маяк — путь. Красный камень осыпается, голубой сохраняет, фиолетовый запускает рывок.", copy);
            string birdAlert = SkyPredatorEpisode.Current?.AlertText;
            if (!string.IsNullOrEmpty(birdAlert))
            {
                float alertWidth = Mathf.Min(620f, Screen.width - 40f);
                var alertRect = new Rect((Screen.width - alertWidth) * .5f, 109f, alertWidth, 27f);
                GUI.Box(alertRect, "");
                GUI.Label(new Rect(alertRect.x + 12f, alertRect.y + 3f, alertWidth - 24f, 22f), birdAlert, warning);
            }
            if (LocalGoatPair.Instance && goat)
            {
                var pair = LocalGoatPair.Instance;
                var interaction = goat.GetComponent<GoatInteraction>();
                GUI.Box(new Rect(18, 137, 650, 85), "");
                GUI.Label(new Rect(28, 143, 620, 24), pair.NetworkMode
                    ? $"КОЗЁЛ {goat.GetComponent<GoatLocalControl>().Label}   F — толчок / удержать — захват"
                    : $"КОЗЁЛ {goat.GetComponent<GoatLocalControl>().Label}   Tab — сменить   F — толчок / удержать — захват", copy);
                GUI.Label(new Rect(28, 169, 620, 24), interaction ? interaction.Status : "", warning);
                GUI.Label(new Rect(28, 194, 620, 24), pair.NetworkMode
                    ? "Ведущий считает физику   F10 — выступ   F11 — камень   F12 — орёл"
                    : "F6–F9 — взаимодействие   F10 — выступ   F11 — камень   F12 — орёл", copy);
            }
        }
    }
}
