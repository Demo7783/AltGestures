# 更新日志

本项目遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

## [Unreleased]

### 新增

- Phase 3A Avalonia 界面骨架：主窗口、左侧六项导航、视觉 token、六个静态设置页与内联 SVG 图标
- Phase 3A 无头渲染流水线：Skia + Headless 生成六张 960×620 PNG，并校验文件非空白
- Phase 2B2 全局热键：热键解析、专用消息线程、`RegisterHotKey` / `UnregisterHotKey` 封装与暂停/恢复热键配置
- Phase 2B2 可配置窗口触发键：默认左右 Alt，支持左右 Ctrl / Shift / Win 多选，仲裁归属锁定语义保持不变
- Phase 2C 窗口操作：移动、边角缩放、点击型动作、滚轮动作、工作区/窗口吸附、双击最大化与角落移动
- Phase 2C 统一仲裁状态机：鼠标按下瞬间锁定 Alt 窗口分支或手势分支，窗口动作移出低级钩子回调线程
- Phase 2C 配置与测试：窗口动作绑定、几何/吸附/节流纯逻辑、JSON 往返与仲裁归属锁定 Linux 回归测试
- Phase 2B1 手势头：低级鼠标/键盘钩子、路径追踪工作者、屏幕边缘摩擦与热角检测、窗口上下文采集
- Phase 2B1 测试：消息映射、边缘/角落判定、位移阈值、步数上限、生命周期与输入标记的 Linux 回归测试
- 修正 `MOUSEINPUT.ExtraInfo` 为原生指针宽度，保证 Windows x64 输入模拟结构布局与注入标记有效
- Phase 2A Windows 互操作基础层：手写 P/Invoke、输入模拟、修饰键状态与多显示器信息
- Phase 2A 测试：结构体布局、常量、修饰键解析与输入构造的 Linux 回归测试
- Phase 1 手势核心：WGestures 数据模型、方向识别、意图查找与 JSON 存储移植到 .NET 8
- Phase 1 测试：路径测试桩与 12 个手势识别、相等性与序列化回归测试
- 项目立项：合并 AltDrag 窗口操作与 WGestures 鼠标手势为单一程序
- 仓库骨架：README / AGENTS / LICENSE(GPL-2.0) / 目录结构

### 说明

- 上游基座：WGestures v1（C#，GPL-2.0）、AltDrag v1.1（C，GPL-3.0，仅行为参考）
