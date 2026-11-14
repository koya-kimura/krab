using System;
using System.Collections.Generic;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>Immutable ordered children. Pivot is always (0,0); bounds never alter it.</summary>
    public sealed class GroupDefinition : INodeDefinition
    {
        public TransformDefinition Transform { get; }
        public GroupRuntimeDefinition Runtime { get; }
        public IReadOnlyList<INodeDefinition> Children { get; }

        public GroupDefinition(TransformDefinition transform = null, params INodeDefinition[] children)
            : this(transform, null, children) { }

        public GroupDefinition(TransformDefinition transform, GroupRuntimeDefinition runtime,
            params INodeDefinition[] children)
        {
            Runtime = runtime;
            if (children == null) throw new ArgumentNullException(nameof(children));
            var copy = (INodeDefinition[])children.Clone();
            foreach (var child in copy)
                if (child == null) throw new ArgumentException("Group children cannot contain null.", nameof(children));
            Transform = transform ?? new TransformDefinition();
            Children = Array.AsReadOnly(copy);
        }
    }
}
