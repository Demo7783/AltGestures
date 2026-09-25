# Phase 2A 规格：Windows 互操作基础层

> 执行方先读仓库根目录 `AGENTS.md`，其约束优先于本规格。

---

## 0. 前置三分类（动手前先输出，等确认）

1. **已确认事实** — 你从 `reference/wgestures/` 核实过的输入
2. **待验证假设** — 标记 `待验`
3. **当前决定** — 本轮的取舍

---

## 1. 一句话目标

建立 **Windows 互操作基础层**（P/Invoke 声明 + 输入模拟 + 显示器信息），为后续手势头（2B）与窗口操作（2C）提供地基。

**核心约束：目标框架保持 `net8.0`（不是 `net8.0-windows`），必须在 Linux 上能编译并通过测试。** 这是执行方能自验的前提，也是本阶段的验收硬指标。

---

## 2. 上下文：Phase 2 全貌

| 子阶段 | 内容 | 状态 |
| --- | --- | --- |
| **2A** | 互操作基础层（本规格） | 现在 |
| 2B | 手势头：路径追踪 + 鼠标/键盘钩子 | 后续 |
| 2C | 窗口操作 + 统一仲裁状态机 | 后续 |

2A 只做**地基**，不实现任何业务逻辑。

---

## 3. 交付物清单

```
src/AltGestures.Core/
  Interop/
    NativeConstants.cs     窗口消息、钩子类型、显示指标、SetWindowPos 标志、SendInput 标志
    NativeStructures.cs    POINT / RECT / SIZE / MONITORINFO / INPUT / MOUSEINPUT / KEYBDINPUT / HARDWAREINPUT
    User32.cs              用户与窗口 API
    Kernel32.cs            模块与线程 API
    Shell32.cs             外壳执行 API
    DisplayApi.cs          多显示器枚举
  Input/
    VirtualKeyCode.cs      虚拟键码枚举（按需，覆盖用到的键）
    ModifierState.cs       修饰键实时状态查询与解析（纯逻辑可测）
    InputSimulator.cs      键盘/鼠标/文本输入模拟
  Windows/
    ScreenInfo.cs          多显示器与工作区信息（替代 WinForms 的 Screen）
```

---

## 4. 搬运与参考规则

- `reference/wgestures/WGestures.Common/OsSpecific/Windows/Win32/*.cs`（User32 / Kernel32 / Shell32 / GDI32）可作**声明写法**参考。
- **不要整份搬运**（上游 User32.cs 有 1864 行，绝大部分本项目用不到）。按第 6 节清单**按需裁剪**，只保留需要的 API。
- 搬运或参考的文件，在文件头加归属声明：
  ```csharp
  // 基于 WGestures (https://github.com/yingDev/WGestures) 修改
  // 原始版权 (C) Ying Yuandong，GPL-2.0
  ```
- **禁止**把 AltDrag（`reference/altdrag/`，C 语言，GPL-3.0）的任何代码翻译进来。

---

## 5. 硬约束

1. `<TargetFramework>net8.0</TargetFramework>` —— **禁止**改成 `net8.0-windows`
2. **禁止引入 CsWin32 或任何源生成器**（可能强制要求 Windows 目标框架）
3. **禁止引用 `System.Windows.Forms`**、任何 WinForms 类型
4. 手写 P/Invoke，全部集中在 `Interop/` 目录，禁止散落
5. 结构体标注 `[StructLayout(LayoutKind.Sequential)]`，字符集显式指定（`CharSet.Unicode`）
6. 需要错误诊断的 API 带 `SetLastError = true`
7. `Nullable enable`，`TreatWarningsAsErrors`

---

## 6. 必须实现的 API 清单

**User32.cs**

