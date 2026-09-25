// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using System.Drawing;
using System.Threading;
using AltGestures.Core.Commands;

namespace AltGestures.Core;

public class GestureParser : IDisposable
{
    private bool _isInCaptureMode;
    private SynchronizationContext? _syncContext;
    private readonly object @lock = new();
    private readonly Dictionary<Action<Gesture>, SynchronizationContext?> _gestureCapturedEventHandlerContexts = new();
    private Gesture _gesture = new();
    private Point _lastPoint;
    private int _pointCount;
    private GestureIntent? _effectiveIntent;
    private ExeApp? _currentApp;
    private bool _isPaused;
    private Point _lastVector;
    private Point _firstStrokeEndPoint;

    public delegate void GestureIntentEventHandler(GestureIntent intent);
    public delegate void IntentExecutedEventHandler(GestureIntent intent, GestureIntent.ExecutionResult result);

    public virtual bool IsInCaptureMode
    {
        get => _isInCaptureMode;
        set
        {
            lock (@lock)
            {
                if (_isInCaptureMode && value)
                {
                    throw new InvalidOperationException();
                }
                if (value)
                {
                    _syncContext = SynchronizationContext.Current;
                    if (_isPaused)
                    {
                        Resume();
                    }
                }
                _isInCaptureMode = value;
            }
        }
    }

    public bool EnableHotCorners { get; set; } = true;
    public bool Enable8DirGesture { get; set; }
    public bool EnableRubEdge { get; set; } = true;
    public int MaxGestureSteps { get; set; }
    public bool IsPaused => _isPaused;
    public IPathTracker PathTracker { get; }
    public IGestureIntentFinder IntentFinder { get; }

    public event GestureIntentEventHandler? IntentRecognized;
    public event IntentExecutedEventHandler? IntentExecuted;
    public event GestureIntentEventHandler? IntentReadyToExecute;
    public event Action<Gesture>? IntentInvalid;
    public event Action? IntentOrPathCanceled;
    public event Action<GestureModifier>? IntentReadyToExecuteOnModifier;
    public event Action<string, GestureIntent>? CommandReportStatus;
    public event Action<State>? StateChanged;

    public event Action<Gesture> GestureCaptured
    {
        add
        {
            lock (@lock)
            {
                if (_gestureCapturedEventHandlerContexts.ContainsKey(value))
                {
                    return;
                }
                _gestureCapturedEventHandlerContexts[value] = SynchronizationContext.Current;
            }
        }
        remove
        {
            lock (@lock)
            {
                _gestureCapturedEventHandlerContexts.Remove(value);
            }
        }
    }

    public GestureParser(IPathTracker pathTracker, IGestureIntentFinder intentFinder)
    {
        PathTracker = pathTracker;
        IntentFinder = intentFinder;
        MaxGestureSteps = 12;
        PathTracker.BeforePathStart += PathTrackerOnBeforePathStart;
        PathTracker.PathStart += PathTrackerOnPathStart;
        PathTracker.PathEnd += PathTrackerOnPathEnd;
        PathTracker.EffectivePathGrow += PathTrackerOnEffectivePathGrow;
        PathTracker.PathModifier += PathTrackerOnPathModifier;
        PathTracker.HotCornerTriggered += PathTracker_HotCornerTriggered;
        PathTracker.EdgeRubbed += PathTracker_EdgeRubbed;
    }

    public virtual void Start() => PathTracker.Start();

    public virtual void Pause()
    {
        PathTracker.Paused = true;
        _isPaused = true;
        OnStateChanged(State.PAUSED);
    }

    public virtual void Resume()
    {
        PathTracker.Paused = false;
        _isPaused = false;
        OnStateChanged(State.RUNNING);
    }

