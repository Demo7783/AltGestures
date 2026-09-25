using AltGestures.Core.Windowing;

namespace AltGestures.Core.Arbitration;

/// <summary>
/// 定义仲裁器调用的窗口操作分支。实现必须快速返回，耗时 Win32 操作需自行移出输入回调线程。
/// </summary>
public interface IWindowBranch
{
    void OnMouseDown(
        WindowAction action,
        WindowMouseButton button,
        int x,
        int y,
        WindowDragModifiers modifiers,
        bool isDoubleClick);

    void OnMouseMove(int x, int y, WindowDragModifiers modifiers);

    void OnMouseUp(WindowMouseButton button, int x, int y);

    void OnMouseWheel(int delta, int x, int y, WindowDragModifiers modifiers);
}
