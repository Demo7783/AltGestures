#!/bin/sh
# AltGestures Phase 2A 派发脚本（小言统筹，Codex 执行）
# 用法：sh tools/run_phase2a.sh
# 日志：/opt/data/logs/altgestures-phase2a.log

LOG=/opt/data/logs/altgestures-phase2a.log
mkdir -p /opt/data/logs

{
  echo "=== START $(date -u '+%F %T') UTC ==="
  docker exec -w /workspace/altgestures codex-cli \
    codex exec --skip-git-repo-check --sandbox danger-full-access \
    "读取仓库根目录 AGENTS.md 与 specs/PHASE2A-SPEC.md，严格按 SPEC 全部规格实现 Phase 2A（Windows 互操作基础层：P/Invoke 声明 + 输入模拟 + 显示器信息）。本次为一次性非交互任务：SPEC 第 0 节的三分类在交付说明开头输出即可，输出后立即继续实现，禁止停下等待确认。关键约束：目标框架必须保持 net8.0 且必须在当前 Linux 环境编译通过，禁止改成 net8.0-windows、禁止引入 CsWin32、禁止引用 System.Windows.Forms。禁止修改 reference/ 与 specs/ 目录；禁止翻译 reference/altdrag 的 C 代码。完成后按 SPEC 第 12 节填写交付检查清单，并附真实命令输出。"
  rc=$?
  echo "=== END $(date -u '+%F %T') UTC rc=$rc ==="
} >> "$LOG" 2>&1
