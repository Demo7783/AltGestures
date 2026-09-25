using System.Drawing;
using AltGestures.Core;

namespace AltGestures.Tests;

public sealed class TestPathTracker : IPathTracker
{
    public bool Paused { get; set; }
    public bool IsSuspended { get; private set; }

    public event BeforePathStartEventHandler? BeforePathStart;
    public event PathTrackEventHandler? PathStart;
    public event PathTrackEventHandler? PathGrow;
    public event PathTrackEventHandler? EffectivePathGrow;
    public event PathTrackEventHandler? PathEnd;
    public event PathTrackEventHandler? PathTimeout;
    public event PathTrackEventHandler? PathModifier;
    public event Action<ScreenCorner>? HotCornerTriggered;
    public event Action<ScreenEdge>? EdgeRubbed;

    public void Start()
    {
    }

    public void Stop()
    {
    }

    public void SuspendTemprarily(GestureModifier filteredModifiers) => IsSuspended = true;
    public void ResumeSuspension() => IsSuspended = false;
    public void Dispose()
    {
    }

    public void StartPath(TestGestureContext context, Point startPoint)
    {
        var eventArgs = new PathEventArgs(startPoint, context);
        var beforeArgs = new BeforePathStartEventArgs(eventArgs);
        BeforePathStart?.Invoke(beforeArgs);
        if (beforeArgs.ShouldPathStart)
        {
            PathStart?.Invoke(eventArgs);
        }
    }

    public void Grow(Point point, GestureModifier modifier = GestureModifier.None)
    {
        var eventArgs = new PathEventArgs(point, new TestGestureContext()) { Modifier = modifier };
        PathGrow?.Invoke(eventArgs);
        EffectivePathGrow?.Invoke(eventArgs);
    }

    public void End(Point point) => PathEnd?.Invoke(new PathEventArgs(point, new TestGestureContext()));
    public void Timeout(Point point) => PathTimeout?.Invoke(new PathEventArgs(point, new TestGestureContext()));
    public void Modifier(GestureModifier modifier) =>
        PathModifier?.Invoke(new PathEventArgs(new Point(-1, -1), new TestGestureContext()) { Modifier = modifier });
    public void HotCorner(ScreenCorner corner) => HotCornerTriggered?.Invoke(corner);
    public void Edge(ScreenEdge edge) => EdgeRubbed?.Invoke(edge);
}

public sealed class TestGestureContext : GestureContext
{
    public override void ActivateTargetWindow()
    {
    }
}
