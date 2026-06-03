using Xunit;
using KeyMuse.Core.Models;
using KeyMuse.Core.Services;

namespace KeyMuse.Tests;

public class RecorderTests
{
    [Fact]
    public void InputEvent_Roundtrip_Serialization()
    {
        var evt = new InputEvent
        {
            TimeOffsetMs = 1234,
            Type = InputEventType.KeyDown,
            VirtualKeyCode = 0x41,
            X = 100,
            Y = 200,
            RelX = 50,
            RelY = 60,
            MouseData = 0,
            WindowHandle = (nint)0x12345,
            WindowLeft = 10,
            WindowTop = 20,
            WindowWidth = 1920,
            WindowHeight = 1080
        };

        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(1);
            writer.Write(evt.TimeOffsetMs);
            writer.Write((int)evt.Type);
            writer.Write(evt.VirtualKeyCode);
            writer.Write(evt.X);
            writer.Write(evt.Y);
            writer.Write(evt.RelX);
            writer.Write(evt.RelY);
            writer.Write(evt.MouseData);
            writer.Write(evt.WindowHandle.ToInt64());
            writer.Write(evt.WindowLeft);
            writer.Write(evt.WindowTop);
            writer.Write(evt.WindowWidth);
            writer.Write(evt.WindowHeight);
        }

        ms.Position = 0;
        using var reader = new BinaryReader(ms);
        var count = reader.ReadInt32();
        Assert.Equal(1, count);

        var loaded = new InputEvent
        {
            TimeOffsetMs = reader.ReadInt32(),
            Type = (InputEventType)reader.ReadInt32(),
            VirtualKeyCode = reader.ReadInt32(),
            X = reader.ReadInt32(),
            Y = reader.ReadInt32(),
            RelX = reader.ReadInt32(),
            RelY = reader.ReadInt32(),
            MouseData = reader.ReadInt32(),
            WindowHandle = (nint)reader.ReadInt64(),
            WindowLeft = reader.ReadInt32(),
            WindowTop = reader.ReadInt32(),
            WindowWidth = reader.ReadInt32(),
            WindowHeight = reader.ReadInt32()
        };

        Assert.Equal(evt.TimeOffsetMs, loaded.TimeOffsetMs);
        Assert.Equal(evt.Type, loaded.Type);
        Assert.Equal(evt.VirtualKeyCode, loaded.VirtualKeyCode);
        Assert.Equal(evt.X, loaded.X);
        Assert.Equal(evt.Y, loaded.Y);
        Assert.Equal(evt.RelX, loaded.RelX);
        Assert.Equal(evt.RelY, loaded.RelY);
        Assert.Equal(evt.WindowHandle, loaded.WindowHandle);
        Assert.Equal(evt.WindowWidth, loaded.WindowWidth);
    }

    [Fact]
    public void RecordingSession_Empty_IsValid()
    {
        var session = new RecordingSession();
        Assert.Equal(0, session.EventCount);
        Assert.NotNull(session.Events);
    }

    [Fact]
    public async Task SaveAndLoad_Roundtrip()
    {
        var hook = new HookManager();
        var recorder = new Recorder(hook);
        var session = new RecordingSession
        {
            CreatedAt = DateTime.Now,
            DurationMs = 5000,
            TargetWindowWidth = 1920,
            TargetWindowHeight = 1080
        };
        session.Events.AddRange([
            new InputEvent { TimeOffsetMs = 0, Type = InputEventType.KeyDown, VirtualKeyCode = 0x41, X = 100, Y = 200 },
            new InputEvent { TimeOffsetMs = 100, Type = InputEventType.KeyUp, VirtualKeyCode = 0x41, X = 100, Y = 200 },
            new InputEvent { TimeOffsetMs = 200, Type = InputEventType.MouseMove, X = 300, Y = 400, RelX = 150, RelY = 200 }
        ]);

        var tempFile = Path.GetTempFileName();
        try
        {
            using (var stream = File.Create(tempFile))
            using (var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
            {
                var jsonEntry = archive.CreateEntry("session.json");
                using (var writer = new StreamWriter(jsonEntry.Open()))
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(session);
                    await writer.WriteAsync(json);
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
                        binWriter.Write(e.X);
                        binWriter.Write(e.Y);
                        binWriter.Write(e.RelX);
                        binWriter.Write(e.RelY);
                        binWriter.Write(e.MouseData);
                        binWriter.Write(e.WindowHandle.ToInt64());
                        binWriter.Write(e.WindowLeft);
                        binWriter.Write(e.WindowTop);
                        binWriter.Write(e.WindowWidth);
                        binWriter.Write(e.WindowHeight);
                    }
                }
            }

            var loaded = await recorder.LoadSessionAsync(tempFile);
            Assert.NotNull(loaded);
            Assert.Equal(3, loaded.EventCount);
            Assert.Equal(session.Events[0].TimeOffsetMs, loaded.Events[0].TimeOffsetMs);
            Assert.Equal(session.Events[0].Type, loaded.Events[0].Type);
            Assert.Equal(session.Events[1].VirtualKeyCode, loaded.Events[1].VirtualKeyCode);
            Assert.Equal(session.Events[2].X, loaded.Events[2].X);
            Assert.Equal(session.Events[2].RelY, loaded.Events[2].RelY);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            hook.Dispose();
            recorder.Dispose();
        }
    }
}
