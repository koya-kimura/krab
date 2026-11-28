using System;
using System.Collections.Generic;
using System.IO;
using Krab.ImageLayer.Core.Presets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Krab.ImageLayer.Performance
{
    /// <summary>
    /// Layout presets and image entries as JSON under StreamingAssets/Krab/ImageLayer/Presets, kept in Git with the
    /// performance. Beside the library root, because every folder inside it is read as a library.
    /// </summary>
    public sealed class ImageLayerPresetStore
    {
        public const string RelativeRoot = "Krab/ImageLayer/Presets";

        private const int SchemaVersion = 1;

        private string _root;

        public ImageLayerPresetStore(string root = null)
        {
            _root = root;
        }

        // Resolved on first use: Unity forbids reading streamingAssetsPath while constructing a MonoBehaviour.
        public string Root => _root ??= Path.Combine(Application.streamingAssetsPath, RelativeRoot);

        public string LayoutsPath => Path.Combine(Root, "Layouts.json");

        public string ImagesPath(string libraryName) => Path.Combine(Root, "Images", libraryName + ".json");

        /// <summary>False without an error when the file does not exist yet, with an error when unreadable.</summary>
        public bool TryLoadLayouts(out List<LayoutPreset> presets, out string error)
        {
            presets = new List<LayoutPreset>();
            if (!TryReadArray(LayoutsPath, "Layouts", out var items, out error)) return false;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items)
                TryRead(item, ReadLayout, presets, ref error);

            return true;

            LayoutPreset ReadLayout(JToken item)
            {
                var id = (string)item["Id"];
                var pattern = (string)item["Pattern"];
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(pattern) || !ids.Add(id))
                    throw new FormatException("a layout needs a unique Id and a Pattern");

                var values = new Dictionary<string, double>(StringComparer.Ordinal);
                if (item["Values"] is JObject stored)
                {
                    foreach (var property in stored.Properties())
                    {
                        if (property.Value.Type is JTokenType.Float or JTokenType.Integer)
                            values[property.Name] = (double)property.Value;
                        else if (property.Value.Type == JTokenType.Boolean)
                            values[property.Name] = (bool)property.Value ? 1 : 0;
                    }
                }

                return new LayoutPreset(id, (string)item["Name"], pattern, new ParameterValues(values));
            }
        }

        public bool TryLoadImages(string libraryName, out List<ImageEntry> entries, out string error)
        {
            entries = new List<ImageEntry>();
            if (!TryReadArray(ImagesPath(libraryName), "Images", out var items, out error)) return false;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items)
                TryRead(item, ReadImage, entries, ref error);

            return true;

            ImageEntry ReadImage(JToken item)
            {
                var id = (string)item["Id"];
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                    throw new FormatException("an image needs a unique Id");

                return new ImageEntry(id, (string)item["Name"], (string)item["Set"],
                    (int?)item["Clip"] ?? ImageEntry.AllClips, (string)item["Look"],
                    (double?)item["FramesPerBeat"] ?? ImageEntry.DefaultFramesPerBeat);
            }
        }

        public void SaveLayouts(IReadOnlyList<LayoutPreset> presets)
        {
            var items = new JArray();
            foreach (var preset in presets)
            {
                var values = new JObject();
                foreach (var pair in preset.Values.Stored)
                    values[pair.Key] = pair.Value;

                items.Add(new JObject
                {
                    ["Id"] = preset.Id,
                    ["Name"] = preset.Name,
                    ["Pattern"] = preset.Pattern,
                    ["Values"] = values
                });
            }

            Write(LayoutsPath, "Layouts", items);
        }

        public void SaveImages(string libraryName, IReadOnlyList<ImageEntry> entries)
        {
            var items = new JArray();
            foreach (var entry in entries)
            {
                items.Add(new JObject
                {
                    ["Id"] = entry.Id,
                    ["Name"] = entry.Name,
                    ["Set"] = entry.SetName,
                    ["Clip"] = entry.Clip,
                    ["Look"] = entry.Look,
                    ["FramesPerBeat"] = entry.FramesPerBeat
                });
            }

            Write(ImagesPath(libraryName), "Images", items);
        }

        /// <summary>Reads one item; a damaged item is skipped and reported so the rest still load.</summary>
        private static void TryRead<T>(JToken item, Func<JToken, T> read, List<T> list, ref string error)
        {
            try
            {
                list.Add(read(item));
            }
            catch (Exception exception) when (exception is FormatException or InvalidCastException
                                                  or ArgumentException)
            {
                error = $"Skipped {item.ToString(Formatting.None)}: {exception.Message}";
            }
        }

        private static bool TryReadArray(string path, string arrayName, out JArray items, out string error)
        {
            items = null;
            error = null;
            if (!File.Exists(path)) return false;

            try
            {
                var root = JObject.Parse(File.ReadAllText(path));
                if ((int?)root["SchemaVersion"] != SchemaVersion)
                {
                    error = $"{Path.GetFileName(path)}: SchemaVersion {SchemaVersion} is required.";
                    return false;
                }

                items = root[arrayName] as JArray;
                if (items != null) return true;

                error = $"{Path.GetFileName(path)}: '{arrayName}' array is missing.";
                return false;
            }
            catch (Exception exception) when (exception is JsonException or IOException or InvalidCastException
                                                  or FormatException)
            {
                error = $"{Path.GetFileName(path)}: {exception.Message}";
                return false;
            }
        }

        private static void Write(string path, string arrayName, JArray items)
        {
            var root = new JObject { ["SchemaVersion"] = SchemaVersion, [arrayName] = items };
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // Write beside the file first so an interrupted save never leaves a half-written preset file.
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, root.ToString(Formatting.Indented) + "\n");
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }
    }
}
