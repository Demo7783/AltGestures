// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using AltGestures.Core.Persistence;

namespace AltGestures.Core;

public interface IGestureIntentFinder
{
    bool IsGesturingEnabledForContext(GestureContext context, out ExeApp? app);
    GestureIntent? Find(Gesture gesture, GestureContext context);
    GestureIntent? Find(Gesture gesture, ExeApp? inApp);
    IGestureIntentStore IntentStore { get; }
}
