using System;
using System.Buffers;
using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Managing.Transporting;
using FishNet.Transporting;
using UnityEngine;

namespace TowTrouble
{
    /// <summary>One-player, in-process FishNet transport. No sockets, addresses, threads or online service.</summary>
    [DisallowMultipleComponent]
    public sealed class RecoveryOfflineTransport : Transport
    {
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RestoreValidationScope()
        { RecoveryPlatform.PreviewSinglePlayerOnly = UnityEditor.SessionState.GetBool("TowTrouble.ValidationOffline", false); }
#endif
        public const int LocalPlayerId = 0;
        private const int Mtu = 1350, MaximumPendingPackets = 256;
        private struct Packet { public byte[] Bytes; public int Count; public byte Channel; }
        private readonly Queue<Packet> toServer = new Queue<Packet>();
        private readonly Queue<Packet> toClient = new Queue<Packet>();
        private LocalConnectionState serverState = LocalConnectionState.Stopped, clientState = LocalConnectionState.Stopped;
        private bool localPlayerPresent;
        private bool CanSend => serverState == LocalConnectionState.Started && clientState == LocalConnectionState.Started && localPlayerPresent;

        // DemoSession calls this early, on every scene load, before manager initialization.
        public static void Prepare(NetworkManager manager)
        {
            if (!RecoveryPlatform.OfflineOnly) return;
            if (manager == null) throw new InvalidOperationException("Offline play requires the configured session manager.");
            var transports = manager.GetComponent<TransportManager>();
            if (manager.Initialized)
            {
                if (transports != null && transports.Transport is RecoveryOfflineTransport) return;
                throw new InvalidOperationException("Select the offline transport before initializing the session manager.");
            }
            if (transports == null) transports = manager.gameObject.AddComponent<TransportManager>();
            var offline = manager.GetComponent<RecoveryOfflineTransport>();
            if (offline == null) offline = manager.gameObject.AddComponent<RecoveryOfflineTransport>();
            transports.Transport = offline;
        }

        public override event Action<ClientConnectionStateArgs> OnClientConnectionState;
        public override event Action<ServerConnectionStateArgs> OnServerConnectionState;
        public override event Action<RemoteConnectionStateArgs> OnRemoteConnectionState;
        public override event Action<ClientReceivedDataArgs> OnClientReceivedData;
        public override event Action<ServerReceivedDataArgs> OnServerReceivedData;
        public override void HandleClientConnectionState(ClientConnectionStateArgs args) => OnClientConnectionState?.Invoke(args);
        public override void HandleServerConnectionState(ServerConnectionStateArgs args) => OnServerConnectionState?.Invoke(args);
        public override void HandleRemoteConnectionState(RemoteConnectionStateArgs args) => OnRemoteConnectionState?.Invoke(args);
        public override void HandleClientReceivedDataArgs(ClientReceivedDataArgs args) => OnClientReceivedData?.Invoke(args);
        public override void HandleServerReceivedDataArgs(ServerReceivedDataArgs args) => OnServerReceivedData?.Invoke(args);
        public override LocalConnectionState GetConnectionState(bool server) => server ? serverState : clientState;
        public override RemoteConnectionState GetConnectionState(int connectionId)
            => connectionId == LocalPlayerId && localPlayerPresent ? RemoteConnectionState.Started : RemoteConnectionState.Stopped;
        public override string GetConnectionAddress(int connectionId) => string.Empty;
        public override bool IsLocalTransport(int connectionId) => connectionId == -1 || connectionId == LocalPlayerId;
        public override int GetMaximumClients() => 1;
        public override void SetMaximumClients(int value) { } // Always exactly one local player.
        public override int GetMTU(byte channel) => Mtu;

