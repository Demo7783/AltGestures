using AltGestures.Core.Arbitration;
using AltGestures.Core.Configuration;
using AltGestures.Core.Input;
using AltGestures.Core.Windowing;
using AltGestures.Core.Windows;
using Xunit;

namespace AltGestures.Tests;

public sealed class Phase2B2Tests
{
    [Fact]
    public void HotKeyParsesModifiersAndKey()
    {
        var succeeded = HotKey.TryParse("Ctrl+Alt+P", out var hotKey, out var error);

        Assert.True(succeeded, error);
        Assert.Equal(HotKeyModifiers.Control | HotKeyModifiers.Alt, hotKey.Modifiers);
        Assert.Equal((VirtualKeyCode)'P', hotKey.Key);
    }

    [Fact]
    public void HotKeyParsingIsCaseInsensitive()
    {
        var succeeded = HotKey.TryParse("ctrl+shift+f1", out var hotKey, out var error);

        Assert.True(succeeded, error);
        Assert.Equal(HotKeyModifiers.Control | HotKeyModifiers.Shift, hotKey.Modifiers);
        Assert.Equal(VirtualKeyCode.VK_F1, hotKey.Key);
    }

    [Fact]
    public void HotKeyParsesBareKey()
    {
        var succeeded = HotKey.TryParse("P", out var hotKey, out var error);

        Assert.True(succeeded, error);
        Assert.Equal(HotKeyModifiers.None, hotKey.Modifiers);
        Assert.Equal((VirtualKeyCode)'P', hotKey.Key);
    }

