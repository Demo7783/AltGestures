# AGENTS.md — AltGestures 开发约束

本文件是给所有参与本项目的 AI/人类协作者的硬约束。**与 README 冲突时以本文件为准。**

## 0. 前置三分类（写码前必须先输出）

任何一轮实现前，先输出三项供确认，确认后再动手：

1. **已确认事实** — 从需求/现有代码/项目文档核实过的输入，可直接使用
2. **待验证假设** — 合理但未确认的猜想，标记 `待验`
3. **当前决定** — 本轮主动做的范围取舍：做了什么、暂时不做什么

## 1. 许可证铁律

- 本项目整体 **GPL-2.0**（衍生自 WGestures，GPL-2.0）。
- **禁止把 AltDrag 的 C 源码翻译成 C# 后提交**。AltDrag 是 GPL-3.0-or-later，与 GPL-2.0-only 不兼容。
  - 允许：阅读其行为、边界条件、默认值作为参考
  - 禁止：逐行翻译、复制代码结构、复制注释
  - 窗口拖拽/缩放必须基于通用 Win32 API（`WindowFromPoint` / `GetAncestor` / `GetWindowRect` / `SetWindowPos`）自主实现
- 从 WGestures 搬运的文件，在文件头保留归属声明：
  ```csharp
  // 基于 WGestures (https://github.com/yingDev/WGestures) 修改
  // 原始版权 (C) Ying Yuandong，GPL-2.0
  ```

## 2. 性能铁律

钩子回调（`WH_MOUSE_LL` / `WH_KEYBOARD_LL`）运行在系统输入链路上，超时（默认 300ms）会被 Windows **静默摘除**——不报错，突然失灵。

- 回调内**只允许**：读坐标、写状态位、返回。
- **禁止**在回调内做：轨迹识别、文件 IO、进程启动、UI 更新、锁等待、任何分配大对象。
- 不在待机态监听 `WM_MOUSEMOVE`；只在进入拖拽/手势模式后采点。
- 目标：常驻内存 < 25 MB，待机 CPU 0%。

## 3. 代码规范

- .NET 8，C# 12，`Nullable enable`，`TreatWarningsAsErrors` 在 Core 项目开启。
- `AltGestures.Core` **不得引用任何 UI 框架**（WPF/WinForms/Avalonia），保证可在 Linux 编译和测试。
- Win32 P/Invoke 统一收在 `Interop/` 目录，禁止散落。
- 无死代码、无注释掉的代码块、无「以防万一」的预留实现。
- 单个文件超过 500 行考虑拆分。

## 4. 视觉/交互规范

- **禁止 emoji**，图标一律内联 SVG 线性风格（stroke 1.6px / round cap / currentColor）。
- 界面配色：黑白灰为基底，点缀色仅在明确要求时使用。
- 中文界面文案。

## 5. 交付纪律

- 每轮交付必须附**检查清单**：`- [x] 任务X: 检查内容 → 操作方法 → 实际结果`，未验证项单列 `- [ ] … 原因`。
- **禁止声称未经实际执行的构建/测试结果**。构建日志必须可复现。
- 编译必须通过（`dotnet build` 零错误）才算交付。
- 行为变更同步更新 `CHANGELOG.md`。

## 6. 仓库维护

- 提交信息格式：`<scope>: <描述>`（scope 如 `core` / `app` / `interop` / `docs`）。
- 不提交 `bin/`、`obj/`、用户配置、日志。
- `reference/` 下的上游源码只读，不作为构建输入。
