using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class MainForm
    {
        private const int REMOTE_DISCOVERY_PORT = 40445;
        private const int REMOTE_COMMAND_PORT = 40446;
        /// <summary>Нет объявлений дольше этого интервала — показываем «оффлайн».</summary>
        private const int REMOTE_PEER_TTL_SECONDS = 18;
        /// <summary>Удаляем запись пира из списка, если давно не было объявлений.</summary>
        private const int REMOTE_PEER_REMOVE_SECONDS = 45;
        private const string REMOTE_MULTICAST_GROUP = "239.255.77.77";
        private readonly string remoteSelfId = Guid.NewGuid().ToString("N");

        private UdpClient? discoveryListener;
        private UdpClient? discoveryBroadcaster;
        private TcpListener? remoteCommandListener;
        private CancellationTokenSource? remoteSyncCts;
        private readonly Dictionary<string, RemotePeerInfo> remotePeers = new Dictionary<string, RemotePeerInfo>();
        private readonly object remotePeersLock = new object();
        private bool remoteTargetsFirstPopulate = true;

        private void InitializeRemoteSync()
        {
            try
            {
                AddOrUpdateSelfPeer();
                RefreshRemoteTargetsUi();
                ConfigureRemoteTargetsUi();

                remoteSyncCts = new CancellationTokenSource();
                StartDiscoveryListener(remoteSyncCts.Token);
                StartDiscoveryBroadcaster(remoteSyncCts.Token);
                StartRemoteCommandListener(remoteSyncCts.Token);
                StartPeerCleanupLoop(remoteSyncCts.Token);
                AddLog($"🌐 Сетевая синхронизация запущена: UDP {REMOTE_DISCOVERY_PORT}, TCP {REMOTE_COMMAND_PORT}");
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Сетевая синхронизация недоступна: {ex.Message}");
            }
        }

        private void ConfigureRemoteTargetsUi()
        {
            if (checkedListRemoteTargets == null)
            {
                return;
            }

            checkedListRemoteTargets.SelectionMode = SelectionMode.One;
            checkedListRemoteTargets.SelectedIndexChanged += (_, __) =>
            {
                if (checkedListRemoteTargets.SelectedIndex >= 0)
                {
                    BeginInvoke(new Action(() => checkedListRemoteTargets.ClearSelected()));
                }
            };
        }

        private void DisposeRemoteSync()
        {
            try
            {
                remoteSyncCts?.Cancel();
                discoveryListener?.Dispose();
                discoveryBroadcaster?.Dispose();
                remoteCommandListener?.Stop();
                remoteSyncCts?.Dispose();
            }
            catch
            {
                // ignore
            }
        }

        private void AddOrUpdateSelfPeer()
        {
            lock (remotePeersLock)
            {
                remotePeers[remoteSelfId] = new RemotePeerInfo
                {
                    Id = remoteSelfId,
                    DeviceName = Environment.MachineName,
                    Address = "127.0.0.1",
                    CommandPort = REMOTE_COMMAND_PORT,
                    IsSelf = true,
                    LastSeenUtc = DateTime.UtcNow,
                    IsOnline = true
                };
            }
        }

        private void StartDiscoveryListener(CancellationToken token)
        {
            discoveryListener = new UdpClient(new IPEndPoint(IPAddress.Any, REMOTE_DISCOVERY_PORT));
            discoveryListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            discoveryListener.EnableBroadcast = true;
            discoveryListener.JoinMulticastGroup(IPAddress.Parse(REMOTE_MULTICAST_GROUP));
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        UdpReceiveResult result = await discoveryListener.ReceiveAsync(token);
                        string json = Encoding.UTF8.GetString(result.Buffer);
                        var announce = JsonSerializer.Deserialize<RemoteAnnounceMessage>(json);
                        if (announce == null || string.IsNullOrEmpty(announce.Id) || announce.Id == remoteSelfId)
                        {
                            continue;
                        }

                        lock (remotePeersLock)
                        {
                            string deviceName = announce.DeviceName ?? "Unknown PC";

                            foreach (var key in remotePeers.Keys.ToList())
                            {
                                var p = remotePeers[key];
                                if (!p.IsSelf && key != announce.Id &&
                                    string.Equals(p.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
                                    remotePeers.Remove(key);
                            }

                            remotePeers[announce.Id] = new RemotePeerInfo
                            {
                                Id = announce.Id,
                                DeviceName = deviceName,
                                Address = result.RemoteEndPoint.Address.ToString(),
                                CommandPort = announce.CommandPort <= 0 ? REMOTE_COMMAND_PORT : announce.CommandPort,
                                IsSelf = false,
                                LastSeenUtc = DateTime.UtcNow,
                                IsOnline = true
                            };
                        }

                        if (announce.MessageType == "announce")
                        {
                            _ = SendDiscoveryAckAsync(result.RemoteEndPoint.Address, token);
                        }

                        RefreshRemoteTargetsUi();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // keep listener alive
                    }
                }
            }, token);
        }

        private void StartDiscoveryBroadcaster(CancellationToken token)
        {
            discoveryBroadcaster = new UdpClient();
            discoveryBroadcaster.EnableBroadcast = true;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await BroadcastDiscoveryOnce(token);
                        await Task.Delay(2000, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        await Task.Delay(3000, token);
                    }
                }
            }, token);
        }

        private void StartRemoteCommandListener(CancellationToken token)
        {
            remoteCommandListener = new TcpListener(IPAddress.Any, REMOTE_COMMAND_PORT);
            remoteCommandListener.Start();

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        TcpClient client = await remoteCommandListener.AcceptTcpClientAsync(token);
                        _ = HandleRemoteCommandClient(client, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // keep listener alive
                    }
                }
            }, token);
        }

        private async Task HandleRemoteCommandClient(TcpClient client, CancellationToken token)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024 * 1024 * 8];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead <= 0) return;

                    string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var command = JsonSerializer.Deserialize<RemoteCommandMessage>(json);
                    if (command == null) return;

                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() => ExecuteRemoteCommand(command)));
                    }
                    else
                    {
                        ExecuteRemoteCommand(command);
                    }
                }
            }
            catch
            {
                // ignore malformed commands
            }
        }

        private void ExecuteRemoteCommand(RemoteCommandMessage command)
        {
            switch (command.Command)
            {
                case "play":
                    if (command.MacroData != null)
                    {
                        StartRemotePlayback(command.MacroData, command.Loop);
                    }
                    break;
                case "toggle_pause":
                    TogglePausePlayback(false);
                    break;
                case "stop":
                    StopPlayback(false);
                    break;
            }
        }

        private void StartPeerCleanupLoop(CancellationToken token)
        {
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        bool changed = false;
                        lock (remotePeersLock)
                        {
                            var expired = remotePeers.Values
                                .Where(p => !p.IsSelf && DateTime.UtcNow - p.LastSeenUtc > TimeSpan.FromSeconds(REMOTE_PEER_REMOVE_SECONDS))
                                .Select(p => p.Id)
                                .ToList();
                            foreach (var id in expired)
                            {
                                remotePeers.Remove(id);
                                changed = true;
                            }
                        }

                        if (changed || !token.IsCancellationRequested)
                        {
                            RefreshRemoteTargetsUi();
                        }

                        await Task.Delay(2000, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        private List<RemotePeerInfo> GetSelectedRemoteTargets()
        {
            var result = new List<RemotePeerInfo>();
            if (checkedListRemoteTargets == null)
            {
                return result;
            }

            foreach (var item in checkedListRemoteTargets.CheckedItems)
            {
                if (item is RemoteTargetListItem target)
                {
                    result.Add(target.Peer);
                }
            }
            return result;
        }

        private bool IsSelfSelected()
        {
            return GetSelectedRemoteTargets().Any(p => p.IsSelf);
        }

        private async Task BroadcastRemoteCommandAsync(RemoteCommandMessage message)
        {
            var selectedRemotePeers = GetSelectedRemoteTargets().Where(p => !p.IsSelf).ToList();
            if (selectedRemotePeers.Count == 0)
            {
                return;
            }

            string json = JsonSerializer.Serialize(message);
            byte[] payload = Encoding.UTF8.GetBytes(json);

            foreach (var peer in selectedRemotePeers)
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(IPAddress.Parse(peer.Address), peer.CommandPort);
                    using NetworkStream stream = client.GetStream();
                    await stream.WriteAsync(payload, 0, payload.Length);
                }
                catch
                {
                    // host may be unavailable at the moment
                }
            }
        }

        private void RefreshRemoteTargetsUi()
        {
            if (checkedListRemoteTargets == null)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshRemoteTargetsUi));
                return;
            }

            var checkedIds = new HashSet<string>();
            var checkedDeviceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in checkedListRemoteTargets.CheckedItems)
            {
                if (item is RemoteTargetListItem existing)
                {
                    checkedIds.Add(existing.Peer.Id);
                    if (!string.IsNullOrEmpty(existing.Peer.DeviceName))
                        checkedDeviceNames.Add(existing.Peer.DeviceName);
                }
            }

            List<RemotePeerInfo> peers;
            lock (remotePeersLock)
            {
                peers = remotePeers.Values
                    .OrderByDescending(p => p.IsSelf)
                    .ThenBy(p => p.DeviceName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            foreach (var peer in peers)
            {
                peer.IsOnline = IsPeerOnline(peer);
            }

            checkedListRemoteTargets.Items.Clear();
            foreach (var peer in peers)
            {
                var item = new RemoteTargetListItem(peer);
                int idx = checkedListRemoteTargets.Items.Add(item);
                bool shouldCheck = checkedIds.Contains(peer.Id) ||
                    (!peer.IsSelf && checkedDeviceNames.Contains(peer.DeviceName)) ||
                    (remoteTargetsFirstPopulate && peer.IsSelf && checkedIds.Count == 0 && checkedDeviceNames.Count == 0);
                checkedListRemoteTargets.SetItemChecked(idx, shouldCheck);
            }

            checkedListRemoteTargets.ClearSelected();
            remoteTargetsFirstPopulate = false;
        }

        private bool IsPeerOnline(RemotePeerInfo peer)
        {
            if (peer.IsSelf)
            {
                return true;
            }
            return DateTime.UtcNow - peer.LastSeenUtc <= TimeSpan.FromSeconds(REMOTE_PEER_TTL_SECONDS);
        }

        private async void btnRefreshRemoteTargets_Click(object? sender, EventArgs e)
        {
            if (remoteSyncCts == null)
            {
                return;
            }
            await BroadcastDiscoveryOnce(remoteSyncCts.Token);
            RefreshRemoteTargetsUi();
        }

        private void btnSelectAllRemoteTargets_Click(object? sender, EventArgs e)
        {
            if (checkedListRemoteTargets == null) return;
            for (int i = 0; i < checkedListRemoteTargets.Items.Count; i++)
            {
                checkedListRemoteTargets.SetItemChecked(i, true);
            }
            checkedListRemoteTargets.ClearSelected();
        }

        private void btnSelectNoneRemoteTargets_Click(object? sender, EventArgs e)
        {
            if (checkedListRemoteTargets == null) return;
            for (int i = 0; i < checkedListRemoteTargets.Items.Count; i++)
            {
                checkedListRemoteTargets.SetItemChecked(i, false);
            }
            checkedListRemoteTargets.ClearSelected();
        }

        private async Task BroadcastDiscoveryOnce(CancellationToken token)
        {
            if (discoveryBroadcaster == null)
            {
                return;
            }

            var announce = new RemoteAnnounceMessage
            {
                Id = remoteSelfId,
                DeviceName = Environment.MachineName,
                CommandPort = REMOTE_COMMAND_PORT,
                MessageType = "announce"
            };

            byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(announce));
            var endpoints = GetDiscoveryEndpoints();
            foreach (var endpoint in endpoints)
            {
                try
                {
                    await discoveryBroadcaster.SendAsync(payload, payload.Length, endpoint);
                }
                catch
                {
                    // continue with other endpoints
                }
            }

            await Task.Delay(100, token);
        }

        private async Task SendDiscoveryAckAsync(IPAddress targetAddress, CancellationToken token)
        {
            if (discoveryBroadcaster == null) return;

            var ack = new RemoteAnnounceMessage
            {
                Id = remoteSelfId,
                DeviceName = Environment.MachineName,
                CommandPort = REMOTE_COMMAND_PORT,
                MessageType = "ack"
            };

            byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ack));
            try
            {
                await discoveryBroadcaster.SendAsync(payload, payload.Length, new IPEndPoint(targetAddress, REMOTE_DISCOVERY_PORT));
                await Task.Delay(10, token);
            }
            catch
            {
                // ignore
            }
        }

        private List<IPEndPoint> GetDiscoveryEndpoints()
        {
            var endpoints = new List<IPEndPoint>
            {
                new IPEndPoint(IPAddress.Broadcast, REMOTE_DISCOVERY_PORT),
                new IPEndPoint(IPAddress.Parse(REMOTE_MULTICAST_GROUP), REMOTE_DISCOVERY_PORT)
            };

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                var ipProps = ni.GetIPProperties();
                foreach (var uni in ipProps.UnicastAddresses)
                {
                    if (uni.Address.AddressFamily != AddressFamily.InterNetwork || uni.IPv4Mask == null)
                    {
                        continue;
                    }

                    IPAddress broadcast = GetBroadcastAddress(uni.Address, uni.IPv4Mask);
                    endpoints.Add(new IPEndPoint(broadcast, REMOTE_DISCOVERY_PORT));
                }
            }

            return endpoints
                .GroupBy(e => $"{e.Address}:{e.Port}")
                .Select(g => g.First())
                .ToList();
        }

        private static IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipBytes = address.GetAddressBytes();
            byte[] maskBytes = subnetMask.GetAddressBytes();
            byte[] broadcastBytes = new byte[ipBytes.Length];

            for (int i = 0; i < ipBytes.Length; i++)
            {
                broadcastBytes[i] = (byte)(ipBytes[i] | (maskBytes[i] ^ 255));
            }

            return new IPAddress(broadcastBytes);
        }

        private sealed class RemotePeerInfo
        {
            public string Id { get; set; } = "";
            public string DeviceName { get; set; } = "";
            public string Address { get; set; } = "";
            public int CommandPort { get; set; }
            public bool IsSelf { get; set; }
            public DateTime LastSeenUtc { get; set; }
            public bool IsOnline { get; set; }
        }

        private sealed class RemoteTargetListItem
        {
            public RemoteTargetListItem(RemotePeerInfo peer) => Peer = peer;
            public RemotePeerInfo Peer { get; }
            public override string ToString()
            {
                string status = Peer.IsOnline ? "онлайн" : "оффлайн";
                return Peer.IsSelf
                    ? $"ПК - {Peer.DeviceName} (Текущее устройство, {status})"
                    : $"ПК - {Peer.DeviceName} ({Peer.Address}, {status})";
            }
        }

        private sealed class RemoteAnnounceMessage
        {
            public string Id { get; set; } = "";
            public string DeviceName { get; set; } = "";
            public int CommandPort { get; set; }
            public string MessageType { get; set; } = "announce";
        }

        private sealed class RemoteCommandMessage
        {
            public string Command { get; set; } = "";
            public bool Loop { get; set; }
            public MacroData? MacroData { get; set; }
        }
    }
}
