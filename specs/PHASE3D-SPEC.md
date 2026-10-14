# Phase 3D 规格：管理员权限支持

> 执行方先读 `AGENTS.md`、`docs/UI.md`。本阶段是新增功能，**不要动 Phase 3C 已交付且通过验收的部分**。

---

## 0. 前置三分类（输出后立即开工，禁止等待确认）

> **本次是一次性非交互任务，没有人会中途回复你。** 三分类写在交付说明**开头**，输出后**立即继续实现**，不要停下等待确认。

1. **已确认事实** 2. **待验证假设** 3. **当前决定**

---

## 1. 一句话目标

让程序能以**管理员权限**运行，从而能够操作以管理员身份运行的程序窗口（突破 Windows 的 UIPI 限制）；并提供开机自启与自启动提权（**注册表方式**，用户已确认）。

---

## 2. 背景

Windows 的 UIPI（用户界面特权隔离）禁止普通权限进程向高完整性级别窗口发送输入。因此当前版本**拖不动以管理员身份运行的程序**（如任务管理器）。AltDrag 的解法是提供"以管理员身份运行"能力，本阶段对齐该能力。

**用户已明确选择注册表方式**（而非计划任务），接受"每次登录弹一次 UAC"的代价。

---

## 3. 交付物

```
src/AltGestures.Core/
  Interop/Shell32.cs            （补充 IsUserAnAdmin / ShellExecuteEx 声明，若缺）
  Windows/ElevationState.cs     （权限状态判定，纯逻辑可测）
src/AltGestures.App/
  Services/ElevationService.cs  权限检测 + 以管理员身份重启自身
  Services/AutoStartService.cs  注册表 Run 项读写
  ViewModels/Pages/GeneralPageViewModel.cs  （补权限状态属性与重启命令）
  Views/Pages/GeneralPage.axaml             （补「权限」分组）
```

---

## 4. 需求

### 4.1 权限检测

- `ElevationService.IsElevated`（bool）
- 实现用 P/Invoke：`shell32.dll` 的 `IsUserAnAdmin()`，或等价的 token 完整性级别检查
- **非 Windows 平台返回 `false` 且不抛异常**（本项目的 Core 要能在 Linux 编译与测试）

### 4.2 以管理员身份重启

- `ElevationService.RestartElevated()` → 返回结果（成功 / 用户取消 / 失败原因）
- 实现：`ShellExecuteEx` + `lpVerb = "runas"` 启动**当前 exe**
- 用户批准 UAC → 新实例以管理员权限启动 → **当前实例退出**
- 用户拒绝 UAC → `ShellExecuteEx` 失败（`GetLastError() == 1223`，`ERROR_CANCELLED`）→ **保持当前实例继续运行**，不得崩溃、不得静默退出
- 不得因重启逻辑产生"无人值守的实例堆积"

### 4.3 自启动提权（注册表方式）

- 启动时若 `AppConfig.StartAsAdministrator == true` **且**当前未提权 → 自动调用 `RestartElevated()`
- **必须防死循环**：已提权时绝不再重启；重启前要有一次性标记，避免新实例再次触发
- 行为与 AltDrag 一致：每次登录会弹一次 UAC

### 4.4 自启动注册（注册表 Run）

- `AutoStartService`：按 `AppConfig.StartWithWindows` 增删
  `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run` 下名为 `AltGestures` 的值
- 值内容为**带引号的 exe 完整路径**（路径含空格时必须）
- 关闭开关时删除该值；值不存在时删除不报错
- 非 Windows 平台：**no-op**，不抛异常

### 4.5 界面（「通用」页新增「权限」分组）

- 显示当前权限状态：`普通权限` / `管理员权限`
- 按钮「以管理员身份重启」（已提权时禁用）
- 说明文字（简洁、中文）：以管理员权限运行后，可操作以管理员身份运行的程序窗口；开启自启动提权后，每次登录会弹一次系统授权确认
- 保留既有 `StartAsAdministrator` 开关——本阶段让它真正生效

---

## 5. 硬约束

1. `<TargetFramework>net8.0</TargetFramework>`，不得改为 `net8.0-windows`
2. **P/Invoke 只能写在 `Interop/` 目录**
3. 非 Windows 平台所有提权/注册表操作**必须安全降级**：no-op 或返回 false，绝不抛异常
4. 不得破坏无头渲染流水线（`ALTGESTURES_HEADLESS=1` 时不得触发任何提权/注册表操作）
5. 不得修改 `AltGestures.Core` 既有语义；不得回退 Phase 3C 的修复
6. 禁 emoji、中文文案、无死代码、`Nullable enable`、`TreatWarningsAsErrors`

---

## 6. 测试（Linux 可跑，≥ 6 条新增）

将下列逻辑抽成**不依赖 Win32 的纯函数/纯类**以便测试：

1. 非 Windows 平台：`IsElevated` 为 false 且不抛异常
2. `ShellExecuteEx` 的参数构造：`lpVerb == "runas"`、`lpFile` 为 exe 全路径、`cbSize` 正确
3. 错误码映射：`1223` → 「用户取消了授权」，其他 → 可读失败原因
4. 注册表值构造：路径带引号；值名恰为 `AltGestures`
5. **防死循环**：已提权时不触发重启；带"已尝试提权"标记时不再尝试
6. 非 Windows 平台：`AutoStartService` 增删均为 no-op 且不抛异常
7. 配置往返：`StartWithWindows` / `StartAsAdministrator` 变更后落盘正确

---

## 7. 验收标准

1. `cd src && dotnet build -c Release` → **0 error，0 warning**
2. `dotnet test` → **全部通过**（现有 95 条不得回归），新增 **≥ 6 条**
3. 无头渲染：`dotnet run --project src/AltGestures.App.Headless -c Release` → 仍输出「已提交 6 张截图」
4. 断言：
   ```bash
   ls src/AltGestures.App/Services/            # 期望含 ElevationService.cs 与 AutoStartService.cs
   grep -rl "管理员" src/AltGestures.App/Views/Pages/GeneralPage.axaml | wc -l    # 期望 1
   grep -rn "IsUserAnAdmin\|ShellExecuteEx" src/AltGestures.Core/Interop/ | wc -l # ≥ 1（P/Invoke 在 Interop）
   grep -rn "DllImport" src/AltGestures.App/ | wc -l                              # 期望 0
   ```

---

## 8. 本轮不做什么

- 不做计划任务方式（用户已选注册表）
- 不做自动更新
- **不做 Windows 实机验证**（留 Phase 4，与核心功能一起测）

---

## 9. 交付说明的输出纪律

- 首行给结论（成功/失败 + 关键数字）
- 报错用陈述句，附原始报错文本
- 禁止开场白、客套收尾、emoji
- **禁止声称未实际执行的构建、测试或渲染结果**

---

## 10. 交付检查清单

```
- [x] 任务X: 检查内容 → 操作方法 → 实际结果
- [ ] 未验证项 → 原因
```

必填：
- [ ] `dotnet build -c Release` 实际输出
- [ ] `dotnet test` 实际输出（通过数/总数）
- [ ] 无头渲染实际输出（是否「已提交 6 张截图」）
- [ ] 四项断言实际结果
- [ ] 提权重启的实现方式与「用户取消 UAC」的处理
- [ ] 防死循环的做法
- [ ] 未完成项及原因
