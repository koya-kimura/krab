namespace Krab.ShapeSynth.Performance
{
    public enum LoopNoteKind
    {
        Trigger,
        HoldStart,
        HoldEnd
    }

    /// <summary>
    /// One recorded performance event. A Hold is recorded as a start and an end that share a HoldId, so
    /// playback releases the same Voice it started.
    /// </summary>
    public readonly struct LoopNote
    {
        private LoopNote(LoopNoteKind kind, string presetId, int holdId)
        {
            Kind = kind;
            PresetId = presetId;
            HoldId = holdId;
        }

        public LoopNoteKind Kind { get; }

        public string PresetId { get; }

        public int HoldId { get; }

        public static LoopNote Trigger(string presetId) => new(LoopNoteKind.Trigger, presetId, 0);

        public static LoopNote HoldStart(string presetId, int holdId) => new(LoopNoteKind.HoldStart, presetId, holdId);

        public static LoopNote HoldEnd(string presetId, int holdId) => new(LoopNoteKind.HoldEnd, presetId, holdId);
    }
}
