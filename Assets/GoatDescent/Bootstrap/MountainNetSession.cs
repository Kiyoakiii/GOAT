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
        public int skinNumber;
        public Vector3 move;
        public Vector3 cameraRight;
        public float horizontal;
        public float vertical;
        public bool grip;
        public bool brake, superHooves;
        public bool firstPerson;
        public byte action;
        public bool shoveDown, shoveUp, passengerBrace;
        public bool jumpDown, jumpUp;
        public bool wallKick, respawn, eagleAttack;
    }

    /// <summary>
    /// The room creator runs every Rigidbody and sends complete world snapshots;
    /// the guest sends world-space input for goat B. Direct TCP remains a debug path.
    /// </summary>
    public sealed class MountainNetSession : GoatNetworkMenuSession
    {
        public static MountainNetSession Current { get; private set; }
        public bool Connected => connected;
        public override string Status => status;
        public override string Address => address;
        public override string RoomCode => room?.Code ?? "";
        public override bool Busy => listener != null || joining || connected || room != null
            || roomCleanup != null && !roomCleanup.IsCompleted;
        private MountainRoomConnection room;
        private Task roomCleanup;
        private int lastAcknowledgedSnapshot;
        private float lastAcknowledgedAt;
        private const int Port = 7777;
        private readonly ConcurrentQueue<string> inbox = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> outbox = new ConcurrentQueue<string>();
        private readonly object connectionGate = new object();
        private int sessionVersion;
        private TcpListener listener;
        private TcpClient socket;
        private volatile bool stopping, connected;
        private bool hosting, joining, networkConfigured;
        private string address = "127.0.0.1";
        private string status = "Локальная игра";
        private string loggedStatus;
        private float nextSnapshot, nextInput;
        private int sequence, lastReceivedSequence;
        private float connectedAt;
        private byte lastLedgePhase = 255;
        private readonly HashSet<string> movingStones = new HashSet<string>();
        private readonly HashSet<string> looseSurfaces = new HashSet<string>();
        private bool shoveDown, shoveUp, jumpDown, jumpUp, wallKick, respawn, eagleAttack;
        private byte action;
        private LocalGoatPair pair;
        private MountainSessionState route;

        private void Start()
        {
            Current = this;
            pair = LocalGoatPair.Instance;
            route = GetComponent<MountainSessionState>();
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument == "--goat-host") HostDirect();
                else if (argument.StartsWith("--goat-join=")) JoinDirect(argument.Substring("--goat-join=".Length));
                else if (argument == "--goat-room-host") Host();
                else if (argument.StartsWith("--goat-room-join=")) Join(argument.Substring("--goat-room-join=".Length));
            }
        }

        public override void Host() => BeginRoom(true, null);

        public override void Join(string code)
        {
            if (Busy) return;
            code = (code ?? "").Trim().Replace(" ", "").ToUpperInvariant();
            if (code.Length != 6) { status = "Введи код комнаты: 6 букв или цифр."; return; }
            foreach (char character in code)
                if ((character < 'A' || character > 'Z') && (character < '0' || character > '9'))
                { status = "В коде комнаты должны быть только латинские буквы и цифры."; return; }
            BeginRoom(false, code);
        }

        private async void BeginRoom(bool host, string code)
        {
            if (Busy) return;
            if (!pair || !route) { status = "Дождись загрузки уровня."; return; }
            int version = Interlocked.Increment(ref sessionVersion);
            hosting = host;
            joining = true;
            var connection = new MountainRoomConnection(
                line => Receive(version, line),
                () =>
                {
                    if (!IsCurrent(version)) return;
                    connected = true;
                    Receive(version, "!CONNECTED");
                },
                message => { if (IsCurrent(version)) status = message; });
            room = connection;
            try
            {
                await connection.OpenAsync(host, code, route.MapSignature);
                if (!IsCurrent(version)) return;
                joining = false;
                if (!networkConfigured)
                    status = host ? "Комната создана. Отправь код другу; ты — ведущий." : "Вхожу в игру ведущего…";
            }
            catch (OperationCanceledException)
            { if (IsCurrent(version)) { Disconnect(); status = "Не удалось завершить подключение. Попробуй войти ещё раз."; } }
            catch (Exception error)
            {
                Debug.LogWarning("GOAT_ROOM_OPEN " + error.GetType().Name + ": " + error.Message
                    + " root=" + error.GetBaseException().Message);
                if (!IsCurrent(version)) return;
                string message = MountainRoomConnection.DescribeError(error);
                Disconnect();
                status = message;
            }
        }

        public void HostDirect()
        {
            if (Busy) return;
            int version = Interlocked.Increment(ref sessionVersion);
            try
            {
                listener = new TcpListener(IPAddress.Any, Port);
                listener.Start(1);
                var acceptor = listener;
                hosting = true;
                status = $"Жду второго ПК на порту {Port}";
                Task.Run(() =>
                {
                    try
                    {
                        while (IsCurrent(version))
                        {
                            var candidate = acceptor.AcceptTcpClient();
                            if (connected) candidate.Close();
                            else AttachSocket(candidate, version);
                        }
                    }
                    catch (Exception error) { Receive(version, "!ERROR " + error.Message); }
                });
            }
            catch (Exception error)
            { listener?.Stop(); listener = null; hosting = false; status = "Не удалось открыть порт: " + error.Message; }
        }

        public void JoinDirect(string hostAddress)
        {
            if (Busy) return;
            if (string.IsNullOrWhiteSpace(hostAddress)) { status = "Введи адрес ведущего"; return; }
            address = hostAddress.Trim();
            string remoteAddress = address;
            int version = Interlocked.Increment(ref sessionVersion);
            joining = true;
            status = "Подключаюсь к " + address;
            var client = new TcpClient();
            socket = client;
            Task.Run(() =>
            {
                try
                {
                    if (!client.ConnectAsync(remoteAddress, Port).Wait(TimeSpan.FromSeconds(8)))
                        throw new TimeoutException("Ведущий не ответил за 8 секунд");
                    AttachSocket(client, version);
                }
                catch (Exception error) { client.Close(); Receive(version, "!ERROR " + error.GetBaseException().Message); }
            });
        }

        private bool IsCurrent(int version) => !stopping && version == Volatile.Read(ref sessionVersion);

        private void Receive(int version, string message)
        {
            lock (connectionGate)
                if (IsCurrent(version)) inbox.Enqueue(message);
        }

        private void AttachSocket(TcpClient client, int version)
        {
            lock (connectionGate)
            {
                if (!IsCurrent(version) || connected) { client.Close(); return; }
                client.NoDelay = true;
                socket = client;
                var stream = client.GetStream();
                var reader = new StreamReader(stream, new UTF8Encoding(false));
                var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                connected = true;
                inbox.Enqueue("!CONNECTED");
                var sendQueue = outbox;
                Task.Run(() =>
                {
                    try
                    {
                        string line;
                        while (IsCurrent(version) && (line = reader.ReadLine()) != null) Receive(version, line);
                    }
                    catch (Exception error) { Receive(version, "!ERROR " + error.Message); }
                    finally { Receive(version, "!DISCONNECTED"); }
                });
                Task.Run(() =>
                {
                    try
                    {
                        while (IsCurrent(version) && connected)
                        {
                            if (sendQueue.TryDequeue(out var line)) writer.WriteLine(line);
                            else Thread.Sleep(3);
                        }
                    }
                    catch (Exception error) { Receive(version, "!ERROR " + error.Message); }
                });
            }
        }

        private void Update()
        {
            if (loggedStatus != status)
            {
                loggedStatus = status;
                Debug.Log("MOUNTAIN_NET_STATUS " + status);
            }
            while (inbox.TryDequeue(out var line))
            {
                if (line == "!CONNECTED")
                {
                    if (!pair || !route) { Disconnect(); status = "Гора ещё не загружена"; return; }
                    MountainAuthority.SetHost(hosting);
                    pair.ConfigureNetwork(hosting);
                    networkConfigured = true;
                    joining = false;
                    connectedAt = Time.unscaledTime;
                    lastAcknowledgedAt = connectedAt;
                    status = hosting ? "Ведущий: козёл A" : "Подключён: козёл B";
                    Debug.Log($"MOUNTAIN_NET_CONNECTED role={(hosting ? "host" : "guest")} peer={socket?.Client.RemoteEndPoint}");
                    if (hosting) SendSnapshot();
                }
                else if (line == "!DISCONNECTED")
                {
                    Disconnect();
                    status = "Связь потеряна. Продолжается локальная игра.";
                }
                else if (line.StartsWith("!ERROR "))
                { Disconnect(); status = line.Substring(7); }
                else if (line.Length > 1 && line[0] == 'I' && hosting && networkConfigured)
                {
                    try { ApplyInput(JsonUtility.FromJson<MountainInput>(line.Substring(1))); }
                    catch (Exception error) { status = "Ошибка управления: " + error.Message; }
                }
                else if (line.Length > 1 && line[0] == 'A' && hosting && networkConfigured
                    && int.TryParse(line.Substring(1), out int acknowledged)
                    && acknowledged > lastAcknowledgedSnapshot && acknowledged <= sequence)
                {
                    if (lastAcknowledgedSnapshot == 0) Debug.Log("MOUNTAIN_NET_FIRST_ACK " + acknowledged);
                    lastAcknowledgedSnapshot = acknowledged;
                    lastAcknowledgedAt = Time.unscaledTime;
                }
                else if (line.Length > 1 && line[0] == 'S' && !hosting && networkConfigured)
                {
                    try
                    {
                        var snapshot = JsonUtility.FromJson<MountainSnapshot>(line.Substring(1));
                        if (snapshot.schema != MountainSnapshot.CurrentSchema || snapshot.mapSignature != route.MapSignature)
                        {
                            Debug.LogError($"MOUNTAIN_NET_MAP_MISMATCH host={snapshot.mapSignature} guest={route.MapSignature} schema={snapshot.schema}");
                            Disconnect();
                            status = "Версии игры или карты не совпадают. Запустите одинаковую сборку.";
                            return;
                        }
                        if (snapshot.sequence <= lastReceivedSequence) continue;
                        lastReceivedSequence = snapshot.sequence;
                        route.ApplySnapshot(snapshot);
                        if (snapshot.sequence == 1) Debug.Log("MOUNTAIN_NET_ACK_QUEUED room=" + (room != null));
                        if (room != null) outbox.Enqueue("A" + snapshot.sequence);
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
            if (room != null && hosting && sequence - lastAcknowledgedSnapshot >= 3
                && Time.unscaledTime - lastAcknowledgedAt > 15f)
            {
                Disconnect();
                status = "Друг перестал получать мир. Продолжается локальная игра.";
                return;
            }
            if (hosting && Time.unscaledTime >= nextSnapshot)
            { nextSnapshot = Time.unscaledTime + .05f; SendSnapshot(); }
            else if (!hosting)
            {
                GatherGuestInput();
                if (Time.unscaledTime >= nextInput)
                {
                    nextInput = Time.unscaledTime + .05f;
                    outbox.Enqueue("I" + JsonUtility.ToJson(MakeInput()));
                    shoveDown = shoveUp = jumpDown = jumpUp = wallKick = respawn = eagleAttack = false;
                    action = 0;
                }
            }
            if (room != null)
                while (outbox.TryDequeue(out var message))
                    try { room.Send(message); }
                    catch (Exception error)
                    { Receive(sessionVersion, "!ERROR " + error.Message); break; }
        }

        private void SendSnapshot()
        {
            if (!connected || !route) return;
            // Bound in-flight full poses so a slow link cannot accumulate old physics.
            if (room != null && sequence - lastAcknowledgedSnapshot >= 3) return;
            // Keep the newest state if the other machine stalls.
            while (outbox.Count > 3) outbox.TryDequeue(out _);
            outbox.Enqueue("S" + JsonUtility.ToJson(route.CaptureSnapshot(++sequence)));
        }

        private void GatherGuestInput()
        {
            if (GoatSkinMenu.IsOpen || GoatSettingsMenu.BlocksInput)
            {
                shoveDown = shoveUp = jumpDown = jumpUp = wallKick = respawn = eagleAttack = false;
                action = 0;
                return;
            }
            shoveDown |= Input.GetKeyDown(KeyCode.F); shoveUp |= Input.GetKeyUp(KeyCode.F);
            jumpDown |= Input.GetKeyDown(KeyCode.Space); jumpUp |= Input.GetKeyUp(KeyCode.Space);
            wallKick |= Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.LeftShift);
            respawn |= Input.GetKeyDown(KeyCode.R);
            eagleAttack |= Input.GetKeyDown(KeyCode.F12);
            if (Input.GetKeyDown(KeyCode.Q)) action = 1;
            else if (Input.GetKeyDown(KeyCode.P)) action = 2;
            else if (Input.GetKeyDown(KeyCode.C)) action = 3;
            else if (Input.GetKeyDown(KeyCode.V)) action = 4;
            else if (Input.GetKeyDown(KeyCode.Z)) action = 5;
            else if (Input.GetKeyDown(KeyCode.X)) action = 6;
            else if (Input.GetKeyDown(KeyCode.T)) action = 7;
            else if (Input.GetKeyDown(KeyCode.B)) action = 8;
            else if (Input.GetKeyDown(KeyCode.G)) action = 9;
            else if (Input.GetKeyDown(KeyCode.H)) action = 10;
            else if (Input.GetKeyDown(KeyCode.J)) action = 11;
            else if (Input.GetKeyDown(KeyCode.K)) action = 12;
            else if (Input.GetKeyDown(KeyCode.I)) action = 13;
            else if (Input.GetKeyDown(KeyCode.Alpha1)) action = 14;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) action = 15;
            else if (Input.GetKeyDown(KeyCode.Alpha3)) action = 16;
            else if (Input.GetKeyDown(KeyCode.U)) action = 17;
            else if (Input.GetKeyDown(KeyCode.E)) action = 18;
        }

        private MountainInput MakeInput()
        {
            int skinNumber=pair?.Secondary?.GetComponent<GoatSkin>()?.Number??0;
            var camera = Camera.main;
            var follow = camera ? camera.GetComponent<ThirdPersonGoatCamera>() : null;
            bool firstPerson = follow && follow.IsFirstPerson;
            Vector3 right = camera ? Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized : Vector3.right;
            if(GoatSkinMenu.IsOpen || GoatSettingsMenu.BlocksInput)
                return new MountainInput {skinNumber=skinNumber,firstPerson=firstPerson,cameraRight=right};
            float horizontal = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float vertical = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            Vector3 forward = camera ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 move = Vector3.ClampMagnitude(right * horizontal + forward * vertical, 1f);
            return new MountainInput
            {
                skinNumber=skinNumber,
                firstPerson=firstPerson,
                horizontal = horizontal, vertical = vertical,
                move = move, cameraRight = right,
                grip = Input.GetKey(KeyCode.LeftShift),
                brake = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
                superHooves = Input.GetKey(KeyCode.LeftAlt),
                action = action,
                shoveDown = shoveDown, shoveUp = shoveUp, passengerBrace = Input.GetKey(KeyCode.F),
                jumpDown = jumpDown, jumpUp = jumpUp,
                wallKick = wallKick, respawn = respawn, eagleAttack = eagleAttack
            };
        }

        private void ApplyInput(MountainInput input)
        {
            if (input == null || !pair || !pair.Secondary) return;
            var goat = pair.Secondary;
            goat.GetComponent<GoatSkin>()?.Set(input.skinNumber);
            var drinking = goat.GetComponent<GoatWaterDrinking>();
            if (drinking && drinking.Active)
            {
                goat.SetRemoteMove(Vector3.zero);
                if (input.action == 18) drinking.TryInteract();
                return;
            }
            goat.SetRemoteMove(input.move);
            goat.SetRemoteMountainInput(input.brake, input.superHooves,
                input.horizontal, input.vertical, input.cameraRight, input.firstPerson);
            goat.GetComponent<GoatCliffGrip>()?.SetRemoteInput(input.grip,
                input.horizontal, input.vertical, input.cameraRight);
            goat.GetComponent<GoatComedyActions>()?.SetRemoteInput(input.passengerBrace,input.move);
            var interaction = goat.GetComponent<GoatInteraction>();
            if (input.shoveDown) interaction.Press();
            if (input.shoveUp) interaction.ReleaseButton();
            var jump = goat.GetComponent<GoatJumpController>();
            if (input.jumpDown) jump.NetworkSpaceDown();
            if (input.jumpUp) jump.NetworkSpaceUp();
            if (input.wallKick) goat.GetComponent<GoatCliffGrip>()?.NetworkWallKick();
            if (input.respawn) goat.GetComponent<RespawnController>()?.Respawn();
            if (input.eagleAttack) SkyPredatorEpisode.Current?.ForceAttack(goat);
            if (input.action == 8) goat.GetComponent<GoatPunchBuff>()?.TryPunch();
            else if (input.action == 9) goat.GetComponent<GoatDescentTricks>()?.TryKick();
            else if (input.action == 10) goat.GetComponent<GoatDescentTricks>()?.TryAnchor();
            else if (input.action == 11) goat.GetComponent<GoatComedyActions>()?.TryFeignDeath();
            else if (input.action == 12) goat.GetComponent<GoatComedyActions>()?.TryRide();
            else if (input.action == 13) goat.GetComponent<GoatDescentInventory>()?.ToggleChest();
            else if (input.action >= 14 && input.action <= 16) goat.GetComponent<GoatDescentInventory>()?.SelectOrTake(input.action-14);
            else if (input.action == 17) goat.GetComponent<GoatDescentInventory>()?.Use();
            else if (input.action == 18) goat.GetComponent<GoatWaterDrinking>()?.TryInteract();
            else if (input.action != 0) goat.GetComponent<GoatVisualController>()?.PlayNetworkAction(input.action);
        }

        public override void Disconnect()
        {
            StopSession(true);
            status = "Локальная игра";
        }

        private void StopSession(bool restoreLocal)
        {
            var previousRoom = room;
            room = null;
            lock (connectionGate)
            {
                Interlocked.Increment(ref sessionVersion);
                connected = joining = hosting = networkConfigured = false;
                socket?.Close(); socket = null;
                listener?.Stop(); listener = null;
                while (inbox.TryDequeue(out _)) { }
                outbox = new ConcurrentQueue<string>();
            }
            if (previousRoom != null) roomCleanup = previousRoom.CloseAsync();
            sequence = lastReceivedSequence = 0;
            lastAcknowledgedSnapshot = 0;
            lastAcknowledgedAt = 0f;
            nextSnapshot = nextInput = connectedAt = 0f;
            lastLedgePhase = 255;
            movingStones.Clear(); looseSurfaces.Clear();
            shoveDown = shoveUp = jumpDown = jumpUp = wallKick = respawn = eagleAttack = false;
            action = 0;
            if (!restoreLocal) return;
            if (pair && pair.Secondary)
            {
                pair.Secondary.SetRemoteMove(Vector3.zero);
                pair.Secondary.GetComponent<GoatInteraction>()?.CancelAll();
            }
            pair?.RestoreLocal();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            stopping = true;
            StopSession(false);
            MountainAuthority.SetHost(true);
        }
    }
}
