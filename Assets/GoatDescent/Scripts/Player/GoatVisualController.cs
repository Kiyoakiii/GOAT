using UnityEngine;

namespace GoatDescent
{
    public sealed class GoatVisualController : MonoBehaviour
    {
        private Rigidbody body; private GoatGroundDetector ground; private Transform visual; private float landSquash;
        public void Configure(Rigidbody targetBody, GoatGroundDetector targetGround) { body = targetBody; ground = targetGround; }
        private void Awake() { body ??= GetComponent<Rigidbody>(); ground ??= GetComponent<GoatGroundDetector>(); }
        private void Start() { visual = transform.Find("VisualRoot — replaceable goat model"); if (!visual) BuildVisual(); }
        private void BuildVisual()
        {
            visual = new GameObject("VisualRoot — replaceable goat model").transform; visual.SetParent(transform); visual.localPosition = new Vector3(0, .33f, 0);
            var fur = MakeMaterial(new Color(.84f, .77f, .61f)); var dark = MakeMaterial(new Color(.17f, .13f, .10f)); var horn = MakeMaterial(new Color(.68f, .58f, .41f)); var trailAccent = MakeMaterial(new Color(.86f, .24f, .10f));
            Part(PrimitiveType.Sphere, "Compact body", new Vector3(0, .25f, 0), new Vector3(.82f, .55f, 1.15f), fur);
            Part(PrimitiveType.Cube, "Trail blanket", new Vector3(0, .43f, -.08f), new Vector3(.86f, .13f, .45f), trailAccent);
            Part(PrimitiveType.Cube, "Tiny climbing pack", new Vector3(0, .39f, -.50f), new Vector3(.48f, .34f, .25f), trailAccent);
            Part(PrimitiveType.Sphere, "Comical head", new Vector3(0, .66f, .72f), new Vector3(.64f, .58f, .57f), fur);
            Part(PrimitiveType.Sphere, "Beard", new Vector3(0, .34f, 1.02f), new Vector3(.22f, .34f, .19f), dark);
            Part(PrimitiveType.Sphere, "Little tail", new Vector3(0, .42f, -.62f), new Vector3(.24f, .24f, .25f), fur);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(PrimitiveType.Capsule, "Powerful leg", new Vector3(.34f * side, -.32f, .38f), new Vector3(.22f, .52f, .22f), dark);
                Part(PrimitiveType.Capsule, "Powerful leg", new Vector3(.34f * side, -.32f, -.45f), new Vector3(.22f, .52f, .22f), dark);
                var h = Part(PrimitiveType.Cylinder, "Curved horn", new Vector3(.28f * side, 1.12f, .72f), new Vector3(.10f, .42f, .10f), horn); h.transform.localRotation = Quaternion.Euler(22f, 0, -25f * side);
                Part(PrimitiveType.Sphere, "Eye", new Vector3(.25f * side, .76f, 1.16f), new Vector3(.09f, .09f, .09f), dark);
                Part(PrimitiveType.Sphere, "Floppy ear", new Vector3(.42f * side, .83f, .70f), new Vector3(.20f, .12f, .25f), fur);
            }
        }
        private GameObject Part(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            var p = GameObject.CreatePrimitive(type); p.name = name; p.transform.SetParent(visual); p.transform.localPosition = position; p.transform.localScale = scale; p.GetComponent<Renderer>().sharedMaterial = material; Destroy(p.GetComponent<Collider>()); return p;
        }
        private Material MakeMaterial(Color color) { var m = new Material(Shader.Find("Standard")) { color = color }; m.SetFloat("_Glossiness", .2f); return m; }
        private void Update()
        {
            body ??= GetComponent<Rigidbody>(); ground ??= GetComponent<GoatGroundDetector>(); visual ??= transform.Find("VisualRoot — replaceable goat model");
            if (!visual || !body) return;
            Vector3 horizontal = body.linearVelocity; horizontal.y = 0;
            if (horizontal.sqrMagnitude > .2f) visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.LookRotation(horizontal.normalized, ground && ground.IsGrounded ? ground.GroundNormal : Vector3.up), Time.deltaTime * 9f);
            if (ground && ground.IsGrounded && body.linearVelocity.y < -2f) landSquash = .14f;
            landSquash = Mathf.MoveTowards(landSquash, 0, Time.deltaTime * 1.6f);
            visual.localScale = new Vector3(1f + landSquash * .35f, 1f - landSquash, 1f + landSquash * .35f);
        }
    }
}
