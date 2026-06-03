using Xunit;
using KeyMuse.Core.Models;
using KeyMuse.Core.Services;

namespace KeyMuse.Tests;

public class ConfigManagerTests
{
    [Fact]
    public void CreateAndLoadProfile_Roundtrip()
    {
        var mgr = new ConfigManager();
        var name = "TestProfile_" + Guid.NewGuid().ToString("N")[..8];

        mgr.CreateProfile(name);
        var loaded = mgr.LoadProfile(name);

        Assert.NotNull(loaded);
        Assert.Equal(name, loaded.Name);
    }

    [Fact]
    public void ListProfiles_ReturnsCreatedProfiles()
    {
        var mgr = new ConfigManager();
        var before = mgr.ListProfiles().Length;

        mgr.CreateProfile("ListTest_" + Guid.NewGuid().ToString("N")[..8]);
        var after = mgr.ListProfiles().Length;

        Assert.Equal(before + 1, after);
    }

    [Fact]
    public void SaveAndLoad_ConfigValues()
    {
        var mgr = new ConfigManager();
        var name = "ConfigTest_" + Guid.NewGuid().ToString("N")[..8];

        var config = mgr.CreateProfile(name);
        config.AutoClickIntervalMs = 500;
        config.AutoClickKeyCode = 0x2D;
        mgr.SaveProfile(config);

        var loaded = mgr.LoadProfile(name);
        Assert.NotNull(loaded);
        Assert.Equal(500, loaded.AutoClickIntervalMs);
        Assert.Equal(0x2D, loaded.AutoClickKeyCode);
    }

    [Fact]
    public void DeleteProfile_RemovesIt()
    {
        var mgr = new ConfigManager();
        var name = "DeleteTest_" + Guid.NewGuid().ToString("N")[..8];

        mgr.CreateProfile(name);
        mgr.DeleteProfile(name);

        var profiles = mgr.ListProfiles();
        Assert.DoesNotContain(name, profiles);
    }

    [Fact]
    public void LoadNonExistentProfile_ReturnsNull()
    {
        var mgr = new ConfigManager();
        var loaded = mgr.LoadProfile("NonExistent_" + Guid.NewGuid().ToString("N")[..8]);
        Assert.Null(loaded);
    }
}
