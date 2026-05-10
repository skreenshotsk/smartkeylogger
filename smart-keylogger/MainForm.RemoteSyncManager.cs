using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private const int REMOTE_PEER_TTL_SECONDS = 8;
        private const int REMOTE_PEER_REMOVE_SECONDS = 120;
        private const string REMOTE_SHARED_KEY = "smartkeylogger-lan-key";
        private readonly string remoteSelfId = Guid.NewGuid().ToString("N");

        private UdpClient? discoveryListener;
        private UdpClient? discoveryBroadcaster;
        private TcpListener? remoteCommandListener;
        private CancellationTokenSource? remoteSyncCts;
        private readonly Dictionary<string, RemotePeerInfo> remotePeers = new Dictionary<string, RemotePeerInfo>();
        private readonly object remotePeersLock = new object();
        private readonly HashSet<string> trustedRemotePeerIds = new HashSet<string>();
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

            checkedListRemoteTargets.SelectionMode = SelectionMode.None;
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
                            remotePeers[announce.Id] = new RemotePeerInfo
                            {
                                Id = announce.Id,
                                DeviceName = announce.DeviceName ?? "Unknown PC",
                                Address = result.RemoteEndPoint.Address.ToString(),
                                CommandPort = announce.CommandPort <= 0 ? REMOTE_COMMAND_PORT : announce.CommandPort,
                                IsSelf = false,
                                LastSeenUtc = DateTime.UtcNow,
                                IsOnline = true
                            };
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
                        var announce = new RemoteAnnounceMessage
                        {
                            Id = remoteSelfId,
                            DeviceName = Environment.MachineName,
                            CommandPort = REMOTE_COMMAND_PORT
                        };

                        byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(announce));
                        await discoveryBroadcaster.SendAsync(payload, payload.Length, new IPEndPoint(IPAddress.Broadcast, REMOTE_DISCOVERY_PORT));
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

                    if (command.AuthToken != REMOTE_SHARED_KEY)
                    {
                        AddLog($"🔒 Отклонена удаленная команда с неверным ключом ({client.Client.RemoteEndPoint})");
                        return;
                    }

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
            if (!IsRemoteSenderTrusted(command))
            {
                return;
            }

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

            message.SenderId = remoteSelfId;
            message.SenderName = Environment.MachineName;
            message.AuthToken = REMOTE_SHARED_KEY;
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
            foreach (var item in checkedListRemoteTargets.CheckedItems)
            {
                if (item is RemoteTargetListItem existing)
                {
                    checkedIds.Add(existing.Peer.Id);
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
                bool shouldCheck = checkedIds.Contains(peer.Id) || (remoteTargetsFirstPopulate && peer.IsSelf && checkedIds.Count == 0);
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

        private bool IsRemoteSenderTrusted(RemoteCommandMessage command)
        {
            if (string.IsNullOrWhiteSpace(command.SenderId))
            {
                return false;
            }

            if (trustedRemotePeerIds.Contains(command.SenderId))
            {
                return true;
            }

            string senderName = string.IsNullOrWhiteSpace(command.SenderName) ? command.SenderId : command.SenderName;
            DialogResult result = MessageBox.Show(
                $"Получена удаленная команда \"{command.Command}\" от устройства \"{senderName}\".\n\nРазрешить управление от этого устройства?",
                "Подтверждение удаленной команды",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                trustedRemotePeerIds.Add(command.SenderId);
                AddLog($"🔐 Устройство добавлено в доверенные: {senderName}");
                return true;
            }

            AddLog($"🔒 Удаленная команда отклонена: {senderName}");
            return false;
        }

        private async void btnRefreshRemoteTargets_Click(object? sender, EventArgs e)
        {
            await BroadcastDiscoveryOnce();
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

        private async Task BroadcastDiscoveryOnce()
        {
            if (discoveryBroadcaster == null)
            {
                return;
            }

            var announce = new RemoteAnnounceMessage
            {
                Id = remoteSelfId,
                DeviceName = Environment.MachineName,
                CommandPort = REMOTE_COMMAND_PORT
            };

            byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(announce));
            await discoveryBroadcaster.SendAsync(payload, payload.Length, new IPEndPoint(IPAddress.Broadcast, REMOTE_DISCOVERY_PORT));
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
        }

        private sealed class RemoteCommandMessage
        {
            public string Command { get; set; } = "";
            public bool Loop { get; set; }
            public MacroData? MacroData { get; set; }
            public string SenderId { get; set; } = "";
            public string SenderName { get; set; } = "";
            public string AuthToken { get; set; } = "";
        }
    }
}
