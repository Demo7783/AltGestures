#!/bin/sh
# AltGestures Phase 3C 派发脚本（小言统筹，Codex 执行）
# 用法：sh tools/run_phase3c.sh
# 日志：/opt/data/logs/altgestures-phase3c.log

LOG=/opt/data/logs/altgestures-phase3c.log
mkdir -p /opt/data/logs

{
  echo "=== START $(date -u '+%F %T') UTC ==="
  docker exec -w /workspace/altgestures codex-cli \
    codex exec --skip-git-repo-check --sandbox danger-full-access \
    "读取 AGENTS.md、docs/UI.md、docs/DESIGN.md 与 specs/PHASE3C-SPEC.md，严格按 SPEC 实现 Phase 3C（配置双向绑定 + 系统托盘 + 应用组装）。本次为一次性非交互任务：SPEC 第 0 节的三分类在交付说明开头输出即可，输出后立即继续实现，禁止停下等待确认。三块：①各页 VM 与 AppConfig 双向绑定，改动立即生效并防抖落盘到 %APPDATA%/AltGestures/config.json（原子写盘）；②托盘图标与菜单，且必须在 ALTGESTURES_HEADLESS=1 时跳过托盘初始化（否则会破坏无头渲染流水线）；③EngineHost 按配置组装钩子/仲裁/手势/窗口分支并管理生命周期，退出时正确卸载，回调链上禁止 IO/UI/锁等待。硬约束：net8.0、不得修改 AltGestures.Core 既有语义（可扩展）、禁 emoji、无外链。必须验证无头渲染未回归（dotnet run --project src/AltGestures.App.Headless -c Release 应输出「已提交 6 张截图」）。禁止修改 reference/ 与 specs/ 目录。完成后按 SPEC 第 9 节填写交付检查清单，并附真实命令输出。"
  rc=$?
  echo "=== END $(date -u '+%F %T') UTC rc=$rc ==="
} >> "$LOG" 2>&1
