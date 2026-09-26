#!/bin/sh
# AltGestures Phase 3A 派发脚本（小言统筹，Codex 执行）
# 用法：sh tools/run_phase3a.sh
# 日志：/opt/data/logs/altgestures-phase3a.log

LOG=/opt/data/logs/altgestures-phase3a.log
mkdir -p /opt/data/logs

{
  echo "=== START $(date -u '+%F %T') UTC ==="
  docker exec -w /workspace/altgestures codex-cli \
    codex exec --skip-git-repo-check --sandbox danger-full-access \
    "读取仓库根目录 AGENTS.md、docs/UI.md 与 specs/PHASE3A-SPEC.md，严格按 SPEC 实现 Phase 3A（Avalonia 应用骨架 + 无头渲染截图流水线）。本次为一次性非交互任务：SPEC 第 0 节的三分类在交付说明开头输出即可，输出后立即继续实现，禁止停下等待确认。技术选型已定：Avalonia 12.1.3 + FluentTheme + Avalonia.Headless + Avalonia.Skia（实测这些包均可从 NuGet 拉取），目标框架必须 net8.0。本阶段验收核心是【无头渲染流水线跑通并产出 6 张非空白 PNG】，优先级高于页面内容完整度；若额度不足按 SPEC 第 10 节降级。视觉 token 严格照 docs/UI.md 原值，禁 emoji、图标用内联 SVG Path、界面文案简体中文、无外链资源。禁止修改 AltGestures.Core 下任何文件；禁止修改 reference/ 与 specs/ 目录。完成后按 SPEC 第 12 节填写交付检查清单，并附真实命令输出与 PNG 尺寸/字节数。"
  rc=$?
  echo "=== END $(date -u '+%F %T') UTC rc=$rc ==="
} >> "$LOG" 2>&1
