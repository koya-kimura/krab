using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Krab.ShapeSynth.Core.Presets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Krab.ShapeSynth.Unity.Persistence
{
    /// <summary>
    /// Stores validated preset documents as separate JSON files, and the Values they share as one library file
    /// next to the preset folder. A loaded preset gets the library attached as its Values.
    /// </summary>
    public sealed class PresetRepository
    {
        public const string ValuesFileName = "Values.json";
        private const int ValuesSchemaVersion = 1;

        public PresetRepository(string directoryPath = null, string valuesPath = null)
        {
            DirectoryPath = directoryPath ?? System.IO.Path.Combine(
                Application.persistentDataPath, "ShapeSynth", "Presets");
            ValuesPath = valuesPath ?? System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(DirectoryPath)
                    .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)),
                ValuesFileName);
        }

        public string DirectoryPath { get; }

        public string ValuesPath { get; }

        public bool Exists(string id)
        {
            return File.Exists(GetFilePath(id));
        }

        /// <summary>Loads the preset with the shared Values read from disk.</summary>
        public PresetDocument Load(string id)
        {
            return Load(id, LoadValues());
        }

        /// <summary>Loads the preset and attaches the given Values (kept by reference) before validating it.</summary>
        public PresetDocument Load(string id, List<PresetValue> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

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
                var token = ParseStrict(json, "Preset");

                // Older schemas are not migrated. Reject them before deserializing, so the error names the
                // version instead of the first field that no longer exists.
                // SchemaVersion 6 is 7 without local Values, so it is read as is.
                var schemaVersion = token.Value<int>("SchemaVersion");
                if (schemaVersion == 6) token["SchemaVersion"] = PresetDocument.CurrentSchemaVersion;
                else if (schemaVersion != PresetDocument.CurrentSchemaVersion)
                {
                    throw new ArgumentException(
                        $"Unsupported SchemaVersion {schemaVersion} (current {PresetDocument.CurrentSchemaVersion}). " +
                        "Old presets are not migrated; delete the file or re-create the preset.");
                }

                document = token.ToObject<PresetDocument>(JsonSerializer.Create(JsonSettings()));
            }
            catch (JsonException exception)
            {
                throw new ArgumentException("Invalid preset JSON: " + exception.Message, exception);
            }

            if (document == null)
            {
                throw new ArgumentException("Preset JSON has no document.", nameof(id));
            }

            document.Values = values;
            document.Validate();
            if (!string.Equals(document.Id, id, StringComparison.Ordinal))
            {
                throw new ArgumentException("Preset document ID does not match its file name.", nameof(id));
            }

            return document;
        }

        /// <summary>Reads the shared Value library; a missing file is an empty library.</summary>
        public List<PresetValue> LoadValues()
        {
            if (!File.Exists(ValuesPath)) return new List<PresetValue>();

            ValueLibraryFile library;
            try
            {
                var token = ParseStrict(File.ReadAllText(ValuesPath), "Value library");
                var schemaVersion = token.Value<int>("SchemaVersion");
                if (schemaVersion != ValuesSchemaVersion)
                {
                    throw new ArgumentException(
                        $"Unsupported Value library SchemaVersion {schemaVersion} (current {ValuesSchemaVersion}).");
                }

                library = token.ToObject<ValueLibraryFile>(JsonSerializer.Create(JsonSettings()));
            }
            catch (JsonException exception)
            {
                throw new ArgumentException("Invalid Value library JSON: " + exception.Message, exception);
            }

            var values = library?.Values ?? throw new ArgumentException("Value library JSON has no Values.");
            PresetCompiler.ValidateValues(values);
            return values;
        }

        public void SaveValues(IReadOnlyList<PresetValue> values)
        {
            // Validate before touching the previous file.
            PresetCompiler.ValidateValues(values);
            var library = new ValueLibraryFile { SchemaVersion = ValuesSchemaVersion, Values = new List<PresetValue>(values) };
            WriteAtomically(ValuesPath, JsonConvert.SerializeObject(library, Formatting.Indented, JsonSettings()));
        }

        /// <summary>File names and write times of the presets and the Value library, to notice saves from elsewhere.</summary>
        public string FilesSignature()
        {
            var signature = new StringBuilder();
            if (File.Exists(ValuesPath))
                signature.Append(ValuesPath).Append('|').Append(File.GetLastWriteTimeUtc(ValuesPath).Ticks).Append('\n');
            if (!Directory.Exists(DirectoryPath)) return signature.ToString();

            var files = Directory.GetFiles(DirectoryPath, "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var file in files)
            {
                signature.Append(file).Append('|').Append(File.GetLastWriteTimeUtc(file).Ticks).Append('\n');
            }

            return signature.ToString();
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
            List<PresetValue> values;
            try
            {
                values = LoadValues();
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException)
            {
                errors = new[] { $"{ValuesFileName}: {exception.Message}" };
                return documents;
            }

            if (Directory.Exists(DirectoryPath))
            {
                var files = Directory.GetFiles(DirectoryPath, "*.json");
                Array.Sort(files, StringComparer.Ordinal);
                foreach (var file in files)
                {
                    var id = System.IO.Path.GetFileNameWithoutExtension(file);
                    try
                    {
                        documents.Add(Load(id, values));
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

        /// <summary>
        /// A deep copy of the preset under a new ID and name, sharing the same Value library. Node IDs are kept: they
        /// only need to be unique inside one preset. Nothing is written; save the copy to keep it.
        /// </summary>
        public PresetDocument Duplicate(PresetDocument document, string name)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            var json = JsonConvert.SerializeObject(document, JsonSettings());
            var copy = JsonConvert.DeserializeObject<PresetDocument>(json, JsonSettings());
            copy.Id = PresetDocument.NewId();
            copy.Name = name;
            copy.Values = document.Values;
            return copy;
        }

        public void Save(PresetDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            // Validate before touching the previous file.
            document.Validate();
            var json = JsonConvert.SerializeObject(document, Formatting.Indented, JsonSettings());
            WriteAtomically(GetFilePath(document.Id), json);
        }

        private static JObject ParseStrict(string json, string label)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException(label + " JSON is empty.");

            using (var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 256 })
            {
                var loadSettings = new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                };
                var token = JObject.Load(reader, loadSettings);
                if (reader.Read()) throw new ArgumentException($"Unexpected content after {label} JSON.");
                if (token["SchemaVersion"]?.Type != JTokenType.Integer)
                    throw new ArgumentException(label + " JSON requires an integer SchemaVersion.");

                return token;
            }
        }

        private static void WriteAtomically(string path, string json)
        {
            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

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

        [Serializable]
        private sealed class ValueLibraryFile
        {
            public int SchemaVersion;
            public List<PresetValue> Values;
        }
    }
}
