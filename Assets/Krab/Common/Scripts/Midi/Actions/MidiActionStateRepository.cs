using System.Collections.Generic;
using PrefsGUI;

namespace Krab.Common.Midi.Actions
{
    public sealed class MidiActionStateRepository
    {
        private readonly Dictionary<string, PrefsBool> _bools = new();
        private readonly Dictionary<string, PrefsInt> _ints = new();
        private readonly Dictionary<string, PrefsString> _strings = new();

        public bool LoadBool(string actionId, bool defaultValue)
        {
            return GetBool(Key(actionId, "Toggle"), defaultValue).Get();
        }

        public void SaveBool(string actionId, bool value)
        {
            GetBool(Key(actionId, "Toggle"), value).Set(value);
        }

        public int LoadRadioIndex(string actionId, int defaultValue)
        {
            return GetInt(Key(actionId, "Radio.SelectedIndex"), defaultValue).Get();
        }

        public void SaveRadioIndex(string actionId, int value)
        {
            GetInt(Key(actionId, "Radio.SelectedIndex"), value).Set(value);
        }

        public int LoadStateIndex(string actionId, int defaultValue)
        {
            return GetInt(Key(actionId, "State.Index"), defaultValue).Get();
        }

        public void SaveStateIndex(string actionId, int value)
        {
            GetInt(Key(actionId, "State.Index"), value).Set(value);
        }

        public string LoadSequencePattern(string actionId, string defaultValue)
        {
            return GetString(Key(actionId, "Sequence.Pattern"), defaultValue).Get();
        }

        public void SaveSequencePattern(string actionId, string value)
        {
            GetString(Key(actionId, "Sequence.Pattern"), value).Set(value);
        }

        private static string Key(string actionId, string suffix)
        {
            return $"Midi.Action.{actionId}.{suffix}";
        }

        private PrefsBool GetBool(string key, bool defaultValue)
        {
            if (_bools.TryGetValue(key, out var prefs)) return prefs;

            prefs = new PrefsBool(key, defaultValue);
            _bools.Add(key, prefs);
            return prefs;
        }

        private PrefsInt GetInt(string key, int defaultValue)
        {
            if (_ints.TryGetValue(key, out var prefs)) return prefs;

            prefs = new PrefsInt(key, defaultValue);
            _ints.Add(key, prefs);
            return prefs;
        }

        private PrefsString GetString(string key, string defaultValue)
        {
            if (_strings.TryGetValue(key, out var prefs)) return prefs;

            prefs = new PrefsString(key, defaultValue);
            _strings.Add(key, prefs);
            return prefs;
        }
    }
}
