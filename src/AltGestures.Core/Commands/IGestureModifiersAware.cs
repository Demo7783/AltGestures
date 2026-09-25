// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

namespace AltGestures.Core.Commands;

/// <summary>实现此接口的命令可响应手势过程中的修饰符事件。</summary>
public interface IGestureModifiersAware
{
    /// <summary>手势被识别且首次执行时触发。</summary>
    /// <param name="observeModifiers">命令感兴趣的修饰符。</param>
    void GestureRecognized(out GestureModifier observeModifiers);

    /// <summary>感兴趣的修饰符发生改变时触发。</summary>
    /// <param name="modifier">发生的修饰符事件。</param>
    void ModifierTriggered(GestureModifier modifier);
    void GestureEnded();
    event Action<string> ReportStatus;
}
