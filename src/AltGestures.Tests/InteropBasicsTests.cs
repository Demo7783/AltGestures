using System.Runtime.CompilerServices;
using AltGestures.Core.Input;
using AltGestures.Core.Interop;
using Xunit;

namespace AltGestures.Tests;

public sealed class InteropBasicsTests
{
    [Fact]
    public void InputStructuresHaveRequiredLayout()
    {
        Assert.Equal(40, Unsafe.SizeOf<INPUT>());
        Assert.Equal(32, Unsafe.SizeOf<MOUSEINPUT>());
        Assert.Equal(24, Unsafe.SizeOf<KEYBDINPUT>());
    }

    [Fact]
    public void GeometryStructuresHaveRequiredLayout()
    {
        Assert.Equal(8, Unsafe.SizeOf<POINT>());
        Assert.Equal(16, Unsafe.SizeOf<RECT>());
        Assert.Equal(40, Unsafe.SizeOf<MONITORINFO>());
    }

    [Fact]
    public void MouseAndKeyboardVirtualKeysHaveCanonicalValues()
    {
        Assert.Equal(0x01, (int)VirtualKeyCode.VK_LBUTTON);
        Assert.Equal(0x02, (int)VirtualKeyCode.VK_RBUTTON);
        Assert.Equal(0x04, (int)VirtualKeyCode.VK_MBUTTON);
        Assert.Equal(0x05, (int)VirtualKeyCode.VK_XBUTTON1);
        Assert.Equal(0x06, (int)VirtualKeyCode.VK_XBUTTON2);
    }

    [Fact]
    public void MenuVirtualKeysHaveCanonicalValues()
    {
        Assert.Equal(0xA4, (int)VirtualKeyCode.VK_LMENU);
        Assert.Equal(0xA5, (int)VirtualKeyCode.VK_RMENU);
    }

    [Fact]
    public void HookAndMessageConstantsHaveCanonicalValues()
    {
        Assert.Equal(14, NativeConstants.WH_MOUSE_LL);
        Assert.Equal(13, NativeConstants.WH_KEYBOARD_LL);
        Assert.Equal(0x0200, NativeConstants.WM_MOUSEMOVE);
        Assert.Equal(0x020B, NativeConstants.WM_XBUTTONDOWN);
    }

    [Fact]
    public void ModifierStateParsesIndividualModifiers()
    {
        var states = new Dictionary<VirtualKeyCode, bool>
        {
            [VirtualKeyCode.VK_LSHIFT] = true,
            [VirtualKeyCode.VK_RCONTROL] = true,
            [VirtualKeyCode.VK_LMENU] = true,
            [VirtualKeyCode.VK_RWIN] = true
        };

        Assert.Equal(
            ModifierModifiers.LeftShift | ModifierModifiers.RightControl |
            ModifierModifiers.LeftAlt | ModifierModifiers.RightWindows,
            ModifierState.FromKeyStates(states));
    }

    [Fact]
    public void ModifierStateParsesRightAltOnly()
    {
        var states = new Dictionary<VirtualKeyCode, bool>
        {
            [VirtualKeyCode.VK_LMENU] = false,
            [VirtualKeyCode.VK_RMENU] = true
        };

        Assert.Equal(ModifierModifiers.RightAlt, ModifierState.FromKeyStates(states));
    }

    [Fact]
    public void ModifierStateIgnoresAbsentAndReleasedKeys()
    {
        var states = new Dictionary<VirtualKeyCode, bool>
        {
            [VirtualKeyCode.VK_LCONTROL] = false,
            [VirtualKeyCode.VK_LWIN] = true
        };

        Assert.Equal(ModifierModifiers.LeftWindows, ModifierState.FromKeyStates(states));
    }

    [Fact]
    public void ModifierStateQueriesInjectedKeyboardState()
    {
        var state = new ModifierState(key => key == VirtualKeyCode.VK_RSHIFT);

        Assert.True(state.IsShiftDown);
        Assert.False(state.IsControlDown);
        Assert.Equal(ModifierModifiers.RightShift, state.Current);
    }

    [Fact]
    public void PressBuildsModifiersFirstAndReleasesInReverseOrder()
    {
        var sender = new CapturingInputSender();
        var simulator = new InputSimulator(sender.Invoke);

        Assert.True(simulator.Press(
            VirtualKeyCode.VK_LCONTROL,
            VirtualKeyCode.VK_LMENU,
            VirtualKeyCode.VK_DELETE));
        Assert.Equal(6, sender.Inputs.Count);
        Assert.Equal(
            [
                VirtualKeyCode.VK_LCONTROL,
                VirtualKeyCode.VK_LMENU,
                VirtualKeyCode.VK_DELETE,
                VirtualKeyCode.VK_DELETE,
                VirtualKeyCode.VK_LMENU,
                VirtualKeyCode.VK_LCONTROL
            ],
            sender.Inputs.Select(input => (VirtualKeyCode)input.Union.Keyboard.VirtualKey));
        Assert.All(
            sender.Inputs.Take(3),
            input => Assert.Equal(0u, input.Union.Keyboard.Flags));
        Assert.All(
            sender.Inputs.Skip(3),
            input => Assert.Equal(NativeConstants.KEYEVENTF_KEYUP, input.Union.Keyboard.Flags));
    }

