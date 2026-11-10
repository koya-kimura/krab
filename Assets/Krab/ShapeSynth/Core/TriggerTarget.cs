using System;

namespace Krab.ShapeSynth.Core
{
    /// <summary>Identity of one trigger placement. Two targets may share the same immutable Group.</summary>
    public sealed class TriggerTarget
    {
        public GroupDefinition Definition { get; }
        public TriggerTarget(GroupDefinition definition) =>
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }
}
