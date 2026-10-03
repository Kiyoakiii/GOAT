using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    public sealed class RouteProgressTracker : MonoBehaviour
    {
        private IReadOnlyList<LandingPlatform> route;
        private Material routeMaterial;
        private GoatGroundDetector ground;
        private Vector3 checkpoint;
        private bool checkpointRespawn = true;
        private float currentProgress;
        private float checkpointProgress;
        private float nextUpdate;

        public float CurrentProgress => currentProgress;
        public Vector3 CheckpointPosition => checkpoint;

        public void Configure(IReadOnlyList<LandingPlatform> platforms, Material material, Vector3 spawn, bool useCheckpoints)
        {
            route = platforms;
            routeMaterial = material;
            checkpoint = spawn;
            checkpointRespawn = useCheckpoints;
            ground = GetComponent<GoatGroundDetector>();
            currentProgress = 0f;
            checkpointProgress = 0f;
            ApplyProgress();
        }

        private void Awake()
        {
            ground ??= GetComponent<GoatGroundDetector>();
        }

        private void Update()
        {
            if (Time.time < nextUpdate || route == null || route.Count == 0) return;
            nextUpdate = Time.time + 0.08f;
            if (!ground) ground = GetComponent<GoatGroundDetector>();
            if (!ground || !ground.IsGrounded) return;

            Vector3 position = transform.position;
            for (int i = 0; i < route.Count; i++)
            {
                LandingPlatform platform = route[i];
                Vector2 offset = new Vector2(position.x - platform.Center.x, position.z - platform.Center.z);
                float radius = Mathf.Max(0.8f, platform.Radius + 1.15f);
                if (offset.sqrMagnitude > radius * radius || Mathf.Abs(position.y - platform.Center.y) > 3.4f) continue;

                if (platform.Progress > currentProgress)
                {
                    currentProgress = platform.Progress;
                    ApplyProgress();
                }

                if (checkpointRespawn && platform.IsCheckpoint && platform.Progress >= checkpointProgress)
                {
                    checkpoint = platform.Center + Vector3.up * 1.45f;
                    checkpointProgress = platform.Progress;
                }
            }
        }

        public void ResetToCheckpoint()
        {
            currentProgress = checkpointRespawn ? checkpointProgress : 0f;
            ApplyProgress();
        }

        public void ResetToSummit()
        {
            if (route != null && route.Count > 0)
                checkpoint = route[0].Center + Vector3.up * 1.45f;
            checkpointProgress = currentProgress = 0f;
            ApplyProgress();
        }

        private void ApplyProgress()
        {
            if (routeMaterial) routeMaterial.SetFloat("_RouteProgress", currentProgress);
        }
    }
}