    [Fact]
    public void TypeTextUsesUnicodeInputPairs()
    {
        var sender = new CapturingInputSender();
        var simulator = new InputSimulator(sender.Invoke);
        var text = "A";

        Assert.True(simulator.TypeText(text));
        Assert.Equal(2, sender.Inputs.Count);
        Assert.All(sender.Inputs, input =>
        {
            Assert.Equal(NativeConstants.INPUT_KEYBOARD, input.Type);
            Assert.Equal(0, input.Union.Keyboard.VirtualKey);
            Assert.Equal('A', (char)input.Union.Keyboard.ScanCode);
        });
        Assert.Equal(NativeConstants.KEYEVENTF_UNICODE, sender.Inputs[0].Union.Keyboard.Flags);
        Assert.Equal(
            NativeConstants.KEYEVENTF_UNICODE | NativeConstants.KEYEVENTF_KEYUP,
            sender.Inputs[1].Union.Keyboard.Flags);
    }

    [Fact]
    public void TypeTextPreservesUnicodeCodeUnits()
    {
        var sender = new CapturingInputSender();
        var simulator = new InputSimulator(sender.Invoke);
        var text = "😀";

        Assert.True(simulator.TypeText(text));
        Assert.Equal(4, sender.Inputs.Count);
        Assert.Equal(0xD83D, sender.Inputs[0].Union.Keyboard.ScanCode);
        Assert.Equal(0xDE00, sender.Inputs[2].Union.Keyboard.ScanCode);
    }

    [Fact]
    public void MouseClickBuildsDownThenUp()
    {
        var sender = new CapturingInputSender();
        var simulator = new InputSimulator(sender.Invoke);

        Assert.True(simulator.RightClick());
        Assert.Equal(2, sender.Inputs.Count);
        Assert.All(sender.Inputs, input => Assert.Equal(NativeConstants.INPUT_MOUSE, input.Type));
        Assert.Equal(NativeConstants.MOUSEEVENTF_RIGHTDOWN, sender.Inputs[0].Union.Mouse.Flags);
        Assert.Equal(NativeConstants.MOUSEEVENTF_RIGHTUP, sender.Inputs[1].Union.Mouse.Flags);
    }

    [Fact]
    public void WheelUsesConfiguredDelta()
    {
        var sender = new CapturingInputSender();
        var simulator = new InputSimulator(sender.Invoke);

        Assert.True(simulator.Wheel(-120));
        Assert.Single(sender.Inputs);
        Assert.Equal(NativeConstants.MOUSEEVENTF_WHEEL, sender.Inputs[0].Union.Mouse.Flags);
        Assert.Equal(unchecked((uint)-120), sender.Inputs[0].Union.Mouse.MouseData);
    }

    [Fact]
    public void XButtonsUseNativeButtonData()
    {
        var sender = new CapturingInputSender();
        var simulator = new InputSimulator(sender.Invoke);

        Assert.True(simulator.XButtonDown(VirtualKeyCode.VK_XBUTTON1));
        Assert.True(simulator.XButtonUp(VirtualKeyCode.VK_XBUTTON2));
        Assert.Equal(NativeConstants.MOUSEEVENTF_XDOWN, sender.Inputs[0].Union.Mouse.Flags);
        Assert.Equal(1u, sender.Inputs[0].Union.Mouse.MouseData);
        Assert.Equal(NativeConstants.MOUSEEVENTF_XUP, sender.Inputs[1].Union.Mouse.Flags);
        Assert.Equal(2u, sender.Inputs[1].Union.Mouse.MouseData);
    }

    [Fact]
    public void MoveToUsesAbsoluteMouseCoordinates()
    {
        var sender = new CapturingInputSender();
        var simulator = new InputSimulator(sender.Invoke);

        Assert.True(simulator.MoveTo(320, 240));
        var mouse = Assert.Single(sender.Inputs).Union.Mouse;
        Assert.Equal(NativeConstants.MOUSEEVENTF_MOVE, mouse.Flags);
        Assert.Equal(320, mouse.Dx);
        Assert.Equal(240, mouse.Dy);
    }

    [Fact]
    public void SendReportsPartialDispatchAsFailure()
    {
        var simulator = new InputSimulator(_ => 0);

        Assert.False(simulator.Press(VirtualKeyCode.VK_RETURN));
    }

    private sealed class CapturingInputSender
    {
        public List<INPUT> Inputs { get; } = [];

        public uint Invoke(INPUT[] inputs)
        {
            Inputs.AddRange(inputs);
            return (uint)inputs.Length;
        }
    }
}
