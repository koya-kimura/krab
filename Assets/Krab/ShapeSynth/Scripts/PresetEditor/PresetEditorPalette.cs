using Krab.ShapeSynth.Core.Presets;
using UnityEngine;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>The few state colors layered on RosettaUI's default look.</summary>
    public static class PresetEditorPalette
    {
        public static readonly Color Title = new(0.62f, 0.67f, 0.74f);
        public static readonly Color Selected = new(0.18f, 0.4f, 0.62f);
        public static readonly Color DropCandidate = new(0.17f, 0.32f, 0.27f);
        public static readonly Color DropTarget = new(0.2f, 0.6f, 0.5f);
        public static readonly Color DragSource = new(0.5f, 0.5f, 0.5f);
        public static readonly Color Bound = new(0.55f, 0.85f, 0.7f);
        public static readonly Color Error = new(1f, 0.45f, 0.4f);
        public static readonly Color Adsr = new(0.35f, 0.75f, 0.95f);
        public static readonly Color Lfo = new(0.95f, 0.65f, 0.3f);
        public static readonly Color Ramp = new(0.75f, 0.55f, 0.95f);

        public static Color Modulator(ModulatorKind kind)
        {
            switch (kind)
            {
                case ModulatorKind.Adsr:
                    return Adsr;
                case ModulatorKind.Ramp:
                    return Ramp;
                default:
                    return Lfo;
            }
        }
    }
}
