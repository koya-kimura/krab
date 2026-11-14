using System;
using System.Collections.Generic;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>
    /// An explicit signal scope at a Group occurrence. Bindings use this ordered slot list.
    /// Repeated references to one source Definition share a state within this scope only.
    /// </summary>
    public sealed class GroupRuntimeDefinition
    {
        public IReadOnlyList<ModulatorSlotDefinition> Modulators { get; }
        public bool InheritModulators { get; }
        public IReadOnlyList<TimerDefinition> Timers { get; }

        public GroupRuntimeDefinition(IReadOnlyList<ModulatorSlotDefinition> modulators = null,
            IReadOnlyList<TimerDefinition> timers = null)
        {
            InheritModulators = modulators == null;
            Modulators = Copy(modulators, nameof(modulators));
            Timers = Copy(timers, nameof(timers));
        }

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> input, string name) where T : class
        {
            var copy = new T[input?.Count ?? 0];
            for (var i = 0; i < copy.Length; i++)
            {
                copy[i] = input[i] ?? throw new ArgumentException("Runtime elements cannot contain null.", name);
            }

            return Array.AsReadOnly(copy);
        }
    }
}