        public override bool StartConnection(bool server)
        {
            if (server)
            {
                if (serverState != LocalConnectionState.Stopped) return false;
                SetServerState(LocalConnectionState.Starting);
                if (serverState != LocalConnectionState.Starting) return false;
                SetServerState(LocalConnectionState.Started);
                return serverState == LocalConnectionState.Started;
            }
            if (serverState != LocalConnectionState.Started || clientState != LocalConnectionState.Stopped) return false;
            SetClientState(LocalConnectionState.Starting);
            if (clientState != LocalConnectionState.Starting || serverState != LocalConnectionState.Started) return false;
            localPlayerPresent = true;
            HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Started, LocalPlayerId, Index));
            if (!localPlayerPresent || serverState != LocalConnectionState.Started) return false;
            SetClientState(LocalConnectionState.Started);
            return clientState == LocalConnectionState.Started;
        }

        public override bool StopConnection(bool server)
        {
            if (!server) return StopLocalPlayer();
            if (serverState == LocalConnectionState.Stopped || serverState == LocalConnectionState.Stopping) return false;
            // Block new sends/reconnects before notifying the player callbacks.
            SetServerState(LocalConnectionState.Stopping);
            StopLocalPlayer();
            ClearPackets();
            SetServerState(LocalConnectionState.Stopped);
            return true;
        }
        public override bool StopConnection(int connectionId, bool immediately)
            => connectionId == LocalPlayerId && StopLocalPlayer();
        private bool StopLocalPlayer()
        {
            if (clientState == LocalConnectionState.Stopped || clientState == LocalConnectionState.Stopping) return false;
            SetClientState(LocalConnectionState.Stopping);
            bool present = localPlayerPresent; localPlayerPresent = false;
            ClearPackets();
            if (present) HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Stopped, LocalPlayerId, Index));
            SetClientState(LocalConnectionState.Stopped);
            return true;
        }
        private void SetServerState(LocalConnectionState state)
        { serverState = state; HandleServerConnectionState(new ServerConnectionStateArgs(state, Index)); }
        private void SetClientState(LocalConnectionState state)
        { clientState = state; HandleClientConnectionState(new ClientConnectionStateArgs(state, Index)); }

        public override void SendToServer(byte channelId, ArraySegment<byte> segment)
        { if (CanSend) Enqueue(toServer, channelId, segment); }
        public override void SendToClient(byte channelId, ArraySegment<byte> segment, int connectionId)
        { if (connectionId == LocalPlayerId && CanSend) Enqueue(toClient, channelId, segment); }
        private void Enqueue(Queue<Packet> queue, byte channel, ArraySegment<byte> segment)
        {
            if (segment.Count == 0 || channel > (byte)Channel.Unreliable) return;
            if (segment.Count > Mtu || queue.Count >= MaximumPendingPackets)
            {
                Debug.LogError("Offline session packet limit exceeded; stopping the local session.");
                Shutdown(); return;
            }
            byte[] copy = ArrayPool<byte>.Shared.Rent(segment.Count);
            Buffer.BlockCopy(segment.Array, segment.Offset, copy, 0, segment.Count);
            queue.Enqueue(new Packet { Bytes = copy, Count = segment.Count, Channel = channel });
        }
        public override void IterateIncoming(bool asServer)
        {
            if (!CanSend) return;
            var queue = asServer ? toServer : toClient;
            // A callback may respond or stop/restart. New packets wait for the next iteration.
            int remaining = queue.Count;
            while (remaining-- > 0 && queue.Count > 0 && CanSend)
            {
                var packet = queue.Dequeue();
                try
                {
                    var data = new ArraySegment<byte>(packet.Bytes, 0, packet.Count);
                    if (asServer) HandleServerReceivedDataArgs(new ServerReceivedDataArgs(data, (Channel)packet.Channel, LocalPlayerId, Index));
                    else HandleClientReceivedDataArgs(new ClientReceivedDataArgs(data, (Channel)packet.Channel, Index));
                }
                finally { ArrayPool<byte>.Shared.Return(packet.Bytes); }
            }
        }
        public override void IterateOutgoing(bool asServer) { } // Delivery stays on the existing FishNet tick loop.
        private void ClearPackets()
        {
            while (toServer.Count > 0) ArrayPool<byte>.Shared.Return(toServer.Dequeue().Bytes);
            while (toClient.Count > 0) ArrayPool<byte>.Shared.Return(toClient.Dequeue().Bytes);
        }
        public override void Shutdown() { StopConnection(true); StopConnection(false); ClearPackets(); }
        private void OnDestroy() { Shutdown(); }
    }
}
