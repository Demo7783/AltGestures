// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using AltGestures.Core.Commands;

namespace AltGestures.Core;

/// <summary>手势、命令与应用配置的集合。</summary>
[Serializable]
public class GestureIntent
{
    public Gesture Gesture { get; set; } = new();
    public AbstractCommand Command { get; set; } = new DoNothingCommand();
    public bool ExecuteOnModifier { get; set; }

    public bool CanExecuteOnModifier()
    {
        return Gesture.Modifier != GestureModifier.None && ExecuteOnModifier;
    }

    public string Name { get; set; } = string.Empty;

    public ExecutionResult Execute(GestureContext context, GestureParser gestureParser)
    {
        if (Command is IGestureContextAware contextAware)
        {
            contextAware.Context = context;
        }

        if (Command is IGestureParserAware parserAware)
        {
            parserAware.Parser = gestureParser;
        }

        if (Command is INeedInit shouldInit && !shouldInit.IsInitialized)
        {
            shouldInit.Init();
        }

        try
        {
            context.ActivateTargetWindow();
            Command.Execute();
        }
        catch (Exception)
        {
        }

        return new ExecutionResult(null, true);
    }

    public class ExecutionResult
    {
        public Exception? Exception { get; }
        public bool IsOk { get; }

        public ExecutionResult(Exception? exception, bool isOk)
        {
            Exception = exception;
            IsOk = isOk;
        }
    }
}

[Serializable]
public abstract class AbstractApp
{
    public string Name { get; set; } = "Noname";
    public GestureIntentDict GestureIntents { get; set; } = new();
    public bool IsGesturingEnabled { get; set; } = true;

    public virtual GestureIntent? Find(Gesture key)
    {
        GestureIntents.TryGetValue(key, out var val);
        return val;
    }

    public virtual void Add(GestureIntent intent) => GestureIntents.Add(intent);
    public virtual void Remove(GestureIntent intent) => GestureIntents.Remove(intent);

    public virtual void Import(AbstractApp from)
    {
        IsGesturingEnabled = from.IsGesturingEnabled;
        Name = from.Name;
        ImportGestures(from);
    }

    public virtual void ImportGestures(AbstractApp from)
    {
        foreach (var kv in from.GestureIntents)
        {
            GestureIntents.AddOrReplace(kv.Value);
        }
    }
}

[Serializable]
public class GestureIntentDict : Dictionary<Gesture, GestureIntent>
{
    public void Add(GestureIntent intent) => Add(intent.Gesture, intent);
    public void Remove(GestureIntent intent) => Remove(intent.Gesture);
    public void AddOrReplace(GestureIntent intent) => this[intent.Gesture] = intent;

    public void Import(GestureIntentDict from)
    {
        foreach (var kv in from)
        {
            this[kv.Key] = kv.Value;
        }
    }
}

[Serializable]
public class ExeApp : AbstractApp
{
    public bool InheritGlobalGestures { get; set; } = true;
    public string ExecutablePath { get; set; } = string.Empty;

    public override void Import(AbstractApp from)
    {
        if (from is ExeApp asExeApp)
        {
            InheritGlobalGestures = asExeApp.InheritGlobalGestures;
        }
        base.Import(from);
    }
}

[Serializable]
public class GlobalApp : AbstractApp
{
    public GlobalApp() => Name = "(Global)";
}
