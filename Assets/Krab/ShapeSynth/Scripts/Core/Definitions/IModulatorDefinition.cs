using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>Immutable settings of one signal kind. Each call creates an independent per-trigger state.</summary>
    public interface IModulatorDefinition
    {
        public IModulator Create(VoiceTime time);
    }
}
