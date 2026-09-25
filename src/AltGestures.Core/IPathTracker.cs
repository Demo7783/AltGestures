// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using System.Drawing;

namespace AltGestures.Core;

public class PathEventArgs : EventArgs
{
    public Point Location { get; set; }
    public GestureTriggerButton Button;
    public GestureModifier Modifier { get; set; }
    public GestureContext? Context { get; set; }

    public PathEventArgs(Point location, GestureContext context)
    {
        Location = location;
        Context = context;
    }

    public PathEventArgs()
    {
    }
}

public class BeforePathStartEventArgs
{
    public PathEventArgs PathEventArgs { get; }
    public bool ShouldPathStart { get; set; }
    public GestureContext? Context { get; }

    public BeforePathStartEventArgs(PathEventArgs pathEventArgs)
    {
        PathEventArgs = pathEventArgs;
        Context = pathEventArgs.Context;
        ShouldPathStart = true;
    }
}

public delegate void PathTrackEventHandler(PathEventArgs args);
public delegate void BeforePathStartEventHandler(BeforePathStartEventArgs args);

public interface IPathTracker : IDisposable
{
    bool Paused { get; set; }
    void Start();
    void Stop();
    void SuspendTemprarily(GestureModifier filteredModifiers);
    bool IsSuspended { get; }
    event BeforePathStartEventHandler BeforePathStart;
    event PathTrackEventHandler PathStart;
    event PathTrackEventHandler PathGrow;
    event PathTrackEventHandler EffectivePathGrow;
    event PathTrackEventHandler PathEnd;
    event PathTrackEventHandler PathTimeout;
    event PathTrackEventHandler PathModifier;
    event Action<ScreenCorner> HotCornerTriggered;
    event Action<ScreenEdge> EdgeRubbed;
}
