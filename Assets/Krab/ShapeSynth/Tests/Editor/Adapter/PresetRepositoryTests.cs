using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Unity.Persistence;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor.Adapter
{
    public sealed class PresetRepositoryTests
    {
        private string _root;
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            // The Value library is written next to the preset folder, so both live under one temporary root.
            _root = Path.Combine(Path.GetTempPath(), "Krab-Presets-" + Guid.NewGuid().ToString("N"));
            _directory = Path.Combine(_root, "Presets");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void LayoutRoundTripKeepsOneSpacingBindingAndWritesNoDerivedValues()
        {
            var repository = new PresetRepository(_directory);
            var document = PresetDocument.CreateNew();
            var shape = document.RootGroup.Children[0];
            shape.Layout.Kind = PresetLayoutKind.Grid;
            shape.Layout.SpacingX.Bind(document.Values[0].Id, 0.25);

            repository.SaveValues(document.Values);
            repository.Save(document);
            var json = File.ReadAllText(Path.Combine(_directory, document.Id + ".json"));
            var loaded = repository.Load(document.Id).RootGroup.Children[0].Layout;

            Assert.That(json, Does.Not.Contain("AxisX").And.Not.Contain("\"Copies\""));
            Assert.That(loaded.SpacingX.Bindings, Has.Count.EqualTo(1));
            Assert.That(loaded.SpacingY.Bindings, Is.Empty);
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
            circle.FillHue.Base = 0.25;
            circle.StrokeBrightness.Base = 0.5;
            circle.Fill = false;
            circle.Stroke = true;
            circle.StrokeWidth.Base = 2.75;
            circle.StrokeWidth.Bind(source.Values[0].Id, 4);
            circle.Opacity.Bindings[0].Depth = 0.6;
            var subgroup = source.AddNode(source.RootGroup.Id, PresetNodeKind.Group);
            subgroup.Name = "Nested group";
            var polygon = source.AddNode(subgroup.Id, PresetNodeKind.Polygon);
            polygon.Name = "Triangle";
            polygon.Vertices[1].X = 73.5;

            repository.SaveValues(source.Values);
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
            Assert.That(loaded.RootGroup.Children[0].FillHue.Base, Is.EqualTo(0.25));
            Assert.That(loaded.RootGroup.Children[0].StrokeBrightness.Base, Is.EqualTo(0.5));
            Assert.That(loaded.RootGroup.Children[0].Fill, Is.False);
            Assert.That(loaded.RootGroup.Children[0].Stroke, Is.True);
            Assert.That(loaded.RootGroup.Children[0].StrokeWidth.Base, Is.EqualTo(2.75));
            Assert.That(loaded.RootGroup.Children[0].StrokeWidth.GetBinding().Depth, Is.EqualTo(4));
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

            repository.SaveValues(source.Values);
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

            repository.SaveValues(draft.Values);
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
            repository.SaveValues(document.Values);
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
            repository.SaveValues(document.Values);
            repository.Save(document);
            document.Name = "Updated";
            repository.SaveValues(document.Values);
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
            repository.SaveValues(valid.Values);
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
            repository.SaveValues(document.Values);
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

            // An old schema with a retired field is reported by its version, not by the unknown field.
            var old = PresetDocument.CreateNew();
            old.SchemaVersion = 4;
            WriteDocument(old);
            var oldJson = File.ReadAllText(FilePath(old.Id + ".json"));
            Assert.That(oldJson, Does.Contain("\"Name\":\"Circle\""));
            File.WriteAllText(FilePath(old.Id + ".json"),
                oldJson.Replace("\"Name\":\"Circle\"", "\"Name\":\"Circle\",\"Red\":1"));
            var exception = Assert.Throws<ArgumentException>(() => repository.Load(old.Id));
            Assert.That(exception.Message, Does.Contain("SchemaVersion 4"));

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

        [Test]
        public void ValuesAreSavedOnceAndSharedByEveryLoadedPreset()
        {
            var repository = new PresetRepository(_directory);
            var first = PresetDocument.CreateNew();
            first.Id = "first";
            var second = PresetDocument.CreateNew(first.Values);
            second.Id = "second";
            repository.SaveValues(first.Values);
            repository.Save(first);
            repository.Save(second);

            var presetJson = File.ReadAllText(FilePath("first.json"));
            var listed = repository.List(out var errors);

            Assert.That(repository.ValuesPath, Is.EqualTo(Path.Combine(_root, PresetRepository.ValuesFileName)));
            Assert.That(presetJson, Does.Not.Contain("\"Values\"").And.Not.Contain("Default ADSR"));
            Assert.That(File.ReadAllText(repository.ValuesPath), Does.Contain("Default ADSR"));
            Assert.That(errors, Is.Empty);
            Assert.That(listed.Select(document => document.Id), Is.EqualTo(new[] { "first", "second" }));
            Assert.That(listed[0].Values, Is.SameAs(listed[1].Values));
            Assert.That(listed[0].Values.Single().Id, Is.EqualTo(first.Values.Single().Id));
        }

        [Test]
        public void EditingASharedValueReachesEveryPresetOnTheNextLoad()
        {
            var repository = new PresetRepository(_directory);
            var first = PresetDocument.CreateNew();
            first.Id = "first";
            var second = PresetDocument.CreateNew(first.Values);
            second.Id = "second";
            repository.SaveValues(first.Values);
            repository.Save(first);
            repository.Save(second);

            first.Values.Single().Components.Single().Attack = 0.75;
            repository.SaveValues(first.Values);

            Assert.That(repository.Load("first").Values.Single().Components.Single().Attack, Is.EqualTo(0.75));
            Assert.That(repository.Load("second").Values.Single().Components.Single().Attack, Is.EqualTo(0.75));
        }

        [Test]
        public void MissingLibraryIsEmptyAndPresetsBindingMissingValuesFailToLoad()
        {
            var repository = new PresetRepository(_directory);
            var document = PresetDocument.CreateNew();
            repository.Save(document);

            Assert.That(repository.LoadValues(), Is.Empty);
            Assert.Throws<ArgumentException>(() => repository.Load(document.Id));
            Assert.DoesNotThrow(() => repository.Load(document.Id, document.Values));
        }

        [Test]
        public void ListReportsAnUnreadableLibraryOnceAndLoadsNoPresets()
        {
            var repository = new PresetRepository(_directory);
            var document = PresetDocument.CreateNew();
            repository.SaveValues(document.Values);
            repository.Save(document);
            File.WriteAllText(repository.ValuesPath, "{\"SchemaVersion\":2,\"Values\":[]}");

            var listed = repository.List(out var errors);

            Assert.That(listed, Is.Empty);
            Assert.That(errors.Count, Is.EqualTo(1));
            StringAssert.Contains(PresetRepository.ValuesFileName, errors[0]);
        }

        [Test]
        public void InvalidValuesAreNotSaved()
        {
            var repository = new PresetRepository(_directory);
            var values = PresetDocument.CreateNew().Values;
            repository.SaveValues(values);
            var saved = File.ReadAllText(repository.ValuesPath);
            values.Add(new PresetValue { Id = values[0].Id, Name = "Duplicate" });

            Assert.Throws<ArgumentException>(() => repository.SaveValues(values));
            Assert.That(File.ReadAllText(repository.ValuesPath), Is.EqualTo(saved));
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
