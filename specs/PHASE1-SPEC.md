# Phase 1 实现规格：手势核心移植

> 本规格发给执行方（Codex CLI）。**先读仓库根目录 `AGENTS.md`，其约束优先于本规格。**

---

## 0. 前置三分类（动手前先输出，等确认）

在写任何代码前，先在交付说明里输出三行：

1. **已确认事实** — 你从 `reference/wgestures/` 源码核实过的输入
2. **待验证假设** — 标记 `待验`
3. **当前决定** — 本轮的取舍

---

## 1. 一句话目标

把 WGestures（.NET Framework 4.8）的**手势核心逻辑**移植进新项目 `AltGestures.Core`，升级为 **net8.0**，做到 `dotnet build` 零错误、`dotnet test` 全绿。

**本轮只搬「纯逻辑层」**：数据模型 + 识别算法 + 意图存储 + 命令抽象。不碰钩子、不碰 UI。

---

## 2. 基座位置

- 上游源码（**只读，禁止修改**）：`reference/wgestures/`
- 关键源文件：
  - `WGestures.Core/Gesture.cs`（97 行）
  - `WGestures.Core/GestureButton.cs`（35 行）
  - `WGestures.Core/GestureModifier.cs`（51 行）
  - `WGestures.Core/GestureIntent.cs`（81 行）
  - `WGestures.Core/GestureContext.cs`
  - `WGestures.Core/GestureParser.cs`（**625 行，本轮核心**）
  - `WGestures.Core/IPathTracker.cs`（67 行）
  - `WGestures.Core/IGestureIntentFinder.cs`
  - `WGestures.Core/Persistence/AbstractGestureIntentFinder.cs`（86 行）
  - `WGestures.Core/Persistence/IGestureIntentStore.cs`
  - `WGestures.Core/Persistence/Impl/JsonGestureIntentStore.cs`（382 行）
  - `WGestures.Core/Commands/AbstractCommand.cs` + 4 个接口文件
  - `WGestures.Core/ScreenCornerAndEdge.cs`

---

## 3. 交付物清单

```
src/
  AltGestures.sln
  AltGestures.Core/
    AltGestures.Core.csproj
    Gesture.cs
    GestureTriggerButton.cs
    GestureModifier.cs
    GestureContext.cs
    GestureIntent.cs
    GestureParser.cs
    IPathTracker.cs
    IGestureIntentFinder.cs
    AbstractGestureIntentFinder.cs
    IGestureIntentStore.cs
    JsonGestureIntentStore.cs
    ScreenCornerAndEdge.cs
    Commands/
      AbstractCommand.cs
      IGestureContextAware.cs
      IGestureModifiersAware.cs
      IGestureParserAware.cs
      INeedInit.cs
  AltGestures.Tests/
    AltGestures.Tests.csproj
    TestPathTracker.cs          ← 测试桩，见 §6
    GestureParserTests.cs
    GestureTests.cs
    GestureIntentStoreTests.cs
```

---

## 4. 依赖替换表（硬约束，逐条执行）

| 原依赖 | 必须替换为 | 说明 |
| --- | --- | --- |
| `System.Drawing.Point/Size/Rectangle/PointF` | NuGet `System.Drawing.Primitives` | 跨平台，可直接用 |
| `System.Windows.Forms` | **禁止引用** | 需要的类型自行定义 |
| `Newtonsoft.Json` 7.0.1 | `System.Text.Json`（内置） | 优先，避免外链依赖 |
| `WindowsInput.Native.VirtualKeyCode` | 自建 `VirtualKeyCode` 枚举 | **只搬实际用到的成员**，Phase 2 再补全 |
| `NLua` / `KeraLua` | 本轮不搬 `ScriptCommand` | Phase 2 处理 |
| `COMReference SHDocVw` | **禁止** | 不搬 `WebSearchCommand` |
| `System.Web` | **禁止** | — |

**为什么禁止 WinForms**：Core 层要求能在 Linux 上 `dotnet build` + `dotnet test`，作为执行方的自验手段。一旦引入 WinForms 就必须切 `net8.0-windows`，Linux 编译直接失败。

---

## 5. 搬运规则

1. **允许**：整文件复制到新项目，然后改命名空间、换依赖、现代化语法。
2. **必须**：每个搬运的文件在**文件头**加归属声明：
   ```csharp
   // 基于 WGestures (https://github.com/yingDev/WGestures) 修改
   // 原始版权 (C) Ying Yuandong，GPL-2.0
   ```
