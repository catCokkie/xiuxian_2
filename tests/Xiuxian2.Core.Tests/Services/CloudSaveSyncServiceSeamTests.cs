using Xiuxian.Scripts.Services;
using Xiuxian2.Core.Tests.Builders;
using Xiuxian2.Core.Tests.Support.Deterministic;

namespace Xiuxian2.Core.Tests.Services;

public sealed class CloudSaveSyncServiceSeamTests
{
    [Fact]
    public void DownloadWritesBytesToFakeGlobalizedPath()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        var bridge = new FakeCloudSaveBridge()
            .AddCloudFile("save_state.cfg", new byte[] { 7, 8, 9 });
        var service = new CloudSaveSyncService(fixture.FileSystem, bridge)
        {
            LocalSavePath = "user://cloud-save.cfg",
            CloudFileName = "save_state.cfg"
        };

        var downloaded = service.TryDownloadToLocal(enabled: true);

        var expectedPath = fixture.FileSystem.GlobalizePath(service.LocalSavePath);
        Assert.True(downloaded);
        Assert.True(fixture.FileSystem.FileExists(expectedPath));
        Assert.Equal(expectedPath, fixture.FileSystem.LastWrittenPath);
        Assert.Equal(1, fixture.FileSystem.WriteAllBytesCallCount);
        Assert.Equal(new byte[] { 7, 8, 9 }, fixture.FileSystem.ReadAllBytes(expectedPath));
        Assert.Equal(service.CloudFileName, bridge.LastReadFileName);
        Assert.Equal(1, bridge.TryReadFileCallCount);
    }

    [Fact]
    public void UploadReadsBytesFromFakeFilesystemAndForwardsThemToCloudBridge()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        var bridge = new FakeCloudSaveBridge();
        var service = new CloudSaveSyncService(fixture.FileSystem, bridge)
        {
            LocalSavePath = "user://cloud-save.cfg",
            CloudFileName = "save_state.cfg"
        };
        var expectedPath = fixture.FileSystem.GlobalizePath(service.LocalSavePath);
        fixture.FileSystem.AddFile(expectedPath, new byte[] { 1, 2, 3, 4 });

        var uploaded = service.TryUploadLocal(enabled: true);

        Assert.True(uploaded);
        Assert.Equal(expectedPath, fixture.FileSystem.LastReadPath);
        Assert.Equal(1, fixture.FileSystem.ReadAllBytesCallCount);
        Assert.Equal(service.CloudFileName, bridge.LastWriteFileName);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, bridge.LastWrittenData);
        Assert.Equal(1, bridge.WriteFileCallCount);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void DisabledOrUnavailableCloudShortCircuitsWithoutFilesystemSideEffects(bool enabled, bool available)
    {
        var fixture = new ServiceFixtureBuilder().Build();
        var bridge = new FakeCloudSaveBridge { IsAvailable = available };
        var service = new CloudSaveSyncService(fixture.FileSystem, bridge)
        {
            LocalSavePath = "user://cloud-save.cfg",
            CloudFileName = "save_state.cfg"
        };
        var expectedPath = fixture.FileSystem.GlobalizePath(service.LocalSavePath);
        fixture.FileSystem.AddFile(expectedPath, new byte[] { 1, 2, 3, 4 });

        var uploaded = service.TryUploadLocal(enabled);
        var downloaded = service.TryDownloadToLocal(enabled);

        Assert.False(uploaded);
        Assert.False(downloaded);
        Assert.Equal(0, fixture.FileSystem.ReadAllBytesCallCount);
        Assert.Equal(0, fixture.FileSystem.WriteAllBytesCallCount);
        Assert.Equal(0, bridge.WriteFileCallCount);
        Assert.Equal(0, bridge.TryReadFileCallCount);
    }
}
