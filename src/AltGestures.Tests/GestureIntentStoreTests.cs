using AltGestures.Core;
using AltGestures.Core.Commands;
using Xunit;

namespace AltGestures.Tests;

public sealed class GestureIntentStoreTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");

    [Fact]
    public void JsonStoreRoundTripsGestures()
    {
        var store = new JsonGestureIntentStore(path);
        var gesture = new Gesture();
        gesture.Add(Gesture.GestureDir.Right, Gesture.GestureDir.Down);
        store.GlobalApp.Add(new GestureIntent
        {
            Gesture = gesture,
            Command = new DoNothingCommand(),
            Name = "测试手势"
        });
        store.HotCornerCommands[0] = new DoNothingCommand();
        store.Save();

        using var stream = File.OpenRead(path);
        var restored = new JsonGestureIntentStore(stream, false);
        var intent = restored.GlobalApp.Find(gesture);
        Assert.NotNull(intent);
        Assert.Equal(gesture, intent!.Gesture);
        Assert.Equal("测试手势", intent.Name);
        Assert.IsType<DoNothingCommand>(restored.HotCornerCommands[0]);
    }

    public void Dispose()
    {
        File.Delete(path);
    }
}
