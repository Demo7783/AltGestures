using System.Collections.Frozen;

namespace AltGestures.Core.Windowing;

/// <summary>
/// 表示按键到窗口动作的只读绑定表。
/// </summary>
public sealed class WindowActionBinding
{
    private readonly FrozenDictionary<WindowTrigger, WindowAction> bindings;

    public WindowActionBinding(IReadOnlyDictionary<WindowTrigger, WindowAction> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        this.bindings = bindings.ToFrozenDictionary();
    }

    public static WindowActionBinding Default { get; } = new(new Dictionary<WindowTrigger, WindowAction>
    {
        [WindowTrigger.Left] = WindowAction.Move,
        [WindowTrigger.Middle] = WindowAction.Resize,
        [WindowTrigger.Right] = WindowAction.Resize
    });

    public WindowAction this[WindowTrigger trigger] =>
        bindings.TryGetValue(trigger, out var action) ? action : WindowAction.None;

    public WindowAction GetWheelAction(int delta)
    {
        return delta == 0 ? WindowAction.None : this[WindowTrigger.Wheel];
    }

    public static WindowActionBinding FromNames(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var result = new Dictionary<WindowTrigger, WindowAction>();

        foreach (var (name, actionName) in values)
        {
            if (!TryParseTrigger(name, out var trigger) || !TryParseAction(actionName, out var action))
            {
                throw new ArgumentException($"无效的窗口动作绑定：{name}={actionName}。");
            }

            result[trigger] = action;
        }

        return new WindowActionBinding(result);
    }

    private static bool TryParseTrigger(string name, out WindowTrigger trigger) => name.ToUpperInvariant() switch
    {
        "LMB" => AsTrigger(WindowTrigger.Left, out trigger),
        "MMB" => AsTrigger(WindowTrigger.Middle, out trigger),
        "RMB" => AsTrigger(WindowTrigger.Right, out trigger),
        "MB4" => AsTrigger(WindowTrigger.X1, out trigger),
        "MB5" => AsTrigger(WindowTrigger.X2, out trigger),
        "SCROLL" => AsTrigger(WindowTrigger.Wheel, out trigger),
        _ => Enum.TryParse(name, ignoreCase: true, out trigger)
    };

    private static bool TryParseAction(string name, out WindowAction action)
    {
        if (name.Equals("Nothing", StringComparison.OrdinalIgnoreCase))
        {
            action = WindowAction.None;
            return true;
        }

        return Enum.TryParse(name, ignoreCase: true, out action);
    }

    private static bool AsTrigger(WindowTrigger value, out WindowTrigger trigger)
    {
        trigger = value;
        return true;
    }
}
