# Phase 3A 规格：Avalonia 应用骨架 + 无头渲染验收流水线

> 执行方先读仓库根目录 `AGENTS.md` 与 `docs/UI.md`，其约束优先于本规格。

---

## 0. 前置三分类（输出后立即开工，禁止等待确认）

> **本次是一次性非交互任务，没有人会中途回复你。** 三分类写在交付说明**开头**，输出后**立即继续实现**，不要停下等待确认。

1. **已确认事实** 2. **待验证假设** 3. **当前决定**

---

## 1. 一句话目标

建立 `AltGestures.App`（Avalonia）的界面骨架：主窗口 + 左侧六项导航 + 六个页面 + 视觉 token 体系，并建立**无头渲染截图流水线**，使界面能在 Linux 上被真实渲染和验证。

**这一步的意义**：UI 是本项目唯一无法靠「编译通过」验收的部分。建立无头渲染后，界面改动可以在容器内产生真实 PNG，与 `docs/prototypes/settings.html` 逐像素比对，而不是靠臆想。

---

## 2. 交付物

```
src/
  AltGestures.App/
    AltGestures.App.csproj
    Program.cs
    App.axaml / App.axaml.cs
    Styles/Tokens.axaml                视觉 token（颜色/字号/圆角/间距）
    Views/MainWindow.axaml (.cs)       主窗口：标题栏 + 左侧导航 + 内容区
    Views/Pages/WindowActionsPage.axaml (.cs)
    Views/Pages/GesturesPage.axaml (.cs)
    Views/Pages/TriggerFeedbackPage.axaml (.cs)
    Views/Pages/AppRulesPage.axaml (.cs)
    Views/Pages/GeneralPage.axaml (.cs)
    Views/Pages/AboutPage.axaml (.cs)
    ViewModels/MainWindowViewModel.cs
    ViewModels/Pages/*.cs
    Assets/icons.axaml                 内联 SVG 图标资源（Path 几何）
  AltGestures.App.Headless/            （或是 Tests 项目内的渲染工具）
    ScreenshotRenderer.cs              无头渲染六页为 PNG
artifacts/screenshots/                 产出：01-window-actions.png ... 06-about.png
```

---

## 3. 技术选型（已定，不要更换）

| 项 | 选择 |
| --- | --- |
| UI 框架 | **Avalonia 12.x**（最新稳定版，实测 `12.1.3` 可从 NuGet 拉取） |
| 主题 | **FluentTheme**（与 `docs/UI.md` 选定的 Fluent 方向一致） |
| MVVM | `CommunityToolkit.Mvvm` |
| 无头渲染 | `Avalonia.Headless` + `Avalonia.Skia` |
| 目标框架 | **`net8.0`**（不是 `net8.0-windows`） |

> 若 Avalonia 12 的包结构相较旧版有变化（例如 headless 相关包更名或需附加包），**以实际能拉取的包为准**，并在交付说明中记录最终使用的版本与包名清单。

---

## 4. 视觉规范（严格照 `docs/UI.md` 的原值，不要自行发挥）

```
背景/导航    #F4F4F4      导航选中  #E4E7EB      标题栏  #F0F0F0
内容面板     #FCFCFC      卡片      #FFFFFF
分隔线       #E3E3E3 ~ #EFEFEF
正文         #2C2C2C      次级      #6B6B6B      弱化    #999999
圆角         6px（控件）/ 8-10px（卡片、窗口）
字号         页标题 23 / 正文 14 / 分组标题 12.5 / 徽章 11.5
```

触发键徽章：右键 `#E8EEF7`/`#3A6EA5` · 中键 `#EFEAE4`/`#8A6A45` · 侧键 `#EDE8F0`/`#6B4A7A`

**硬性警告**：字号不得小于上表；禁 emoji，图标一律内联 SVG 线性风格（stroke 1.6，round cap）。

---

## 5. 页面内容（照 `docs/prototypes/settings.html` 的结构实现）

