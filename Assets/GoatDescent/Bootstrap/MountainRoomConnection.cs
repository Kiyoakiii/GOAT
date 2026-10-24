using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace GoatDescent
{
    internal sealed class MountainRoomConnection
    {
        private const string MessageName = "goat-mountain";
        private const int MaxMessageBytes = 256 * 1024;
        private static Task initializeTask;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly Action<string> receive, progress;
        private readonly Action connected;
        private NetworkManager manager;
        private GameObject node;
        private ISession session;
        private Task opening, closingTask;
        private bool host, closing, hasPeer, applicationReady, loggedSnapshot, loggedReceive, loggedAck;
        private CustomMessagingManager registeredMessages;
        private ulong peer;
        private string handshake;

        public string Code => closing ? "" : session?.Code ?? "";

        public MountainRoomConnection(Action<string> receive, Action connected, Action<string> progress)
        { this.receive = receive; this.connected = connected; this.progress = progress; }

        public Task OpenAsync(bool asHost, string code, string mapSignature)
            => opening = OpenCoreAsync(asHost, code, mapSignature);

        private async Task OpenCoreAsync(bool asHost, string code, string mapSignature)
        {
            host = asHost;
            handshake = MountainSnapshot.CurrentSchema + ":" + mapSignature;
            if (!Guid.TryParse(Application.cloudProjectId, out _))
                throw new InvalidOperationException("Для комнат нужно привязать проект GOAT к Unity Cloud в редакторе.");
            progress("Подключаю сетевые сервисы…");
            if (initializeTask == null || initializeTask.IsFaulted || initializeTask.IsCanceled)
                initializeTask = InitializeAsync();
            await initializeTask;
            lifetime.Token.ThrowIfCancellationRequested();
            CreateNetworkManager();
            progress(host ? "Создаю комнату…" : "Вхожу в комнату…");
            if (host)
            {
                var options = new SessionOptions { MaxPlayers = 2, IsPrivate = true, Type = "goat-mountain" }
                    .WithRelayNetwork().WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });
                session = await MultiplayerService.Instance.CreateSessionAsync(options, lifetime.Token);
            }
            else
            {
                var options = new JoinSessionOptions { Type = "goat-mountain" }
                    .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });
                session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code, options, lifetime.Token);
            }
            lifetime.Token.ThrowIfCancellationRequested();
            session.Deleted += OnSessionLost;
            session.RemovedFromSession += OnSessionLost;
            session.SessionHostChanged += OnHostChanged;
        }

        private static async Task InitializeAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions();
                foreach (string argument in Environment.GetCommandLineArgs())
                    if (argument.StartsWith("--goat-profile="))
                        options.SetProfile(argument.Substring("--goat-profile=".Length));
                await UnityServices.InitializeAsync(options);
            }
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        private void CreateNetworkManager()
        {
            if (NetworkManager.Singleton)
                throw new InvalidOperationException("Предыдущее сетевое соединение ещё закрывается.");
            node = new GameObject("Goat room transport");
            node.SetActive(false);
            var transport = node.AddComponent<UnityTransport>();
            transport.UseWebSockets = true;
            transport.MaxPayloadSize = MaxMessageBytes + 1024;
            transport.MaxSendQueueSize = 2 * 1024 * 1024;
            manager = node.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                ProtocolVersion = MountainSnapshot.CurrentSchema,
                TickRate = 20,
                EnableSceneManagement = false,
                ConnectionApproval = true,
                ConnectionData = Encoding.UTF8.GetBytes(handshake),
                ClientConnectionBufferTimeout = 15,
                ForceSamePrefabs = false
            };
            manager.ConnectionApprovalCallback = Approve;
            manager.OnServerStarted += RegisterMessages;
            manager.OnClientStarted += RegisterMessages;
            manager.OnClientConnectedCallback += OnConnected;
            manager.OnClientDisconnectCallback += OnDisconnected;
            manager.OnTransportFailure += OnTransportFailure;
            node.SetActive(true);
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            bool sameMap = request.Payload != null && request.Payload.Length <= 512
                && Encoding.UTF8.GetString(request.Payload) == handshake;
            bool available = request.ClientNetworkId == NetworkManager.ServerClientId
                || manager.ConnectedClientsIds.Count < 2;
            response.Approved = sameMap && available && !closing;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = !sameMap ? "Версии игры или карты не совпадают. Запустите одинаковую сборку."
                : "В комнате уже два игрока.";
            if (!response.Approved) Debug.LogWarning("GOAT_ROOM_REJECT " + response.Reason);
        }

        private void RegisterMessages()
        {
            if (closing || manager.CustomMessagingManager == null
                || ReferenceEquals(registeredMessages, manager.CustomMessagingManager)) return;
            registeredMessages = manager.CustomMessagingManager;
            registeredMessages.RegisterNamedMessageHandler(MessageName, OnMessage);
        }

        private void OnConnected(ulong client)
        {
            RegisterMessages();
            Debug.Log($"GOAT_ROOM_PEER_CONNECTED host={host} client={client} closing={closing}");
            if (closing || hasPeer || host && client == NetworkManager.ServerClientId) return;
            peer = host ? client : NetworkManager.ServerClientId;
            hasPeer = true;
            if (!host)
            {
                applicationReady = true;
                connected();
            }
        }

        private void OnDisconnected(ulong client)
        {
            Debug.Log($"GOAT_ROOM_PEER_DISCONNECTED host={host} client={client} reason={manager?.DisconnectReason}");
            if (closing || !hasPeer || host && client != peer) return;
            hasPeer = false;
            string reason = manager.DisconnectReason;
            bool gameReason = reason != null && (reason.StartsWith("Версии игры") || reason.StartsWith("В комнате"));
            receive(gameReason ? "!ERROR " + reason : "!DISCONNECTED");
        }

        private void OnTransportFailure()
        { if (!closing) receive("!ERROR Не удалось установить связь с ведущим. Проверьте интернет и попробуйте снова."); }

        private void OnSessionLost()
        { if (!closing) receive("!ERROR Комната закрыта. Продолжается локальная игра."); }

        private void OnHostChanged(string _)
        { if (!closing) receive("!ERROR Ведущий вышел из комнаты. Продолжается локальная игра."); }

        private void OnMessage(ulong sender, FastBufferReader reader)
        {
            if (!loggedReceive)
            {
                loggedReceive = true;
                Debug.Log($"GOAT_ROOM_FIRST_MESSAGE host={host} sender={sender} peer={peer} ready={hasPeer} length={reader.Length}");
            }
            if (closing || !hasPeer || sender != peer) return;
            try
            {
                reader.ReadValueSafe(out int length);
                if (length < 1 || length > MaxMessageBytes || length > reader.Length - reader.Position)
                    throw new InvalidOperationException("Неверный размер сетевого сообщения.");
                var bytes = new byte[length];
                reader.ReadBytesSafe(ref bytes, length);
                string line = Encoding.UTF8.GetString(bytes);
                if (line[0] == 'A' && !loggedAck) { loggedAck = true; Debug.Log("GOAT_ROOM_ACK_RX " + line); }
                bool validDirection = host ? line[0] == 'I' || line[0] == 'A' : line[0] == 'S';
                // NGO can report a server-side connection before the guest has finished
                // its startup. The first input proves the guest can already run the game.
                if (host && !applicationReady && line[0] == 'I')
                {
                    applicationReady = true;
                    connected();
                }
                if (validDirection) receive(line);
            }
            catch (Exception)
            { receive("!ERROR Получено повреждённое сетевое сообщение."); }
        }

        public void Send(string line)
        {
            if (line[0] == 'A' && !loggedAck)
            { loggedAck = true; Debug.Log($"GOAT_ROOM_ACK_TX {line} ready={hasPeer} closing={closing} listening={manager?.IsListening}"); }
            if (closing || !hasPeer || !manager || !manager.IsListening) return;
            byte[] bytes = Encoding.UTF8.GetBytes(line);
            if (line[0] == 'S' && !loggedSnapshot)
            {
                loggedSnapshot = true;
                Debug.Log($"GOAT_ROOM_SNAPSHOT_BYTES {bytes.Length} peer={peer} local={manager.LocalClientId} hash={manager.NetworkConfig.RpcHashSize}");
            }
            if (bytes.Length > MaxMessageBytes)
                throw new InvalidOperationException("Состояние мира превышает допустимый размер сетевого сообщения.");
            using (var writer = new FastBufferWriter(bytes.Length + sizeof(int), Allocator.Temp))
            {
                writer.WriteValueSafe(bytes.Length);
                writer.WriteBytesSafe(bytes);
                manager.CustomMessagingManager.SendNamedMessage(MessageName, peer, writer,
                    NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        public Task CloseAsync() => closingTask ?? (closingTask = CloseCoreAsync());

        private async Task CloseCoreAsync()
        {
            closing = true;
            hasPeer = false;
            lifetime.Cancel();
            if (opening != null)
                try { await opening; } catch (Exception) { }
            if (manager)
            {
                manager.OnClientConnectedCallback -= OnConnected;
                manager.OnClientDisconnectCallback -= OnDisconnected;
                manager.OnTransportFailure -= OnTransportFailure;
                manager.OnServerStarted -= RegisterMessages;
                manager.OnClientStarted -= RegisterMessages;
                registeredMessages?.UnregisterNamedMessageHandler(MessageName);
            }
            try
            {
                if (session != null)
                {
                    session.Deleted -= OnSessionLost;
                    session.RemovedFromSession -= OnSessionLost;
                    session.SessionHostChanged -= OnHostChanged;
                    if (host && session.IsHost) await session.AsHost().DeleteAsync();
                    else await session.LeaveAsync();
                }
            }
            catch (Exception error) { Debug.LogWarning("GOAT_ROOM_CLOSE " + error.GetType().Name); }
            finally
            {
                if (manager && manager.IsListening)
                {
                    manager.Shutdown();
                    float until = Time.realtimeSinceStartup + 3f;
                    while (manager && manager.ShutdownInProgress && Time.realtimeSinceStartup < until)
                        await Task.Yield();
                }
                if (node) UnityEngine.Object.Destroy(node);
                session = null;
                lifetime.Dispose();
            }
        }

        public static string DescribeError(Exception error)
        {
            if (error is SessionException sessionError)
            {
                switch (sessionError.Error)
                {
                    case SessionError.SessionNotFound:
                    case SessionError.SessionDeleted:
                    case SessionError.InvalidSessionIdentifier:
                    case SessionError.InvalidParameter:
                        return "Комната не найдена. Проверь код; ведущий должен оставаться в игре.";
                    case SessionError.Forbidden:
                        return "Вход недоступен: комната закрыта или уже заполнена.";
                    case SessionError.RateLimitExceeded:
                        return "Слишком много попыток. Подожди немного и попробуй снова.";
                    default:
                        return "Не удалось подключить комнату. Проверь интернет и доступность сетевых сервисов ("
                            + sessionError.Error + ").";
                }
            }
            return error is InvalidOperationException ? error.Message
                : "Сетевые сервисы недоступны. Проверь интернет и настройку проекта Unity Cloud.";
        }
    }
}
