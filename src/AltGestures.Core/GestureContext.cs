// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using System.Drawing;

namespace AltGestures.Core;

public abstract class GestureContext
{
    public Point StartPoint;
    public Point EndPoint;
    public uint ProcId;
    public IntPtr WinId;
    public GestureTriggerButton GestureButton;

    public abstract void ActivateTargetWindow();
}
