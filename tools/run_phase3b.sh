#!/bin/sh
# AltGestures Phase 3B 派发脚本（小言统筹，Codex 执行）
# 用法：sh tools/run_phase3b.sh
# 日志：/opt/data/logs/altgestures-phase3b.log

LOG=/opt/data/logs/altgestures-phase3b.log
mkdir -p /opt/data/logs

{
  echo "=== START $(date -u '+%F %T') UTC ==="
  docker exec -w /workspace/altgestures codex-cli \
    codex exec --skip-git-repo-check --sandbox danger-full-access \
    "读取 AGENTS.md、docs/UI.md、docs/prototypes/settings.html 与 specs/PHASE3B-SPEC.md，严格按 SPEC 实现 Phase 3B（导航图标修订 + 版本号移位 + 六页对齐原型）。本次为一次性非交互任务：SPEC 第 0 节的三分类在交付说明开头输出即可，输出后立即继续实现，禁止停下等待确认。三个任务：A 六个导航图标严格用 SPEC 第 2 节的 Path 原值替换（不要自行美化，图形已放大验证过）；B 版本号从 MainWindow 标题栏删除、改到关于页并从程序集读取（csproj 定义 Version）；C 六页对照原型补齐控件与分组。完成后必须重新执行无头渲染命令更新 artifacts/screenshots 下六张 PNG。硬约束：net8.0、禁 emoji、无外链、不得修改 AltGestures.Core。禁止修改 reference/ 与 specs/ 目录。完成后按 SPEC 第 7 节填写交付检查清单，并附真实命令输出与 PNG 尺寸字节数。"
  rc=$?
  echo "=== END $(date -u '+%F %T') UTC rc=$rc ==="
} >> "$LOG" 2>&1
