using System;
using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Managing.Transporting;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowTrouble.Tests
{
    public sealed class RecoveryOfflineTransportTests
    {
        private static void WithTransport(Action<RecoveryOfflineTransport> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var go = new GameObject("Disposable offline transport"); go.SetActive(false); SceneManager.MoveGameObjectToScene(go, scene);
            try { var transport = go.AddComponent<RecoveryOfflineTransport>(); transport.Initialize(null, 7); check(transport); }
            finally { UnityEngine.Object.DestroyImmediate(go); EditorSceneManager.ClosePreviewScene(scene); }
        }
        private static void Start(RecoveryOfflineTransport t)
        { Assert.That(t.StartConnection(true), Is.True); Assert.That(t.StartConnection(false), Is.True); }

        [Test] public void OnlyOneInProcessPlayerCanStartWithoutAnyAddressOrPort()
        {
            WithTransport(t =>
            {
                Assert.That(t.StartConnection(false), Is.False);
                Start(t);
                Assert.That(t.StartConnection(true), Is.False); Assert.That(t.StartConnection(false), Is.False);
                Assert.That(t.GetMaximumClients(), Is.EqualTo(1)); t.SetMaximumClients(64); Assert.That(t.GetMaximumClients(), Is.EqualTo(1));
                Assert.That(t.GetPort(), Is.Zero); Assert.That(t.GetClientAddress(), Is.Empty); Assert.That(t.GetConnectionAddress(0), Is.Empty);
                Assert.That(t.GetConnectionState(0), Is.EqualTo(RemoteConnectionState.Started));
                Assert.That(t.GetConnectionState(1), Is.EqualTo(RemoteConnectionState.Stopped));
                Assert.That(t.StopConnection(1, true), Is.False);
                Assert.That(t.GetConnectionState(false), Is.EqualTo(LocalConnectionState.Started));
                Assert.That(t.IsLocalTransport(0), Is.True); Assert.That(t.IsLocalTransport(1), Is.False);
            });
        }

        [Test] public void PacketsPreserveOffsetsChannelsOrderAndTransportIndexAfterCallerReusesBuffer()
        {
            WithTransport(t =>
            {
                Start(t); var received = new List<int>(); var channels = new List<Channel>(); int clientPackets = 0;
                t.OnServerReceivedData += args => { Assert.That(args.ConnectionId, Is.Zero); Assert.That(args.TransportIndex, Is.EqualTo(7)); received.Add(args.Data.Array[args.Data.Offset]); channels.Add(args.Channel); };
                t.OnClientReceivedData += args => { Assert.That(args.TransportIndex, Is.EqualTo(7)); Assert.That(args.Data.Count, Is.EqualTo(2)); Assert.That(args.Data.Array[args.Data.Offset], Is.EqualTo(9)); clientPackets++; };
                byte[] bytes = { 99, 3, 4, 88 };
                t.SendToServer((byte)Channel.Reliable, new ArraySegment<byte>(bytes, 1, 2)); bytes[1] = 8;
                t.SendToServer((byte)Channel.Unreliable, new ArraySegment<byte>(bytes, 1, 2)); bytes[1] = 0;
                Assert.That(received, Is.Empty); t.IterateIncoming(true);
                Assert.That(received.ToArray(), Is.EqualTo(new[] { 3, 8 })); Assert.That(channels.ToArray(), Is.EqualTo(new[] { Channel.Reliable, Channel.Unreliable }));
                t.SendToClient(0, new ArraySegment<byte>(new byte[] { 9, 10 }), 1); t.IterateIncoming(false); Assert.That(clientPackets, Is.Zero);
                t.SendToClient(0, new ArraySegment<byte>(new byte[] { 9, 10 }), 0); t.IterateIncoming(false); Assert.That(clientPackets, Is.EqualTo(1));
            });
        }

        [Test] public void RestartDropsPreviousSessionPacketsAndEmitsOneDisconnect()
        {
            WithTransport(t =>
            {
                int packets = 0, stopped = 0;
                t.OnServerReceivedData += args => packets++;
                t.OnRemoteConnectionState += args => { if (args.ConnectionState == RemoteConnectionState.Stopped) stopped++; };
                Start(t); t.SendToServer(0, new ArraySegment<byte>(new byte[] { 1 }));
                Assert.That(t.StopConnection(false), Is.True); Assert.That(t.StopConnection(false), Is.False); Assert.That(stopped, Is.EqualTo(1));
                Assert.That(t.StartConnection(false), Is.True); t.IterateIncoming(true); Assert.That(packets, Is.Zero);
                t.SendToServer(0, new ArraySegment<byte>(new byte[] { 2 })); t.IterateIncoming(true); Assert.That(packets, Is.EqualTo(1));
                t.Shutdown(); t.Shutdown(); Assert.That(stopped, Is.EqualTo(2));
                Assert.That(t.GetConnectionState(true), Is.EqualTo(LocalConnectionState.Stopped)); Assert.That(t.GetConnectionState(false), Is.EqualTo(LocalConnectionState.Stopped));
            });
        }

        [Test] public void DisconnectFromReceiveCallbackDiscardsTheRestOfTheBatch()
        {
            WithTransport(t =>
            {
                Start(t); int packets = 0;
                t.OnServerReceivedData += args => { packets++; t.StopConnection(false); };
                for (int i = 0; i < 10; i++) t.SendToServer(0, new ArraySegment<byte>(new byte[] { 1 }));
                Assert.DoesNotThrow(() => t.IterateIncoming(true)); Assert.That(packets, Is.EqualTo(1));
                Assert.That(t.StartConnection(false), Is.True); t.IterateIncoming(true); Assert.That(packets, Is.EqualTo(1));
            });
        }

        [Test] public void OfflinePolicyInstallsBeforeInitializationAndDesktopKeepsItsExistingTransport()
        {
            bool previous = RecoveryPlatform.PreviewSinglePlayerOnly;
            var scene = EditorSceneManager.NewPreviewScene(); var go = new GameObject("Inactive session manager"); go.SetActive(false); SceneManager.MoveGameObjectToScene(go, scene);
            try
            {
                var online = go.AddComponent<Tugboat>(); var tm = go.AddComponent<TransportManager>(); tm.Transport = online; var manager = go.AddComponent<NetworkManager>();
#if !UNITY_SWITCH
                RecoveryPlatform.PreviewSinglePlayerOnly = false; RecoveryOfflineTransport.Prepare(manager);
                Assert.That(tm.Transport, Is.SameAs(online)); Assert.That(go.GetComponent<RecoveryOfflineTransport>(), Is.Null);
#endif
                RecoveryPlatform.PreviewSinglePlayerOnly = true; RecoveryOfflineTransport.Prepare(manager);
                var offline = tm.Transport as RecoveryOfflineTransport; Assert.That(offline, Is.Not.Null);
                RecoveryOfflineTransport.Prepare(manager); Assert.That(tm.Transport, Is.SameAs(offline));
                Assert.That(manager.Initialized, Is.False); Assert.That(offline.GetConnectionState(true), Is.EqualTo(LocalConnectionState.Stopped));
                Assert.That(online.GetConnectionState(true), Is.EqualTo(LocalConnectionState.Stopped));
                var order = (DefaultExecutionOrder)Attribute.GetCustomAttribute(typeof(DemoSession), typeof(DefaultExecutionOrder)); var managerOrder = (DefaultExecutionOrder)Attribute.GetCustomAttribute(typeof(NetworkManager), typeof(DefaultExecutionOrder)); Assert.That(order.order, Is.LessThan(managerOrder.order));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); EditorSceneManager.ClosePreviewScene(scene); RecoveryPlatform.PreviewSinglePlayerOnly = previous; }
        }

        [Test] public void OfflineSoloRefusesSocketTransportBeforeStartingAnyManager()
        {
            bool previous = RecoveryPlatform.PreviewSinglePlayerOnly;
            var scene = EditorSceneManager.NewPreviewScene(); var go = new GameObject("Inactive socket fallback guard"); go.SetActive(false); SceneManager.MoveGameObjectToScene(go, scene);
            try
            {
                RecoveryPlatform.PreviewSinglePlayerOnly = true;
                var socket = go.AddComponent<Tugboat>(); var tm = go.AddComponent<TransportManager>(); tm.Transport = socket;
                var manager = go.AddComponent<NetworkManager>();
                typeof(NetworkManager).GetProperty("TransportManager").SetValue(manager, tm);
                var session = go.AddComponent<DemoSession>(); session.Configure(manager, null, null);
                typeof(DemoSession).GetProperty("Profile").SetValue(session, new RecoveryProfile { starterChosen = true, SuppressSaving = true });
                // ServerManager/ClientManager are intentionally absent: touching either would fail.
                Assert.DoesNotThrow(() => session.Host(true));
                Assert.That(session.Connecting, Is.False); Assert.That(session.Connected, Is.False);
                Assert.That(session.Status, Does.Contain("Offline play"));
                Assert.That(socket.NetworkManager, Is.Null); Assert.That(socket.GetConnectionState(true), Is.EqualTo(LocalConnectionState.Stopped));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); EditorSceneManager.ClosePreviewScene(scene); RecoveryPlatform.PreviewSinglePlayerOnly = previous; }
        }

        [Test] public void SwitchDoesNotAutoInitializeSocketsOrNetworkInterfaces()
        {
            Assert.That(PlayerSettings.Switch.socketInitializeEnabled, Is.False);
            Assert.That(PlayerSettings.Switch.networkInterfaceManagerInitializeEnabled, Is.False);
        }
    }
}
