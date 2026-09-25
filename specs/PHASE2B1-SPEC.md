# Phase 2B1 规格：手势头（路径追踪 + 鼠标钩子）

> 执行方先读仓库根目录 `AGENTS.md`，其约束优先于本规格。

---

## 0. 前置三分类（输出后立即开工，禁止等待确认）

> **本次是一次性非交互任务，没有任何人会中途回复你。** 请在交付说明的**开头**输出下面三项，然后**立即继续实现**，绝对不要输出「请确认」之类的话并停下。

1. **已确认事实** — 你从 `reference/wgestures/` 核实过的输入
2. **待验证假设** — 标记 `待验`
3. **当前决定** — 本轮的取舍

---

## 1. 一句话目标

把 WGestures 的**手势头**（鼠标/键盘钩子 + 路径追踪器 + 屏幕边缘检测）移植进 `AltGestures.Core`，与 Phase 2A 的互操作层和 Phase 1 的 `IPathTracker` 接口对接，做到 `dotnet build` 零错误、`dotnet test` 全绿。

**硬约束不变：目标框架保持 `net8.0`，必须在 Linux 上可编译可测试。**

---

## 2. 交付物清单

```
src/AltGestures.Core/
  Windows/
    MouseKeyboardHook.cs        鼠标+键盘低级钩子（含消息分发）
    MouseMsg.cs                 MouseMsg / KeyboardEventType / XButtonNumber 枚举
    PathTracker.cs              路径追踪器（实现 IPathTracker）
    ScreenEdgeInteractDetector.cs  屏幕边缘/热角检测
    Win32GestureContext.cs      窗口上下文（进程名/窗口类名/标题）
    ScreenInfo.cs               （2A 已存在，按需扩展）
  Interop/
    User32.cs 等                （2A 已存在，按需补充缺失声明）
```

---

## 3. 搬运映射（上游 → 本项目）

| 上游文件 | 行数 | 目标文件 | 说明 |
| --- | --- | --- | --- |
| `WGestures.Core/Impl/Windows/Win32MousePathTracker2.cs` | 1269 | `Windows/PathTracker.cs` | 核心，保留全部状态机语义 |
| `WGestures.Core/Impl/Windows/MouseHook.cs` | 324 | `Windows/MouseKeyboardHook.cs` + `Windows/MouseMsg.cs` | 三类枚举可拆到 `MouseMsg.cs` |
| `WGestures.Core/Impl/Windows/ScreenEdgeInteractDetector.cs` | 445 | `Windows/ScreenEdgeInteractDetector.cs` | 边缘摩擦与热角 |
| `WGestures.Core/Impl/Windows/Win32GestureContext.cs` | 39 | `Windows/Win32GestureContext.cs` | — |

**保留行为语义**：路径增长判定、起始停留超时、位移阈值、最大步数、边缘摩擦触发条件、热角触发条件 —— 这些是上游打磨出来的，**不要"优化"或"简化"**，只做编译层面必要的改动。

每个搬运文件在文件头加归属声明：

```csharp
// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0
```

---

## 4. 依赖剥离清单（逐条替换，禁止保留原依赖）

| 上游用法 | 必须替换为 |
| --- | --- |
| `System.Windows.Forms.Cursor.Position` | `Interop/User32.GetCursorPos` |
| `System.Windows.Forms.Screen.*` | `Windows/ScreenInfo`（2A 已实现） |
| `System.Windows.Forms.Control.ModifierKeys` | `Input/ModifierState`（2A 已实现） |
| `System.Windows.Forms.SystemInformation.*` | `Interop/User32.GetSystemMetrics` / `SystemParametersInfo` |
| `System.Windows.Forms.Keys.*` | `Input/VirtualKeyCode`（2A 已实现，缺失的键按需补） |
| `WindowsInput`（第三方库） | `Input/InputSimulator`（2A 已实现） |
| `Win32` 命名空间类型 | `Interop/` 中已有的对应声明，缺失的按需补 |
| `System.Drawing.Point/Size/Rectangle` | NuGet `System.Drawing.Primitives`（跨平台，可用） |

**新增 P/Invoke 一律加到 `Interop/` 下已有文件里**，禁止在 `Windows/` 目录内直接写 `[DllImport]`。

---

## 5. 性能纪律（本阶段最重要的非功能性要求）

钩子回调运行在系统输入链路上，超时（默认 300 ms）会被 Windows **静默摘除**：不报错，突然失灵。

**在钩子回调链上（`SetWindowsHookEx` 回调 → 事件分发 → 追踪器记录点）绝对禁止**：

- 文件 IO、日志写盘、`Console.Write*`
- 进程启动、注册表读写
- UI 更新、窗口枚举遍历
- 锁等待、`Thread.Sleep`、同步等待
- 每次移动都分配大对象（如新建集合、字符串拼接）

