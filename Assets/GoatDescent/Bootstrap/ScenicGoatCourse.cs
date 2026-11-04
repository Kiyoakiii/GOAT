using UnityEngine;

namespace GoatDescent
{
    public sealed class ScenicGoatCourse : MonoBehaviour
    {
        [Header("Route")]
        [SerializeField, Tooltip("World-space trail samples.")] private Vector3[] points;
        [SerializeField, Tooltip("Indices of safe resting places.")] private int[] checkpoints;
        [SerializeField, Tooltip("Material used for route progress.")] private Material trailMaterial;
        [SerializeField] private Vector3[] jumpTakeoffs = new Vector3[0];
        [SerializeField] private Vector3[] jumpLandings = new Vector3[0];
        [SerializeField] private float[] landingRadii = new float[0];
        [SerializeField] private bool descending;
        public bool Descending => descending;
        public void ConfigureDescent(float[] radii) { landingRadii=radii;descending=true; }
        public Vector3[] JumpTakeoffs => jumpTakeoffs;
        public Vector3[] JumpLandings => jumpLandings;
        public float ElapsedSeconds { get; private set; }
        public void ConfigureJumps(Vector3[] takeoffs, Vector3[] landings)
        {
            jumpTakeoffs = takeoffs;
            jumpLandings = landings;
        }
        private RouteProgressTracker progress;
        private RespawnController life;
        private GoatController goat;
        private float nextUpdate;
        private Material runtimeMaterial;
        public Vector3[] Points => points;
        public bool Finished { get; private set; }

        public void Configure(Vector3[] route, int[] restPoints, Material material)
        {
            points = route;
            checkpoints = restPoints;
            trailMaterial = material;
        }

        private void Start()
        {
            if (points == null || points.Length < 2) return;
            var platforms = new LandingPlatform[points.Length];
            int next = 0;
            for (int i = 0; i < points.Length; i++)
            {
                bool checkpoint = next < checkpoints.Length && checkpoints[next] == i;
                if (checkpoint) next++;
                platforms[i] = new LandingPlatform
                {
                    Center = points[i], Radius = landingRadii.Length == points.Length ? landingRadii[i] : checkpoint ? 5f : 2.5f,
                    Index = i, Progress = i / (float)(points.Length - 1), IsCheckpoint = checkpoint
                };
            }
            Vector3 heading = points[1] - points[0];
            runtimeMaterial = new Material(trailMaterial);
            int spawnIndex = !descending && jumpTakeoffs.Length > 0 ? Mathf.Min(6, points.Length - 1) : 0;
            goat = GoatPlayerFactory.Create(points[spawnIndex] + Vector3.up * 1.2f,
                Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg, true, true, platforms, runtimeMaterial);
            progress = goat.GetComponent<RouteProgressTracker>();
            life = goat.GetComponent<RespawnController>();
            var camera = Camera.main;
            if (camera)
            {
                camera.farClipPlane = 24000f;
                camera.nearClipPlane = .08f;
                var cinematic = camera.GetComponent<CinematicPostProcess>();
                if (cinematic) cinematic.enabled = false;
                if (gameObject.scene.name == "TerraceCliffStudy")
                {
                    var follow = camera.GetComponent<ThirdPersonGoatCamera>();
                    if (follow) follow.Configure(goat.transform, Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg, 27f);
                }
            }
            SetKillPlane();
        }

        private void Update()
        {
            if (goat && !Finished) ElapsedSeconds += Time.deltaTime;
            if (!goat || Time.time < nextUpdate) return;
            nextUpdate = Time.time + .2f;
            SetKillPlane();
            if (!Finished && progress.CurrentProgress > .995f && Vector3.Distance(goat.transform.position, points[points.Length - 1]) < 8f)
            {
                Finished = true;
                Debug.Log("SCENIC_COURSE_FINISHED " + gameObject.scene.name + " seconds=" + ElapsedSeconds);
            }
        }

        private void OnDestroy()
        {
            if (runtimeMaterial) Destroy(runtimeMaterial);
        }

        private void OnDrawGizmosSelected()
        {
            if (points == null) return;
            Gizmos.color = new Color(1f,.75f,.15f);
            for (int i = 1; i < points.Length; i++) Gizmos.DrawLine(points[i-1],points[i]);
            if (checkpoints == null) return;
            Gizmos.color = Color.green;
            foreach (int checkpoint in checkpoints)
                if (checkpoint >= 0 && checkpoint < points.Length) Gizmos.DrawWireSphere(points[checkpoint],4.5f);
        }

        private void SetKillPlane()
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(progress.CurrentProgress * (points.Length - 1)), 0, points.Length - 1);
            float low = points[index].y;
            for (int i = index; i < Mathf.Min(points.Length, index + (descending ? 2 : 24)); i++) low = Mathf.Min(low, points[i].y);
            life.SetKillPlane(low - 45f);
        }
    }
}
