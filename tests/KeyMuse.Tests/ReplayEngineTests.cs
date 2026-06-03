using Xunit;
using KeyMuse.Core.Models;
using KeyMuse.Core.Services;

namespace KeyMuse.Tests;

public class ReplayEngineTests
{
    [Fact]
    public async Task Play_EmptySession_CompletesImmediately()
    {
        var coordinator = new InputCoordinator();
        var engine = new ReplayEngine(coordinator);
        var session = new RecordingSession();

        var completed = false;
        engine.OnCompleted += () => completed = true;

        await engine.PlayAsync(session, LoopMode.Single);

        Assert.True(completed);
        Assert.Equal(0, engine.TotalEvents);
    }

    [Fact]
    public async Task Play_SingleLoop_InvokesCompleted()
    {
        var coordinator = new InputCoordinator();
        var engine = new ReplayEngine(coordinator);
        var session = new RecordingSession();
        session.Events.Add(new InputEvent
        {
            TimeOffsetMs = 0,
            Type = InputEventType.KeyDown,
            VirtualKeyCode = 0x41
        });

        var completed = false;
        engine.OnCompleted += () => completed = true;

        await engine.PlayAsync(session, LoopMode.Single);

        Assert.True(completed);
        Assert.Equal(1, engine.CurrentLoop);
    }

    [Fact]
    public async Task Play_MultipleLoops_CompletesCorrectCount()
    {
        var coordinator = new InputCoordinator();
        var engine = new ReplayEngine(coordinator);
        var session = new RecordingSession();
        session.Events.Add(new InputEvent
        {
            TimeOffsetMs = 0,
            Type = InputEventType.KeyDown,
            VirtualKeyCode = 0x41
        });

        await engine.PlayAsync(session, LoopMode.Count, loopCount: 3, loopIntervalMs: 10);

        Assert.Equal(3, engine.CurrentLoop);
    }

    [Fact]
    public async Task Stop_InterruptsPlayback()
    {
        var coordinator = new InputCoordinator();
        var engine = new ReplayEngine(coordinator);
        var session = new RecordingSession();
        for (int i = 0; i < 10; i++)
        {
            session.Events.Add(new InputEvent
            {
                TimeOffsetMs = i * 50,
                Type = InputEventType.KeyDown,
                VirtualKeyCode = 0x41
            });
        }

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);
        await engine.PlayAsync(session, LoopMode.Infinite, loopIntervalMs: 10, externalToken: cts.Token);

        Assert.True(engine.CurrentLoop > 0 || !engine.IsPlaying);
        Assert.False(engine.IsPlaying);
    }
}
