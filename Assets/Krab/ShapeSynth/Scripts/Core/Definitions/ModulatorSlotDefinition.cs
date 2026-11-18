using System;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>One signal slot of a Group scope. AutoRelease applies only to ADSR.</summary>
    public sealed class ModulatorSlotDefinition
    {
        public IModulatorDefinition Definition { get; }
        public bool AutoRelease { get; }

        public AdsrDefinition Adsr => Definition as AdsrDefinition;
        public LfoDefinition Lfo => Definition as LfoDefinition;

        public ModulatorSlotDefinition(AdsrDefinition definition, bool autoRelease = true)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            AutoRelease = autoRelease;
        }

        public ModulatorSlotDefinition(IModulatorDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            AutoRelease = true;
        }

        internal object Source => Definition;

        internal IModulator Create(VoiceTime time)
        {
            return Definition is AdsrDefinition adsr
                ? new AdsrInstance(adsr, time, AutoRelease)
                : Definition.Create(time);
        }
    }
}
