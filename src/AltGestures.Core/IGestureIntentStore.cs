// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using AltGestures.Core.Commands;

namespace AltGestures.Core.Persistence;

public interface IGestureIntentStore : IEnumerable<ExeApp>
{
    GlobalApp GlobalApp { get; }
    bool TryGetExeApp(string key, out ExeApp? found);
    ExeApp GetExeApp(string key);
    AbstractCommand?[] HotCornerCommands { get; set; }
    void Remove(string app);
    void Remove(ExeApp app);
    void Add(ExeApp app);
    void Save();
}
