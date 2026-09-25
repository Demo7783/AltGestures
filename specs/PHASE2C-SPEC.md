# Phase 2C 规格：窗口操作 + 统一仲裁状态机

> 执行方先读仓库根目录 `AGENTS.md`，其约束优先于本规格。

---

## 0. 前置三分类（输出后立即开工，禁止等待确认）

> **本次是一次性非交互任务，没有任何人会中途回复你。** 请在交付说明的**开头**输出下面三项，然后**立即继续实现**，绝对不要输出「请确认」之类的话并停下。

1. **已确认事实** — 你从现有代码与 `reference/` 核实过的输入
2. **待验证假设** — 标记 `待验`
3. **当前决定** — 本轮的取舍

---

## 1. 一句话目标

实现**窗口操作**（按住 Alt + 鼠标键移动/缩放/点击型动作/吸附/双击）与**统一仲裁状态机**（Alt 分支与手势分支互不抢占），并与 Phase 2B1 的钩子、Phase 1 的手势解析器完成对接。

**硬约束不变：目标框架保持 `net8.0`，必须在 Linux 上可编译可测试。**

---

## 2. 最重要的架构约束：禁止翻译 AltDrag 代码

窗口操作的行为参考 AltDrag（`reference/altdrag/`），但它是 **GPL-3.0-or-later**，与本项目 GPL-2.0 不兼容。

- **允许**：阅读其行为、默认值、边界条件（如 `AltDrag.ini` 里的默认参数、`hooks.c` 里的动作语义）
- **禁止**：逐行翻译、复制代码结构、复制注释、复制标识符命名体系
- 必须基于**通用 Win32 API** 自主实现：`WindowFromPoint` / `GetAncestor` / `GetWindowRect` / `SetWindowPos` / `GetDoubleClickTime`

交付说明中需给出你在本阶段参考了 AltDrag 的哪些**行为**（不是代码）。

---

## 3. 交付物清单

```
src/AltGestures.Core/
  Windowing/
    WindowAction.cs             动作枚举（11 种）
    WindowActionBinding.cs      按键 → 动作的绑定表与解析
    WindowGeometry.cs           边/角判定、缩放锚点计算（纯几何，可测）
    SnapEngine.cs               吸附目标计算（纯几何，可测）
    WindowDragSession.cs        拖拽会话状态（起点、窗口、原始矩形）
    WindowMover.cs              移动：节流 + SetWindowPos
    WindowResizer.cs            缩放：边角锚点 + 节流 + 光标反馈
    WindowActionExecutor.cs     点击型动作与滚轮动作执行
  Arbitration/
    InputArbiter.cs             统一仲裁状态机（核心）
    ArbitrationState.cs         状态与归属枚举
    IWindowBranch.cs            窗口分支接口（便于用假实现测试仲裁）
  Configuration/
    AppConfig.cs                配置模型（窗口动作绑定 + 参数）
    ConfigStore.cs              JSON 读写
```

---

## 4. 仲裁设计（本阶段的核心，必须严格按此实现）

### 4.1 接入方式

`PathTracker`（2B1）通过 `internal interface IMouseKeyboardHook` 接收钩子事件，且其 `internal` 构造函数**支持注入**。

**因此：`InputArbiter` 实现 `IMouseKeyboardHook`，作为钩子与手势分支之间的中间人。**

```
MouseKeyboardHook（真实钩子）
        │  MouseHookEvent / KeyboardHookEvent
        ▼
  InputArbiter  ──(Alt 分支)──►  IWindowBranch（窗口操作）
        │
        └──(非 Alt 分支)──►  转发事件 → PathTracker（手势）
```

- `PathTracker` **的内部逻辑不得修改**，只把它的钩子源换成 `InputArbiter`。
- `InputArbiter` 只在「非 Alt 分支」时触发自己对外暴露的 `MouseHookEvent` / `KeyboardHookEvent`。

### 4.2 分发规则

鼠标键按下时（B = 物理键，M = 修饰键状态）：

