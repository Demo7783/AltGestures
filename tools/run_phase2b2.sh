#!/bin/sh
# AltGestures Phase 2B2 派发脚本（小言统筹，Codex 执行）
# 用法：sh tools/run_phase2b2.sh
# 日志：/opt/data/logs/altgestures-phase2b2.log

LOG=/opt/data/logs/altgestures-phase2b2.log
mkdir -p /opt/data/logs

{
  echo "=== START $(date -u '+%F %T') UTC ==="
  docker exec -w /workspace/altgestures codex-cli \
    codex exec --skip-git-repo-check --sandbox danger-full-access \
    "读取仓库根目录 AGENTS.md 与 specs/PHASE2B2-SPEC.md，严格按 SPEC 实现 Phase 2B2（全局热键 + 窗口操作触发键改为配置驱动）。本次为一次性非交互任务：SPEC 第 0 节的三分类在交付说明开头输出即可，输出后立即继续实现，禁止停下等待确认。重点：InputArbiter 当前把 Alt 硬编码为触发条件，须改为从配置读取触发键集合（默认左右 Alt，支持左右 Ctrl/Shift/Win），但仲裁语义与归属锁定逻辑不得改变。目标框架保持 net8.0 且必须在当前 Linux 环境编译通过；禁止 System.Windows.Forms / WindowsInput / CsWin32；P/Invoke 只能写在 Interop/。禁止修改 reference/ 与 specs/ 目录；禁止翻译 reference/altdrag 的 C 代码。完成后按 SPEC 第 9 节填写交付检查清单，并附真实命令输出。"
  rc=$?
  echo "=== END $(date -u '+%F %T') UTC rc=$rc ==="
} >> "$LOG" 2>&1
