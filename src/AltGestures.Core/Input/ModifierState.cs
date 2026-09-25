using AltGestures.Core.Interop;

namespace AltGestures.Core.Input;

[Flags]
public enum ModifierModifiers
{
    None = 0,
    LeftShift = 1 << 0,
    RightShift = 1 << 1,
    LeftControl = 1 << 2,
    RightControl = 1 << 3,
    LeftAlt = 1 << 4,
    RightAlt = 1 << 5,
    LeftWindows = 1 << 6,
    RightWindows = 1 << 7
}

public sealed class ModifierState
{
    private const short KeyPressedMask = unchecked((short)0x8000);
    private static readonly (VirtualKeyCode Key, ModifierModifiers Modifier)[] ModifierKeys =
    [
        (VirtualKeyCode.VK_LSHIFT, ModifierModifiers.LeftShift),
        (VirtualKeyCode.VK_RSHIFT, ModifierModifiers.RightShift),
        (VirtualKeyCode.VK_LCONTROL, ModifierModifiers.LeftControl),
        (VirtualKeyCode.VK_RCONTROL, ModifierModifiers.RightControl),
        (VirtualKeyCode.VK_LMENU, ModifierModifiers.LeftAlt),
        (VirtualKeyCode.VK_RMENU, ModifierModifiers.RightAlt),
        (VirtualKeyCode.VK_LWIN, ModifierModifiers.LeftWindows),
        (VirtualKeyCode.VK_RWIN, ModifierModifiers.RightWindows)
    ];

    private readonly Func<VirtualKeyCode, bool> isKeyDown;

    public ModifierState()
        : this(key => (User32.GetKeyState((int)key) & KeyPressedMask) != 0)
    {
    }

    public ModifierState(Func<VirtualKeyCode, bool> isKeyDown)
    {
        this.isKeyDown = isKeyDown ?? throw new ArgumentNullException(nameof(isKeyDown));
    }

    public ModifierModifiers Current
    {
        get
        {
            var result = ModifierModifiers.None;
            foreach (var (key, modifier) in ModifierKeys)
            {
                if (isKeyDown(key))
                {
                    result |= modifier;
                }
            }

            return result;
        }
    }

    public bool IsShiftDown => (Current & (ModifierModifiers.LeftShift | ModifierModifiers.RightShift)) != 0;

    public bool IsControlDown => (Current & (ModifierModifiers.LeftControl | ModifierModifiers.RightControl)) != 0;

    public bool IsAltDown => (Current & (ModifierModifiers.LeftAlt | ModifierModifiers.RightAlt)) != 0;

    public bool IsWindowsDown => (Current & (ModifierModifiers.LeftWindows | ModifierModifiers.RightWindows)) != 0;

    public static ModifierModifiers FromKeyStates(IReadOnlyDictionary<VirtualKeyCode, bool> keyStates)
    {
        ArgumentNullException.ThrowIfNull(keyStates);

        var result = ModifierModifiers.None;
        foreach (var (key, modifier) in ModifierKeys)
        {
            if (keyStates.TryGetValue(key, out var isDown) && isDown)
            {
                result |= modifier;
            }
        }

        return result;
    }
}
