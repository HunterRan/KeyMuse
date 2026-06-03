using Xunit;
using KeyMuse.Core.Models;
using KeyMuse.Core.Services;
using System.IO.Compression;
using System.Text.Json;

namespace KeyMuse.Tests;

public class RecordingManagerTests : IDisposable
{
    private readonly string _testDir;
    private readonly RecordingManager _manager;

    public RecordingManagerTests()
    {
        _testDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KMTest_" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(_testDir);
        _manager = new RecordingManager(_testDir);
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(_testDir))
            System.IO.Directory.Delete(_testDir, true);
    }

    private string CreateTestRecording(string category, string name, int events = 10)
    {
        var catDir = System.IO.Path.Combine(_testDir, category);
        System.IO.Directory.CreateDirectory(catDir);
        var filePath = System.IO.Path.Combine(catDir, name + ".keymuse");

        var session = new RecordingSession
        {
            CreatedAt = DateTime.Now,
            DurationMs = 5000,
            Events = Enumerable.Range(0, events).Select(i => new InputEvent
            {
                TimeOffsetMs = i * 100,
                Type = InputEventType.KeyDown,
                VirtualKeyCode = 0x41
            }).ToList()
        };

        using var stream = new FileStream(filePath, FileMode.Create);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        var jsonEntry = archive.CreateEntry("session.json");
        using (var writer = new StreamWriter(jsonEntry.Open()))
        {
            writer.Write(JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true }));
        }
        var eventsEntry = archive.CreateEntry("events.bin");
        using (var binWriter = new BinaryWriter(eventsEntry.Open()))
        {
            binWriter.Write(session.Events.Count);
            foreach (var e in session.Events)
            {
                binWriter.Write(e.TimeOffsetMs);
                binWriter.Write((int)e.Type);
                binWriter.Write(e.VirtualKeyCode);
                binWriter.Write(e.X); binWriter.Write(e.Y);
                binWriter.Write(e.RelX); binWriter.Write(e.RelY);
                binWriter.Write(e.MouseData);
                binWriter.Write(e.WindowHandle.ToInt64());
                binWriter.Write(e.WindowLeft); binWriter.Write(e.WindowTop);
                binWriter.Write(e.WindowWidth); binWriter.Write(e.WindowHeight);
            }
        }
        return filePath;
    }

    [Fact]
    public void ListCategories_Empty_ReturnsEmpty()
    {
        var cats = _manager.ListCategories();
        Assert.Empty(cats);
    }

    [Fact]
    public void CreateCategory_CreatesDirectory()
    {
        _manager.CreateCategory("test_cat");
        Assert.True(System.IO.Directory.Exists(System.IO.Path.Combine(_testDir, "test_cat")));
    }

    [Fact]
    public void ListCategories_ReturnsCreated()
    {
        _manager.CreateCategory("game");
        _manager.CreateCategory("work");
        var cats = _manager.ListCategories();
        Assert.Contains("game", cats);
        Assert.Contains("work", cats);
    }

    [Fact]
    public void ListRecordings_ReturnsRecordingInfo()
    {
        _manager.CreateCategory("test");
        CreateTestRecording("test", "rec1", 5);
        var list = _manager.ListRecordings("test");
        Assert.Single(list);
        Assert.Equal(5, list[0].EventCount);
        Assert.Equal("rec1", list[0].FileName);
    }

    [Fact]
    public void ListAllRecordings_AggregatesAll()
    {
        _manager.CreateCategory("a");
        _manager.CreateCategory("b");
        CreateTestRecording("a", "r1");
        CreateTestRecording("b", "r2");
        CreateTestRecording("b", "r3");
        Assert.Equal(3, _manager.ListAllRecordings().Length);
    }

    [Fact]
    public void DeleteRecording_RemovesFile()
    {
        _manager.CreateCategory("test");
        var path = CreateTestRecording("test", "del_me");
        _manager.DeleteRecording(path);
        Assert.False(System.IO.File.Exists(path));
    }

    [Fact]
    public void SaveRecording_MovesToCategory()
    {
        _manager.CreateCategory("target");
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_save.keymuse");
        // Create test recording file in temp
        var session = new RecordingSession { DurationMs = 100 };
        using (var stream = new FileStream(tempFile, FileMode.Create))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            var je = archive.CreateEntry("session.json");
            using var w = new StreamWriter(je.Open());
            w.Write(JsonSerializer.Serialize(session));
        }

        var saved = _manager.SaveRecording(tempFile, "target");
        Assert.True(System.IO.File.Exists(saved));
        Assert.Contains("target", saved);
    }

    [Fact]
    public void RenameRecording_ChangesName()
    {
        _manager.CreateCategory("test");
        var path = CreateTestRecording("test", "old_name");
        var renamed = _manager.RenameRecording(path, "new_name");
        Assert.False(System.IO.File.Exists(path));
        Assert.True(System.IO.File.Exists(renamed));
        Assert.Contains("new_name", renamed);
    }
}
