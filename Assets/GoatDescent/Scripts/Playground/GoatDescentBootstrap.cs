using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoatDescent
{
    public static class GoatDescentBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreatePrototype()
        {
            if (SceneManager.GetActiveScene().name != "MovementTestScene" || Object.FindFirstObjectByType<MovementTestBootstrap>() != null) return;
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            Application.targetFrameRate = 120;

            var playground = new GameObject("Goat Playground Runtime");
            playground.AddComponent<MovementTestBootstrap>();
            playground.AddComponent<PrototypeHud>();
        }
    }

    public sealed class PrototypeHud : MonoBehaviour
    {
        private GUIStyle title;
        private GUIStyle copy;
        private void OnGUI()
        {
            if (SlopeRun.Instance) return;
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            copy ??= new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(1f, 1f, 1f, .9f) } };
            GUI.Label(new Rect(24, 22, 650, 32), "GOAT MOUNTAIN — DOWNHILL!", title);
            GUI.Label(new Rect(25, 56, 850, 25), "WASD move   Space jump   Shift+Space rocket   Space in air hop   C stomp", copy);
            GUI.Label(new Rect(25, 77, 850, 25), "Q in air: RAINBOW DASH!   E near wall: WALL BOING!   T: slow motion   R: reset   Esc: cursor", copy);
            var goat = Object.FindFirstObjectByType<GoatController>();
            string held = "";
            if (Input.GetKey(KeyCode.W)) held += "W "; if (Input.GetKey(KeyCode.A)) held += "A ";
            if (Input.GetKey(KeyCode.S)) held += "S "; if (Input.GetKey(KeyCode.D)) held += "D ";
            if (Input.GetKey(KeyCode.Space)) held += "SPACE ";
            float speed = goat ? goat.Velocity.magnitude : 0f;
            GUI.Label(new Rect(25, 102, 850, 25), $"Input: {(string.IsNullOrEmpty(held) ? "— (click Game View if keys do not light up)" : held)}   Grounded: {(goat && goat.Grounded ? "yes" : "no")}   Speed: {speed:0.0} m/s", copy);
            var slow = Object.FindFirstObjectByType<MovementTestTimeControls>();
            var wall = Object.FindFirstObjectByType<GoatWallJumpController>();
            GUI.Label(new Rect(25, 125, 750, 25), $"Time: {(slow && slow.IsSlow ? "SLOW" : "normal")}   Wall jump: {(wall && wall.WallNearby ? "READY — press E" : "jump close to wall")}   Left: {(wall ? wall.JumpsLeft : 0)}", copy);
            var jump = Object.FindFirstObjectByType<GoatJumpController>();
            if (jump && jump.IsTrickShowing)
                GUI.Label(new Rect(25, 155, 500, 35), jump.CurrentTrick, title);
        }
    }
}