    public void TogglePause()
    {
        if (IsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public virtual void Stop()
    {
        PathTracker.Stop();
        OnStateChanged(State.STOPPED);
    }

    private bool Parse(PathEventArgs args)
    {
        var gestureChanged = false;
        if (_pointCount != 0 && args.Location != _lastPoint)
        {
            var vector = new Point(args.Location.X - _lastPoint.X, -args.Location.Y + _lastPoint.Y);
            Gesture.GestureDir dir;

            if (Enable8DirGesture)
            {
                var count = _gesture.Count();
                switch (count)
                {
                    case 0:
                        dir = Get8DirectionDir(vector);
                        _lastVector = vector;
                        _firstStrokeEndPoint = args.Location;
                        break;
                    case 1:
                        var last = _gesture.Last()!.Value;
                        if ((int)last % 2 == 0)
                        {
                            dir = Get4DirectionDir(vector);
                            break;
                        }

                        dir = Get8DirectionDir(vector);
                        if (dir != last)
                        {
                            if (args.Context != null && GetAngle(
                                    new Point(_firstStrokeEndPoint.X - args.Context.StartPoint.X,
                                        _firstStrokeEndPoint.Y - args.Context.StartPoint.X),
                                    new Point(args.Location.X - _firstStrokeEndPoint.X,
                                        args.Location.Y - _firstStrokeEndPoint.Y)) < 36f)
                            {
                                dir = last;
                                break;
                            }

                            dir = Get4DirectionDir(vector);
                            var lastDirShouldBe = Get4DirectionDir(_lastVector);
                            _gesture.Dirs[0] = lastDirShouldBe;
                            gestureChanged = true;
                        }
                        else
                        {
                            _firstStrokeEndPoint = args.Location;
                        }
                        break;
                    default:
                        dir = Get4DirectionDir(vector);
                        break;
                }
            }
            else
            {
                dir = Get4DirectionDir(vector);
            }

            if (dir != _gesture.Last())
            {
                _gesture.Add(dir);
                gestureChanged = true;
            }

            if (_gesture.Modifier != args.Modifier)
            {
                _gesture.Modifier = args.Modifier;
                gestureChanged = true;
            }
        }

        _lastPoint = args.Location;
        _pointCount++;
        return gestureChanged;
    }

    private void PathTrackerOnBeforePathStart(BeforePathStartEventArgs args)
    {
        if (IsInCaptureMode || args.PathEventArgs.Context == null)
        {
            return;
        }
        var shouldStart = IntentFinder.IsGesturingEnabledForContext(args.PathEventArgs.Context, out _currentApp);
        args.ShouldPathStart = shouldStart;
    }

    private void PathTrackerOnEffectivePathGrow(PathEventArgs args)
    {
        if (_gesture.Count() >= MaxGestureSteps)
        {
            return;
        }

        var gestureChanged = Parse(args);
        if (!gestureChanged)
        {
            return;
        }

        if (IsInCaptureMode)
        {
            return;
        }

        var lastEffectiveIntent = _effectiveIntent;
        _effectiveIntent = IntentFinder.Find(_gesture, _currentApp);
        if (_effectiveIntent != null)
        {
            IntentRecognized?.Invoke(_effectiveIntent);
        }
        else if (lastEffectiveIntent != null)
        {
            IntentInvalid?.Invoke(_gesture);
        }
    }

    private void PathTrackerOnPathEnd(PathEventArgs args)
    {
        if (IsInCaptureMode)
        {
            OnGestureCaptured(_gesture);
            return;
        }

        if (_effectiveIntent == null)
        {
            IntentOrPathCanceled?.Invoke();
            return;
        }

        if (_effectiveIntent.CanExecuteOnModifier())
        {
            if (_effectiveIntent.Command is IGestureModifiersAware modifierStateAwareCmd)
            {
                modifierStateAwareCmd.GestureEnded();
                modifierStateAwareCmd.ReportStatus -= OnCommandReportStatus;
            }
            OnIntentReadyToExecute(_effectiveIntent);
            IntentExecuted?.Invoke(_effectiveIntent, new GestureIntent.ExecutionResult(null, true));
            return;
        }

        OnIntentReadyToExecute(_effectiveIntent);
        var result = _effectiveIntent.Execute(args.Context ?? throw new InvalidOperationException("手势上下文为空。"), this);
        IntentExecuted?.Invoke(_effectiveIntent, result);
    }

    private void PathTrackerOnPathStart(PathEventArgs args)
    {
        _effectiveIntent = null;
        _pointCount = 1;
        _gesture = new Gesture(args.Context?.GestureButton ?? GestureTriggerButton.Right);
        _lastPoint = args.Location;
    }

    private void PathTrackerOnPathModifier(PathEventArgs args)
    {
        _gesture.Modifier = args.Modifier;
        if (IsInCaptureMode)
        {
            return;
        }

        if (_effectiveIntent != null && PathTracker.IsSuspended &&
            _effectiveIntent.Command is IGestureModifiersAware modifierStateAwareCommand)
        {
            modifierStateAwareCommand.ModifierTriggered(args.Modifier);
            return;
        }

        var lastEffectiveIntent = _effectiveIntent;
        _effectiveIntent = args.Context == null
            ? IntentFinder.Find(_gesture, _currentApp)
            : IntentFinder.Find(_gesture, args.Context);

        if (_effectiveIntent != null)
        {
            if (IntentRecognized != null && _effectiveIntent != lastEffectiveIntent)
            {
                IntentRecognized(_effectiveIntent);
            }

            if (!_effectiveIntent.CanExecuteOnModifier())
            {
                return;
            }

            OnIntentReadyToExecuteOnModifier(args.Modifier);
            if (_effectiveIntent.Command is IGestureModifiersAware stateAwareCommand)
            {
                if (stateAwareCommand is INeedInit shouldInit && !shouldInit.IsInitialized)
                {
                    shouldInit.Init();
                }
                stateAwareCommand.ReportStatus += OnCommandReportStatus;
                stateAwareCommand.GestureRecognized(out var observedModifiers);
                PathTracker.SuspendTemprarily(GestureModifier.All & ~observedModifiers);
            }
            else
            {
                PathTracker.SuspendTemprarily(GestureModifier.None);
                _effectiveIntent.Execute(args.Context ?? throw new InvalidOperationException("手势上下文为空。"), this);
            }
        }
        else if (lastEffectiveIntent != null)
        {
            IntentInvalid?.Invoke(_gesture);
        }
    }

    private void PathTracker_HotCornerTriggered(ScreenCorner corner)
    {
        if (!EnableHotCorners)
        {
            return;
        }
        var cmd = IntentFinder.IntentStore.HotCornerCommands[(int)corner];
        if (cmd is INeedInit shouldInit && !shouldInit.IsInitialized)
        {
            shouldInit.Init();
        }
        cmd?.Execute();
    }

    private void PathTracker_EdgeRubbed(ScreenEdge edge)
    {
        if (!EnableRubEdge)
        {
            return;
        }
        var cmd = IntentFinder.IntentStore.HotCornerCommands[4 + (int)edge];
        if (cmd is INeedInit shouldInit && !shouldInit.IsInitialized)
        {
            shouldInit.Init();
        }
        cmd?.Execute();
    }

    protected void OnGestureCaptured(Gesture gesture)
    {
        lock (@lock)
        {
            if (_gestureCapturedEventHandlerContexts.Count == 0)
            {
                return;
            }

            foreach (var kv in _gestureCapturedEventHandlerContexts)
            {
                var syncContext = kv.Value;
                var handler = kv.Key;
                if (syncContext == null)
                {
                    handler(gesture);
                }
                else
                {
                    syncContext.Post(_ => handler(gesture), null);
                }
            }
        }
    }

    protected void OnStateChanged(State state) => StateChanged?.Invoke(state);
    protected void OnIntentReadyToExecute(GestureIntent intent) => IntentReadyToExecute?.Invoke(intent);
    protected void OnIntentReadyToExecuteOnModifier(GestureModifier modifier) => IntentReadyToExecuteOnModifier?.Invoke(modifier);
    protected void OnCommandReportStatus(string status) => CommandReportStatus?.Invoke(status, _effectiveIntent!);

    private static Gesture.GestureDir Get4DirectionDir(Point vector)
    {
        var dir = Gesture.GestureDir.Up;
        if (vector.X >= 0 && vector.Y >= 0)
        {
            dir = vector.X > vector.Y ? Gesture.GestureDir.Right : Gesture.GestureDir.Up;
        }
        else if (vector.X <= 0 && vector.Y >= 0)
        {
            dir = -vector.X > vector.Y ? Gesture.GestureDir.Left : Gesture.GestureDir.Up;
        }
        else if (vector.X <= 0 && vector.Y <= 0)
        {
            dir = -vector.X > -vector.Y ? Gesture.GestureDir.Left : Gesture.GestureDir.Down;
        }
        else if (vector.X >= 0 && vector.Y <= 0)
        {
            dir = vector.X > -vector.Y ? Gesture.GestureDir.Right : Gesture.GestureDir.Down;
        }
        return dir;
    }

    private static Gesture.GestureDir Get8DirectionDir(Point vector)
    {
        var angle = GetAngle(new Point(0, 1), vector);
        if (vector.X < 0)
        {
            angle = 360 - angle;
        }

        var mod = angle % 90;
        var n = (int)Math.Floor(angle / 45);
        var nIsEven = (n & 1) == 0;
        const float slashRange = 50;

        if (nIsEven && mod > 45 - slashRange / 2 || !nIsEven && mod > 45 + slashRange / 2)
        {
            n++;
            if (n == 8)
            {
                n = 0;
            }
        }
        return (Gesture.GestureDir)n;
    }

    private static float GetAngle(Point vectorA, Point vectorB)
    {
        float vaX = vectorA.X;
        float vaY = vectorA.Y;
        float vbX = vectorB.X;
        float vbY = vectorB.Y;
        float productValue = (vaX * vbX) + (vaY * vbY);
        float vaVal = (float)Math.Sqrt(vaX * vaX + vaY * vaY);
        float vbVal = (float)Math.Sqrt(vbX * vbX + vbY * vbY);
        float cosValue = productValue / (vaVal * vbVal);
        if (cosValue < -1 && cosValue > -2)
        {
            cosValue = -1;
        }
        else if (cosValue > 1 && cosValue < 2)
        {
            cosValue = 1;
        }
        return (float)(Math.Acos(cosValue) * 180 / Math.PI);
    }

    public enum State
    {
        RUNNING, PAUSED, STOPPED
    }

    public void Dispose()
    {
        PathTracker.BeforePathStart -= PathTrackerOnBeforePathStart;
        PathTracker.PathStart -= PathTrackerOnPathStart;
        PathTracker.PathEnd -= PathTrackerOnPathEnd;
        PathTracker.EffectivePathGrow -= PathTrackerOnEffectivePathGrow;
        PathTracker.PathModifier -= PathTrackerOnPathModifier;
        PathTracker.HotCornerTriggered -= PathTracker_HotCornerTriggered;
        PathTracker.EdgeRubbed -= PathTracker_EdgeRubbed;
    }
}
