using Godot;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Cloud save bridge with Steam-first behavior.
    /// Uses reflection so project can run without direct Steamworks dependency.
    /// </summary>
    public partial class CloudSaveSyncService : Node
    {
        [Export] public string LocalSavePath = "user://save_state.cfg";
        [Export] public string CloudFileName = "save_state.cfg";

        private ISteamCloudBridge _bridge = new NoopSteamCloudBridge();

        public override void _Ready()
        {
            ReflectionSteamCloudBridge steamBridge = ReflectionSteamCloudBridge.TryCreate();
            if (steamBridge != null)
            {
                _bridge = steamBridge;
            }
            else
            {
                _bridge = new NoopSteamCloudBridge();
            }
            GD.Print($"CloudSaveSyncService: Steam cloud available = {_bridge.IsAvailable}");
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

            string path = ProjectSettings.GlobalizePath(LocalSavePath);
            File.WriteAllBytes(path, data);
            GD.Print("CloudSaveSyncService: downloaded cloud save to local.");
            return true;
        }

        public bool TryUploadLocal(bool enabled)
        {
            if (!enabled || !_bridge.IsAvailable)
            {
                return false;
            }

            string path = ProjectSettings.GlobalizePath(LocalSavePath);
            if (!File.Exists(path))
            {
                return false;
            }

            byte[] data = File.ReadAllBytes(path);
            bool ok = _bridge.WriteFile(CloudFileName, data);
            if (ok)
            {
                GD.Print("CloudSaveSyncService: uploaded local save to cloud.");
            }
            else
            {
                GD.PushWarning("CloudSaveSyncService: failed to upload local save.");
            }
            return ok;
        }

        private interface ISteamCloudBridge
        {
            bool IsAvailable { get; }
            bool WriteFile(string fileName, byte[] data);
            bool TryReadFile(string fileName, out byte[] data);
        }

        private sealed class NoopSteamCloudBridge : ISteamCloudBridge
        {
            public bool IsAvailable => false;
            public bool WriteFile(string fileName, byte[] data) => false;
            public bool TryReadFile(string fileName, out byte[] data)
            {
                data = Array.Empty<byte>();
                return false;
            }
        }

        private sealed class ReflectionSteamCloudBridge : ISteamCloudBridge
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
