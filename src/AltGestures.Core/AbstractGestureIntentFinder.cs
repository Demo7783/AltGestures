// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using AltGestures.Core.Persistence;

namespace AltGestures.Core;

/// <summary>提供应用级与全局手势查找算法。</summary>
public abstract class AbstractGestureIntentFinder : IGestureIntentFinder
{
    public IGestureIntentStore IntentStore { get; }

    protected AbstractGestureIntentFinder(IGestureIntentStore intentStore)
    {
        IntentStore = intentStore;
    }

    public bool IsGesturingEnabledForContext(GestureContext context, out ExeApp? foundApp)
    {
        foundApp = GetExeAppByContext(context);
        return foundApp != null ? foundApp.IsGesturingEnabled : IntentStore.GlobalApp.IsGesturingEnabled;
    }

    public GestureIntent? Find(Gesture gesture, GestureContext context)
    {
        return Find(gesture, GetExeAppByContext(context));
    }

    public GestureIntent? Find(Gesture gesture, ExeApp? inApp)
    {
        GestureIntent? found;
        if (inApp != null)
        {
            found = inApp.Find(gesture);
            if (found == null && inApp.InheritGlobalGestures)
            {
                found = IntentStore.GlobalApp.Find(gesture);
            }
        }
        else
        {
            found = IntentStore.GlobalApp.Find(gesture);
        }
        return found;
    }

    public abstract ExeApp? GetExeAppByContext(GestureContext context);
}
