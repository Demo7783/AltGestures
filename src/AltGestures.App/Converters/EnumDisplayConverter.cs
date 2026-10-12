using System.Globalization;
using Avalonia.Data.Converters;

namespace AltGestures.App.Converters;

public sealed class EnumDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        AltGestures.Core.GestureTriggerButton.Right => "右键",
        AltGestures.Core.GestureTriggerButton.Middle => "中键",
        AltGestures.Core.GestureTriggerButton.X1 => "侧键 4",
        AltGestures.Core.GestureTriggerButton.X2 => "侧键 5",
        AltGestures.Core.Windowing.WindowAction.None => "无",
        AltGestures.Core.Windowing.WindowAction.Move => "移动窗口",
        AltGestures.Core.Windowing.WindowAction.Resize => "缩放窗口",
        AltGestures.Core.Windowing.WindowAction.Close => "关闭窗口",
        AltGestures.Core.Windowing.WindowAction.Minimize => "最小化",
        AltGestures.Core.Windowing.WindowAction.Lower => "下沉窗口",
        AltGestures.Core.Windowing.WindowAction.AlwaysOnTop => "置顶切换",
        AltGestures.Core.Windowing.WindowAction.Center => "居中",
        AltGestures.Core.Windowing.WindowAction.AltTab => "切换窗口",
        AltGestures.Core.Windowing.WindowAction.Volume => "调节音量",
        AltGestures.Core.Windowing.WindowAction.Transparency => "调节透明度",
        AltGestures.Core.Configuration.InterfaceLanguage.Chinese => "简体中文",
        AltGestures.Core.Configuration.InterfaceLanguage.English => "English",
        null => string.Empty,
        _ => value.ToString()
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("枚举选项使用枚举值绑定，不需要反向转换。");
}
