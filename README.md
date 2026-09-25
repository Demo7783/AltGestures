# AltGestures

> Windows 鼠标增强工具：**按住 Alt 拖拽窗口** + **按住右键画手势**，一套底层钩子、一个进程、一份配置。

把 [AltDrag](https://github.com/stefansundin/altdrag) 的窗口拖拽/缩放/吸附能力和 [WGestures](https://github.com/yingDev/WGestures) 的鼠标手势能力合并进同一个程序。

装两个软件时，系统输入链路上挂着两套全局鼠标钩子，每次鼠标事件都要串行过两遍，延迟叠加、规则互相打架；合并后只有一个钩子 + 一个仲裁状态机：**按 Alt 走窗口操作，不按 Alt 走手势**，边界由程序自己判定。

## 功能特性

### 窗口操作（按住 Alt + 鼠标）

| 能力 | 说明 |
| --- | --- |
| 移动窗口 | Alt + 左键在窗口任意位置拖拽 |
| 缩放窗口 | Alt + 中键/右键拖拽，按光标最近的边或角缩放 |
| 点击型动作 | 关闭 / 最小化 / 居中 / 置顶 / 下沉，按下即执行 |
| 滚轮动作 | Alt + 滚轮：切换窗口 / 调节音量 / 调节透明度 |
| 自动吸附 | 拖到屏幕边界、其他窗口外侧/内侧时吸附（阈值可调） |
| 双击 | Alt + 左键双击 = 最大化/还原；Alt + 右键双击 = 按光标方向移到角落 |
| 修饰键 | Shift 吸附 / Ctrl 提升到前台 / 空格临时关闭吸附 |
| 滚动非活动窗口 | 不激活窗口也能滚动其内容（Shift 水平滚动） |

### 鼠标手势（不按修饰键，按住右键画）

| 能力 | 说明 |
| --- | --- |
| 手势识别 | 8 方向组合，容错识别 |
| 多键触发 | 右键 / 中键 / 侧键，各自独立配色 |
| 轨迹绘制 | 手势轨迹实时绘制，淡出，可显示命令名 |
| 命令系统 | 发送热键、发送文本、打开文件、打开网址、命令行、脚本、网页搜索、任务切换、窗口控制、音量、搜索框 |
| 按应用适配 | 为不同程序绑定不同手势集 |
| 其他 | 屏幕四角热角、屏幕边缘滚动、起始停留超时防误触、全屏模式禁用、暂停/恢复热键 |

### 通用

- 统一的进程/窗口黑名单规则引擎
- 单一配置文件，改动热重载，不需要重启
- 常驻无感：内存占用与 CPU 占用为设计指标（见下）

## 环境要求

- Windows 10 / 11（x64）
- 运行：[.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0)
- 构建：.NET 8 SDK

## 构建

```bash
git clone https://github.com/Demo7783/AltGestures.git
cd AltGestures
dotnet build -c Release
```

产物在 `src/AltGestures.App/bin/Release/`。

## 使用

1. 运行 `AltGestures.exe`，托盘出现图标。
2. 按住 **Alt** 拖拽任意窗口即可移动；Alt + 中键/右键拖拽可缩放。
3. 按住 **右键** 在屏幕上画轨迹，松手执行绑定的动作。
4. 托盘图标右键 → 设置，配置动作分配、手势绑定、黑名单等。

配置文件默认位置：`%APPDATA%\AltGestures\config.json`

## 性能设计目标

| 指标 | 目标 |
| --- | --- |
| 常驻内存 | < 25 MB |
| 待机 CPU | 0%（不监听鼠标移动，仅在触发模式下采点） |
| 钩子回调 | 只记录坐标与状态位，重活全部移交工作线程 |
| 鼠标操作手感 | 零可感延迟 |

钩子回调运行在系统输入链路上，超时会被 Windows 静默摘除，因此回调内**严禁**做轨迹识别、动作执行等重活。

## 项目结构

```
src/
  AltGestures.Core/     核心：钩子、状态机、手势识别、命令系统、配置（跨平台可编译）
  AltGestures.App/      界面与托盘
  AltGestures.Tests/    单元测试
docs/                   设计与验收文档
specs/                  各阶段实现规格
reference/              上游参考源码（WGestures / AltDrag，只读）
```

## 架构要点

- **单一仲裁状态机**：鼠标键按下瞬间判定归属（Alt 分支 / 手势分支），中途锁定不切换。
- **核心层与界面分离**：`AltGestures.Core` 不依赖任何 UI 框架，可在 Linux 上编译并跑单元测试。
- **AltDrag 部分为独立实现**：仅参考其行为与边界条件，不复制其源码（许可证不兼容，见下）。

## 致谢

- [WGestures](https://github.com/yingDev/WGestures) by Ying Yuandong — 手势功能基于其核心改造而来。
- [AltDrag](https://github.com/stefansundin/altdrag) by Stefan Sundin — 窗口拖拽功能的行为参考。

## 许可证

[GPL-2.0](./LICENSE)

本项目为 WGestures（GPL-2.0）的衍生作品，整体以 GPL-2.0 发布。窗口拖拽部分为独立实现，未复制 AltDrag（GPL-3.0）源码。
