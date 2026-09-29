using System;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>The host alone advances shared physics; clients display host snapshots.</summary>
    public static class MountainAuthority
    {
        public static bool IsHost { get; private set; } = true;
        public static void SetHost(bool isHost) => IsHost = isHost;
    }

    [Serializable]
    public sealed class MountainSnapshot
    {
        public int sequence;
        public int schema;
        public string mapSignature;
        public float hostTime;
        public LedgeState[] ledges;
        public StoneState[] stones;
        public LooseState[] loose;
        public BellState[] bellsState;
        public BirdState[] birds;
        public GoatState[] goats;
        public PlayerMountainProgress[] players;
        public int teamProgress;
        public int scenario;
        public bool completed;
        public int falls;
        public int bells;
    }

    [Serializable]
    public struct LedgeState
    {
        public string id;
        public byte phase;
        public float loadKg;
        public float capacityKg;
        public float stress;
        public float secondsLeft;
        public string reason;
        public string contributions;
    }

    [Serializable]
    public struct StoneState
    {
        public string id;
        public bool active;
        public bool moving;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public string cause;
    }

    [Serializable]
    public struct LooseState
    {
        public string id;
        public bool active;
        public float secondsLeft;
    }

    [Serializable]
    public struct BellState
    {
        public string id;
        public bool collected;
    }

    [Serializable]
    public struct BirdState
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
        public byte phase;
        public string target;
        public int attempt;
        public float secondsLeft;
    }

    [Serializable]
    public struct GoatState
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public bool dead;
        public bool linked;
        public bool holding;
        public string interactionStatus;
        public string animation;
        public Vector3 facing;
    }

    [Serializable]
    public struct PlayerMountainProgress
    {
        public string id;
        public int ledgeIndex;
        public int checkpointIndex;
        public bool finished;
    }
}