**允许**：读取坐标、写状态位、向内存集合追加一个点、转发 `CallNextHookEx`。

**要求**：在交付说明中明确写出你如何处理这条约束 —— 哪些代码在回调链上、哪些被移出（工作线程 / 定时器 / 轨迹结束时的单次解析），并给出判断依据。

---

## 6. 接口对接

- `PathTracker` 必须实现 Phase 1 已定义的 `IPathTracker`（`src/AltGestures.Core/IPathTracker.cs`），**不得修改该接口签名**。
- `GestureParser`（Phase 1 已实现）依赖该接口，二者对接后必须仍能通过现有测试。
- 钩子的生命周期（安装/卸载）要可反复启停而不泄漏（`IDisposable` 语义正确）。

---

## 7. 单元测试（必须能在 Linux 上跑通）

**≥ 8 条新增**。由于钩子与 Win32 调用在 Linux 上无法运行，测试对象是**可从 Win32 解耦的纯逻辑**：

要求把下列逻辑抽成 `internal static` 方法（用 `InternalsVisibleTo` 暴露给测试项目）：
- 鼠标消息常量与枚举的映射（不依赖运行时）
- 边缘/角落判定（给定光标坐标 + 屏幕矩形 → 判定结果）
- 轨迹点的步数上限与有效位移判定

测试覆盖：

1. `MouseMsg` 枚举值与 `WM_*` 常量一致（`WM_MOUSEMOVE`=0x0200、`WM_LBUTTONDOWN`=0x0201、`WM_RBUTTONDOWN`=0x0204、`WM_XBUTTONDOWN`=0x020B 等）
2. `XButtonNumber` 映射正确（XBUTTON1=1、XBUTTON2=2）
3. 屏幕边缘判定：左/右/上/下边缘各一例，中心区域一例
4. 屏幕角落判定：四个角各一例
5. 轨迹步数上限：超过 `MaxGestureSteps` 的点被忽略
6. 有效位移阈值：小于阈值的抖动不计入方向
7. `PathTracker` 的 `Start/Stop/Pause/Resume` 状态迁移正确（`IPathTracker` 契约）
8. `Dispose` 语义：重复 Dispose 不抛异常

---

## 8. 验收标准（可执行断言）

1. `cd src && dotnet build -c Release` → **0 error，0 warning**
2. `dotnet test` → **全部通过**（现有 29 条 + 新增 ≥ 8 条）
3. 断言命令：
   ```bash
   grep -rn "System.Windows.Forms" src/AltGestures.Core/ | wc -l     # 期望 0
   grep -rn "WindowsInput" src/AltGestures.Core/ | wc -l             # 期望 0
   grep -rn "net8.0-windows" src/AltGestures.Core/*.csproj | wc -l    # 期望 0
   grep -rn "DllImport" src/AltGestures.Core/Windows/ | wc -l         # 期望 0（P/Invoke 只在 Interop/）
   ```

---

## 9. 本轮不做什么

- 不实现窗口拖拽/缩放/吸附（2C）
- 不实现统一仲裁状态机（2C）
- 不实现全局热键注册与键盘热键（2B2）
- 不做 UI
- **不修改 `GestureParser.cs` 等 Phase 1 已交付的文件**（除非是纯新增的内部可见性标注）

---

## 10. 额度不足时的降级路径

若单次执行无法完成全部交付物，**优先保证**：

1. `MouseMsg.cs` 枚举
2. `MouseKeyboardHook.cs`
3. `PathTracker.cs`

`ScreenEdgeInteractDetector.cs` 与 `Win32GestureContext.cs` 可留到下一轮。此时必须：

- 保证已完成的部分**编译通过、测试全绿**后再结束
- 在交付说明中**明确列出未完成项及原因**
- 不得留下编译不过的半成品

---

## 11. 交付说明的输出纪律

- 首行给结论（成功/失败 + 关键数字）
- 报错用陈述句，附原始报错文本
- 禁止开场白、客套收尾、emoji
- **禁止声称未实际执行的构建或测试结果**

---

## 12. 交付检查清单（交付时逐条填写）

```
- [x] 任务X: 检查内容 → 操作方法 → 实际结果
- [ ] 未验证项 → 原因
```

必填项：
- [ ] `dotnet build -c Release` 实际输出（错误数/警告数）
- [ ] `dotnet test` 实际输出（通过数/总数）
- [ ] 四项 grep 断言实际结果
- [ ] 搬运映射表（目标文件 → 上游文件 → 行数变化）
- [ ] **性能纪律的处理说明**（哪些代码在钩子回调链上，哪些被移出）
- [ ] 未完成项及原因
