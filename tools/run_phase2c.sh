#!/bin/sh
# AltGestures Phase 2C 派发脚本（小言统筹，Codex 执行）
# 用法：sh tools/run_phase2c.sh
# 日志：/opt/data/logs/altgestures-phase2c.log

LOG=/opt/data/logs/altgestures-phase2c.log
mkdir -p /opt/data/logs

{
  echo "=== START $(date -u '+%F %T') UTC ==="
  docker exec -w /workspace/altgestures codex-cli \
    codex exec --skip-git-repo-check --sandbox danger-full-access \
    "读取仓库根目录 AGENTS.md 与 specs/PHASE2C-SPEC.md，严格按 SPEC 全部规格实现 Phase 2C（窗口操作 + 统一仲裁状态机）。本次为一次性非交互任务：SPEC 第 0 节的三分类在交付说明开头输出即可，输出后立即继续实现，禁止停下等待确认。两条最关键约束：①禁止翻译 reference/altdrag 的 C 代码（GPL-3.0 不兼容），只参考其行为、基于通用 Win32 API 自主实现；②仲裁归属必须在鼠标键按下瞬间判定并锁定（InputArbiter 实现 IMouseKeyboardHook 作为中间人，PathTracker 内部逻辑不得修改）。目标框架保持 net8.0 且必须在当前 Linux 环境编译通过；禁止 System.Windows.Forms / WindowsInput / CsWin32；P/Invoke 只能写在 Interop/。注意 SPEC 第 6 节可测性要求（几何与状态机必须与 Win32 解耦）与第 9 节额度降级路径。禁止修改 reference/ 与 specs/ 目录。完成后按 SPEC 第 12 节填写交付检查清单，并附真实命令输出。"
  rc=$?
  echo "=== END $(date -u '+%F %T') UTC rc=$rc ==="
} >> "$LOG" 2>&1
