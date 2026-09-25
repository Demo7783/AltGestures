using AltGestures.Core.Interop;

namespace AltGestures.Core.Input;

public sealed class InputSimulator
{
    private readonly Func<INPUT[], uint> sendInput;

    public nint ExtraInfo { get; set; }

    public InputSimulator()
        : this(inputs => User32.SendInput(
            (uint)inputs.Length,
            inputs,
            System.Runtime.InteropServices.Marshal.SizeOf<INPUT>()))
    {
    }

    public InputSimulator(Func<INPUT[], uint> sendInput)
    {
        this.sendInput = sendInput ?? throw new ArgumentNullException(nameof(sendInput));
    }

    public bool KeyDown(VirtualKeyCode virtualKey) => Send([CreateKeyboardInput(virtualKey)]);

    public bool KeyUp(VirtualKeyCode virtualKey) =>
        Send([CreateKeyboardInput(virtualKey, NativeConstants.KEYEVENTF_KEYUP)]);

    public bool Press(params VirtualKeyCode[] virtualKeys)
    {
        ArgumentNullException.ThrowIfNull(virtualKeys);
        if (virtualKeys.Length == 0)
        {
            throw new ArgumentException("至少需要一个虚拟键。", nameof(virtualKeys));
        }

        var inputs = new INPUT[virtualKeys.Length * 2];
        for (var index = 0; index < virtualKeys.Length; index++)
        {
            inputs[index] = CreateKeyboardInput(virtualKeys[index]);
        }

        for (var index = virtualKeys.Length - 1; index >= 0; index--)
        {
            inputs[inputs.Length - 1 - index] = CreateKeyboardInput(
                virtualKeys[index],
                NativeConstants.KEYEVENTF_KEYUP);
        }

        return Send(inputs);
    }

    public bool TypeText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return true;
        }

        var inputs = new INPUT[text.Length * 2];
        for (var index = 0; index < text.Length; index++)
        {
            inputs[index * 2] = CreateUnicodeInput(text[index]);
            inputs[index * 2 + 1] = CreateUnicodeInput(
                text[index],
                NativeConstants.KEYEVENTF_UNICODE | NativeConstants.KEYEVENTF_KEYUP);
        }

        return Send(inputs);
    }

    public bool MoveTo(int x, int y) =>
        Send([CreateMouseInput(NativeConstants.MOUSEEVENTF_MOVE, 0, x, y)]);

    public bool LeftButtonDown() => Send([CreateMouseInput(NativeConstants.MOUSEEVENTF_LEFTDOWN)]);

    public bool LeftButtonUp() => Send([CreateMouseInput(NativeConstants.MOUSEEVENTF_LEFTUP)]);

    public bool RightButtonDown() => Send([CreateMouseInput(NativeConstants.MOUSEEVENTF_RIGHTDOWN)]);

    public bool RightButtonUp() => Send([CreateMouseInput(NativeConstants.MOUSEEVENTF_RIGHTUP)]);

    public bool MiddleButtonDown() => Send([CreateMouseInput(NativeConstants.MOUSEEVENTF_MIDDLEDOWN)]);

    public bool MiddleButtonUp() => Send([CreateMouseInput(NativeConstants.MOUSEEVENTF_MIDDLEUP)]);

    public bool LeftClick() => Send(
    [
        CreateMouseInput(NativeConstants.MOUSEEVENTF_LEFTDOWN),
        CreateMouseInput(NativeConstants.MOUSEEVENTF_LEFTUP)
    ]);

    public bool RightClick() => Send(
    [
        CreateMouseInput(NativeConstants.MOUSEEVENTF_RIGHTDOWN),
        CreateMouseInput(NativeConstants.MOUSEEVENTF_RIGHTUP)
    ]);

    public bool MiddleClick() => Send(
    [
        CreateMouseInput(NativeConstants.MOUSEEVENTF_MIDDLEDOWN),
        CreateMouseInput(NativeConstants.MOUSEEVENTF_MIDDLEUP)
    ]);

    public bool Wheel(int delta) =>
        Send([CreateMouseInput(NativeConstants.MOUSEEVENTF_WHEEL, unchecked((uint)delta))]);

    public bool XButtonDown(VirtualKeyCode virtualKey) =>
        Send([CreateXButtonInput(virtualKey, NativeConstants.MOUSEEVENTF_XDOWN)]);

    public bool XButtonUp(VirtualKeyCode virtualKey) =>
        Send([CreateXButtonInput(virtualKey, NativeConstants.MOUSEEVENTF_XUP)]);

    private bool Send(INPUT[] inputs)
    {
        if (inputs.Length == 0)
        {
            return true;
        }

        return sendInput(inputs) == inputs.Length;
    }

    private INPUT CreateKeyboardInput(VirtualKeyCode virtualKey, uint flags = 0) => new()
    {
        Type = NativeConstants.INPUT_KEYBOARD,
        Union = new InputUnion
        {
            Keyboard = new KEYBDINPUT
            {
                VirtualKey = (ushort)virtualKey,
                Flags = flags,
                ExtraInfo = (UIntPtr)ExtraInfo
            }
        }
    };

    private INPUT CreateUnicodeInput(char character, uint flags = NativeConstants.KEYEVENTF_UNICODE) => new()
    {
        Type = NativeConstants.INPUT_KEYBOARD,
        Union = new InputUnion
        {
            Keyboard = new KEYBDINPUT
            {
                VirtualKey = 0,
                ScanCode = character,
                Flags = flags,
                ExtraInfo = (UIntPtr)ExtraInfo
            }
        }
    };

    private INPUT CreateMouseInput(
        uint flags,
        uint mouseData = 0,
        int x = 0,
        int y = 0)
    {
        return new INPUT
        {
            Type = NativeConstants.INPUT_MOUSE,
            Union = new InputUnion
            {
                Mouse = new MOUSEINPUT
                {
                    Dx = x,
                    Dy = y,
                    MouseData = mouseData,
                    Flags = flags,
                    ExtraInfo = (UIntPtr)ExtraInfo
                }
            }
        };
    }

    private INPUT CreateXButtonInput(VirtualKeyCode virtualKey, uint flags)
    {
        var mouseData = virtualKey switch
        {
            VirtualKeyCode.VK_XBUTTON1 => 1u,
            VirtualKeyCode.VK_XBUTTON2 => 2u,
            _ => throw new ArgumentOutOfRangeException(
                nameof(virtualKey),
                virtualKey,
                "仅支持 VK_XBUTTON1 与 VK_XBUTTON2。")
        };

        return CreateMouseInput(flags, mouseData);
    }
}
