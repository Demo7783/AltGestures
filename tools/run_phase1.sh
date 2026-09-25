#!/bin/sh
# AltGestures Phase 1 派发脚本（小言统筹，Codex 执行）
# 用法：sh tools/run_phase1.sh
# 日志：/opt/data/logs/altgestures-phase1.log

LOG=/opt/data/logs/altgestures-phase1.log
mkdir -p /opt/data/logs

{
  echo "=== START $(date -u '+%F %T') UTC ==="
  docker exec -w /workspace/altgestures codex-cli \
    codex exec --skip-git-repo-check --sandbox danger-full-access \
    "读取仓库根目录 AGENTS.md 与 specs/PHASE1-SPEC.md，严格按 SPEC 全部规格实现 Phase 1（把 WGestures 手势核心移植到 net8.0）。禁止修改 reference/ 与 specs/ 目录；禁止翻译 reference/altdrag 的 C 代码。完成后按 SPEC 第 11 节填写交付检查清单，并附真实命令输出。"
  rc=$?
  echo "=== END $(date -u '+%F %T') UTC rc=$rc ==="
} >> "$LOG" 2>&1
