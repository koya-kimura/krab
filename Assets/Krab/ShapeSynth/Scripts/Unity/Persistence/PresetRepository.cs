using System;
using System.Collections.Generic;
using System.IO;
using Krab.ShapeSynth.Core.Presets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Krab.ShapeSynth.Unity.Persistence
{
    /// <summary>Stores validated preset documents as separate JSON files.</summary>
    public sealed class PresetRepository
    {
        public string DirectoryPath { get; }

        public PresetRepository(string directoryPath = null)
        {
            DirectoryPath = directoryPath ?? System.IO.Path.Combine(
                Application.persistentDataPath, "ShapeSynth", "Presets");
        }

        public bool Exists(string id)
        {
            return File.Exists(GetFilePath(id));
        }

        public PresetDocument Load(string id)
        {
            var path = GetFilePath(id);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Preset '{id}' was not found.", path);
            }

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Preset JSON is empty.", nameof(id));
            }

            PresetDocument document;
            try
            {
                using (var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 256 })
                {
                    var loadSettings = new JsonLoadSettings
                    {
                        DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                    };
                    var token = JObject.Load(reader, loadSettings);
                    if (reader.Read())
                    {
                        throw new ArgumentException("Unexpected content after preset JSON.");
                    }

                    if (token["SchemaVersion"]?.Type != JTokenType.Integer)
                    {
                        throw new ArgumentException("Preset JSON requires an integer SchemaVersion.");
                    }

                    // Older schemas are not migrated. Reject them before deserializing, so the error names the
                    // version instead of the first field that no longer exists.
                    var schemaVersion = token.Value<int>("SchemaVersion");
                    if (schemaVersion != PresetDocument.CurrentSchemaVersion)
                    {
                        throw new ArgumentException(
                            $"Unsupported SchemaVersion {schemaVersion} (current {PresetDocument.CurrentSchemaVersion}). " +
                            "Old presets are not migrated; delete the file or re-create the preset.");
                    }

                    document = token.ToObject<PresetDocument>(JsonSerializer.Create(JsonSettings()));
                }
            }
            catch (JsonException exception)
            {
                throw new ArgumentException("Invalid preset JSON: " + exception.Message, exception);
            }

            if (document == null)
            {
                throw new ArgumentException("Preset JSON has no document.", nameof(id));
            }

            document.Validate();
            if (!string.Equals(document.Id, id, StringComparison.Ordinal))
            {
                throw new ArgumentException("Preset document ID does not match its file name.", nameof(id));
            }

            return document;
        }

        public IReadOnlyList<PresetDocument> List()
        {
            return List(out _);
        }

        /// <summary>Lists valid documents and returns malformed entries as readable errors.</summary>
        public IReadOnlyList<PresetDocument> List(out IReadOnlyList<string> errors)
        {
            var documents = new List<PresetDocument>();
            var failures = new List<string>();
            if (Directory.Exists(DirectoryPath))
            {
                var files = Directory.GetFiles(DirectoryPath, "*.json");
                Array.Sort(files, StringComparer.Ordinal);
                foreach (var file in files)
                {
                    var id = System.IO.Path.GetFileNameWithoutExtension(file);
                    try
                    {
                        documents.Add(Load(id));
                    }
                    catch (Exception exception) when (exception is ArgumentException || exception is IOException)
                    {
                        failures.Add($"{System.IO.Path.GetFileName(file)}: {exception.Message}");
                    }
                }
            }

            errors = failures;
            return documents;
        }

        public void Save(PresetDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            // Validate before touching the previous file.
            document.Validate();
            var path = GetFilePath(document.Id);
            var json = JsonConvert.SerializeObject(document, Formatting.Indented, JsonSettings());
            Directory.CreateDirectory(DirectoryPath);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, json);
                if (File.Exists(path))
                {
                    File.Replace(temporary, path, null);
                }
                else
                {
                    File.Move(temporary, path);
                }
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        private static JsonSerializerSettings JsonSettings()
        {
            return new JsonSerializerSettings
            {
                MaxDepth = 256,
                TypeNameHandling = TypeNameHandling.None,
                MissingMemberHandling = MissingMemberHandling.Error
            };
        }

        private string GetFilePath(string id)
        {
            PresetDocument.RequireId(id);
            return System.IO.Path.Combine(DirectoryPath, id + ".json");
        }
    }
}
