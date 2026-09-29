using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GoatDescent
{
    [Serializable]
    public sealed class MountainInput
    {
        public Vector3 move;
        public Vector3 cameraRight;
        public float horizontal;
        public float vertical;
        public bool grip;
        public bool shoveDown, shoveUp;
        public bool jumpDown, jumpUp;
        public bool wallKick, respawn, vault, eagleAttack;
    }

    /// <summary>
    /// Direct two-PC connection. The host runs every Rigidbody and sends complete
    /// world snapshots; the guest sends world-space input for goat B only.
    /// </summary>
    public sealed class MountainNetSession : MonoBehaviour
    {
        public static MountainNetSession Current { get; private set; }
        public bool Connected => connected;
        private const int Port = 7777;
        private readonly ConcurrentQueue<string> inbox = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> outbox = new ConcurrentQueue<string>();
        private TcpListener listener;
        private TcpClient socket;
        private volatile bool stopping, connected;
        private bool hosting, joining, networkConfigured;
        private string address = "127.0.0.1";
        private string status = "Локальная игра";
        private float nextSnapshot, nextInput;
        private int sequence, lastReceivedSequence;
        private bool automatedDemo;
        private bool automatedHazards;
        private int hazardStep;
        private float connectedAt;
        private byte lastLedgePhase = 255;
        private readonly HashSet<string> movingStones = new HashSet<string>();
        private readonly HashSet<string> looseSurfaces = new HashSet<string>();
        private bool singleLogged, breakLogged;
        private bool shoveDown, shoveUp, jumpDown, jumpUp, wallKick, respawn, vault, eagleAttack;
        private LocalGoatPair pair;
        private PillarDescentLevel route;

        private void Start()
        {
            Current = this;
            pair = LocalGoatPair.Instance;
            route = FindFirstObjectByType<PillarDescentLevel>();
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument == "--goat-verify") automatedDemo = true;
                else if (argument == "--goat-verify-hazards") automatedHazards = true;
                else if (argument == "--goat-host") Host();
                else if (argument.StartsWith("--goat-join=")) Join(argument.Substring("--goat-join=".Length));
            }
        }

        public void Host()
        {
            if (listener != null || joining) return;
            try
            {
                listener = new TcpListener(IPAddress.Any, Port);
                listener.Start(1);
                hosting = true;
                status = $"Жду второго ПК на порту {Port}";
                Task.Run(() =>
                {
                    try
                    {
                        while (!stopping)
                        {
                            var candidate = listener.AcceptTcpClient();
                            if (connected) candidate.Close();
                            else AttachSocket(candidate);
                        }
                    }
                    catch (Exception error) { if (!stopping) inbox.Enqueue("!ERROR " + error.Message); }
                });
            }
            catch (Exception error)
            { listener = null; hosting = false; status = "Не удалось открыть порт: " + error.Message; }
        }

        public void Join(string hostAddress)
        {
            if (listener != null || joining || connected) return;
            address = hostAddress.Trim();
            joining = true;
            status = "Подключаюсь к " + address;
            Task.Run(() =>
            {
                try
                {
                    var client = new TcpClient();
                    if (!client.ConnectAsync(address, Port).Wait(TimeSpan.FromSeconds(8)))
                        throw new TimeoutException("Ведущий не ответил за 8 секунд");
                    AttachSocket(client);
                }
                catch (Exception error) { inbox.Enqueue("!ERROR " + error.GetBaseException().Message); }
            });
        }

        private void AttachSocket(TcpClient client)
        {
            if (stopping) { client.Close(); return; }
            client.NoDelay = true;
            socket = client;
            var stream = client.GetStream();
            var reader = new StreamReader(stream, new UTF8Encoding(false));
            var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            connected = true;
            inbox.Enqueue("!CONNECTED");
            Task.Run(() =>
            {
                try
                {
                    string line;
                    while (!stopping && (line = reader.ReadLine()) != null) inbox.Enqueue(line);
                }
                catch (Exception error) { if (!stopping) inbox.Enqueue("!ERROR " + error.Message); }
                finally { inbox.Enqueue("!DISCONNECTED"); }
            });
            Task.Run(() =>
            {
                try
                {
                    while (!stopping && connected)
                    {
                        if (outbox.TryDequeue(out var line)) writer.WriteLine(line);
                        else Thread.Sleep(3);
                    }
                }
                catch (Exception error) { if (!stopping) inbox.Enqueue("!ERROR " + error.Message); }
            });
        }

        private void Update()
        {
            while (inbox.TryDequeue(out var line))
            {
                if (line == "!CONNECTED")
                {
                    if (!pair || !route) { status = "Гора ещё не загружена"; continue; }
                    MountainAuthority.SetHost(hosting);
                    pair.ConfigureNetwork(hosting);
                    networkConfigured = true;
                    connectedAt = Time.unscaledTime;
                    status = hosting ? "Ведущий: козёл A" : "Подключён: козёл B";
                    Debug.Log($"MOUNTAIN_NET_CONNECTED role={(hosting ? "host" : "guest")} peer={socket?.Client.RemoteEndPoint}");
                    if (hosting && automatedDemo) pair.ResetFragilePractice();
                    else if (hosting && automatedHazards)
                    { hazardStep = 0; pair.ResetRockPractice(); }
                    if (hosting) SendSnapshot();
                }
                else if (line == "!DISCONNECTED")
                {
                    connected = false;
                    joining = false;
                    if (hosting && pair && pair.Secondary)
                    {
                        pair.Secondary.SetRemoteMove(Vector3.zero);
                        pair.Secondary.GetComponent<GoatInteraction>().CancelAll();
                    }
                    pair?.RestoreLocal();
                    status = "Связь потеряна — локальная игра, Tab переключает козлов";
                }
                else if (line.StartsWith("!ERROR "))
                { status = line.Substring(7); if (!hosting) joining = false; }
                else if (line.Length > 1 && line[0] == 'I' && hosting && networkConfigured)
                {
                    try { ApplyInput(JsonUtility.FromJson<MountainInput>(line.Substring(1))); }
                    catch (Exception error) { status = "Ошибка управления: " + error.Message; }
                }
                else if (line.Length > 1 && line[0] == 'S' && !hosting && networkConfigured)
                {
                    try
                    {
                        var snapshot = JsonUtility.FromJson<MountainSnapshot>(line.Substring(1));
                        if (snapshot.schema != 5 || snapshot.mapSignature != route.MapSignature)
                        {
                            status = "Версии карты не совпадают";
                            Debug.LogError($"MOUNTAIN_NET_MAP_MISMATCH host={snapshot.mapSignature} guest={route.MapSignature} schema={snapshot.schema}");
                            continue;
                        }
                        if (snapshot.sequence <= lastReceivedSequence) continue;
                        lastReceivedSequence = snapshot.sequence;
                        route.ApplySnapshot(snapshot);
                        if (snapshot.stones != null)
                            foreach (var stone in snapshot.stones)
                            {
                                if (stone.moving && movingStones.Add(stone.id))
                                    Debug.Log($"MOUNTAIN_NET_STONE_MOVING id={stone.id} cause={stone.cause} position={stone.position}");
                                else if (!stone.moving) movingStones.Remove(stone.id);
                            }
                        if (snapshot.loose != null)
                            foreach (var surface in snapshot.loose)
                            {
                                if (surface.active && looseSurfaces.Add(surface.id))
                                    Debug.Log($"MOUNTAIN_NET_LOOSE_STAGE id={surface.id} active=True secondsLeft={surface.secondsLeft:0.00}");
                                else if (!surface.active && looseSurfaces.Remove(surface.id))
                                    Debug.Log($"MOUNTAIN_NET_LOOSE_STAGE id={surface.id} active=False secondsLeft=0");
                            }
                        if (snapshot.sequence % 100 == 0)
                            Debug.Log($"MOUNTAIN_NET_SNAPSHOT_RX sequence={snapshot.sequence} goats={snapshot.goats?.Length} ledges={snapshot.ledges?.Length}");
                        if (snapshot.ledges != null && snapshot.ledges.Length > 10
                            && lastLedgePhase != snapshot.ledges[10].phase)
                        {
                            lastLedgePhase = snapshot.ledges[10].phase;
                            Debug.Log($"MOUNTAIN_NET_LEDGE_STAGE id={snapshot.ledges[10].id} phase={lastLedgePhase} load={snapshot.ledges[10].loadKg:0} secondsLeft={snapshot.ledges[10].secondsLeft:0.00} sequence={snapshot.sequence}");
                        }
                    }
                    catch (Exception error) { status = "Ошибка состояния: " + error.Message; }
                }
            }
            if (!networkConfigured || !connected) return;
            if (hosting && automatedDemo && route)
            {
                var ledge = route.GetLedge(10);
                if (!singleLogged && Time.unscaledTime - connectedAt >= 20f)
                {
                    singleLogged = true;
                    Debug.Log($"MOUNTAIN_VERIFY_SINGLE id={ledge?.StableId} phase={ledge?.Phase} load={ledge?.LoadKg:0} elapsed=20");
                }
                if (!breakLogged && ledge && ledge.Phase == DescentLedge.LedgePhase.Broken)
                {
                    breakLogged = true;
                    Debug.Log($"MOUNTAIN_VERIFY_OVERLOAD id={ledge.StableId} load={ledge.LoadKg:0} reason={ledge.LastReason}");
                }
            }
            if (hosting && automatedHazards && route) VerifyHazards();
            if (hosting && Time.unscaledTime >= nextSnapshot)
            { nextSnapshot = Time.unscaledTime + .05f; SendSnapshot(); }
            else if (!hosting)
            {
                GatherGuestInput();
                if (Time.unscaledTime >= nextInput)
                {
                    nextInput = Time.unscaledTime + .05f;
                    outbox.Enqueue("I" + JsonUtility.ToJson(MakeInput()));
                    shoveDown = shoveUp = jumpDown = jumpUp = wallKick = respawn = vault = eagleAttack = false;
                }
            }
        }

        private void VerifyHazards()
        {
            float elapsed = Time.unscaledTime - connectedAt;
            if (hazardStep == 0 && elapsed >= 1f)
            {
                hazardStep = 1;
                bool shoved = pair.Primary.GetComponent<GoatInteraction>().TryShove();
                Debug.Log($"MOUNTAIN_VERIFY_ROCK_SHOVE requested={shoved}");
            }
            if (hazardStep == 1 && elapsed >= 4f)
            {
                hazardStep = 2;
                var snow = route.GetLedge(5);
                var a = snow.transform.TransformPoint(new Vector3(-.55f, .16f, 0f));
                var b = snow.transform.TransformPoint(new Vector3(.55f, .16f, 0f));
                var facing = Quaternion.LookRotation(snow.transform.right);
                pair.Primary.GetComponent<RespawnController>().ResetTo(a, facing);
                pair.Secondary.GetComponent<RespawnController>().ResetTo(b, facing);
                Debug.Log($"MOUNTAIN_VERIFY_SNOW_PLACED id={snow.StableId}");
            }
            if (hazardStep == 2 && elapsed >= 5.5f)
            {
                hazardStep = 3;
                pair.ResetScenario(0);
                Debug.Log("MOUNTAIN_VERIFY_SNOW_RELEASED");
            }
        }

        private void SendSnapshot()
        {
            if (!connected || !route) return;
            // Keep the newest state if the other machine stalls.
            while (outbox.Count > 3) outbox.TryDequeue(out _);
            outbox.Enqueue("S" + JsonUtility.ToJson(route.CaptureSnapshot(++sequence)));
        }

        private void GatherGuestInput()
        {
            shoveDown |= Input.GetKeyDown(KeyCode.F); shoveUp |= Input.GetKeyUp(KeyCode.F);
            jumpDown |= Input.GetKeyDown(KeyCode.Space); jumpUp |= Input.GetKeyUp(KeyCode.Space);
            wallKick |= Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.LeftShift);
            respawn |= Input.GetKeyDown(KeyCode.R);
            vault |= Input.GetKeyDown(KeyCode.G);
            eagleAttack |= Input.GetKeyDown(KeyCode.F12);
        }

        private MountainInput MakeInput()
        {
            float horizontal = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float vertical = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            var camera = Camera.main;
            Vector3 right = camera ? Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized : Vector3.right;
            Vector3 forward = camera ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 move = Vector3.ClampMagnitude(right * horizontal + forward * vertical, 1f);
            if (automatedDemo && route && pair && pair.Secondary)
            {
                move = Vector3.zero;
                if (Time.unscaledTime - connectedAt > 21f)
                {
                    var ledge = route.GetLedge(10);
                    if (ledge)
                    {
                        Vector3 target = ledge.transform.TransformPoint(new Vector3(.7f, .1f, 0f));
                        Vector3 delta = Vector3.ProjectOnPlane(target - pair.Secondary.transform.position, Vector3.up);
                        if (delta.magnitude > .25f) move = delta.normalized;
                    }
                }
            }
            return new MountainInput
            {
                horizontal = horizontal, vertical = vertical,
                move = move, cameraRight = right,
                grip = Input.GetKey(KeyCode.LeftShift),
                shoveDown = shoveDown, shoveUp = shoveUp,
                jumpDown = jumpDown, jumpUp = jumpUp,
                wallKick = wallKick, respawn = respawn, vault = vault, eagleAttack = eagleAttack
            };
        }

        private void ApplyInput(MountainInput input)
        {
            if (input == null || !pair || !pair.Secondary) return;
            var goat = pair.Secondary;
            goat.SetRemoteMove(input.move);
            goat.GetComponent<GoatCliffGrip>()?.SetRemoteInput(input.grip,
                input.horizontal, input.vertical, input.cameraRight);
            var interaction = goat.GetComponent<GoatInteraction>();
            if (input.shoveDown) interaction.Press();
            if (input.shoveUp) interaction.ReleaseButton();
            var jump = goat.GetComponent<GoatJumpController>();
            if (input.jumpDown) jump.NetworkSpaceDown();
            if (input.jumpUp) jump.NetworkSpaceUp();
            if (input.wallKick) goat.GetComponent<GoatCliffGrip>()?.NetworkWallKick();
            if (input.respawn) goat.GetComponent<RespawnController>()?.Respawn();
            if (input.eagleAttack) SkyPredatorEpisode.Current?.ForceAttack(goat);
            if (input.vault)
            {
                var horn = goat.GetComponent<GoatHornVault>();
                if (horn && horn.NearbyStone) horn.Launch(horn.NearbyStone);
            }
        }

        private void OnGUI()
        {
            float top = 114f;
            float left = Screen.width - 346f;
            GUI.Box(new Rect(left, top, 330f, 116f), "Гора для двух ПК");
            GUI.Label(new Rect(left + 10f, top + 24f, 310f, 22f), status);
            if (networkConfigured && (connected || hosting)) return;
            address = GUI.TextField(new Rect(left + 10f, top + 49f, 195f, 25f), address);
            if (GUI.Button(new Rect(left + 210f, top + 49f, 110f, 25f),
                networkConfigured ? "Повторить" : "Подключиться")) Join(address);
            if (!networkConfigured && GUI.Button(new Rect(left + 10f, top + 81f, 150f, 24f), "Создать игру")) Host();
            GUI.Label(new Rect(left + 167f, top + 83f, 150f, 22f), "TCP, порт 7777");
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            stopping = true; connected = false;
            socket?.Close(); listener?.Stop();
            MountainAuthority.SetHost(true);
        }
    }
}