| 分组 | API |
| --- | --- |
| 窗口获取 | `GetForegroundWindow`、`WindowFromPoint`、`GetAncestor`、`GetWindowRect`、`GetClientRect`、`GetClassName`、`GetWindowText` |
| 窗口操作 | `SetWindowPos`、`MoveWindow`、`ShowWindow`、`IsWindowVisible`、`IsIconic`、`IsZoomed`、`IsWindow` |
| 窗口样式 | `GetWindowLongPtr`、`SetWindowLongPtr`（`GWL_EXSTYLE`，用于置顶切换） |
| 钩子 | `SetWindowsHookEx`、`UnhookWindowsHookEx`、`CallNextHookEx`、`GetModuleHandle` |
| 光标与屏幕 | `GetCursorPos`、`GetSystemMetrics` |
| 输入 | `SendInput`、`GetAsyncKeyState`、`GetKeyState` |
| 消息 | `PostMessage`、`SendMessage`、`SendMessageTimeout` |
| 前台/焦点 | `SetForegroundWindow`、`BringWindowToTop`、`AttachThreadInput`、`GetWindowThreadProcessId` |
| 系统参数 | `SystemParametersInfo`（`SPI_GETWORKAREA`） |
| 热键（仅声明，2B 使用） | `RegisterHotKey`、`UnregisterHotKey` |

**Kernel32.cs**：`GetModuleHandle`、`GetCurrentThreadId`、`GetTickCount64`

**Shell32.cs**：`ShellExecuteEx`（打开文件 / URL）

**DisplayApi.cs**：`EnumDisplayMonitors`、`MonitorFromPoint`、`MonitorFromWindow`、`GetMonitorInfo`

---

## 7. 输入模拟规格（`InputSimulator.cs`）

| 能力 | 要求 |
| --- | --- |
| 键盘 | `KeyDown(vk)` / `KeyUp(vk)` / `Press(vk)` / 组合键（按住修饰键 → 主键 → 逆序释放） |
| 文本 | `TypeText(string)` —— 走 `KEYEVENTF_UNICODE`，不得逐字符查表 |
| 鼠标 | `MoveTo(x, y)` / 左中右键 down、up、click / `Wheel(delta)` |
| 侧键 | `XButtonDown(vk)` / `XButtonUp(vk)`，支持 `VK_XBUTTON1/2` |

**结构体布局必须正确**：x64 下 `Marshal.SizeOf<INPUT>()` 必须等于 **40**，`MOUSEINPUT` 等于 **24**。这条会被单元测试断言。

---

## 8. 单元测试（必须能在 Linux 上跑通）

1. 结构体布局断言：`INPUT` = 40、`MOUSEINPUT` = 24、`KEYBDINPUT` = 24、`POINT` = 8、`RECT` = 16、`MONITORINFO` = 40
2. 虚拟键码关键值断言：`VK_LMENU`=0xA4、`VK_RMENU`=0xA5、`VK_XBUTTON1`=0x05、`VK_XBUTTON2`=0x06、`VK_LBUTTON`=0x01、`VK_RBUTTON`=0x02、`VK_MBUTTON`=0x04
3. `ModifierState` 的位解析逻辑（不依赖真实按键，纯函数可测）
4. `InputSimulator` 构造的 `INPUT[]`：键盘组合键的数组长度与顺序、`TypeText` 的 Unicode 标志位
5. 常量断言：`WH_MOUSE_LL`=14、`WH_KEYBOARD_LL`=13、`WM_MOUSEMOVE`=0x0200、`WM_XBUTTONDOWN`=0x020B

测试目标：**≥ 12 条**，全部在 Linux 上通过。

---

## 9. 本轮不做什么

- 不实现任何钩子回调逻辑（2B）
- 不实现窗口拖拽/缩放业务（2C）
- 不实现状态机（2C）
- 不做 UI
- 不实际调用 Win32 运行时（Linux 上跑不了；本阶段只保证编译与结构定义正确）

---

## 10. 验收标准（可执行断言）

1. `cd src && dotnet build -c Release` → **0 error，0 warning**
2. `dotnet test` → **全部通过**，新增用例 **≥ 12 条**
3. **在 Linux 容器内编译通过**（本阶段的硬指标，不许改 TFM 绕过）
4. 断言命令：
   ```bash
   grep -rn "System.Windows.Forms" src/AltGestures.Core/ | wc -l      # 期望 0
   grep -rn "net8.0-windows" src/AltGestures.Core/*.csproj | wc -l     # 期望 0
   grep -rn "CsWin32" src/AltGestures.Core/ | wc -l                    # 期望 0
   grep -c "基于 WGestures" src/AltGestures.Core/Interop/*.cs          # 每个搬运文件 1
   ```

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
- [ ] 新增文件清单（路径 + 行数 + 用途）
- [ ] 推迟/未实现的部分及原因
