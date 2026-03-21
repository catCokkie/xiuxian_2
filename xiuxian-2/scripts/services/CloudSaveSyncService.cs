using Godot;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xiuxian.Scripts.Adapters.Godot;
using Xiuxian.Scripts.Contracts;

[assembly: InternalsVisibleTo("Xiuxian2.Core.Tests")]

namespace Xiuxian.Scripts.Services
{
    internal interface ICloudSaveBridge
    {
        bool IsAvailable { get; }
        bool WriteFile(string fileName, byte[] data);
        bool TryReadFile(string fileName, out byte[] data);
    }

    /// <summary>
    /// Cloud save bridge with Steam-first behavior.
    /// Uses reflection so project can run without direct Steamworks dependency.
    /// </summary>
    public partial class CloudSaveSyncService : Node
    {
        private readonly CloudSaveSyncRuntime _runtime;

        [Export]
        public string LocalSavePath
        {
            get => _runtime.LocalSavePath;
            set => _runtime.LocalSavePath = value;
        }

        [Export]
        public string CloudFileName
        {
            get => _runtime.CloudFileName;
            set => _runtime.CloudFileName = value;
        }

        public CloudSaveSyncService()
            : this(new CloudSaveSyncRuntime(
                new GodotFileSystem(),
                static () => ReflectionSteamCloudBridge.TryCreate(),
                GD.Print,
                GD.PushWarning))
        {
        }

        internal CloudSaveSyncService(IFileSystem fileSystem, ICloudSaveBridge bridge)
            : this(new CloudSaveSyncRuntime(fileSystem, () => bridge, static _ => { }, static _ => { }))
        {
        }

        private CloudSaveSyncService(CloudSaveSyncRuntime runtime)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public override void _Ready()
        {
            _runtime.InitializeBridge();
        }

        public bool TryDownloadToLocal(bool enabled) => _runtime.TryDownloadToLocal(enabled);

        public bool TryUploadLocal(bool enabled) => _runtime.TryUploadLocal(enabled);

        internal sealed class CloudSaveSyncRuntime
        {
            private readonly IFileSystem _fileSystem;
            private readonly Func<ICloudSaveBridge?> _bridgeFactory;
            private readonly Action<string> _logInfo;
            private readonly Action<string> _logWarning;
            private ICloudSaveBridge _bridge;

            public CloudSaveSyncRuntime(
                IFileSystem fileSystem,
                Func<ICloudSaveBridge?> bridgeFactory,
                Action<string> logInfo,
                Action<string> logWarning)
            {
                _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
                _bridgeFactory = bridgeFactory ?? throw new ArgumentNullException(nameof(bridgeFactory));
                _logInfo = logInfo ?? throw new ArgumentNullException(nameof(logInfo));
                _logWarning = logWarning ?? throw new ArgumentNullException(nameof(logWarning));
                _bridge = new NoopSteamCloudBridge();
            }

            public string LocalSavePath { get; set; } = "user://save_state.cfg";

            public string CloudFileName { get; set; } = "save_state.cfg";

            public void InitializeBridge()
            {
                ICloudSaveBridge? steamBridge = _bridgeFactory();
                _bridge = steamBridge ?? new NoopSteamCloudBridge();
                _logInfo($"CloudSaveSyncService: Steam cloud available = {_bridge.IsAvailable}");
            }

            public bool TryDownloadToLocal(bool enabled)
            {
                if (!enabled || !_bridge.IsAvailable)
                {
                    return false;
                }

                if (!_bridge.TryReadFile(CloudFileName, out byte[] data))
                {
                    return false;
                }

                string path = _fileSystem.GlobalizePath(LocalSavePath);
                _fileSystem.WriteAllBytes(path, data);
                _logInfo("CloudSaveSyncService: downloaded cloud save to local.");
                return true;
            }

            public bool TryUploadLocal(bool enabled)
            {
                if (!enabled || !_bridge.IsAvailable)
                {
                    return false;
                }

                string path = _fileSystem.GlobalizePath(LocalSavePath);
                if (!_fileSystem.FileExists(path))
                {
                    return false;
                }

                byte[] data = _fileSystem.ReadAllBytes(path);
                bool ok = _bridge.WriteFile(CloudFileName, data);
                if (ok)
                {
                    _logInfo("CloudSaveSyncService: uploaded local save to cloud.");
                }
                else
                {
                    _logWarning("CloudSaveSyncService: failed to upload local save.");
                }

                return ok;
            }
        }

        private sealed class NoopSteamCloudBridge : ICloudSaveBridge
        {
            public bool IsAvailable => false;
            public bool WriteFile(string fileName, byte[] data) => false;
            public bool TryReadFile(string fileName, out byte[] data)
            {
                data = Array.Empty<byte>();
                return false;
            }
        }

        private sealed class ReflectionSteamCloudBridge : ICloudSaveBridge
        {
            private readonly Type _remoteStorageType;
            private readonly MethodInfo _fileWrite;
            private readonly MethodInfo _fileRead;
            private readonly MethodInfo _fileExists;
            private readonly MethodInfo _getFileSize;

            public bool IsAvailable => true;

            private ReflectionSteamCloudBridge(
                Type remoteStorageType,
                MethodInfo fileWrite,
                MethodInfo fileRead,
                MethodInfo fileExists,
                MethodInfo getFileSize)
            {
                _remoteStorageType = remoteStorageType;
                _fileWrite = fileWrite;
                _fileRead = fileRead;
                _fileExists = fileExists;
                _getFileSize = getFileSize;
            }

            public static ReflectionSteamCloudBridge? TryCreate()
            {
                Type? remoteStorageType = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(a => a.GetType("Steamworks.SteamRemoteStorage"))
                    .FirstOrDefault(t => t != null);

                if (remoteStorageType == null)
                {
                    return null;
                }

                MethodInfo? fileWrite = remoteStorageType.GetMethod("FileWrite", new[] { typeof(string), typeof(byte[]), typeof(int) });
                MethodInfo? fileRead = remoteStorageType.GetMethod("FileRead", new[] { typeof(string), typeof(byte[]), typeof(int) });
                MethodInfo? fileExists = remoteStorageType.GetMethod("FileExists", new[] { typeof(string) });
                MethodInfo? getFileSize = remoteStorageType.GetMethod("GetFileSize", new[] { typeof(string) });

                if (fileWrite == null || fileRead == null || fileExists == null || getFileSize == null)
                {
                    return null;
                }

                return new ReflectionSteamCloudBridge(remoteStorageType, fileWrite, fileRead, fileExists, getFileSize);
            }

            public bool WriteFile(string fileName, byte[] data)
            {
                object? result = _fileWrite.Invoke(_remoteStorageType, new object[] { fileName, data, data.Length });
                return result is bool ok && ok;
            }

            public bool TryReadFile(string fileName, out byte[] data)
            {
                data = Array.Empty<byte>();

                object? existsResult = _fileExists.Invoke(_remoteStorageType, new object[] { fileName });
                if (existsResult is not bool exists || !exists)
                {
                    return false;
                }

                object? sizeResult = _getFileSize.Invoke(_remoteStorageType, new object[] { fileName });
                int size = sizeResult is int n ? n : 0;
                if (size <= 0)
                {
                    return false;
                }

                byte[] buffer = new byte[size];
                object? readResult = _fileRead.Invoke(_remoteStorageType, new object[] { fileName, buffer, size });
                int read = readResult is int r ? r : 0;
                if (read <= 0)
                {
                    return false;
                }

                data = buffer;
                return true;
            }
        }
    }
}
