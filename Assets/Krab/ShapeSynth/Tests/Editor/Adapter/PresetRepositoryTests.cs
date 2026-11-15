using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Unity.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor.Adapter
{
    public sealed class PresetRepositoryTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "Krab-Presets-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void SaveLoadRoundTripPreservesNestedNodesBindingsAndModulatorSettings()
        {
            var repository = new PresetRepository(_directory);
            var source = PresetDocument.CreateNew();
            source.Id = "performance_main-01";
            source.Name = "Main performance";
            source.Values[0].Name = "Envelope custom";
            source.Values[0].Components[0].Attack = 0.42;
            source.Values[0].Components.Add(PresetComponent.Create(ModulatorKind.Lfo));
            source.Values[0].Components[1].Weight = -0.25;
            var circle = source.RootGroup.Children[0];
            circle.Name = "Pulse circle";
            circle.Width.Base = 0.37;
            circle.Red = 0.25;
            circle.Fill = false;
            circle.Stroke = true;
            circle.StrokeWidth = 2.75;
            circle.Opacity.Bindings[0].Depth = 0.6;
            var subgroup = source.AddNode(source.RootGroup.Id, PresetNodeKind.Group);
            subgroup.Name = "Nested group";
            var polygon = source.AddNode(subgroup.Id, PresetNodeKind.Polygon);
            polygon.Name = "Triangle";
            polygon.Vertices[1].X = 73.5;

            repository.Save(source);

            Assert.That(repository.Exists(source.Id), Is.True);
            var loaded = repository.Load(source.Id);
            Assert.That(loaded.SchemaVersion, Is.EqualTo(PresetDocument.CurrentSchemaVersion));
            Assert.That(loaded.Name, Is.EqualTo("Main performance"));
            Assert.That(loaded.Values[0].Id, Is.EqualTo(source.Values[0].Id));
            Assert.That(loaded.Values[0].Name, Is.EqualTo("Envelope custom"));
            Assert.That(loaded.Values[0].Components[0].Attack, Is.EqualTo(0.42));
            Assert.That(loaded.Values[0].Components[1].Kind, Is.EqualTo(ModulatorKind.Lfo));
            Assert.That(loaded.Values[0].Components[1].Weight, Is.EqualTo(-0.25));
            Assert.That(loaded.RootGroup.Children[0].Name, Is.EqualTo("Pulse circle"));
            Assert.That(loaded.RootGroup.Children[0].Width.Base, Is.EqualTo(0.37));
            Assert.That(loaded.RootGroup.Children[0].Red, Is.EqualTo(0.25));
            Assert.That(loaded.RootGroup.Children[0].Fill, Is.False);
            Assert.That(loaded.RootGroup.Children[0].Stroke, Is.True);
            Assert.That(loaded.RootGroup.Children[0].StrokeWidth, Is.EqualTo(2.75));
            Assert.That(loaded.RootGroup.Children[0].Opacity.Bindings[0].Depth, Is.EqualTo(0.6));
            Assert.That(loaded.RootGroup.Children[1].Children[0].Vertices[1].X, Is.EqualTo(73.5));
            Assert.That(Directory.GetFiles(_directory), Has.Length.EqualTo(1));
        }

        [Test]
        public void SaveLoadRoundTripPreservesLfoReferencesAndTransformParameters()
        {
            var repository = new PresetRepository(_directory);
            var source = PresetDocument.CreateNew();
            var lfoId = "motion-lfo";
            var lfo = PresetComponent.Create(ModulatorKind.Lfo);
            lfo.CyclesPerBeat = 1.75;
            lfo.PhaseOffset = 0.375;
            lfo.Waveform = LfoWaveform.Triangle;
            source.Values.Add(new PresetValue
            {
                Id = lfoId, Name = "Motion", Components = new List<PresetComponent> { lfo }
            });
            var circle = source.RootGroup.Children[0];
            circle.PositionX.Reference = ReferenceSource.CanvasWidth;
            circle.PositionX.Multiplier = 0.23;
            circle.PositionX.Base = -0.12;
            circle.PositionX.Bind(lfoId, 0.8);
            circle.Rotation.Reference = ReferenceSource.MaxDimension;
            circle.Rotation.Multiplier = 0.5;
            circle.Rotation.AbsoluteValue = 37.5;
            circle.Rotation.Base = 12.25;

            repository.Save(source);

            var loaded = repository.Load(source.Id);
            var loadedLfo = loaded.Values[1].Components.Single();
            Assert.That(loadedLfo.Kind, Is.EqualTo(ModulatorKind.Lfo));
            Assert.That(loadedLfo.CyclesPerBeat, Is.EqualTo(1.75));
            Assert.That(loadedLfo.PhaseOffset, Is.EqualTo(0.375));
            Assert.That(loadedLfo.Waveform, Is.EqualTo(LfoWaveform.Triangle));
            Assert.That(loaded.RootGroup.Children[0].PositionX.Reference, Is.EqualTo(ReferenceSource.CanvasWidth));
            Assert.That(loaded.RootGroup.Children[0].PositionX.Multiplier, Is.EqualTo(0.23));
            Assert.That(loaded.RootGroup.Children[0].PositionX.Base, Is.EqualTo(-0.12));
            Assert.That(loaded.RootGroup.Children[0].PositionX.Bindings[0].ValueId, Is.EqualTo(lfoId));
            Assert.That(loaded.RootGroup.Children[0].PositionX.Bindings[0].Depth, Is.EqualTo(0.8));
            Assert.That(loaded.RootGroup.Children[0].Rotation.Reference, Is.EqualTo(ReferenceSource.MaxDimension));
            Assert.That(loaded.RootGroup.Children[0].Rotation.Multiplier, Is.EqualTo(0.5));
            Assert.That(loaded.RootGroup.Children[0].Rotation.AbsoluteValue, Is.EqualTo(37.5));
            Assert.That(loaded.RootGroup.Children[0].Rotation.Base, Is.EqualTo(12.25));
        }

        [Test]
        public void LoadUpgradesSchemaV1ModulatorsToSingleComponentValues()
        {
            var repository = new PresetRepository(_directory);
            var current = PresetDocument.CreateNew();
            var v1 = JObject.FromObject(current);
            var value = (JObject)v1["Values"][0];
            var component = (JObject)value["Components"][0];
            component.Remove("Weight");
            component["Id"] = value["Id"];
            component["Name"] = value["Name"];
            component["Attack"] = 0.33;
            v1.Remove("Values");
            v1["Modulators"] = new JArray(component);
            v1["SchemaVersion"] = 1;
            foreach (var binding in v1.Descendants().OfType<JObject>().Where(token => token["ValueId"] != null).ToArray())
            {
                binding["ModulatorId"] = binding["ValueId"];
                binding.Remove("ValueId");
            }

            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath(current.Id + ".json"), v1.ToString());

            var loaded = repository.Load(current.Id);

            Assert.That(loaded.SchemaVersion, Is.EqualTo(PresetDocument.CurrentSchemaVersion));
            Assert.That(loaded.Values.Single().Id, Is.EqualTo(current.Values[0].Id));
            Assert.That(loaded.Values.Single().Name, Is.EqualTo(current.Values[0].Name));
            Assert.That(loaded.Values.Single().Components.Single().Weight, Is.EqualTo(1));
            Assert.That(loaded.Values.Single().Components.Single().Attack, Is.EqualTo(0.33));
            Assert.That(loaded.RootGroup.Children[0].Opacity.GetBinding().ValueId, Is.EqualTo(current.Values[0].Id));
        }

        [Test]
        public void SaveLoadAllowsDraftWithoutLifetime()
        {
            var repository = new PresetRepository(_directory);
            var draft = PresetDocument.CreateNew();
            draft.Values.Clear();
            foreach (var node in draft.Nodes())
            {
                foreach (var parameter in node.Parameters())
                {
                    parameter.Bindings.Clear();
                }
            }

            repository.Save(draft);

            var loaded = repository.Load(draft.Id);
            Assert.That(loaded.Values, Is.Empty);
            Assert.That(loaded.RootGroup.Children, Has.Count.EqualTo(1));
        }

        [Test]
        public void DeepHierarchyRoundTripPreservesEveryNode()
        {
            var repository = new PresetRepository(_directory);
            var document = PresetDocument.CreateNew();
            var parent = document.RootGroup;
            for (var i = 0; i < 60; i++)
            {
                parent = document.AddNode(parent.Id, PresetNodeKind.Group);
            }

            var shape = document.AddNode(parent.Id, PresetNodeKind.Rectangle);
            shape.PositionX.Base = 123;
            repository.Save(document);

            var loaded = repository.Load(document.Id);

            Assert.That(loaded.Nodes().Select(node => node.Id), Is.EqualTo(document.Nodes().Select(node => node.Id)));
            Assert.That(loaded.Nodes().Last().PositionX.Base, Is.EqualTo(123));
            Assert.DoesNotThrow(() => PresetCompiler.Compile(loaded));
        }

        [Test]
        public void MissingSchemaVersionAndDuplicateJsonMembersAreRejected()
        {
            var repository = new PresetRepository(_directory);
            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath("invalid.json"), "{}");
            Assert.Throws<ArgumentException>(() => repository.Load("invalid"));

            File.WriteAllText(FilePath("invalid.json"), "{\"SchemaVersion\":1,\"SchemaVersion\":1}");
            Assert.Throws<ArgumentException>(() => repository.Load("invalid"));
        }

        [Test]
        public void SaveReplacementIsAtomicAndInvalidDocumentKeepsPreviousFile()
        {
            var repository = new PresetRepository(_directory);
            var document = PresetDocument.CreateNew();
            repository.Save(document);
            document.Name = "Updated";
            repository.Save(document);

            Assert.That(repository.Load(document.Id).Name, Is.EqualTo("Updated"));
            Assert.That(Directory.GetFiles(_directory), Has.Length.EqualTo(1));

            var previous = File.ReadAllText(FilePath(document.Id + ".json"));
            document.Name = " ";

            Assert.Throws<ArgumentException>(() => repository.Save(document));
            Assert.That(File.ReadAllText(FilePath(document.Id + ".json")), Is.EqualTo(previous));
            Assert.That(Directory.GetFiles(_directory), Has.Length.EqualTo(1));
        }

        [Test]
        public void LoadAndExistsRejectPathTraversalIDs()
        {
            var repository = new PresetRepository(_directory);

            Assert.Throws<ArgumentException>(() => repository.Load("../outside"));
            Assert.Throws<ArgumentException>(() => repository.Exists("_hidden"));
            Assert.That(Directory.Exists(_directory), Is.False);
        }

        [Test]
        public void ListReturnsValidPresetsAndReportsMalformedFiles()
        {
            var repository = new PresetRepository(_directory);
            var valid = PresetDocument.CreateNew();
            valid.Id = "valid";
            repository.Save(valid);
            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath("broken.json"), "not JSON");

            var listed = repository.List(out var errors);

            Assert.That(listed, Has.Count.EqualTo(1));
            Assert.That(listed[0].Id, Is.EqualTo("valid"));
            Assert.That(errors, Has.Count.EqualTo(1));
            StringAssert.Contains("broken.json", errors[0]);
        }

        [Test]
        public void LoadRejectsDocumentWhoseIdDoesNotMatchFileName()
        {
            var repository = new PresetRepository(_directory);
            var document = PresetDocument.CreateNew();
            repository.Save(document);
            File.Move(FilePath(document.Id + ".json"), FilePath("different-id.json"));

            Assert.Throws<ArgumentException>(() => repository.Load("different-id"));
        }

        [Test]
        public void LoadRejectsUnsupportedSchemaAndDuplicateNodeIds()
        {
            var repository = new PresetRepository(_directory);
            var unsupported = PresetDocument.CreateNew();
            unsupported.SchemaVersion = 99;
            WriteDocument(unsupported);

            Assert.Throws<ArgumentException>(() => repository.Load(unsupported.Id));

            var duplicateNodes = PresetDocument.CreateNew();
            duplicateNodes.RootGroup.Children[0].Id = duplicateNodes.RootGroup.Id;
            WriteDocument(duplicateNodes);

            Assert.Throws<ArgumentException>(() => repository.Load(duplicateNodes.Id));
        }

        [Test]
        public void LoadRejectsMalformedJson()
        {
            var repository = new PresetRepository(_directory);
            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath("broken.json"), "{");

            Assert.Throws<ArgumentException>(() => repository.Load("broken"));
        }

        private void WriteDocument(PresetDocument document)
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath(document.Id + ".json"), JsonConvert.SerializeObject(document));
        }

        private string FilePath(string fileName)
        {
            return Path.Combine(_directory, fileName);
        }
    }
}