    [Theory]
    [InlineData("Ctrl+")]
    [InlineData("Foo+Bar")]
    [InlineData("")]
    [InlineData("+")]
    [InlineData(" ")]
    public void HotKeyRejectsInvalidTextWithReadableError(string text)
    {
        var succeeded = HotKey.TryParse(text, out _, out var error);

        Assert.False(succeeded);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void HotKeyToStringRoundTrips()
    {
        var succeeded = HotKey.TryParse("alt+win+shift+ctrl+VK_RETURN", out var hotKey, out var error);

        Assert.True(succeeded, error);
        Assert.Equal("Ctrl+Alt+Shift+Win+VK_RETURN", hotKey.ToString());
        Assert.True(HotKey.TryParse(hotKey.ToString(), out var parsed, out _));
        Assert.Equal(hotKey, parsed);
    }

    [Fact]
    public void AltTriggerExpandsToBothAltKeys()
    {
        var succeeded = HotKey.TryParseTriggerKeys(["Alt"], out var triggerKeys, out var error);

        Assert.True(succeeded, error);
        Assert.Equal(
            ModifierModifiers.LeftAlt | ModifierModifiers.RightAlt,
            HotKey.ToPhysicalModifiers(triggerKeys));
    }

    [Fact]
    public void CtrlTriggerExpandsToBothCtrlKeys()
    {
        var succeeded = HotKey.TryParseTriggerKeys(["Ctrl"], out var triggerKeys, out var error);

        Assert.True(succeeded, error);
        Assert.Equal(
            ModifierModifiers.LeftControl | ModifierModifiers.RightControl,
            HotKey.ToPhysicalModifiers(triggerKeys));
    }

    [Fact]
    public void MultipleTriggersContainAllSupportedSides()
    {
        var succeeded = HotKey.TryParseTriggerKeys(["Alt", "Ctrl", "Shift", "Win"], out var triggerKeys, out var error);

        Assert.True(succeeded, error);
        Assert.Equal(
            ModifierModifiers.LeftAlt | ModifierModifiers.RightAlt
            | ModifierModifiers.LeftControl | ModifierModifiers.RightControl
            | ModifierModifiers.LeftShift | ModifierModifiers.RightShift
            | ModifierModifiers.LeftWindows | ModifierModifiers.RightWindows,
            HotKey.ToPhysicalModifiers(triggerKeys));
    }

    [Theory]
    [InlineData("Command")]
    [InlineData("")]
    public void TriggerKeyParsingRejectsInvalidNames(string name)
    {
        var succeeded = HotKey.TryParseTriggerKeys([name], out _, out var error);

        Assert.False(succeeded);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void AppConfigRoundTripsTriggerAndHotKeySettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"altgestures-{Guid.NewGuid():N}.json");
        try
        {
            var config = new AppConfig
            {
                TriggerKeys = ["Ctrl", "Shift"],
                PauseResumeHotKey = "Win+F1"
            };
            new ConfigStore(path).Save(config);
            var loaded = new ConfigStore(path).Load();

            Assert.Equal(config.TriggerKeys, loaded.TriggerKeys);
            Assert.Equal(config.PauseResumeHotKey, loaded.PauseResumeHotKey);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingAppConfigUsesDefaultTriggerAndPauseHotKey()
    {
        var path = Path.Combine(Path.GetTempPath(), $"altgestures-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{}");
            var loaded = new ConfigStore(path).Load();

            Assert.Equal(["Alt"], loaded.TriggerKeys);
            Assert.Equal("Ctrl+Alt+P", loaded.PauseResumeHotKey);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GlobalHotKeyManagerRepeatedDisposeDoesNotThrow()
    {
        using var manager = new GlobalHotKeyManager();
        manager.Dispose();
        manager.Dispose();
    }

    [Fact]
    public void ConfiguredCtrlTriggerActivatesWindowBranch()
    {
        using var hook = new FakeMouseKeyboardHook();
        var branch = new FakeWindowBranch();
        using var arbiter = CreateArbiter(hook, branch, HotKeyModifiers.Control, ModifierModifiers.LeftControl);

        var args = Mouse(MouseMsg.WMLButtonDown);
        hook.RaiseMouse(args);

        Assert.True(args.Handled);
        Assert.Single(branch.Presses);
        Assert.Equal(InputOwnership.Window, arbiter.Ownership);
    }

    [Fact]
    public void UnconfiguredAltDoesNotStealGestureBranch()
    {
        using var hook = new FakeMouseKeyboardHook();
        var branch = new FakeWindowBranch();
        using var arbiter = CreateArbiter(hook, branch, HotKeyModifiers.Control, ModifierModifiers.LeftAlt);
        var forwarded = new List<MouseHookEventArgs>();
        arbiter.MouseHookEvent += args => forwarded.Add(args);

        var args = Mouse(MouseMsg.WMRButtonDown);
        hook.RaiseMouse(args);

        Assert.False(args.Handled);
        Assert.Same(args, Assert.Single(forwarded));
        Assert.Empty(branch.Presses);
        Assert.Equal(InputOwnership.Gesture, arbiter.Ownership);
    }

    [Fact]
    public void ConfiguredTriggerOwnershipLocksUntilMouseRelease()
    {
        using var hook = new FakeMouseKeyboardHook();
        var branch = new FakeWindowBranch();
        var triggerDown = true;
        using var arbiter = CreateArbiter(
            hook,
            branch,
            HotKeyModifiers.Shift,
            () => triggerDown ? ModifierModifiers.RightShift : ModifierModifiers.None);
        arbiter.MouseHookEvent += _ => Assert.Fail("窗口拖拽中的事件不应转给手势分支。");

        var down = Mouse(MouseMsg.WMLButtonDown);
        hook.RaiseMouse(down);
        triggerDown = false;
        hook.RaiseKeyboard(KeyboardEventType.KeyUp, VirtualKeyCode.VK_RSHIFT);
        var move = Mouse(MouseMsg.WMMouseMove, 40, 40);
        hook.RaiseMouse(move);
        var up = Mouse(MouseMsg.WMLButtonUp, 42, 42);
        hook.RaiseMouse(up);

        Assert.True(down.Handled);
        Assert.True(move.Handled);
        Assert.True(up.Handled);
        Assert.Single(branch.Presses);
        Assert.Single(branch.Moves);
        Assert.Equal(InputOwnership.None, arbiter.Ownership);
    }

    private static InputArbiter CreateArbiter(
        FakeMouseKeyboardHook hook,
        FakeWindowBranch branch,
        HotKeyModifiers triggerKeys,
        ModifierModifiers modifiers) => CreateArbiter(
            hook,
            branch,
            triggerKeys,
            () => modifiers);

    private static InputArbiter CreateArbiter(
        FakeMouseKeyboardHook hook,
        FakeWindowBranch branch,
        HotKeyModifiers triggerKeys,
        Func<ModifierModifiers> modifiers) => new(
            hook,
            branch,
            WindowActionBinding.Default,
            triggerKeys,
            modifiers,
            () => 0,
            () => 500,
            dispatchSynchronously: true);

    private static MouseHookEventArgs Mouse(
        MouseMsg message,
        int x = 10,
        int y = 10) => new(message, x, y, 0, UIntPtr.Zero);

    private sealed class FakeMouseKeyboardHook : IMouseKeyboardHook
    {
        public event Action<MouseHookEventArgs>? MouseHookEvent;

        public event Action<KeyboardHookEventArgs>? KeyboardHookEvent;

        public bool IsInstalled { get; private set; }

        public void Install() => IsInstalled = true;

        public void Uninstall() => IsInstalled = false;

        public void Dispose() => Uninstall();

        public void RaiseMouse(MouseMsg message, int x = 10, int y = 10) =>
            MouseHookEvent?.Invoke(new MouseHookEventArgs(message, x, y, 0, UIntPtr.Zero));

        public void RaiseMouse(MouseHookEventArgs args) => MouseHookEvent?.Invoke(args);

        public void RaiseKeyboard(KeyboardEventType eventType, VirtualKeyCode key) =>
            KeyboardHookEvent?.Invoke(new KeyboardHookEventArgs(eventType, key, UIntPtr.Zero));
    }

    private sealed class FakeWindowBranch : IWindowBranch
    {
        public List<(WindowAction Action, WindowMouseButton Button, int X, int Y, bool IsDoubleClick)> Presses { get; } = [];

        public List<(int X, int Y)> Moves { get; } = [];

        public void OnMouseDown(
            WindowAction action,
            WindowMouseButton button,
            int x,
            int y,
            WindowDragModifiers modifiers,
            bool isDoubleClick) => Presses.Add((action, button, x, y, isDoubleClick));

        public void OnMouseMove(int x, int y, WindowDragModifiers modifiers) => Moves.Add((x, y));

        public void OnMouseUp(WindowMouseButton button, int x, int y)
        {
        }

        public void OnMouseWheel(int delta, int x, int y, WindowDragModifiers modifiers)
        {
        }
    }
}
