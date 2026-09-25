using AltGestures.Core;
using Xunit;

namespace AltGestures.Tests;

public sealed class GestureTests
{
    [Fact]
    public void EqualGesturesHaveEqualHashCodes()
    {
        var first = CreateGesture();
        var second = CreateGesture();
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ToStringUsesUpstreamMnemonicFormat()
    {
        Assert.Equal("◑→↓", CreateGesture().ToString());
    }

    private static Gesture CreateGesture()
    {
        var gesture = new Gesture();
        gesture.Add(Gesture.GestureDir.Right, Gesture.GestureDir.Down);
        return gesture;
    }
}
