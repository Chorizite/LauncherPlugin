﻿using Chorizite.Common;
using Chorizite.Core.Backend.Launcher;
using Microsoft.Extensions.Logging;
using RmlUi.Lib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Launcher.Lib {
    public class UpdateChecker : IDisposable {
        private readonly HttpClient client = new HttpClient();
        private LauncherPlugin launcherPlugin;
        private DateTime _lastUpdateCheck = DateTime.MinValue;
        private string? _downloadUrl;
        private bool _hasUpdate;
        private Panel? _panel;
        private float _downloadProgress;
        private readonly string tmpDir = Path.Combine(Path.GetTempPath(), "chorizite");

        /// <summary>
        /// The number of minutes between update checks
        /// </summary>
        public int UpdateCheckMinutes { get; set; } = 120;

        /// <summary>
        /// Whether or not to check for updates
        /// </summary>
        public bool CheckForUpdates { get; set; } = true;

        /// <summary>
        /// The currently installed Chorizite version
        /// </summary>
        public Version? CurrentVersion { get; private set; } = new Version(0, 0, 0);

        /// <summary>
        /// Whether or not an update is available
        /// </summary>
        public bool HasUpdateAvailable => _hasUpdate;

        /// <summary>
        /// The download progress, from 0-1
        /// </summary>
        public float DownloadProgress {
            get => _downloadProgress;
            set {
                if (value == _downloadProgress) return;
                _downloadProgress = value;
                _OnUpdateProgressChanged.Invoke(this, new UpdateProgressEventArgs(value));
            }
        }

        /// <summary>
        /// Called when update progress changes
        /// </summary>
        public event EventHandler<UpdateProgressEventArgs> OnUpdateProgressChanged {
            add => _OnUpdateProgressChanged.Subscribe(value);
            remove => _OnUpdateProgressChanged.Unsubscribe(value);
        }
        private WeakEvent<UpdateProgressEventArgs> _OnUpdateProgressChanged = new();

        /// <summary>
        /// The latest available version of Chorizite. Relies on an update check to be populated.
        /// </summary>
        public Version? LatestVersion { get; private set; }

        public UpdateChecker(LauncherPlugin launcherPlugin) {
            this.launcherPlugin = launcherPlugin;

            try {
                var fvi = FileVersionInfo.GetVersionInfo(typeof(Chorizite.Core.ChoriziteConfig).Assembly.Location);
                CurrentVersion = new Version(fvi.ProductVersion.Split('-').First());
            }
            catch (Exception ex) {
                LauncherPlugin.Log?.LogError(ex, "Failed to get current version");
                CurrentVersion = new Version(0, 0, 0);
            }

            UpdateLoop();
        }

        private void UpdateLoop() {
            Task.Run(async () => {
                while (true) {
                    await Task.Delay(TimeSpan.FromSeconds(4));
                    try {
                        if (CheckForUpdates) {
                            await CheckForUpdate();
                        }
                    }
                    catch (Exception ex) {
                        LauncherPlugin.Log?.LogError(ex, "Failed to check for updates");
                    }
                    await Task.Delay(TimeSpan.FromMinutes(UpdateCheckMinutes));
                }
            });
        }

        public void Update() {
            if (string.IsNullOrEmpty(_downloadUrl)) {
                LauncherPlugin.Log.LogWarning("No update available");
            }
            Task.Run(async () => {
                var request = new HttpRequestMessage(HttpMethod.Get, _downloadUrl);
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(Path.Combine(tmpDir, "chorizite-installer.exe"), FileMode.Create, FileAccess.Write, FileShare.None)) {
                    byte[] buffer = new byte[8192];
                    long totalBytesRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0) {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);

                        totalBytesRead += bytesRead;

                        if (totalBytes > 0) {
                            double progressPercentage = (double)totalBytesRead / totalBytes;
                            launcherPlugin.Backend.Invoke(() => { DownloadProgress = (float)progressPercentage; });
                        }
                    }
                }
                launcherPlugin.Backend.Invoke(() => {
                    LaunchInstaller();
                });
            });
        }

        public async Task CheckForUpdate() {
            try {
                var url = "https://chorizite.github.io/plugin-index/index.json";

                var response = await client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                var index = JsonObject.Parse(json);

                if (index is not null && index["Chorizite"] is JsonObject releaseInfo) {
                    LatestVersion = new Version(releaseInfo["Version"].ToString());

                    LauncherPlugin.Log?.LogDebug($"Latest Chorizite release is {LatestVersion} vs installed version {CurrentVersion}");

                    _downloadUrl = releaseInfo["DownloadUrl"]?.ToString() ?? null;

                    if (LatestVersion > CurrentVersion) {
                        _hasUpdate = true;
                        CreateUpdatePanel();
                    }
                }
            }
            catch (Exception ex) {
                LauncherPlugin.Log?.LogError(ex, "Failed to check for updates");
            }
        }

        private void CreateUpdatePanel() {
            launcherPlugin.Backend.Invoke(() => {
                _panel = launcherPlugin.RmlUi.CreatePanel("ChoriziteUpdateNotifier", Path.Combine(launcherPlugin.AssemblyDirectory, "assets", "panels", "updatenotifier.rml"));
                _panel.WantsAttention = true;
                _panel.ShowInBar = true;
                launcherPlugin.Backend.PlaySound(0x0A00057B);
            });
        }

        private void LaunchInstaller() {
            Task.Run(async () => {
                try {
                    Process.Start(new ProcessStartInfo() {
                        FileName = Path.Combine(tmpDir, "chorizite-installer.exe"),
                        Verb = "RunAs",
                        UseShellExecute = true
                    });
                    launcherPlugin.LauncherBackend.Exit();
                }
                catch (Exception ex) {
                    LauncherPlugin.Log?.LogError(ex, "Failed to launch installer");
                }
            });
        }

        public void Dispose() {
            client.Dispose();
        }
    }
}