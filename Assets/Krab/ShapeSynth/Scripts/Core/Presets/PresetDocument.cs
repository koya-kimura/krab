using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Krab.ShapeSynth.Core.Definitions;

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
        public const int CurrentSchemaVersion = 7;

        /// <summary>Deepest allowed Node, counting the Root Group as 0.</summary>
        public const int MaxDepth = 64;

        public int SchemaVersion = CurrentSchemaVersion;
        public string Id;
        public string Name = "New Preset";

        /// <summary>
        /// The Values that Bindings refer to. Not saved with the preset: the app attaches the shared Value library,
        /// so editing a Value changes every preset bound to it.
        /// </summary>
        [NonSerialized]
        public List<PresetValue> Values = new();

        /// <summary>
        /// Values that belong to this preset only, saved in its file (SchemaVersion 7). Editing one changes no
        /// other preset. IDs are unique across both lists, so a Binding's ValueId finds exactly one Value.
        /// </summary>
        public List<PresetValue> LocalValues = new();

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

        public static PresetDocument CreateNew() => CreateNew(new List<PresetValue>());

        /// <summary>
        /// One Circle whose Opacity follows the first Value with an ADSR. Adds a Default ADSR to the given Values
        /// only when none of them has one.
        /// </summary>
        public static PresetDocument CreateNew(List<PresetValue> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            var envelope = values.FirstOrDefault(value =>
                value?.Components != null && value.Components.Any(component => component?.Kind == ModulatorKind.Adsr));
            if (envelope == null)
            {
                envelope = new PresetValue
                {
                    Id = NewId(),
                    Name = "Default ADSR",
                    Components = new List<PresetComponent> { PresetComponent.Create(ModulatorKind.Adsr) }
                };
                values.Add(envelope);
            }

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
                Values = values
            };
        }

        public IEnumerable<PresetNode> Nodes() => Walk(RootGroup);

        /// <summary>The shared Values, then this preset's local Values.</summary>
        public IEnumerable<PresetValue> AllValues() => Values.Concat(LocalValues);

        public bool IsLocal(string valueId) => LocalValues.Any(value => value.Id == valueId);

        /// <summary>Adds a local copy of a Value (shared or local) under a new ID and returns it.</summary>
        public PresetValue AddLocalCopy(PresetValue source, string name)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var copy = source.Duplicate(NewId(), name);
            LocalValues.Add(copy);
            return copy;
        }

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
            var error = MoveError(nodeId, parentId, insertionIndex, out var node, out var sourceParent,
                out var targetParent);
            if (error != null) throw new ArgumentException(error);

            var sourceIndex = sourceParent.Children.IndexOf(node);
            var finalIndex = insertionIndex;
            if (ReferenceEquals(sourceParent, targetParent) && sourceIndex < insertionIndex)
                finalIndex--;

            sourceParent.Children.RemoveAt(sourceIndex);
            targetParent.Children.Insert(finalIndex, node);
        }

        /// <summary>
        /// Whether MoveNode would accept these arguments, without changing the document or validating the rest.
        /// Editors use it to show drop targets with the same rules as the move itself.
        /// </summary>
        public bool CanMoveNode(string nodeId, string parentId, int insertionIndex)
        {
            return MoveError(nodeId, parentId, insertionIndex, out _, out _, out _) == null;
        }

        /// <summary>Why the move is not allowed, or null when it is.</summary>
        private string MoveError(
            string nodeId,
            string parentId,
            int insertionIndex,
            out PresetNode node,
            out PresetNode sourceParent,
            out PresetNode targetParent)
        {
            targetParent = null;
            if (!Locate(nodeId, out node, out sourceParent, out _)) return "Node was not found.";
            if (sourceParent == null) return "Root Group cannot be moved.";
            if (!Locate(parentId, out targetParent, out _, out var destinationDepth)
                || targetParent.Kind != PresetNodeKind.Group)
                return "Destination must be a Group.";
            if (ContainsNode(node, targetParent)) return "A Node cannot be moved into itself or one of its descendants.";
            if (insertionIndex < 0 || insertionIndex > targetParent.Children.Count)
                return "Insertion index is out of range.";
            if (destinationDepth + 1 + MaximumDepth(node, 0) > MaxDepth)
                return $"Move would exceed the maximum hierarchy depth of {MaxDepth}.";

            return null;
        }

        /// <summary>Finds a node, its parent (null for the Root Group) and its depth in one walk.</summary>
        private bool Locate(string id, out PresetNode node, out PresetNode parent, out int depth)
        {
            var pending = new Stack<(PresetNode Node, PresetNode Parent, int Depth)>();
            pending.Push((RootGroup, null, 0));
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (current.Node.Id == id)
                {
                    (node, parent, depth) = current;
                    return true;
                }

                foreach (var child in current.Node.Children)
                {
                    pending.Push((child, current.Node, current.Depth + 1));
                }
            }

            (node, parent, depth) = (null, null, -1);
            return false;
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
            LocalValues.RemoveAll(value => value.Id == id);
        }

        /// <summary>The Values this preset binds, in Value order; only these become its modulators.</summary>
        public IEnumerable<PresetValue> BoundValues()
        {
            var bound = new HashSet<string>(Nodes()
                .SelectMany(node => node.Parameters())
                .SelectMany(parameter => parameter.Bindings)
                .Select(binding => binding.ValueId), StringComparer.Ordinal);
            return AllValues().Where(value => bound.Contains(value.Id));
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

        // Distance moves the node along Direction (degrees, 0 = right, counterclockwise) on top of Position.
        public PresetParameter Direction = new();
        public PresetParameter Distance = new();
        public PresetParameter Rotation = new();
        public PresetParameter ScaleX = new() { Base = 1 };
        public PresetParameter ScaleY = new() { Base = 1 };
        public PresetParameter Width = new() { Base = 0.2, Reference = ReferenceSource.MinDimension };
        public PresetParameter Height = new() { Base = 0.2, Reference = ReferenceSource.MinDimension };
        public PresetParameter Opacity = new() { Base = 1 };

        // Fill and stroke colors are the same HSB form: Hue in degrees, Saturation and Brightness 0–1, and a
        // constant Alpha. The defaults are white.
        public bool Fill = true, Stroke;
        public PresetParameter FillHue = new();
        public PresetParameter FillSaturation = new();
        public PresetParameter FillBrightness = new() { Base = 1 };
        public double FillAlpha = 1;
        public PresetParameter StrokeWidth = new() { Base = 1 };
        public PresetParameter StrokeHue = new();
        public PresetParameter StrokeSaturation = new();
        public PresetParameter StrokeBrightness = new() { Base = 1 };
        public double StrokeAlpha = 1;
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
            yield return Direction;
            yield return Distance;
            yield return Rotation;
            yield return ScaleX;
            yield return ScaleY;
            yield return Width;
            yield return Height;
            yield return Opacity;
            yield return FillHue;
            yield return FillSaturation;
            yield return FillBrightness;
            yield return StrokeWidth;
            yield return StrokeHue;
            yield return StrokeSaturation;
            yield return StrokeBrightness;
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

        // ±Random is added to Base once per copy and trigger (Voice seed). Flip reverses Depth on some copies.
        public double Random;
        public PresetFlip Flip;

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
