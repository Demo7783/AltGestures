#!/bin/sh
# AltGestures Phase 3D 派发脚本（管理员权限支持）
# 用法：sh tools/run_phase3d.sh
# 日志：/opt/data/logs/altgestures-phase3d.log

LOG=/opt/data/logs/altgestures-phase3d.log
mkdir -p /opt/data/logs

{
  echo "=== START $(date -u '+%F %T') UTC ==="
  docker exec -w /workspace/altgestures codex-cli \
    codex exec --skip-git-repo-check --sandbox danger-full-access \
    "读取仓库根目录 AGENTS.md 与 specs/PHASE3D-SPEC.md，严格按 SPEC 实现 Phase 3D（管理员权限支持）。本次为一次性非交互任务：SPEC 第 0 节的三分类在交付说明开头输出即可，输出后立即继续实现，禁止停下等待确认。三块：①ElevationService 用 P/Invoke 检测管理员权限并用 ShellExecuteEx 的 runas verb 以管理员身份重启自身（用户取消 UAC 即错误码 1223 时必须保持当前实例继续运行）；②AutoStartService 按配置增删注册表 HKCU Run 项（值名为 AltGestures，路径带引号）；③通用页新增「权限」分组显示当前权限状态与「以管理员身份重启」按钮。硬约束：TFM 保持 net8.0、P/Invoke 只能在 Interop/、非 Windows 平台所有提权与注册表操作必须安全降级为 no-op 不抛异常、ALTGESTURES_HEADLESS=1 时不得触发任何提权或注册表操作。自启动提权必须防死循环（已提权绝不再重启）。不得修改 AltGestures.Core 既有语义，不得回退 Phase 3C 的修复。完成后必须验证：build 0 error、test 95+ 全绿、无头渲染仍输出『已提交 6 张截图』。禁止修改 reference/ 与 specs/ 目录。完成后按 SPEC 第 10 节填写交付检查清单并附真实命令输出。"
  rc=$?
  echo "=== END $(date -u '+%F %T') UTC rc=$rc ==="
} >> "$LOG" 2>&1
