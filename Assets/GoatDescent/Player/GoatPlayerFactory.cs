using UnityEngine;

namespace GoatDescent
{
    /// <summary>Creates the same playable goat for the small practice scene.</summary>
    public static class GoatPlayerFactory
    {
        public static GoatController Create(Vector3 spawn, float initialYaw = 0f,
            bool checkpointRespawn = true, bool configureCamera = true)
        {
            var goat = new GameObject("Mountain Goat");
            goat.transform.position = spawn;
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0) goat.layer = playerLayer;

            var body = goat.AddComponent<Rigidbody>();
            body.mass = 72f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            var capsule = goat.AddComponent<CapsuleCollider>();
            capsule.radius = .34f;
            capsule.height = 1.15f;
            capsule.center = new Vector3(0f, .58f, 0f);

            var detector = goat.AddComponent<GoatGroundDetector>();
            var controller = goat.AddComponent<GoatController>();
            controller.Configure(body, detector);
            goat.AddComponent<GoatLocalControl>();
            goat.AddComponent<GoatJumpController>().Configure(controller, detector);
            goat.AddComponent<GoatCliffGrip>();
            goat.AddComponent<GoatGripController>();
            goat.AddComponent<GoatSlopeBalance>();
            goat.AddComponent<GoatLandingAssist>().Configure(body, detector);
            goat.AddComponent<GoatVisualController>().Configure(body, detector);
            goat.AddComponent<GoatPhysicalBody>();
            goat.AddComponent<GoatInteraction>();
            goat.AddComponent<GoatSpectacle>();
            var routeProgress = goat.AddComponent<RouteProgressTracker>();
            routeProgress.Configure(MountainGenerator.CurrentRoute, MountainMaterial.Get(), spawn, checkpointRespawn);
            goat.AddComponent<RespawnController>().Configure(body, spawn, routeProgress);

            var camera = Camera.main;
            if (!camera)
            {
                var cameraObject = new GameObject("Goat Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(.52f, .73f, .90f);
            camera.farClipPlane = 900f;
            if (!camera.GetComponent<CinematicPostProcess>())
                camera.gameObject.AddComponent<CinematicPostProcess>();
            var follow = camera.GetComponent<ThirdPersonGoatCamera>();
            if (!follow) follow = camera.gameObject.AddComponent<ThirdPersonGoatCamera>();
            if (configureCamera) follow.Configure(goat.transform, initialYaw);
            return controller;
        }
    }
}
