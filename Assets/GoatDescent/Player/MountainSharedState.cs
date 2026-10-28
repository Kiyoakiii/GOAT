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
        public const int CurrentSchema = 18;
        public int sequence;
        public int schema;
        public string mapSignature;
        public float hostTime;
        public LedgeState[] ledges;
        public StoneState[] stones;
        public LooseState[] loose;
        public BellState[] bellsState;
        public BirdState[] birds;
        public WeatherState weather;
        public RockfallState rockfall;
        public GoatState[] goats;
        public ChestState chest;
        public SupplyChestState supplies;
        public PuddleState[] puddles;
        public PlayerMountainProgress[] players;
        public int teamProgress;
        public int scenario;
        public bool completed;
        public int falls;
        public int bells;
    }

    [Serializable]
    public struct RockfallState
    {
        public bool active;
        public byte phase;
        public float secondsLeft;
        public Vector3 center, outward;
        public RockfallStoneState[] stones;
    }

    [Serializable]
    public struct RockfallStoneState
    {
        public bool active;
        public Vector3 position;
        public Quaternion rotation;
        public float radius;
    }

    [Serializable]
    public struct RockfallShelterState
    {
        public float protection;
        public bool wall, still, bracing;
    }

    [Serializable]
    public struct WeatherState
    {
        public bool active;
        public byte preset;
        public float rain, wetness, wind, clock;
        public Vector3 direction;
    }

    [Serializable]
    public struct PuddleState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public bool purple;
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
    public struct GoatPartState
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
    public struct GoatState
    {
        public RockfallShelterState rockfallShelter;
        public GoatHerdState herd;
        public GoatCoatState coat;
        public float drinkingRemaining;
        public float health;
        public float beerRemaining;
        public GoatToiletState toilet;
        public int skinNumber;
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
        public bool predatorCarried;
        public float punchBuffRemaining;
        public DescentInventoryState inventory;
        public float anchorLength;
        public float birdSafeRemaining, anchorRemaining, madnessRemaining;
        public Vector3 anchorPosition, madnessDirection;
        public bool playingDead, gettingUp;
        public string ridingOn;
        public float rideLean;
        public GoatPartState[] parts;
    }

    [Serializable]
    public struct ChestState
    {
        public bool exists;
        public Vector3 position;
        public Quaternion rotation;
        public float cooldown;
    }

    [Serializable]
    public struct SupplyChestState
    {
        public bool exists;
        public Vector3 position;
        public Quaternion rotation;
        public int rope, axe, parachute;
        public float openRemaining;
    }
    [Serializable]
    public struct DescentInventoryState
    {
        public int rope, axe, parachute, selected;
        public bool browsing;
        public float chuteRemaining, axeRemaining;
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
