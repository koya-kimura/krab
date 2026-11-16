using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Performance;

namespace Krab.ShapeSynth.Core.Presets
{
    public enum PresetNodeKind
    {
        Group,
        Circle,
        Rectangle,
        Polygon
    }

    /// <summary>Mutable authoring model. Runtime definitions are always independent snapshots.</summary>
    [Serializable]
    public sealed class PresetDocument
    {
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion = CurrentSchemaVersion;
        public string Id;
        public string Name = "New Preset";
        public List<PresetValue> Values = new();
        public PresetNode RootGroup;

        public static string NewId() => Guid.NewGuid().ToString("N");

        public static void RequireId(string id)
        {
            if (id == null || !Regex.IsMatch(id, @"\A[a-zA-Z0-9][a-zA-Z0-9_-]{0,79}\z"))
            {
                throw new ArgumentException(
                    "ID must be 1–80 ASCII letters, numbers, underscores or hyphens, starting with a letter or number.");
            }
        }

        public static PresetDocument CreateNew()
        {
            var envelope = new PresetValue
            {
                Id = NewId(),
                Name = "Default ADSR",
                Components = new List<PresetComponent> { PresetComponent.Create(ModulatorKind.Adsr) }
            };
            var root = PresetNode.Create(PresetNodeKind.Group);
            root.Name = "Root Group";
            var circle = PresetNode.Create(PresetNodeKind.Circle);
            circle.Opacity.Base = 0;
            circle.Opacity.Bind(envelope.Id, 1);
            root.Children.Add(circle);
            return new PresetDocument
            {
                Id = NewId(),
                RootGroup = root,
                Values = new List<PresetValue> { envelope }
            };
        }

        public IEnumerable<PresetNode> Nodes() => Walk(RootGroup);

        public PresetNode AddNode(string parentId, PresetNodeKind kind)
        {
            var parent = Nodes().SingleOrDefault(node => node.Id == parentId);
            if (parent == null || parent.Kind != PresetNodeKind.Group)
                throw new ArgumentException("Select a Group to add a child.");

            var child = PresetNode.Create(kind);
            parent.Children.Add(child);
            return child;
        }

        /// <summary>
        /// Moves a node under another Group. insertionIndex refers to the target's child list before removal.
        /// Moving within one Group adjusts the final index when removal shifts that list.
        /// </summary>
        public void MoveNode(string nodeId, string parentId, int insertionIndex)
        {
            Validate();
            var nodes = Nodes().ToArray();
            var node = nodes.SingleOrDefault(candidate => candidate.Id == nodeId);
            if (node == null) throw new ArgumentException("Node was not found.", nameof(nodeId));
            if (ReferenceEquals(node, RootGroup)) throw new ArgumentException("Root Group cannot be moved.", nameof(nodeId));

            var targetParent = nodes.SingleOrDefault(candidate => candidate.Id == parentId);
            if (targetParent == null || targetParent.Kind != PresetNodeKind.Group)
                throw new ArgumentException("Destination must be a Group.", nameof(parentId));

            if (ContainsNode(node, targetParent))
                throw new ArgumentException("A Node cannot be moved into itself or one of its descendants.", nameof(parentId));

            if (insertionIndex < 0 || insertionIndex > targetParent.Children.Count)
                throw new ArgumentOutOfRangeException(nameof(insertionIndex));

            var destinationDepth = FindDepth(RootGroup, targetParent, 0);
            var movedSubtreeHeight = MaximumDepth(node, 0);
            if (destinationDepth + 1 + movedSubtreeHeight > 64)
                throw new ArgumentException("Move would exceed the maximum hierarchy depth of 64.", nameof(parentId));

            PresetNode sourceParent = null;
            var sourceIndex = -1;
            foreach (var candidate in nodes)
            {
                if (candidate.Kind != PresetNodeKind.Group) continue;
                var index = candidate.Children.FindIndex(child => ReferenceEquals(child, node));
                if (index < 0) continue;
                sourceParent = candidate;
                sourceIndex = index;
                break;
            }
            if (sourceParent == null) throw new ArgumentException("Node has no parent in this document.", nameof(nodeId));

            var finalIndex = insertionIndex;
            if (ReferenceEquals(sourceParent, targetParent) && sourceIndex < insertionIndex)
                finalIndex--;

            sourceParent.Children.RemoveAt(sourceIndex);
            targetParent.Children.Insert(finalIndex, node);
        }

        private static IEnumerable<PresetNode> Walk(PresetNode node)
        {
            yield return node;

            foreach (var child in node.Children)
            {
                foreach (var descendant in Walk(child))
                {
                    yield return descendant;
                }
            }
        }

        private static bool ContainsNode(PresetNode root, PresetNode candidate)
        {
            if (ReferenceEquals(root, candidate)) return true;

            foreach (var child in root.Children)
            {
                if (ContainsNode(child, candidate)) return true;
            }

            return false;
        }

