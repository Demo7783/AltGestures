# Phase 3B 规格：图标修订 + 版本号移位 + 六页对齐原型

> 执行方先读 `AGENTS.md`、`docs/UI.md`、`docs/prototypes/settings.html`。

---

## 0. 前置三分类（输出后立即开工，禁止等待确认）

> **本次是一次性非交互任务，没有人会中途回复你。** 三分类写在交付说明**开头**，输出后**立即继续实现**，不要停下等待确认。

1. **已确认事实** 2. **待验证假设** 3. **当前决定**

---

## 1. 一句话目标

修掉林哥指出的两个问题（导航图标语义不对、版本号位置错），并把六页内容与 `docs/prototypes/settings.html` 对齐，最后重新渲染截图。

---

## 2. 任务 A：六个导航图标换成定稿几何

改 `ViewModels/Pages/*PageViewModel.cs` 里 `IconGeometry` 的值，**严格使用下表 Path 原值**（`viewBox 0 0 20 20`，stroke 1.6，fill none，round cap/join）：

| 文件 | 语义 | Path |
| --- | --- | --- |
| `WindowActionsPageViewModel` | 窗口框 + 标题栏 | `M2.5 4.5 H17.5 V15.5 H2.5 Z M2.5 8 H17.5` |
| `GesturesPageViewModel` | 轨迹箭头 | `M3 10 H17 M13 5.5 L17.5 10 L13 14.5` |
| `TriggerFeedbackPageViewModel` | 闪电 | `M11.5 2.5 L5.5 11 H10 L9 17.5 L15 9 H10.5 Z` |
| `AppRulesPageViewModel` | **盾牌** | `M10 2.5 L16 5 V9.5 Q16 14.5 10 17.5 Q4 14.5 4 9.5 V5 Z` |
| `GeneralPageViewModel` | 滑杆（圆点把手） | `M3.5 7 H16.5 M3.5 13 H16.5 M8 5.7 A1.3 1.3 0 1 0 8 8.3 A1.3 1.3 0 1 0 8 5.7 M13 11.7 A1.3 1.3 0 1 0 13 14.3 A1.3 1.3 0 1 0 13 11.7` |
| `AboutPageViewModel` | 闭合圆 + i | `M10 2.5 A7.5 7.5 0 1 0 10 17.5 A7.5 7.5 0 1 0 10 2.5 M10 9 V14 M10 6.2 V6.3` |

注意：这些图标在**实际界面里只有 16px**，比放大图钝得多，所以不要自行"美化"或加细节。若渲染出来某个形状在 16px 下糊掉，在交付说明里指出，但**不要擅自改动 Path**。

## 3. 任务 B：版本号移入「关于」

1. 从 `Views/MainWindow.axaml`（当前第 49 行附近）**删除**标题栏右侧的 `0.1.0（开发中）` 文本块
2. 版本号改在「关于」页显示，值**从程序集读取**：
   - 在 `AltGestures.App.csproj` 中定义 `<Version>0.1.0</Version>`
   - 关于页通过 `Assembly.GetExecutingAssembly()` 的版本信息读取（不要硬编码字符串）
3. 关于页的版本行保持「版本 / 许可证 / 项目主页」的结构

## 4. 任务 C：六页对齐原型

逐页对照 `docs/prototypes/settings.html` 核对，补齐缺失的**控件与分组**（不接配置读写，仍用示例数据）：

- 窗口操作：六个鼠标键下拉、拖拽行为开关与数值输入、修饰键说明行
- 鼠标手势：紧凑表格（轨迹图标 + 手势名 + 触发键徽章 + 命令 + 参数）
- 触发与反馈：四色轨迹色块、显示开关、触发判定参数、屏幕边缘开关
- 应用规则：已配置程序列表、全局黑名单
- 通用：启动 / 托盘 / 语言与更新
- 关于：版本 / 许可 / 来源致谢

**视觉 token 以 `docs/UI.md` 原值为准**（不得小于规定字号）。

## 5. 硬约束

1. `net8.0`，不得改 `net8.0-windows`
2. 不修改 `AltGestures.Core` 任何文件，不修改 `specs/` 与 `reference/`
3. 禁 emoji；图标一律内联 SVG Path；无外链资源
4. 界面文案简体中文
5. 无死代码，`Nullable enable`，`TreatWarningsAsErrors`

## 6. 验收标准

1. `cd src && dotnet build -c Release` → **0 error，0 warning**
2. `dotnet test` → **全部通过**（现有 95 条不得回归）
3. **重新执行无头渲染命令**，`artifacts/screenshots/` 六张 PNG 全部更新（尺寸 ≥ 960×620、> 10 KB）
4. 断言：
   ```bash
   grep -rn "0.1.0（开发中）\|0\.1\.0（开发中）" src/AltGestures.App/Views/ | wc -l   # 期望 0（标题栏版本号已移除）
   grep -c "M10 2.5 L16 5 V9.5" src/AltGestures.App/ViewModels/Pages/AppRulesPageViewModel.cs  # 期望 1（盾牌已落地）
   grep -c "<Version>" src/AltGestures.App/AltGestures.App.csproj    # 期望 1
   ```

## 7. 交付检查清单

```
- [x] 任务X: 检查内容 → 操作方法 → 实际结果
- [ ] 未验证项 → 原因
```

必填：
- [ ] `dotnet build -c Release` 实际输出
- [ ] `dotnet test` 实际输出
- [ ] 无头渲染命令 + 六张 PNG 的实际尺寸与字节数
- [ ] 三项 grep 断言实际结果
- [ ] 六个图标是否已按表落地（逐条确认）
- [ ] 16px 下是否有形状糊掉的情况（如实报告）
- [ ] 未完成项及原因
