# Phase 3C 规格：配置双向绑定 + 系统托盘 + 应用组装

> 执行方先读 `AGENTS.md`、`docs/UI.md`、`docs/DESIGN.md`。

---

## 0. 前置三分类（输出后立即开工，禁止等待确认）

> **本次是一次性非交互任务，没有人会中途回复你。** 三分类写在交付说明**开头**，输出后**立即继续实现**，不要停下等待确认。

1. **已确认事实** 2. **待验证假设** 3. **当前决定**

---

## 1. 一句话目标

让界面真正管用：**控件读写真实配置**、**配置落盘并热应用**、**托盘常驻**、**启动时把 Core 引擎组装起来**。

前两个阶段（3A/3B）做的是「看得见」，这一阶段做的是「用得上」。

---

## 2. 交付物

```
src/AltGestures.App/
  Services/
    ConfigService.cs        配置加载 / 保存 / 防抖写盘 / 变更通知
    EngineHost.cs           按配置组装 Core 引擎并管理生命周期
  Tray/
    TrayController.cs       托盘图标与右键菜单
  ViewModels/Pages/*.cs     各页 VM 改为读写 AppConfig（TwoWay）
  Views/Pages/*.axaml       控件绑定改为双向
  App.axaml.cs              启动流程调整
```

---

## 3. 需求

### 3.1 配置双向绑定

- 各页 VM 暴露与 `AppConfig` 字段一一对应的属性，XAML 用 `{Binding X, Mode=TwoWay}`
- 改动**立即生效**，不设「应用」按钮
- 启动时加载；配置缺失或 JSON 损坏时**回退默认值并记录**，绝不崩溃
- 枚举型控件（如窗口动作下拉）要与 `AppConfig` 里的动作枚举直接对应，不要用字符串中转

### 3.2 配置持久化

- 复用 `AltGestures.Core` 的 `Configuration/AppConfig.cs` 与 `ConfigStore.cs`
  - **可为其扩展字段**，但不得修改既有字段语义
- 路径：`Environment.GetFolderPath(SpecialFolder.ApplicationData)/AltGestures/config.json`（跨平台）
- **写盘要防抖**：连续改动合并成一次写入（例如 300ms 静默后落盘），避免每敲一个字符就 IO
- 原子写盘：先写临时文件再替换，避免中断产生半截 JSON

### 3.3 系统托盘

- `TrayIcon` + 右键菜单：启用 / 禁用、暂停手势、打开设置、退出
- **关闭设置窗口不退出程序**（隐藏到托盘）
- ⚠️ **无头渲染模式必须跳过托盘创建**：环境变量 `ALTGESTURES_HEADLESS=1` 时不得初始化 `TrayIcon`（Linux 无 X11 会失败），否则会破坏 Phase 3A 建立的无头渲染流水线
- Linux 上托盘不可用属正常，编译通过即可；运行时验证留 Phase 4

### 3.4 应用组装（EngineHost）

- 启动时按配置创建并接线：`MouseKeyboardHook` → `InputArbiter` → `PathTracker`（手势分支）与窗口操作分支
- 配置变更**热应用**：触发键变化、手势启停、窗口动作绑定变化等不需要重启
- `IDisposable` 语义正确：退出时卸载钩子、注销热键、停止工作线程
- **禁止在钩子回调链上做 UI 更新、文件 IO、锁等待**（沿用 Phase 2 的性能纪律）
- 引擎启动失败要可恢复（例如钩子安装失败时给出可读提示，不崩溃）

---

## 4. 硬约束

1. `net8.0`，不得改 `net8.0-windows`
2. 不修改 `AltGestures.Core` 既有语义（可扩展字段/新增成员）
3. 禁 emoji、无外链资源、界面文案简体中文
4. 无死代码、`Nullable enable`、`TreatWarningsAsErrors`
5. **不得破坏无头渲染流水线**（见 5.3）

---

## 5. 验收标准

1. `cd src && dotnet build -c Release` → **0 error，0 warning**
2. `dotnet test` → **全部通过**（现有 95 条不得回归），新增 **≥ 10 条**
3. **无头渲染回归**：`dotnet run --project src/AltGestures.App.Headless -c Release` 仍能成功输出「已提交 6 张截图」，六张 PNG 更新
4. 断言：
   ```bash
   grep -rl "Mode=TwoWay" src/AltGestures.App/Views/Pages/ | wc -l        # 期望 ≥ 5（各页均有双向绑定）
   ls src/AltGestures.App/Services/                                        # 期望含 ConfigService.cs 与 EngineHost.cs
   ls src/AltGestures.App/Tray/                                            # 期望含 TrayController.cs
   grep -rn "ALTGESTURES_HEADLESS" src/AltGestures.App/Tray/ src/AltGestures.App/App.axaml.cs | wc -l   # 期望 ≥ 1（无头模式跳过托盘）
   ```

**建议的新增测试**：
- VM 属性 ↔ `AppConfig` 双向映射（含默认值与非法值回退）
- 配置 JSON 往返相等；损坏 JSON 回退默认值不抛异常
- 防抖写盘：连续多次改动只落盘一次（用可注入的时钟/延迟）
- 原子写盘：写入过程中断不破坏原有文件
- `EngineHost` 生命周期：用假钩子验证 Start/Dispose 正确调用
- `EngineHost` 在钩子安装失败时不崩溃、返回可读错误

---

## 6. 本轮不做什么

- 不做 Windows 实机验证（Phase 4）
- 不做安装程序与自动更新
- 不新增界面页面

---

## 7. 额度不足时的降级路径

优先保证顺序：

1. `ConfigService` + 各页 VM 双向绑定 + 配置落盘（核心）
2. `EngineHost` 组装与生命周期
3. 托盘

托盘可留到下一轮，但必须保证已完成部分编译通过、测试全绿、**无头渲染不回归**。

---

## 8. 交付说明的输出纪律

- 首行给结论（成功/失败 + 关键数字）
- 报错用陈述句，附原始报错文本
- 禁止开场白、客套收尾、emoji
- **禁止声称未实际执行的构建、测试或渲染结果**

---

## 9. 交付检查清单

```
- [x] 任务X: 检查内容 → 操作方法 → 实际结果
- [ ] 未验证项 → 原因
```

必填：
- [ ] `dotnet build -c Release` 实际输出
- [ ] `dotnet test` 实际输出（通过数/总数）
- [ ] **无头渲染回归结果**（是否输出「已提交 6 张截图」）
- [ ] 四项断言实际结果
- [ ] 配置落盘路径与防抖策略说明
- [ ] 托盘在无头模式下的跳过方式
- [ ] 未完成项及原因