        private static int FindDepth(PresetNode root, PresetNode target, int depth)
        {
            if (ReferenceEquals(root, target)) return depth;

            foreach (var child in root.Children)
            {
                var childDepth = FindDepth(child, target, depth + 1);
                if (childDepth >= 0) return childDepth;
            }

            return -1;
        }

        private static int MaximumDepth(PresetNode node, int depth)
        {
            var maximum = depth;
            foreach (var child in node.Children)
                maximum = Math.Max(maximum, MaximumDepth(child, depth + 1));

            return maximum;
        }

        public bool DeleteNode(string id)
        {
            if (RootGroup.Id == id) throw new ArgumentException("Root Group cannot be deleted.");

            foreach (var group in Nodes().Where(node => node.Kind == PresetNodeKind.Group))
            {
                var index = group.Children.FindIndex(node => node.Id == id);
                if (index < 0) continue;
                group.Children.RemoveAt(index);
                return true;
            }

            return false;
        }

        public void DeleteValue(string id)
        {
            if (BindingCount(id) > 0) throw new ArgumentException("Value is in use. Unbind it first.");

            Values.RemoveAll(value => value.Id == id);
        }

        public int BindingCount(string valueId)
        {
            return Nodes()
                .SelectMany(node => node.Parameters())
                .SelectMany(parameter => parameter.Bindings)
                .Count(binding => binding.ValueId == valueId);
        }

        /// <summary>Validates a draft, including references/numbers, without requiring a Lifetime.</summary>
        public void Validate() => PresetCompiler.ValidateDocument(this);
    }

    [Serializable]
    public sealed class PresetNode
    {
        public string Id;
        public string Name;
        public PresetNodeKind Kind;
        public List<PresetNode> Children = new();
        public PresetParameter PositionX = new();
        public PresetParameter PositionY = new();
        public PresetParameter Rotation = new();
        public PresetParameter ScaleX = new() { Base = 1 };
        public PresetParameter ScaleY = new() { Base = 1 };
        public PresetParameter Width = new() { Base = 0.2, Reference = ReferenceSource.MinDimension };
        public PresetParameter Height = new() { Base = 0.2, Reference = ReferenceSource.MinDimension };
        public PresetParameter Opacity = new() { Base = 1 };
        public double Red = 1, Green = 1, Blue = 1, Alpha = 1;

        // Fill = blend(Color, To Color, ColorMix 0–1) with its hue rotated by HueShift degrees.
        public double ToRed = 1, ToGreen = 1, ToBlue = 1, ToAlpha = 1;
        public PresetParameter ColorMix = new();
        public PresetParameter HueShift = new();
        public bool Fill = true, Stroke;
        public double StrokeWidth = 1;
        public double StrokeRed = 1, StrokeGreen = 1, StrokeBlue = 1, StrokeAlpha = 1;
        public List<PresetPoint> Vertices = new();

        // Only used by Shapes; Groups keep the default None.
        public PresetLayout Layout = new();

        public static PresetNode Create(PresetNodeKind kind)
        {
            if (!Enum.IsDefined(typeof(PresetNodeKind), kind)) throw new ArgumentException("Unknown node kind.");

            var node = new PresetNode { Id = PresetDocument.NewId(), Name = kind.ToString(), Kind = kind };
            if (kind == PresetNodeKind.Polygon)
            {
                for (var i = 0; i < 3; i++)
                {
                    var angle = Math.PI / 2 + i * Math.PI * 2 / 3;
                    node.Vertices.Add(new PresetPoint { X = Math.Cos(angle) * 100, Y = Math.Sin(angle) * 100 });
                }
            }

            return node;
        }

        public IEnumerable<PresetParameter> Parameters()
        {
            yield return PositionX;
            yield return PositionY;
            yield return Rotation;
            yield return ScaleX;
            yield return ScaleY;
            yield return Width;
            yield return Height;
            yield return Opacity;
            yield return ColorMix;
            yield return HueShift;
            yield return Layout.SpacingX;
            yield return Layout.SpacingY;
            yield return Layout.Radius;
        }
    }

    [Serializable]
    public sealed class PresetParameter
    {
        public ReferenceSource Reference;
        public double Multiplier = 1, AbsoluteValue = 1, Base;

        // Kept as a list for the JSON schema; a Document allows at most one Binding per Parameter.
        public List<PresetBinding> Bindings = new();

        /// <summary>The bound Value, or null when the Parameter only uses Base.</summary>
        public PresetBinding GetBinding()
        {
            return Bindings.Count == 0 ? null : Bindings[0];
        }

        /// <summary>Binds one Value, replacing any previous Binding.</summary>
        public PresetBinding Bind(string valueId, double depth)
        {
            if (valueId == null) throw new ArgumentNullException(nameof(valueId));

            var binding = new PresetBinding { ValueId = valueId, Depth = depth };
            Bindings.Clear();
            Bindings.Add(binding);
            return binding;
        }

        public void Unbind()
        {
            Bindings.Clear();
        }
    }

    [Serializable]
    public sealed class PresetBinding
    {
        public string ValueId;
        public double Depth = 1;
    }

    [Serializable]
    public sealed class PresetPoint
    {
        public double X, Y;
    }
}
