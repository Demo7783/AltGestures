# 更新日志

本项目遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

## [Unreleased]

### 新增

- Phase 2A Windows 互操作基础层：手写 P/Invoke、输入模拟、修饰键状态与多显示器信息
- Phase 2A 测试：结构体布局、常量、修饰键解析与输入构造的 Linux 回归测试
- Phase 1 手势核心：WGestures 数据模型、方向识别、意图查找与 JSON 存储移植到 .NET 8
- Phase 1 测试：路径测试桩与 12 个手势识别、相等性与序列化回归测试
- 项目立项：合并 AltDrag 窗口操作与 WGestures 鼠标手势为单一程序
- 仓库骨架：README / AGENTS / LICENSE(GPL-2.0) / 目录结构

### 说明

- 上游基座：WGestures v1（C#，GPL-2.0）、AltDrag v1.1（C，GPL-3.0，仅行为参考）
