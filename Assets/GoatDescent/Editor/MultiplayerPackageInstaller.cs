using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace GoatDescent.Editor
{
    public static class MultiplayerPackageInstaller
    {
        private static AddAndRemoveRequest request;
        private static double deadline;

        public static void Install()
        {
            request = Client.AddAndRemove(new[]
            {
                "com.unity.services.multiplayer",
                "com.unity.netcode.gameobjects"
            }, new string[0]);
            deadline = EditorApplication.timeSinceStartup + 600;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!request.IsCompleted && EditorApplication.timeSinceStartup < deadline) return;
            EditorApplication.update -= Poll;
            if (!request.IsCompleted)
            {
                Debug.LogError("GOAT_MULTIPLAYER_PACKAGES timed out");
                EditorApplication.Exit(2);
                return;
            }
            if (request.Status != StatusCode.Success)
            {
                Debug.LogError("GOAT_MULTIPLAYER_PACKAGES failed: " + request.Error?.message);
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("GOAT_MULTIPLAYER_PACKAGES installed: " + string.Join(", ", request.Result.Select(p => p.name + "@" + p.version)));
            EditorApplication.Exit(0);
        }
    }
}
