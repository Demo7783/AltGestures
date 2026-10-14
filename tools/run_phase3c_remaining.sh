#!/bin/sh
# AltGestures Phase 3C 续做派发脚本（额度中断后的重派）
# 用法：sh tools/run_phase3c_remaining.sh
# 日志：/opt/data/logs/altgestures-phase3c-remaining.log
#
# 前置条件：GLM 额度已恢复（首次执行因 5 小时上限中断）
# 执行前建议先确认额度可用，避免再次空转。

LOG=/opt/data/logs/altgestures-phase3c-remaining.log
mkdir -p /opt/data/logs

{
  echo "=== START $(date -u '+%F %T') UTC ==="
  docker exec -w /workspace/altgestures codex-cli \
    codex exec --skip-git-repo-check --sandbox danger-full-access \
    "读取仓库根目录 AGENTS.md 与 specs/PHASE3C-REMAINING.md，按该文件实现 Phase 3C 的剩余部分。本次为一次性非交互任务：SPEC 第 0 节的三分类在交付说明开头输出即可，输出后立即继续实现，禁止停下等待确认。本轮唯一目标是把各页 XAML 控件接到已就绪的 VM 属性上实现双向绑定（VM 层已完成，不要重写）。务必先读 PHASE3C-REMAINING.md 的『当前状态』与『绝对禁止』两节：不要修改 AltGestures.Core、不要回退统筹方已修复的内容（SetConfig 用法、枚举名 X1/X2、托盘事件 +=、EngineHost 的 pauseHotKey）、不要改动 ScreenshotRenderer 的暂存与原子提交逻辑。页面根元素需加 x:DataType（项目开了 CompiledBindings）。完成后必须验证：build 0 error、test 95/95、无头渲染仍输出『已提交 6 张截图』。禁止修改 reference/ 与 specs/ 目录。完成后按 SPEC 第 6 节填写交付检查清单并附真实命令输出。"
  rc=$?
  echo "=== END $(date -u '+%F %T') UTC rc=$rc ==="
} >> "$LOG" 2>&1
