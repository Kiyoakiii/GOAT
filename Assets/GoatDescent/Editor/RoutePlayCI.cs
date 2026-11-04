using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    [InitializeOnLoad]
    public static class RoutePlayCI
    {
        private const string Armed = "GOAT.Route.PlayCheck";
        private static Vector3[] route;
        private static GoatController goat;
        private static Rigidbody body;
        private static RespawnController life;
        private static GoatJumpController jump;
        private static int index;
        private static bool wasDead;
        private static double pollAt, beginAt;
        private static float simStart, lastProgressTime, lastJump;
        private static float pathLow;
        private static float nextLog = 5f;
        private static float sideSign, nextSideSwitch;
        private static int deaths, checkpointCount, nextCheckpointWp = 150;
        private static int checkpointWpIndex, respawnIndex;
        private static readonly List<string> deathLog = new List<string>();
        private static Vector3 lastProgressPos;
        private static float timeout = 1800f;

        static RoutePlayCI() => EditorApplication.update += Poll;

        public static void Check()
        {
            string scene = Environment.GetEnvironmentVariable("PLAY_SCENE");
            string type = Environment.GetEnvironmentVariable("PLAY_TYPE");
            string limit = Environment.GetEnvironmentVariable("PLAY_TIMEOUT");
            if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(type))
            {
                Debug.LogError("ROUTE_PLAY_FAIL PLAY_SCENE/PLAY_TYPE env required");
                EditorApplication.Exit(1);
                return;
            }
            if (float.TryParse(limit, out float parsed)) timeout = parsed;
            SessionState.SetString("GOAT.Route.PlayType", type);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            EditorSceneManager.OpenScene(scene);
            SessionState.SetBool(Armed, true);
            beginAt = 0d;
            EditorApplication.EnterPlaymode();
        }

        private static void Poll()
        {
            if (!SessionState.GetBool(Armed, false) || !EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup - pollAt < .04) return;
            pollAt = EditorApplication.timeSinceStartup;
            try
            {
                if (route == null) Begin();
                else Step();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.Log($"ROUTE_PLAY_FAIL scene={SessionState.GetString("GOAT.Route.PlayType", "")} reason={exception.Message} wp={index}/{route?.Length ?? 0}");
                SessionState.SetBool(Armed, false);
                EditorApplication.Exit(1);
            }
        }

        private static void Begin()
        {
            if (beginAt == 0d) beginAt = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - beginAt < 2.5) return;
            string typeName = "GoatDescent.EditorTools." + SessionState.GetString("GOAT.Route.PlayType", "");
            string limit = Environment.GetEnvironmentVariable("PLAY_TIMEOUT");
            if (float.TryParse(limit, out float parsedTimeout)) timeout = parsedTimeout;
            Type authoring = typeof(RoutePlayCI).Assembly.GetType(typeName);
            if (authoring == null) throw new InvalidOperationException("Авторинг не найден: " + typeName);
            MethodInfo method = authoring.GetMethod("PlayRoute", BindingFlags.Public | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("PlayRoute() не найден в " + typeName);
            route = (Vector3[])method.Invoke(null, null);
            if (route == null || route.Length < 10) throw new InvalidOperationException("Маршрут пустой");
            for (int i = 0; i < route.Length; i++)
            {
                if (Physics.Raycast(route[i] + Vector3.up * 6f, Vector3.down, out RaycastHit hit, 4000f,
                        ~0, QueryTriggerInteraction.Ignore))
                    route[i].y = hit.point.y;
            }
            var startDump = new System.Text.StringBuilder();
            for (int i = 0; i < Mathf.Min(10, route.Length); i++) startDump.Append($" wp{i}={route[i].y:F1}");
            Debug.Log("ROUTE_PLAY_HEIGHTS" + startDump);

            Vector3 spawn = route[0] + Vector3.up * 1.2f;
            Vector3 heading = route[Mathf.Min(2, route.Length - 1)] - route[0];
            float yaw = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
            goat = GoatPlayerFactory.Create(spawn, yaw, false, false, null, null);
            goat.SetRemoteControl(true);
            body = goat.GetComponent<Rigidbody>();
            life = goat.GetComponent<RespawnController>();
            jump = goat.GetComponent<GoatJumpController>();
            life.SetKillPlane(spawn.y - 60f);
            body.position = spawn;
            body.linearVelocity = Vector3.zero;
            simStart = Time.time;
            nextLog = 5f;
            pathLow = spawn.y;
            lastProgressTime = 0f;
            lastProgressPos = spawn;
            Debug.Log($"ROUTE_PLAY_START waypoints={route.Length} spawn={spawn}");
        }

        private static void Step()
        {
            if (life.IsDead && !wasDead)
            {
                wasDead = true;
                deaths++;
                respawnIndex = checkpointWpIndex;
                string entry = $"wp={index} pos={body.position:F1} target={route[index]:F1} cp={checkpointWpIndex}";
                deathLog.Add(entry);
                Debug.Log($"ROUTE_PLAY_DEATH scene={SessionState.GetString("GOAT.Route.PlayType", "")} {entry} total={deaths}");
            }
            else if (!life.IsDead && wasDead)
            {
                wasDead = false;
                index = respawnIndex;
                Vector3 spot = route[index] + Vector3.up * 1.5f;
                life.Teleport(spot);
                body.position = spot;
                body.linearVelocity = Vector3.zero;
                pathLow = Mathf.Min(pathLow, spot.y);
                life.SetKillPlane(pathLow - 80f);
                lastProgressPos = body.position;
                lastProgressTime = Time.time;
            }

            Vector3 position = body.position;
            while (index < route.Length - 1 && Horizontal(position, route[index]) < 3.2f) index++;

            if (index >= nextCheckpointWp)
            {
                checkpointWpIndex = Mathf.Max(0, index - 5);
                nextCheckpointWp = index + 150;
                Debug.Log($"ROUTE_PLAY_CHECKPOINT wp={checkpointWpIndex} pos={route[checkpointWpIndex]:F1}");
            }

            if (index >= route.Length - 1 && Vector3.Distance(position, route[route.Length - 1]) < 6f)
            {
                float minutes = (Time.time - simStart) / 60f;
                Debug.Log($"ROUTE_PLAY_OK scene={SessionState.GetString("GOAT.Route.PlayType", "")} waypoints={route.Length} time={minutes:F1}min deaths={deaths} checkpoints={checkpointCount}");
                foreach (string death in deathLog) Debug.Log("ROUTE_PLAY_DEATH_SPOT " + death);
                SessionState.SetBool(Armed, false);
                EditorApplication.Exit(0);
                return;
            }

            if (Time.time - simStart > timeout)
            {
                throw new InvalidOperationException($"timeout {timeout:F0}s");
            }
            float lowestAhead = position.y;
            for (int probe = index; probe < Mathf.Min(index + 10, route.Length); probe++)
                lowestAhead = Mathf.Min(lowestAhead, route[probe].y);
            if (position.y < lowestAhead - 60f)
            {
                throw new InvalidOperationException($"fell below route at wp {index}");
            }
            if (Horizontal(position, lastProgressPos) > 2.5f)
            {
                lastProgressPos = position;
                lastProgressTime = Time.time;
                sideSign = 0f;
            }

            Vector3 target = route[index];
            pathLow = Mathf.Min(pathLow, position.y);
            life.SetKillPlane(pathLow - 80f);
            float slopeNow = 0f;
            if (goat.Grounded) slopeNow = goat.GetComponent<GoatGroundDetector>().SlopeAngle;

            float stuckFor = Time.time - lastProgressTime;
            if (stuckFor > 26f)
            {
                throw new InvalidOperationException($"stuck at wp {index} pos={position:F1} target={target:F1}");
            }

            Vector3 to = target - position;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            Vector3 move;
            if (sideSign != 0f)
            {
                Vector3 side = new Vector3(-flat.z, 0f, flat.x).normalized * sideSign;
                move = flat.normalized * .35f + side;
                if (stuckFor > nextSideSwitch) { sideSign = -sideSign; nextSideSwitch = stuckFor + 4f; }
            }
            else
            {
                move = flat.sqrMagnitude > .0001f ? flat.normalized : Vector3.zero;
                if (stuckFor > 8f)
                {
                    sideSign = (index % 2) == 0 ? -1f : 1f;
                    nextSideSwitch = stuckFor + 4f;
                }
            }
            if (slopeNow > 35f) move *= .6f;
            if (!goat.Grounded && body.linearVelocity.y < -3f)
            {
                Vector3 velFlat = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
                if (velFlat.magnitude > 3f) move = -velFlat.normalized * .8f;
            }
            goat.SetRemoteMove(move);

            if (goat.Grounded && Time.time - lastJump > .9f)
            {
                Vector3 next = route[Mathf.Min(index + 1, route.Length - 1)];
                float rise = target.y - position.y;
                float nextRise = next.y - position.y;
                float nextFlat = Horizontal(position, next);
                bool needJump = (rise > .5f && flat.magnitude < 5.5f) || (nextRise > .9f && nextFlat < 5.5f);
                if (needJump)
                {
                    goat.Jump(jump.ChargedJumpVelocity);
                    lastJump = Time.time;
                }
            }

            if (stuckFor > 14f)
            {
                body.WakeUp();
                if (goat.Grounded && Time.time - lastJump > 1.1f)
                {
                    goat.Jump(jump.ChargedJumpVelocity);
                    lastJump = Time.time;
                }
            }

            if (Time.time - simStart > nextLog)
            {
                Debug.Log($"ROUTE_PLAY progress scene={SessionState.GetString("GOAT.Route.PlayType", "")} wp={index}/{route.Length} t={Time.time - simStart:F0}s deaths={deaths} pos={position:F1} vel={body.linearVelocity:F2} grounded={goat.Grounded} slope={(goat.Grounded ? goat.GetComponent<GoatGroundDetector>().SlopeAngle.ToString("F0") : "-")} target={target:F1} grip={goat.GetComponent<GoatCliffGrip>().IsGripping} slip={goat.GetComponent<GoatSlopeBalance>().IsSlipping} sleep={body.IsSleeping()} ctrl={goat.enabled} stuck={stuckFor:F0}");
                nextLog += 20f;
            }
        }

        private static float Horizontal(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
