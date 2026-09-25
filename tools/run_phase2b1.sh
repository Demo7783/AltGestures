#!/bin/sh
# AltGestures Phase 2B1 派发脚本（小言统筹，Codex 执行）
# 用法：sh tools/run_phase2b1.sh
# 日志：/opt/data/logs/altgestures-phase2b1.log

LOG=/opt/data/logs/altgestures-phase2b1.log
mkdir -p /opt/data/logs

{
  echo "=== START $(date -u '+%F %T') UTC ==="
  docker exec -w /workspace/altgestures codex-cli \
    codex exec --skip-git-repo-check --sandbox danger-full-access \
    "读取仓库根目录 AGENTS.md 与 specs/PHASE2B1-SPEC.md，严格按 SPEC 全部规格实现 Phase 2B1（手势头：鼠标键盘钩子 + 路径追踪器 + 屏幕边缘检测）。本次为一次性非交互任务：SPEC 第 0 节的三分类在交付说明开头输出即可，输出后立即继续实现，禁止停下等待确认。关键约束：目标框架必须保持 net8.0 且必须在当前 Linux 环境编译通过；禁止 System.Windows.Forms、禁止 WindowsInput 第三方库、禁止在 Windows/ 目录写 DllImport（P/Invoke 只在 Interop/）。注意 SPEC 第 5 节的性能纪律与第 10 节的额度降级路径。禁止修改 reference/ 与 specs/ 目录；禁止翻译 reference/altdrag 的 C 代码。完成后按 SPEC 第 12 节填写交付检查清单，并附真实命令输出。"
  rc=$?
  echo "=== END $(date -u '+%F %T') UTC rc=$rc ==="
} >> "$LOG" 2>&1
