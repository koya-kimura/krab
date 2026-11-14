using System;
using System.IO;
using Krab.ShapeSynth.Core.Performance;
using UnityEngine;

namespace Krab.ShapeSynth.Unity.Persistence
{
    /// <summary>Versioned authoring DTO storage; never serializes live Core instances.</summary>
    public sealed class PerformanceSettingsStore
    {
        public string Path { get; }
        public PerformanceSettingsStore(string path = null) => Path = path ?? System.IO.Path.Combine(
            Application.persistentDataPath, "ShapeSynth", "Performance.v1.json");

        public static string Encode(PerformanceSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.BuildLibrary();
            return JsonUtility.ToJson(settings, true);
        }

        public static PerformanceSettings Decode(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Settings JSON is empty.", nameof(json));
            var settings = JsonUtility.FromJson<PerformanceSettings>(json);
            if (settings == null) throw new ArgumentException("Settings JSON has no document.", nameof(json));
            settings.BuildLibrary();
            settings.EnsureAdditionalPresets();
            settings.BuildLibrary();
            return settings;
        }

        public bool Exists => File.Exists(Path);
        public PerformanceSettings Load() => Decode(File.ReadAllText(Path));

        public void Save(PerformanceSettings settings)
        {
            var json = Encode(settings); // Validate before touching the last good file.
            var folder = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path));
            Directory.CreateDirectory(folder);
            var temporary = Path + ".tmp";
            try
            {
                File.WriteAllText(temporary, json);
                if (File.Exists(Path)) File.Replace(temporary, Path, null);
                else File.Move(temporary, Path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
