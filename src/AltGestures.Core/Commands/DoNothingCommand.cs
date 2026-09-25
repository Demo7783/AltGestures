// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

namespace AltGestures.Core.Commands;

[Serializable]
public sealed class DoNothingCommand : AbstractCommand
{
    public override void Execute()
    {
    }

    public override string Description() => string.Empty;
}
