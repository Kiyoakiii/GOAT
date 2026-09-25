using UnityEngine;

namespace GoatDescent
{
    public static class GoatDescentBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreatePrototype()
        {
            if (Object.FindFirstObjectByType<ProceduralWorldGameplayBootstrap>() != null) return;
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            Application.targetFrameRate = 120;

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
        private void OnGUI()
        {
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            copy ??= new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(1f, 1f, 1f, .9f) } };
            GUI.Label(new Rect(24, 22, 430, 32), "GOAT DESCENT", title);
            GUI.Label(new Rect(25, 56, 850, 25), "WASD — move   Space — jump   Mouse — camera   Wheel — zoom   R — summit reset   Esc — cursor", copy);
            var goat = Object.FindFirstObjectByType<GoatController>();
            string held = "";
            if (Input.GetKey(KeyCode.W)) held += "W "; if (Input.GetKey(KeyCode.A)) held += "A ";
            if (Input.GetKey(KeyCode.S)) held += "S "; if (Input.GetKey(KeyCode.D)) held += "D ";
            if (Input.GetKey(KeyCode.Space)) held += "SPACE ";
            float speed = goat ? goat.Velocity.magnitude : 0f;
            GUI.Label(new Rect(25, 81, 700, 25), $"Input: {(string.IsNullOrEmpty(held) ? "— (click Game View if keys do not light up)" : held)}   Grounded: {(goat && goat.Grounded ? "yes" : "no")}   Speed: {speed:0.0} m/s", copy);
        }
    }
}