3. **保留原始算法的行为语义**。方向判定阈值、容错逻辑、状态流转是上游十几年打磨的成果，**不要"优化"、不要"简化"、不要重命名公开成员**。只做编译层面的必要改动。
4. 命名空间映射：`WGestures.Core` → `AltGestures.Core`（含子命名空间 `WGestures.Core.Commands` → `AltGestures.Core.Commands`）。
5. **禁止**把 AltDrag（`reference/altdrag/`，C 语言）的任何代码翻译进来。该目录仅作后续阶段的行为参考。

---

## 6. 本轮不做什么（划清边界）

- **不实现 `IPathTracker` 的 Windows 版本**。本轮只搬接口，并写一个测试桩 `TestPathTracker` 供单测使用。
- 不搬：`MouseHook.cs`、`TouchHook.cs`、`ScreenEdgeInteractDetector.cs`、`Win32MousePathTracker2.cs`、`Win32GestureContext.cs`、`Win32GestrueIntentFinder.cs`（全部 Phase 2）。
- 不搬：`App.cs`、任何 `Gui/` 与 UI 相关文件（Phase 3）。
- 不搬：`Plist.cs` / `PlistConfig.cs`（配置改用 JSON）。
- 不搬命令实现类（`Commands/Impl/*`），只搬 `AbstractCommand.cs` 与 4 个接口。`DoNothingCommand` 例外——它无外部依赖，可以搬来作为测试用的最小命令。

---

## 7. 测试桩规格（`TestPathTracker.cs`）

`IPathTracker` 是手势识别的输入源。本轮用桩件驱动 `GestureParser`：

- 提供方法：按顺序注入路径点（`PathEventArgs`），并能在合适时机触发 `PathStart` / `EffectivePathGrow` / `PathEnd` / `BeforePathStart` / `PathModifier` 事件。
- 要求：能模拟出「向右 → 向下」这类方向序列，使 `GestureParser` 产出预期的 `Gesture` 对象。
- 事件签名**必须**与 `IPathTracker` 接口一致（照搬接口定义）。

---

## 8. 验收标准（可执行断言，逐条跑给你看）

1. `cd src && dotnet build -c Release` → **0 error，0 warning**
2. `dotnet test` → **全部通过**，测试用例数 **≥ 12**
3. 测试必须覆盖以下场景（每条一个 `[Fact]` 或 `[Theory]`）：
   - [ ] 单方向手势识别（右）
   - [ ] 两方向组合识别（右→下）
   - [ ] 8 方向模式下的对角方向识别（`Enable8DirGesture = true`）
   - [ ] 同一轨迹两次解析产出 `Equals` 相等的 `Gesture`
   - [ ] `Gesture.GetHashCode()` 对相等对象一致
   - [ ] 噪声点（小幅抖动）不改变识别结果
   - [ ] 超过 `MaxGestureSteps` 的行为符合上游语义
   - [ ] `JsonGestureIntentStore` 序列化往返相等
   - [ ] `Gesture` 的 `ToString()` 与上游格式一致
4. 断言命令（执行方自行跑并贴结果）：
   ```bash
   grep -rn "System.Windows.Forms" src/AltGestures.Core/ | wc -l    # 期望 0
   grep -rn "Newtonsoft" src/AltGestures.Core/ | wc -l              # 期望 0
   grep -c "基于 WGestures" src/AltGestures.Core/*.cs               # 每个搬运文件 1
   ```

---

## 9. 硬约束

- `net8.0`，`<Nullable>enable</Nullable>`，Core 项目 `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- Core 项目**零 UI 框架依赖**、零第三方 Win 专用包
- 无死代码、无注释掉的代码块、无「以防万一」的预留实现
- 中文 XML 注释，简明扼要
- 单个文件超 500 行考虑拆分（`GestureParser.cs` 保留原结构即可，不强制拆）

---

## 10. 交付说明的输出纪律

- 首行直接给结论（成功/失败 + 关键数字）
- 报错用陈述句，附原始报错文本与你的判断
- 禁止开场白、禁止"总结一下"式收尾、禁止 emoji
- 禁止声称未实际执行的构建或测试结果；每条断言必须贴真实命令输出

---

## 11. 交付检查清单（交付时逐条填写）

```
- [x] 任务X: 检查内容 → 操作方法 → 实际结果
- [ ] 未验证项 → 原因
```

必填项：
- [ ] `dotnet build -c Release` 的实际输出（错误数/警告数）
- [ ] `dotnet test` 的实际输出（通过数/总数）
- [ ] 三项 grep 断言的实际结果
- [ ] 搬运文件清单（文件名 + 行数 + 对应上游文件）
- [ ] 未实现/推迟的部分及原因
