using System;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>A signal slot's immutable source and ADSR release policy. No runtime state.</summary>
    public sealed class ModulatorSlotDefinition
    {
        public AdsrDefinition Adsr { get; }
        public LfoDefinition Lfo { get; }
        public bool AutoRelease { get; }
        internal object Source => (object)Adsr ?? Lfo;

        public ModulatorSlotDefinition(AdsrDefinition definition, bool autoRelease = true)
        {
            Adsr = definition ?? throw new ArgumentNullException(nameof(definition));
            AutoRelease = autoRelease;
        }

        public ModulatorSlotDefinition(LfoDefinition definition)
        {
            Lfo = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        internal IModulator Create(VoiceTime time) => Adsr != null
            ? (IModulator)new AdsrInstance(Adsr, time, AutoRelease)
            : new LfoInstance(Lfo, time);
    }
}
