# Phase 2B2 规格：全局热键与可配置触发键

> 执行方先读仓库根目录 `AGENTS.md`，其约束优先于本规格。本阶段任务量小，请一次做完。

---

## 0. 前置三分类（输出后立即开工，禁止等待确认）

> **本次是一次性非交互任务，没有人会中途回复你。** 三分类写在交付说明**开头**，输出后**立即继续实现**，不要停下等待确认。

1. **已确认事实** 2. **待验证假设** 3. **当前决定**

---

## 1. 一句话目标

补齐全局热键能力，并把窗口操作的**触发键从硬编码改为配置驱动**（支持左右 Alt/Ctrl/Shift/Win 区分）。

**硬约束不变：TFM 保持 `net8.0`，必须在 Linux 上可编译可测试。**

---

## 2. 交付物

```
src/AltGestures.Core/
  Input/
    GlobalHotKeyManager.cs      RegisterHotKey / UnregisterHotKey 封装
    HotKey.cs                   热键定义（修饰键 + 虚拟键码）与解析
  Configuration/
    AppConfig.cs                （扩展）新增 TriggerKeys / PauseResumeHotKey 字段
  Arbitration/
    InputArbiter.cs             （修改）触发键改为从配置读取
```

---

## 3. 需求

### 3.1 可配置触发键（重点）

当前 `InputArbiter` 把「Alt 按下」硬编码为窗口操作分支的触发条件。改为**从配置读取一组触发键**：

- 默认值：`VK_LMENU`（左 Alt，0xA4）+ `VK_RMENU`（右 Alt，0xA5）
- 支持配置为：左右 Alt / 左右 Ctrl / 左右 Shift / 左右 Win，可多选
- 判断方式：任一配置的触发键处于按下状态即视为「窗口操作分支激活」
- **仲裁语义不得改变**：触发键按下走窗口分支、否则转发手势分支、归属在按下瞬间锁定

### 3.2 全局热键管理器

- 封装 `RegisterHotKey` / `UnregisterHotKey`
- 支持注册「暂停/恢复」热键（默认 `Ctrl+Alt+P`，可配置、可清空）
- 热键触发时**在注册线程**回调（`WM_HOTKEY` 语义），回调内不做重活
- 生命周期正确：重复注册需先注销；`Dispose` 时注销全部

### 3.3 热键定义解析

`HotKey` 需支持从字符串解析（如 `"Ctrl+Alt+P"`）与格式化回字符串：

- 修饰键：`Ctrl` / `Alt` / `Shift` / `Win`（大小写不敏感）
- 主键：单个字母、数字或已支持的 `VirtualKeyCode` 名称
- 非法输入返回失败结果（不抛异常），失败原因可读

---

## 4. 测试（必须能在 Linux 上跑通，≥ 10 条新增）

1. `HotKey.TryParse("Ctrl+Alt+P")` → 修饰键与主键正确
2. `HotKey.TryParse("ctrl+shift+f1")` → 大小写不敏感
3. `HotKey.TryParse("P")` → 无修饰键
4. `HotKey.TryParse("Ctrl+")` / `"Foo+Bar"` / 空串 → 返回失败且原因可读
5. `HotKey.ToString()` 往返一致
6. 触发键解析：配置 `Alt` → 含 `VK_LMENU` 与 `VK_RMENU`
7. 触发键解析：配置 `Ctrl` → 含 `VK_LCONTROL` 与 `VK_RCONTROL`
8. 触发键解析：多选（`Alt` + `Ctrl`）→ 四种键码齐全
9. 触发键解析：非法名称 → 返回失败
10. `AppConfig` JSON 往返：含 `TriggerKeys` 与 `PauseResumeHotKey` 的配置序列化/反序列化相等
11. 配置缺省时 `TriggerKeys` 默认为左右 Alt
12. `GlobalHotKeyManager` 重复 `Dispose` 不抛异常

---

## 5. 硬约束

1. `<TargetFramework>net8.0</TargetFramework>`
2. 禁止 `System.Windows.Forms` / `WindowsInput` / CsWin32
3. **P/Invoke 一律加到 `Interop/` 已有文件中**
4. 允许修改 `InputArbiter` 的触发键判断（改为配置驱动），但**不得改动仲裁语义与归属锁定逻辑**
5. 不得修改 `PathTracker.cs` / `MouseKeyboardHook.cs` / `GestureParser.cs`
6. 钩子与热键回调链上禁止 IO / 进程启动 / 锁等待
7. `Nullable enable`，`TreatWarningsAsErrors`，中文 XML 注释，无死代码

---

## 6. 本轮不做什么

- 不做 UI（Phase 3）
- 不做配置界面交互
- 不做 Windows 实机验证（Phase 4，统一做）

---

## 7. 验收标准

1. `cd src && dotnet build -c Release` → **0 error，0 warning**
2. `dotnet test` → **全部通过**（现有 75 条 + 新增 ≥ 10 条）
3. 断言命令：
   ```bash
   grep -rn "System.Windows.Forms" src/AltGestures.Core/ | wc -l        # 期望 0
   grep -rn "net8.0-windows" src/AltGestures.Core/*.csproj | wc -l      # 期望 0
   grep -rn "DllImport" src/AltGestures.Core/Input/ src/AltGestures.Core/Arbitration/ | wc -l   # 期望 0
   grep -rn "VK_LMENU\|VK_RMENU" src/AltGestures.Core/Arbitration/InputArbiter.cs | wc -l        # 期望 0（不得硬编码，须走配置）
   ```
   最后一条是本阶段的重点断言：触发键必须来自配置，`InputArbiter` 内不得再出现硬编码键码。

---

## 8. 交付说明的输出纪律

- 首行给结论（成功/失败 + 关键数字）
- 报错用陈述句，附原始报错文本
- 禁止开场白、客套收尾、emoji
- **禁止声称未实际执行的构建或测试结果**

---

## 9. 交付检查清单

```
- [x] 任务X: 检查内容 → 操作方法 → 实际结果
- [ ] 未验证项 → 原因
```

必填：
- [ ] `dotnet build -c Release` 实际输出
- [ ] `dotnet test` 实际输出（通过数/总数）
- [ ] 四项 grep 断言实际结果
- [ ] `InputArbiter` 的改动说明（改了哪几行、仲裁语义为何未变）
- [ ] 未完成项及原因
