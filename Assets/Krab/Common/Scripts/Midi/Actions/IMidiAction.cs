using System;
using Krab.Common.Midi.Core;
using UnityEngine;

namespace Krab.Common.Midi.Actions
{
    public enum MidiActionKind
    {
        Toggle,
        Float,
        OneShot,
        Momentary,
        State,
        Radio,
        Sequence
    }

    public interface IMidiAction : IDisposable
    {
        MidiActionKind Kind { get; }
        string DisplayName { get; }
        void Execute(MidiMessage message);
        Color ResolveLedColor(Color configuredColor);
        IMidiActionRuntime Runtime { get; }
    }

    public interface ITransientMidiAction
    {
        void ResetTransientState();
    }

    internal static class MidiLedColor
    {
        public const float InactiveBrightness = 0.25f;

        public static Color Dim(Color color, float brightness = InactiveBrightness)
        {
            return new Color(color.r * brightness, color.g * brightness, color.b * brightness, color.a);
        }
    }
}