| 页 | 内容 |
| --- | --- |
| 窗口操作 | 六个鼠标键的动作下拉（左/中/右/侧键4/侧键5/滚轮）· 拖拽行为开关与数值输入 · 修饰键说明 |
| 鼠标手势 | **紧凑表格**：轨迹折线图标 + 手势名 + 触发键徽章 + 命令 + 参数；顶部触发键筛选 |
| 触发与反馈 | 四色轨迹色块 · 显示开关 · 触发判定（起始超时/8方向/步数/全屏禁用）· 屏幕边缘 |
| 应用规则 | 已配置程序列表（程序/手势/窗口操作/来源）· 全局黑名单文本 |
| 通用 | 启动（自启/隐藏托盘/提权）· 托盘 · 语言 / 更新 / 多实例 |
| 关于 | 版本 · 许可证 GPL-2.0 · 项目主页 · 来源致谢 |

**骨架阶段可用静态示例数据**（配置读写属于 3C），但控件类型、层级、间距要与原型一致。

手势轨迹图标用 `Path` 几何绘制折线（如「向右」= 水平线 + 箭头），不要用图片。

---

## 6. 无头渲染流水线（本阶段的核心交付）

要求提供一个**可执行方式**，在无图形界面的容器内把六个页面各渲染成一张 PNG：

- 使用 `Avalonia.Headless` 初始化平台，用 `RenderTargetBitmap` 渲染，保存到 `artifacts/screenshots/`
- 文件命名：`01-window-actions.png` ~ `06-about.png`
- 尺寸 ≥ **960 × 620**，缩放 1.0
- 提供便捷入口（如 `dotnet run --project src/AltGestures.App.Headless` 或一个测试用例），并在交付说明中写明**确切命令**
- 渲染产物**必须非空白**：交付时说明你如何验证（例如检查 PNG 文件大小阈值、或断言像素非纯色）

---

## 7. 硬约束

1. `<TargetFramework>net8.0</TargetFramework>`，不得改成 `net8.0-windows`
2. `AltGestures.App` 可引用 `AltGestures.Core`，但**不得修改 Core 的任何文件**
3. 禁止 emoji；图标全部内联 SVG Path
4. 界面文案为简体中文
5. 不使用外部字体文件（用系统字体栈），不引用任何外网资源
6. 无死代码，`Nullable enable`，`TreatWarningsAsErrors`

---

## 8. 验收标准（可执行断言）

1. `cd src && dotnet build -c Release` → **0 error，0 warning**
2. `dotnet test` → **全部通过**（现有 95 条不得回归）
3. **无头渲染命令执行成功**，`artifacts/screenshots/` 下产出 **6 张 PNG**
4. 每张 PNG 尺寸 ≥ 960×620，且**文件大小 > 10 KB**（排除空白渲染）
5. 断言命令：
   ```bash
   ls -la artifacts/screenshots/                      # 期望 6 个文件
   grep -rn "net8.0-windows" src/AltGestures.App/*.csproj | wc -l   # 期望 0
   grep -rn "http://\|https://" src/AltGestures.App/ --include=*.axaml | wc -l  # 期望 0（无外链资源）
   ```

---

## 9. 本轮不做什么

- 不接配置读写与双向绑定（3C）
- 不做系统托盘（3C，需 Windows 实机验证）
- 不做与 `Core` 的运行时段接（3C）
- 不做高 DPI / DPI 缩放的像素级适配（Phase 4 实机阶段处理）

---

## 10. 额度不足时的降级路径

优先保证：

1. `AltGestures.App` 项目骨架 + 主窗口 + 左侧导航 + 六页框架（可先只有标题）
2. **无头渲染流水线跑通**（这条优先级最高，是本阶段的验收核心）
3. 窗口操作页与鼠标手势页的完整内容

其余页面内容可留到 3B。此时必须保证编译通过、测试全绿，并在交付说明中列出未完成项。

---

## 11. 交付说明的输出纪律

- 首行给结论（成功/失败 + 关键数字）
- 报错用陈述句，附原始报错文本
- 禁止开场白、客套收尾、emoji
- **禁止声称未实际执行的构建、测试或渲染结果**

---

## 12. 交付检查清单

```
- [x] 任务X: 检查内容 → 操作方法 → 实际结果
- [ ] 未验证项 → 原因
```

必填：
- [ ] `dotnet build -c Release` 实际输出
- [ ] `dotnet test` 实际输出（通过数/总数）
- [ ] **无头渲染的确切命令 + 六张 PNG 的实际尺寸与字节数**
- [ ] 三项 grep 断言实际结果
- [ ] 视觉 token 的落地位置（文件:行）
- [ ] 未完成项及原因