| 条件 | 处理 |
| --- | --- |
| `M` 含 Alt 且 B 已绑定动作，动作是**拖拽型**（Move / Resize） | 进入 `WindowDragging`，**吞掉事件**（`Handled = true`） |
| `M` 含 Alt 且 B 已绑定动作，动作是**点击型**（Close / Minimize / Center / AlwaysOnTop / Lower） | **按下即执行**，**吞掉事件** |
| `M` 含 Alt 且 B 未绑定动作（None） | 正常转发（放行） |
| `M` 不含 Alt | 转发给 PathTracker，由它自行判断是否触发手势 |

### 4.3 归属锁定（关键）

**归属必须在按下瞬间判定一次，之后锁定到该键抬起为止**，中途不因修饰键变化而切换：

- Alt + 左键按下 → 进入拖拽 → 即使中途松开 Alt，拖拽继续直到左键抬起
- 右键按下（无 Alt）→ 转发给手势分支 → 即使中途按下 Alt，也不切到窗口操作

用一个「当前已被接管的按键」字段实现锁定；该键抬起时解除。

### 4.4 双击判定

在**按下时**用 `GetDoubleClickTime()` 判断距上次同类点击的间隔：

- Alt + 左键双击 → 最大化 / 还原（并居中该窗口如果它不在任何显示器内）
- Alt + 右键双击 → 按光标在窗口内的相对位置，移到对应的角落或边

命中双击时**必须吞掉 mouseup**，否则桌面或资源管理器会跟着弹出右键菜单。

### 4.5 修饰键叠加（拖拽中生效）

| 键 | 行为 |
| --- | --- |
| Shift | 吸附到其他窗口边缘（手动吸附） |
| Ctrl | 把正在拖拽的窗口提到前台 |
| 空格 | 临时关闭自动吸附 |

---

## 5. 窗口操作规格

### 5.1 动作集（与上游 AltDrag 语义一致）

| 动作 | 类型 | 行为 |
| --- | --- | --- |
| Move | 拖拽型 | 跟随光标移动窗口 |
| Resize | 拖拽型 | 按光标最近的边/角缩放；中心区域为对称缩放 |
| Close | 点击型 | 发送 `WM_CLOSE` |
| Minimize | 点击型 | `SC_MINIMIZE` |
| Lower | 点击型 | `SetWindowPos(HWND_BOTTOM)`；按住 Shift 时改为最小化 |
| AlwaysOnTop | 点击型 | 切换 `WS_EX_TOPMOST` |
| Center | 点击型 | 在当前显示器居中 |
| AltTab | 滚轮型 | 切换窗口 |
| Volume | 滚轮型 | 调节音量（Shift 微调） |
| Transparency | 滚轮型 | 调节透明度（Shift 微调） |
| None | — | 不处理 |

### 5.2 窗口拾取

- `WindowFromPoint` + `GetAncestor(GA_ROOT)` 取顶层窗口
- 排除：桌面、任务栏、本项目自己的窗口
- 目标窗口若以管理员权限运行，普通权限下操作会被系统静默丢弃（UIPI）—— 在文档注释中说明，不做特殊处理

### 5.3 参数

| 参数 | 默认 | 说明 |
| --- | --- | --- |
| SnapThreshold | 20 px | 开始吸附的距离 |
| MoveRate | 2 px | 移动节流：累计位移超过该值才更新窗口 |
| ResizeRate | 5 px | 缩放节流 |
| AutoFocus | false | 拖拽时自动聚焦并提升窗口 |
| Aero | true | 拖拽到屏幕边缘/角落时贴靠 |

节流要求：更新间隔有上限（不得每像素调用一次 `SetWindowPos`）。

---

## 6. 测试要求（必须能在 Linux 上跑通，≥ 12 条新增）

**为可测性，以下逻辑必须与 Win32 调用解耦，写成纯函数或纯类：**

| 待测单元 | 说明 |
| --- | --- |
| `WindowGeometry` | 给定窗口矩形 + 光标坐标 → 判定边/角（上/下/左/右/四角/中心） |
| `SnapEngine` | 给定窗口矩形 + 屏幕工作区 + 其他窗口矩形集合 + 阈值 → 吸附结果 |
| `WindowActionBinding` | 从配置解析按键 → 动作；未配置的按键返回 None |
| `InputArbiter` | 用**假的** `IWindowBranch` 与假钩子事件驱动，断言归属判定与分发结果 |
| 节流逻辑 | 累计位移未达阈值不触发更新，达到则触发并重置计数 |

