namespace AltGestures.Core.Arbitration;

/// <summary>
/// 表示当前输入序列的归属。
/// </summary>
public enum InputOwnership
{
    None,
    Gesture,
    Window
}

/// <summary>
/// 表示统一输入仲裁状态机的当前状态。
/// </summary>
public enum ArbitrationState
{
    Idle,
    GestureTracking,
    WindowDragging,
    WindowCommand
}
