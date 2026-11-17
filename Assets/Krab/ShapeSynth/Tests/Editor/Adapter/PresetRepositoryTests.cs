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
        public void LayoutRoundTripKeepsOneSpacingBindingAndWritesNoDerivedValues()
        {
            var repository = new PresetRepository(_directory);
            var document = PresetDocument.CreateNew();
            var shape = document.RootGroup.Children[0];
            shape.Layout.Kind = PresetLayoutKind.Grid;
            shape.Layout.SpacingX.Bind(document.Values[0].Id, 0.25);

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
        public void LoadUpgradesSchemaV2ColorsToHsbParameters()
        {
            var repository = new PresetRepository(_directory);
            var current = PresetDocument.CreateNew();
            var envelopeId = current.Values[0].Id;
            var v2 = JObject.FromObject(current);
            v2["SchemaVersion"] = 2;
            var node = (JObject)v2["RootGroup"]["Children"][0];
            foreach (var field in new[]
                     {
                         "FillHue", "FillSaturation", "FillBrightness", "FillAlpha", "StrokeHue", "StrokeSaturation",
                         "StrokeBrightness", "StrokeAlpha"
                     })
            {
                node.Remove(field);
            }

            // Red blended halfway to blue (alpha 0.5 → 1), hue-shifted by 30° plus a bound 360° sweep.
            node["Red"] = 1;
            node["Green"] = 0;
            node["Blue"] = 0;
            node["Alpha"] = 0.5;
            node["ToRed"] = 0;
            node["ToGreen"] = 0;
            node["ToBlue"] = 1;
            node["ToAlpha"] = 1;
            node["ColorMix"] = new JObject { ["Base"] = 0.5 };
            node["HueShift"] = new JObject
            {
                ["Base"] = 30, ["Random"] = 5,
                ["Bindings"] = new JArray(new JObject { ["ValueId"] = envelopeId, ["Depth"] = 360 })
            };
            node["StrokeRed"] = 0;
            node["StrokeGreen"] = 1;
            node["StrokeBlue"] = 0;
            node["StrokeAlpha"] = 0.25;
            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath(current.Id + ".json"), v2.ToString());

            var loaded = repository.Load(current.Id);
            var shape = loaded.RootGroup.Children[0];

            Assert.That(loaded.SchemaVersion, Is.EqualTo(PresetDocument.CurrentSchemaVersion));
            // The resting fill (0.5, 0, 0.5) is hue 300°, saturation 1, brightness 0.5; the shift adds 30°.
            Assert.That(shape.FillHue.Base, Is.EqualTo(330).Within(1e-9));
            Assert.That(shape.FillHue.Random, Is.EqualTo(5));
            Assert.That(shape.FillHue.GetBinding().ValueId, Is.EqualTo(envelopeId));
            Assert.That(shape.FillHue.GetBinding().Depth, Is.EqualTo(360));
            Assert.That(shape.FillSaturation.Base, Is.EqualTo(1).Within(1e-9));
            Assert.That(shape.FillBrightness.Base, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(shape.FillAlpha, Is.EqualTo(0.75).Within(1e-9));
            Assert.That(shape.StrokeHue.Base, Is.EqualTo(120).Within(1e-9));
            Assert.That(shape.StrokeSaturation.Base, Is.EqualTo(1).Within(1e-9));
            Assert.That(shape.StrokeBrightness.Base, Is.EqualTo(1).Within(1e-9));
            Assert.That(shape.StrokeAlpha, Is.EqualTo(0.25));
            Assert.DoesNotThrow(() => PresetCompiler.Compile(loaded));
        }

        [Test]
        public void LoadUpgradesSchemaV3StrokeWidthNumbersToParameters()
        {
            var repository = new PresetRepository(_directory);
            var current = PresetDocument.CreateNew();
            var v3 = JObject.FromObject(current);
            v3["SchemaVersion"] = 3;
            v3["RootGroup"]["StrokeWidth"] = 1;
            v3["RootGroup"]["Children"][0]["StrokeWidth"] = 2.5;
            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath(current.Id + ".json"), v3.ToString());

            var loaded = repository.Load(current.Id);

            Assert.That(loaded.SchemaVersion, Is.EqualTo(PresetDocument.CurrentSchemaVersion));
            Assert.That(loaded.RootGroup.StrokeWidth.Base, Is.EqualTo(1));
            Assert.That(loaded.RootGroup.Children[0].StrokeWidth.Base, Is.EqualTo(2.5));
            Assert.That(loaded.RootGroup.Children[0].StrokeWidth.GetBinding(), Is.Null);
            Assert.DoesNotThrow(() => PresetCompiler.Compile(loaded));
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
