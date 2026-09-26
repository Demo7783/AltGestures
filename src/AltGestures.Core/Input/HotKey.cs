using System.Diagnostics.CodeAnalysis;

namespace AltGestures.Core.Input;

/// <summary>
/// 表示全局热键的通用修饰键。
/// </summary>
[Flags]
public enum HotKeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

/// <summary>
/// 表示一个由修饰键与主键组成的全局热键。
/// </summary>
public readonly record struct HotKey
{
    /// <summary>
    /// 初始化热键。
    /// </summary>
    public HotKey(HotKeyModifiers modifiers, VirtualKeyCode key)
    {
        if (modifiers.HasUnknownFlags()) throw new ArgumentOutOfRangeException(nameof(modifiers));
        if (key.IsModifierKey()) throw new ArgumentOutOfRangeException(nameof(key));

        Modifiers = modifiers;
        Key = key;
    }

    /// <summary>
    /// 获取热键修饰键。
    /// </summary>
    public HotKeyModifiers Modifiers { get; }

    /// <summary>
    /// 获取热键主键。
    /// </summary>
    public VirtualKeyCode Key { get; }

    /// <summary>
    /// 解析形如 “Ctrl+Alt+P” 的热键文本。
    /// </summary>
    public static bool TryParse(
        string? text,
        out HotKey hotKey,
        [MaybeNullWhen(true)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            hotKey = default;
            error = "热键不能为空。";
            return false;
        }

        var parts = text.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Any(string.IsNullOrWhiteSpace))
        {
            hotKey = default;
            error = "热键每个部分不能为空。";
            return false;
        }

        var modifiers = HotKeyModifiers.None;
        for (var index = 0; index < parts.Length - 1; index++)
        {
            if (!TryParseModifier(parts[index], out var modifier))
            {
                hotKey = default;
                error = $"无法识别的修饰键：{parts[index]}。";
                return false;
            }

            modifiers |= modifier;
        }

        if (!TryParseKey(parts[^1], out var key))
        {
            hotKey = default;
            error = $"无法识别的主键：{parts[^1]}。";
            return false;
        }

        hotKey = new HotKey(modifiers, key);
        error = null;
        return true;
    }

    /// <summary>
    /// 解析窗口操作触发键名称，Alt、Ctrl、Shift 与 Win 均代表左右两侧。
    /// </summary>
    public static bool TryParseTriggerKeys(
        IEnumerable<string>? names,
        out HotKeyModifiers modifiers,
        [MaybeNullWhen(true)] out string? error)
    {
        modifiers = HotKeyModifiers.None;
        if (names is null)
        {
            error = "触发键配置不能为空。";
            return false;
        }

        foreach (var name in names)
        {
            if (!TryParseModifier(name, out var modifier) || modifier == HotKeyModifiers.None)
            {
                error = $"无法识别的触发键：{name}。";
                return false;
            }

            modifiers |= modifier;
        }

        if (modifiers == HotKeyModifiers.None)
        {
            error = "至少需要配置一个触发键。";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// 返回规范化的热键文本。
    /// </summary>
    public override string ToString()
    {
        var text = string.Empty;
        if (Modifiers.HasFlag(HotKeyModifiers.Control)) text += "Ctrl+";
        if (Modifiers.HasFlag(HotKeyModifiers.Alt)) text += "Alt+";
        if (Modifiers.HasFlag(HotKeyModifiers.Shift)) text += "Shift+";
        if (Modifiers.HasFlag(HotKeyModifiers.Windows)) text += "Win+";
        return text + Key.ToString("G");
    }

    /// <summary>
    /// 将通用触发键转换为左右两侧的物理修饰键状态。
    /// </summary>
    public static ModifierModifiers ToPhysicalModifiers(HotKeyModifiers value) =>
        (value.HasFlag(HotKeyModifiers.Alt) ? ModifierModifiers.LeftAlt | ModifierModifiers.RightAlt : ModifierModifiers.None)
        | (value.HasFlag(HotKeyModifiers.Control) ? ModifierModifiers.LeftControl | ModifierModifiers.RightControl : ModifierModifiers.None)
        | (value.HasFlag(HotKeyModifiers.Shift) ? ModifierModifiers.LeftShift | ModifierModifiers.RightShift : ModifierModifiers.None)
        | (value.HasFlag(HotKeyModifiers.Windows) ? ModifierModifiers.LeftWindows | ModifierModifiers.RightWindows : ModifierModifiers.None);

    private static bool TryParseModifier(string text, out HotKeyModifiers modifier) => text.ToLowerInvariant() switch
    {
        "ctrl" or "control" => Set(HotKeyModifiers.Control, out modifier),
        "alt" => Set(HotKeyModifiers.Alt, out modifier),
        "shift" => Set(HotKeyModifiers.Shift, out modifier),
        "win" or "windows" => Set(HotKeyModifiers.Windows, out modifier),
        _ => Set(default, out modifier)
    };

    private static bool TryParseKey(string text, out VirtualKeyCode key)
    {
        if (string.Equals(text, "F1", StringComparison.OrdinalIgnoreCase))
        {
            key = VirtualKeyCode.VK_F1;
            return true;
        }

        if (text.Length == 1)
        {
            var character = text[0];
            if (character is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
            {
                key = (VirtualKeyCode)char.ToUpperInvariant(character);
                return true;
            }

            if (character is >= '0' and <= '9')
            {
                key = (VirtualKeyCode)character;
                return true;
            }
        }

        if (Enum.TryParse<VirtualKeyCode>(text, true, out key) && !key.IsModifierKey())
        {
            return true;
        }

        key = default;
        return false;
    }

    private static bool Set(HotKeyModifiers value, out HotKeyModifiers modifier)
    {
        modifier = value;
        return value != HotKeyModifiers.None;
    }
}

internal static class HotKeyExtensions
{
    public static bool HasUnknownFlags(this HotKeyModifiers value) =>
        (value & ~(HotKeyModifiers.Alt | HotKeyModifiers.Control | HotKeyModifiers.Shift | HotKeyModifiers.Windows)) != 0;

    public static bool IsModifierKey(this VirtualKeyCode value) => value is
        VirtualKeyCode.VK_SHIFT or VirtualKeyCode.VK_CONTROL or VirtualKeyCode.VK_MENU or
        VirtualKeyCode.VK_LSHIFT or VirtualKeyCode.VK_RSHIFT or
        VirtualKeyCode.VK_LCONTROL or VirtualKeyCode.VK_RCONTROL or
        VirtualKeyCode.VK_LMENU or VirtualKeyCode.VK_RMENU or
        VirtualKeyCode.VK_LWIN or VirtualKeyCode.VK_RWIN;
}
