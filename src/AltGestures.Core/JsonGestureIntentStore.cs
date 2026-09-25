// 基于 WGestures (https://github.com/yingDev/WGestures) 修改
// 原始版权 (C) Ying Yuandong，GPL-2.0

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using AltGestures.Core.Commands;
using AltGestures.Core.Persistence;

namespace AltGestures.Core;

public sealed class JsonGestureIntentStore : IGestureIntentStore
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();
    private string jsonPath;

    public string FileVersion { get; set; }
    public Dictionary<string, ExeApp> Apps { get; set; } = new();
    public GlobalApp GlobalApp { get; set; } = new();
    public AbstractCommand?[] HotCornerCommands { get; set; } = new AbstractCommand?[8];

    private JsonGestureIntentStore()
    {
        jsonPath = string.Empty;
        FileVersion = "3";
    }

    public JsonGestureIntentStore(string jsonPath, string fileVersion = "3")
    {
        this.jsonPath = jsonPath;
        FileVersion = fileVersion;
        if (File.Exists(jsonPath))
        {
            using var file = File.OpenRead(jsonPath);
            Deserialize(file);
        }
    }

    public JsonGestureIntentStore(Stream stream, bool closeStream, string fileVersion = "3")
    {
        jsonPath = string.Empty;
        FileVersion = fileVersion;
        try
        {
            Deserialize(stream);
        }
        finally
        {
            if (closeStream)
            {
                stream.Dispose();
            }
        }
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new GestureConverter(),
                new GestureIntentDictConverter(),
                new AbstractCommandConverter()
            }
        };
    }

    private void Deserialize(Stream stream)
    {
        var result = JsonSerializer.Deserialize<SerializeWrapper>(stream, SerializerOptions)
                     ?? throw new InvalidOperationException("手势配置内容为空。");
        FileVersion = result.FileVersion;
        GlobalApp = result.Global;
        Apps = new Dictionary<string, ExeApp>();
        foreach (var app in result.Apps.Values)
        {
            app.ExecutablePath = app.ExecutablePath.ToLowerInvariant();
            Apps.Add(app.ExecutablePath, app);
        }

        if (FileVersion == "1" || FileVersion == "2")
        {
            var globalIntents = GlobalApp.GestureIntents.Values.ToArray();
            GlobalApp.GestureIntents.Clear();
            foreach (var intent in globalIntents)
            {
                intent.Gesture.GestureButton += 1;
                GlobalApp.GestureIntents.Add(intent);
            }

            foreach (var app in Apps.Values)
            {
                var intents = app.GestureIntents.Values.ToArray();
                app.GestureIntents.Clear();
                foreach (var intent in intents)
                {
                    intent.Gesture.GestureButton += 1;
                    app.GestureIntents.Add(intent);
                }
            }
        }

        HotCornerCommands = new AbstractCommand?[8];
        Array.Copy(result.HotCornerCommands, HotCornerCommands, result.HotCornerCommands.Length);
    }

    bool IGestureIntentStore.TryGetExeApp(string key, out ExeApp? found)
    {
        found = Apps.TryGetValue(key.ToLowerInvariant(), out var app) ? app : null;
        return found != null;
    }

    public ExeApp GetExeApp(string key) => Apps[key.ToLowerInvariant()];
    public void Remove(string key) => Apps.Remove(key.ToLowerInvariant());
    public void Remove(ExeApp app) => Remove(app.ExecutablePath.ToLowerInvariant());

    public void Add(ExeApp app)
    {
        app.ExecutablePath = app.ExecutablePath.ToLowerInvariant();
        Apps.Add(app.ExecutablePath, app);
    }

    public void Save()
    {
        using var stream = File.Create(jsonPath);
        JsonSerializer.Serialize(stream, new SerializeWrapper
        {
            Apps = Apps,
            FileVersion = FileVersion,
            Global = GlobalApp,
            HotCornerCommands = HotCornerCommands
        }, SerializerOptions);
    }

    public JsonGestureIntentStore Clone() => new()
    {
        GlobalApp = GlobalApp,
        Apps = Apps,
        FileVersion = FileVersion,
        jsonPath = jsonPath,
        HotCornerCommands = HotCornerCommands
    };

    public void Import(JsonGestureIntentStore from, bool replace = false)
    {
        if (from == null)
        {
            return;
        }

        if (replace)
        {
            GlobalApp.GestureIntents.Clear();
            GlobalApp.IsGesturingEnabled = from.GlobalApp.IsGesturingEnabled;
            Apps.Clear();
            HotCornerCommands = from.HotCornerCommands;
        }
        else
        {
            for (var i = 0; i < from.HotCornerCommands.Length; i++)
            {
                if (from.HotCornerCommands[i] != null)
                {
                    HotCornerCommands[i] = from.HotCornerCommands[i];
                }
            }
        }

        GlobalApp.ImportGestures(from.GlobalApp);
        foreach (var kv in from.Apps)
        {
            if (Apps.TryGetValue(kv.Key.ToLowerInvariant(), out var appInSelf))
            {
                appInSelf.ImportGestures(kv.Value);
                appInSelf.IsGesturingEnabled = appInSelf.IsGesturingEnabled && kv.Value.IsGesturingEnabled;
            }
            else
            {
                Add(kv.Value);
            }
        }
    }

    public IEnumerator<ExeApp> GetEnumerator() => Apps.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal sealed class SerializeWrapper
    {
        public string FileVersion { get; set; } = "3";
        public Dictionary<string, ExeApp> Apps { get; set; } = new();
        public GlobalApp Global { get; set; } = new();
        public AbstractCommand?[] HotCornerCommands { get; set; } = new AbstractCommand?[8];
    }

    private sealed class GestureConverter : JsonConverter<Gesture>
    {
        public override Gesture Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            var gesture = new Gesture
            {
                GestureButton = (GestureTriggerButton)root.GetProperty("gB").GetInt32(),
                Modifier = (GestureModifier)root.GetProperty("m").GetInt32()
            };
            foreach (var value in root.GetProperty("d").EnumerateArray())
            {
                gesture.Add((Gesture.GestureDir)value.GetInt32());
            }
            return gesture;
        }

        public override void Write(Utf8JsonWriter writer, Gesture value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("gB", (int)value.GestureButton);
            writer.WriteStartArray("d");
            foreach (var dir in value.Dirs)
            {
                writer.WriteNumberValue((int)dir);
            }
            writer.WriteEndArray();
            writer.WriteNumber("m", (int)value.Modifier);
            writer.WriteEndObject();
        }
    }

    private sealed class GestureIntentDictConverter : JsonConverter<GestureIntentDict>
    {
        public override GestureIntentDict Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dict = new GestureIntentDict();
            foreach (var intent in JsonSerializer.Deserialize<List<GestureIntent>>(ref reader, options) ?? [])
            {
                dict.Add(intent);
            }
            return dict;
        }

        public override void Write(Utf8JsonWriter writer, GestureIntentDict value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.Values.ToList(), options);
        }
    }

    private sealed class AbstractCommandConverter : JsonConverter<AbstractCommand>
    {
        public override AbstractCommand Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var typeName = document.RootElement.GetProperty("$type").GetString();
            return typeName == nameof(DoNothingCommand)
                ? new DoNothingCommand()
                : throw new NotSupportedException($"不支持的命令类型：{typeName}");
        }

        public override void Write(Utf8JsonWriter writer, AbstractCommand value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("$type", value.GetType().Name);
            writer.WriteEndObject();
        }
    }
}
