# Phase 3C 续做说明（额度中断后的重派）

> **本文件优先于 `PHASE3C-SPEC.md`。** 原规格描述完整目标，但首次执行已完成约七成；
> 本文件说明**当前状态**与**本轮唯一剩余任务**。开工前先读 `AGENTS.md`。

---

## 0. 前置三分类（输出后立即开工，禁止等待确认）

> **本次是一次性非交互任务，没有人会中途回复你。** 三分类写在交付说明**开头**，输出后**立即继续实现**，不要停下等待确认。

1. **已确认事实** 2. **待验证假设** 3. **当前决定**

---

## 1. 背景（必读）

Phase 3C 首次执行在 GLM 额度耗尽前中断，交付的是**编译不过的半成品**（App 项目 58 个编译错误）。
统筹方已接手修复，**当前仓库处于全绿状态**。你本轮的职责是**接着做剩下的部分**，不是重做。

### 当前状态（开工前请自行核实，确认与下面一致）

| 项 | 状态 |
| --- | --- |
| `dotnet build -c Release` | 0 warning 0 error |
| `dotnet test` | 95/95 通过 |
| 无头渲染 | 输出「已提交 6 张截图」 |

### 已经就绪、不要重写的部分

- `Services/ConfigService.cs` —— 配置加载 / 防抖保存 / 变更通知
- `Services/EngineHost.cs` —— 引擎组装与生命周期（`IEngineFactory` 抽象）
- `Tray/TrayController.cs` —— 托盘图标与菜单（`ALTGESTURES_HEADLESS=1` 时跳过）
- `App.axaml.cs` —— 启动流程：创建 ConfigService、注入 VM、启动 EngineHost
- **6 个页面 VM 全部就绪**：均已改为 `class` + `ConfigService` 注入，并暴露了与 `AppConfig` 对应的配置属性（get 读配置、set 写配置并 `Commit()`）
- `PageViewModel.SetConfig<T>(Func<AppConfig,T> read, Action<T> write, T value)` —— **注意第二个参数接收的是「值」而不是「配置」**，所以写法是 `v => ConfigService.Config.X = v`

### 绝对禁止

- 修改 `AltGestures.Core` 的任何文件（既定语义）
- **回退统筹方的修复**：`SetConfig` 的 lambda 用法、枚举成员名 `X1`/`X2`（不是 `XButton1/2`）、托盘事件用 `+=` 而非对象初始化器赋值、`EngineHost` 用 `pauseHotKey` 字段、`MainWindow` 不自建 DataContext
- 改动 `ScreenshotRenderer` 的「暂存目录 + 原子提交」逻辑（那是防止渲染失败销毁已交付截图的保护）
- 重新生成页面 VM（它们已就绪，重写会丢失配置绑定）

---

## 2. 本轮唯一目标

**把各页 XAML 控件接到 VM 属性上，实现双向绑定。** 目前 VM 层已能读写配置并落盘，但界面上还是静态文本，两者尚未连通。

---

## 3. 具体做法

1. 各页 `Views/Pages/*.axaml` 中控件的静态值改为绑定：
   - 下拉框：`ItemsSource="{Binding XXXOptions}"` + `SelectedItem="{Binding XXX, Mode=TwoWay}"`
   - 复选框：`IsChecked="{Binding XXX, Mode=TwoWay}"`
   - 数字输入：`Value="{Binding XXX, Mode=TwoWay}"`（注意 `NumericUpDown` 的 `Value` 是 `decimal?`，需确认与 `int` 属性的转换方式；必要时加转换器或用 `TextBox` + 绑定）
   - 文本框：`Text="{Binding XXX, Mode=TwoWay}"`
2. **页面根元素必须加 `x:DataType`**（项目开了 `AvaloniaUseCompiledBindingsByDefault`，缺了会报 `AVLN2100`），例如：
   ```xml
   xmlns:vm="using:AltGestures.App.ViewModels.Pages"
   x:DataType="vm:WindowActionsPageViewModel"
   ```
3. 控件与分组以 `docs/prototypes/settings.html` 为准；视觉 token 以 `docs/UI.md` 为准（字号不得小于规定值）。
4. 枚举下拉需要显示中文名时，用 `Converters/EnumDisplayConverter.cs`（已存在）。

---

## 4. 验收标准

1. `cd src && dotnet build -c Release` → **0 error，0 warning**
2. `dotnet test` → **95/95 通过**（不得回归）
3. 无头渲染：`dotnet run --project src/AltGestures.App.Headless -c Release` → 仍输出「已提交 6 张截图」
4. 断言：
   ```bash
   grep -rl "Mode=TwoWay" src/AltGestures.App/Views/Pages/ | wc -l   # 期望 ≥ 5
   grep -rl "x:DataType" src/AltGestures.App/Views/Pages/ | wc -l    # 期望 6
   ```
5. **交互验证**：无头渲染出的截图应与之前一致（配置项显示默认值）；如有明显变化需说明原因。

---

## 5. 交付说明的输出纪律

- 首行给结论（成功/失败 + 关键数字）
- 报错用陈述句，附原始报错文本
- 禁止开场白、客套收尾、emoji
- **禁止声称未实际执行的构建、测试或渲染结果**

---

## 6. 交付检查清单

```
- [x] 任务X: 检查内容 → 操作方法 → 实际结果
- [ ] 未验证项 → 原因
```

必填：
- [ ] `dotnet build -c Release` 实际输出
- [ ] `dotnet test` 实际输出
- [ ] 无头渲染实际输出（是否「已提交 6 张截图」）
- [ ] 两项断言实际结果
- [ ] 逐页列出绑定清单（页面 → 控件 → 绑定属性）
- [ ] 未完成项及原因