测试覆盖：

1. `WindowAction` 枚举值与绑定解析（含 None）
2. 边/角判定：四边各一例、四角各一例、中心一例
3. 吸附：左边缘贴靠、右边缘贴靠、上边缘贴靠、超过阈值不吸附
4. 节流：未达阈值不更新、达到阈值更新并重置
5. 仲裁分发：Alt + 已绑定键 → 走窗口分支且吞事件
6. 仲裁分发：无 Alt 键 → 转发给手势分支
7. 仲裁分发：Alt + 未绑定键（None）→ 放行
8. **归属锁定**：进入拖拽后释放 Alt，拖拽仍继续；抬起鼠标键后解除
9. **归属锁定**：先按下右键（转发手势）后按下 Alt，仍归属手势分支
10. 双击判定：间隔小于 `GetDoubleClickTime()` 视为双击（用可注入的时间源测试）
11. 配置 JSON 往返（`AppConfig` 序列化/反序列化相等）
12. `InputArbiter.Dispose` 重复调用不抛异常

---

## 7. 硬约束

1. `<TargetFramework>net8.0</TargetFramework>` —— 禁止 `net8.0-windows`
2. 禁止 `System.Windows.Forms`、禁止 `WindowsInput` 第三方库、禁止 CsWin32
3. **P/Invoke 一律加到 `Interop/` 已有文件中**，禁止在 `Windowing/` 或 `Arbitration/` 内写 `[DllImport]`
4. 不修改 `GestureParser.cs` / `PathTracker.cs` / `MouseKeyboardHook.cs` 的既有逻辑（接线改动除外）
5. 钩子回调链上禁止 IO / 进程启动 / 锁等待 / 大对象分配
6. 无死代码、无注释掉的代码
7. 中文 XML 注释
8. `Nullable enable`，`TreatWarningsAsErrors`

---

## 8. 本轮不做什么

- 不做 UI（Phase 3）
- 不做配置界面与热重载（Phase 3 接入）
- 不做全局热键注册（2B2）
- 不做 Windows 实机验证（Phase 4）

---

## 9. 额度不足时的降级路径

优先保证以下顺序，前 4 项做完即为可接受交付：

1. `WindowAction.cs` + `WindowActionBinding.cs`
2. `WindowGeometry.cs`
3. `InputArbiter.cs` + `ArbitrationState.cs` + `IWindowBranch.cs`
4. `WindowMover.cs` + `WindowResizer.cs` + `WindowActionExecutor.cs`

可延后到下一轮：`SnapEngine.cs`（吸附）、滚轮型动作、`ConfigStore.cs` 热重载。

此时必须：已完成部分**编译通过、测试全绿**，并在交付说明中列出未完成项与原因，不得留半成品。

---

## 10. 验收标准（可执行断言）

1. `cd src && dotnet build -c Release` → **0 error，0 warning**
2. `dotnet test` → **全部通过**（现有 53 条 + 新增 ≥ 12 条）
3. 断言命令：
   ```bash
   grep -rn "System.Windows.Forms" src/AltGestures.Core/ | wc -l            # 期望 0
   grep -rn "net8.0-windows" src/AltGestures.Core/*.csproj | wc -l          # 期望 0
   grep -rn "DllImport" src/AltGestures.Core/Windowing/ src/AltGestures.Core/Arbitration/ | wc -l  # 期望 0
   grep -rn "altdrag" src/AltGestures.Core/Windowing/*.cs | wc -l           # 期望 0（不得出现上游标识）
   ```
4. **AltDrag 参考说明**：交付说明中列出参考了哪些行为（非代码）

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
- [ ] 仲裁归属锁定的实现方式（简述）
- [ ] 节流实现方式与更新间隔上限
- [ ] AltDrag 行为参考说明（参考了什么、如何独立实现）
- [ ] 未完成项及原因
