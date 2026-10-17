using System;
using System.Collections.Generic;
using GoatDescent.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    [InitializeOnLoad]
    public static class CrownLevelPlayCI
    {
        private const string Armed = "GOAT.Crown.PlayCheck";
        private static double phaseAt;
        private static int phase;
        private static int jumpIndex, jumpPhase;
        private static float jumpAt;
        private static bool airborne;
        private static List<int> jumps;
        static CrownLevelPlayCI() => EditorApplication.update += Poll;
        public static void Check()
        {
            EditorSceneManager.OpenScene("Assets/GoatDescent/Scenes/GoatPlayground.unity");
            SessionState.SetBool(Armed, true);
            EditorApplication.EnterPlaymode();
        }
        private static void Poll()
        {
            if (!SessionState.GetBool(Armed, false) || !EditorApplication.isPlaying) return;
            if (phaseAt == 0) phaseAt = EditorApplication.timeSinceStartup;
            if (phase < 3 && EditorApplication.timeSinceStartup - phaseAt < 3f) return;
            try
            {
                var pair = LocalGoatPair.Instance;
                Require(pair && pair.Primary && pair.Secondary, "Не создана пара козлов");
                var resources = UnityEngine.Object.FindFirstObjectByType<LevelGeneratedResources>();
                Require(resources && resources.Route != null, "Не собран уровень");
                var goat = pair.Primary;
                var life = goat.GetComponent<RespawnController>();
                if (phase == 0)
                {
                    Require(!life.IsDead && goat.Grounded, "Козёл не стоит на стартовой полке");
                    Require(!pair.Secondary.GetComponent<RespawnController>().IsDead && pair.Secondary.Grounded, "Второй козёл упал со старта");
                    life.Teleport(resources.Route.Platforms[20].Center + Vector3.up * .5f);
                    phase = 1; phaseAt = EditorApplication.timeSinceStartup;
                }
                else if (phase == 1)
                {
                    Require(goat.GetComponent<RouteProgressTracker>().CurrentProgress > .1f, "Прогресс не обновился");
                    life.Respawn(); phase = 2; phaseAt = EditorApplication.timeSinceStartup;
                }
                else if (phase == 2)
                {
                    Require(!life.IsDead && goat.Grounded, "Респавн не вернул козла на опору");
                    Require(goat.GetComponent<RouteProgressTracker>().CurrentProgress > .1f, "Чекпоинт потерял прогресс");
                    jumps = new List<int>();
                    for (int i = 0; i < resources.Route.Connections.Length; i++)
                        if (resources.Route.Connections[i] == RouteConnection.Jump) jumps.Add(i);
                    Require(jumps.Count >= 4, "Недостаточно прыжковых переходов");
                    jumps = new List<int> { jumps[0], jumps[jumps.Count / 3], jumps[jumps.Count * 2 / 3], jumps[jumps.Count - 1] };
                    goat.SetRemoteControl(true); goat.SetRemoteMove(Vector3.zero);
                    phase = 3;
                }
                else if (phase == 3)
                {
                    if (CheckJump(goat, resources.Route))
                    {
                        goat.SetRemoteControl(false);
                        life.Teleport(resources.Route.Finish + Vector3.up * .5f);
                        pair.Secondary.GetComponent<RespawnController>().Teleport(resources.Route.Finish + Vector3.right * 1.8f + Vector3.up * .5f);
                        phase = 4; phaseAt = EditorApplication.timeSinceStartup;
                    }
                }
                else if (EditorApplication.timeSinceStartup - phaseAt > 3f)
                {
                    Require(resources.GetComponent<LevelRunTracker>().Completed, "Финиш не засчитан обеим козам");
                    Debug.Log("STONE_CROWN_PLAY_OK pair=2 progress=ok respawn=ok physicsJumps=4 finish=ok");
                    SessionState.SetBool(Armed, false); EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception); SessionState.SetBool(Armed, false); EditorApplication.Exit(1);
            }
        }
        private static bool CheckJump(GoatController goat, LevelRoute route)
        {
            if (jumpIndex >= jumps.Count) return true;
            int index = jumps[jumpIndex];
            var a = route.Platforms[index]; var b = route.Platforms[index + 1];
            var life = goat.GetComponent<RespawnController>();
            Vector3 direction = Vector3.ProjectOnPlane(b.Center - a.Center, Vector3.up).normalized;
            var body = goat.GetComponent<Rigidbody>();
            if (jumpPhase == 0)
            {
                life.Teleport(a.Center + direction * (a.Radius - .9f) + Vector3.up * .2f);
                jumpAt = Time.time; jumpPhase = 1; airborne = false;
            }
            else if (jumpPhase == 1 && Time.time - jumpAt > .7f)
            {
                Require(goat.Grounded && !life.IsDead, "Нет устойчивого старта прыжка " + index);
                float velocity = goat.GetComponent<GoatJumpController>().ChargedJumpVelocity;
                float gravity = -Physics.gravity.y;
                float duration = (velocity + Mathf.Sqrt(velocity * velocity + 2f * gravity * (body.position.y - b.Center.y))) / gravity;
                float distance = Vector3.ProjectOnPlane(b.Center - body.position, Vector3.up).magnitude;
                float speed = distance / duration / .76f;
                Require(speed <= goat.MaxGroundSpeed, "Прыжок требует недоступной скорости " + index);
                body.linearVelocity = direction * speed;
                goat.Jump(velocity);
                jumpAt = Time.time; jumpPhase = 2;
            }
            else if (jumpPhase == 2)
            {
                if (!goat.Grounded) airborne = true;
                Require(!life.IsDead && Time.time - jumpAt < 5f, "Прыжок не завершён на опоре " + index + " position=" + body.position);
                if (airborne && goat.Grounded && Time.time - jumpAt > .4f)
                {
                    float distance = Vector3.ProjectOnPlane(body.position - b.Center, Vector3.up).magnitude;
                    Require(distance < b.Radius, "Приземление вне целевой площадки " + index + " distance=" + distance);
                    Debug.Log("STONE_CROWN_PHYSICS_JUMP_OK index=" + index + " elapsed=" + (Time.time - jumpAt).ToString("F2"));
                    jumpIndex++; jumpPhase = 0;
                }
            }
            return false;
        }
        private static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); }
    }
}
