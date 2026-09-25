using System.Drawing;
using AltGestures.Core;
using AltGestures.Core.Persistence;
using Xunit;

namespace AltGestures.Tests;

public sealed class GestureParserTests : IDisposable
{
    private readonly TestPathTracker tracker = new();
    private readonly TestIntentFinder intentFinder;
    private readonly GestureParser parser;
    private Gesture? captured;

    public GestureParserTests()
    {
        intentFinder = new TestIntentFinder(new JsonGestureIntentStore(GetTemporaryStorePath()));
        parser = new GestureParser(tracker, intentFinder);
        parser.GestureCaptured += gesture => captured = gesture;
        parser.IsInCaptureMode = true;
    }

    [Fact]
    public void RecognizesSingleRightDirection()
    {
        Capture((100, 100), (130, 100), (160, 100), (190, 100));
        Assert.NotNull(captured);
        Assert.Equal(new[] { Gesture.GestureDir.Right }, captured!.Dirs);
    }

    [Fact]
    public void RecognizesRightThenDownDirection()
    {
        Capture((100, 100), (140, 100), (180, 100), (180, 140), (180, 180));
        Assert.NotNull(captured);
        Assert.Equal(new[] { Gesture.GestureDir.Right, Gesture.GestureDir.Down }, captured!.Dirs);
    }

    [Fact]
    public void SmallNoiseDoesNotChangeRecognition()
    {
        Capture((100, 100), (110, 99), (120, 101), (130, 100), (140, 102), (180, 100));
        Assert.NotNull(captured);
        Assert.Equal(new[] { Gesture.GestureDir.Right }, captured!.Dirs);
    }

    [Fact]
    public void PointsAfterMaxGestureStepsAreIgnored()
    {
        parser.MaxGestureSteps = 2;
        Capture((100, 100), (140, 100), (140, 140), (100, 140), (60, 140));
        Assert.NotNull(captured);
        Assert.Equal(new[] { Gesture.GestureDir.Right, Gesture.GestureDir.Down }, captured!.Dirs);
    }

    [Fact]
    public void ParsingSameTrajectoryTwiceProducesEqualGestures()
    {
        var first = CaptureTrajectory();
        var second = CaptureTrajectory();
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(1, -1, Gesture.GestureDir.RightUp)]
    [InlineData(1, 1, Gesture.GestureDir.RightDown)]
    [InlineData(-1, 1, Gesture.GestureDir.LeftDown)]
    [InlineData(-1, -1, Gesture.GestureDir.LeftUp)]
    public void RecognizesDiagonalDirectionsInEightDirectionMode(int x, int y, Gesture.GestureDir expected)
    {
        parser.Enable8DirGesture = true;
        Capture((100, 100), (100 + 30 * x, 100 + 30 * y), (100 + 60 * x, 100 + 60 * y));
        Assert.NotNull(captured);
        Assert.Equal(new[] { expected }, captured!.Dirs);
    }

    private Gesture CaptureTrajectory()
    {
        Capture((100, 100), (140, 100), (180, 100), (180, 140), (180, 180));
        Assert.NotNull(captured);
        return captured!;
    }

    private static string GetTemporaryStorePath() =>
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");

    private void Capture(params (int X, int Y)[] points)
    {
        tracker.StartPath(new TestGestureContext(), new Point(points[0].X, points[0].Y));
        foreach (var point in points[1..])
        {
            tracker.Grow(new Point(point.X, point.Y));
        }
        tracker.End(new Point(points[^1].X, points[^1].Y));
        parser.IsInCaptureMode = false;
        parser.IsInCaptureMode = true;
    }

    public void Dispose() => parser.Dispose();

    private sealed class TestIntentFinder : AbstractGestureIntentFinder
    {
        public TestIntentFinder(IGestureIntentStore store) : base(store)
        {
        }

        public override ExeApp? GetExeAppByContext(GestureContext context) => null;
    }
}
