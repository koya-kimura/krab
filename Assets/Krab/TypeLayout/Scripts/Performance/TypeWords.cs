using System;
using System.Collections.Generic;
using System.Globalization;
using Krab.TypeLayout.Core.Text;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance
{
    /// <summary>
    /// The strings every pattern shows, edited in one place. Patterns hold templates such as "{main}" or
    /// "{time}" and resolve them here once per frame; results are cached until a value changes, so unchanged
    /// text keeps the same string instance (and the drawer's measurement cache).
    /// </summary>
    public sealed class TypeWords
    {
        public const string DefaultTimeFormat = "HH:mm:ss";
        public const string DefaultDateFormat = "yyyy.MM.dd";

        private const string TokenHelp =
            "{main} {sub}  chosen words\n" +
            "{word1} {word2} ...  words by number\n" +
            "{words}  all words separated by spaces\n" +
            "{list}  all words separated by | (Scatter)\n" +
            "{event}  event name\n" +
            "{time} {date}  now, in the formats above\n" +
            "{bpm}  tempo, {bar} {beat}  position (4/4), {position}  bar.beat\n" +
            "Add :upper or :lower to change case, e.g. {main:upper}.";

        private readonly PrefsList<string> _words = new("TypeLayout Words",
            new List<string> { "KIMURA", "Middle Model", "KRAB" });
        private readonly PrefsInt _mainIndex = new("TypeLayout Words Main", 0);
        private readonly PrefsInt _subIndex = new("TypeLayout Words Sub", 1);
        private readonly PrefsString _eventName = new("TypeLayout Words Event", "MIDDLE MODEL NIGHT");
        private readonly PrefsString _timeFormat = new("TypeLayout Words Time Format", DefaultTimeFormat);
        private readonly PrefsString _dateFormat = new("TypeLayout Words Date Format", DefaultDateFormat);

        private readonly Dictionary<string, string> _resolved = new();
        private readonly List<string> _wordsSnapshot = new();
        private readonly Func<string, string> _resolveToken;

        private string _main = string.Empty;
        private string _sub = string.Empty;
        private string _event = string.Empty;
        private string _time = string.Empty;
        private string _date = string.Empty;
        private string _bpm = string.Empty;
        private long _beatIndex = long.MinValue;
        private string _joined = string.Empty;
        private string _list = string.Empty;

        public TypeWords()
        {
            _resolveToken = ResolveToken;
        }

        /// <summary>Increments whenever the word list itself changes (used to rebuild the Main/Sub choices).</summary>
        public int WordsVersion { get; private set; }

        public string FormatError { get; private set; }

        /// <summary>Takes this frame's values; drops cached results when any of them changed.</summary>
        public void BeginFrame(double beats, float bpm, DateTime now)
        {
            var changed = UpdateWords();
            changed |= Assign(ref _main, Word(_mainIndex.Get()));
            changed |= Assign(ref _sub, Word(_subIndex.Get()));
            changed |= Assign(ref _event, _eventName.Get() ?? string.Empty);
            changed |= Assign(ref _bpm, bpm.ToString("0", CultureInfo.InvariantCulture));

            FormatError = null;
            changed |= Assign(ref _time, Format(now, _timeFormat.Get(), DefaultTimeFormat));
            changed |= Assign(ref _date, Format(now, _dateFormat.Get(), DefaultDateFormat));

            var beatIndex = (long)Math.Floor(beats);
            if (beatIndex != _beatIndex)
            {
                _beatIndex = beatIndex;
                changed = true;
            }

            if (changed) _resolved.Clear();
        }

        /// <summary>The template with every token replaced by this frame's value.</summary>
        public string Resolve(string template)
        {
            if (string.IsNullOrEmpty(template) || template.IndexOf('{') < 0) return template ?? string.Empty;
            if (_resolved.TryGetValue(template, out var resolved)) return resolved;

            resolved = TextTemplate.Expand(template, _resolveToken);
            _resolved.Add(template, resolved);
            return resolved;
        }

        public Element CreateElement()
        {
            return UI.Column(
                UI.Label("Patterns show these through tokens such as {main}.\n" +
                         "Edit a pattern's text to change which token it uses."),
                _words.CreateElement("Words"),
                UI.DynamicElementOnStatusChanged(() => WordsVersion, _ => UI.Column(
                    WordChoice("Main", _mainIndex),
                    WordChoice("Sub", _subIndex))),
                UI.Space(),
                _eventName.CreateElement("Event"),
                _timeFormat.CreateElement("Time Format"),
                UI.Label(() => $"    {_time}"),
                _dateFormat.CreateElement("Date Format"),
                UI.Label(() => $"    {_date}"),
                UI.DynamicElementIf(() => FormatError != null,
                    () => UI.HelpBox(UI.Label(() => FormatError), HelpBoxType.Warning)),
                UI.Fold("Tokens", UI.Label(TokenHelp)));
        }

        private Element WordChoice(string label, PrefsInt index)
        {
            var options = _wordsSnapshot.Count == 0 ? new[] { "(no words)" } : _wordsSnapshot.ToArray();
            return UI.Dropdown(label, () => Math.Clamp(index.Get(), 0, options.Length - 1),
                value => index.Set(value), options);
        }

        private string ResolveToken(string name)
        {
            switch (name)
            {
                case "main":
                    return _main;
                case "sub":
                    return _sub;
                case "event":
                    return _event;
                case "time":
                    return _time;
                case "date":
                    return _date;
                case "bpm":
                    return _bpm;
                case "words":
                    return _joined;
                case "list":
                    return _list;
                case "bar":
                    return (Math.Floor(_beatIndex / 4.0) + 1).ToString("0", CultureInfo.InvariantCulture);
                case "beat":
                    return (((_beatIndex % 4) + 4) % 4 + 1).ToString(CultureInfo.InvariantCulture);
                case "position":
                    return $"{ResolveToken("bar")}.{ResolveToken("beat")}";
            }

            if (name.StartsWith("word", StringComparison.Ordinal)
                && int.TryParse(name.Substring(4), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                return number >= 1 && number <= _wordsSnapshot.Count ? _wordsSnapshot[number - 1] : string.Empty;

            return null;
        }

        private bool UpdateWords()
        {
            var words = _words.Get();
            var count = words?.Count ?? 0;
            var same = count == _wordsSnapshot.Count;
            for (var i = 0; same && i < count; i++)
                same = string.Equals(words[i], _wordsSnapshot[i], StringComparison.Ordinal);

            if (same) return false;

            _wordsSnapshot.Clear();
            for (var i = 0; i < count; i++)
                _wordsSnapshot.Add(words[i] ?? string.Empty);

            _joined = string.Join(" ", _wordsSnapshot);
            _list = string.Join("|", _wordsSnapshot);
            WordsVersion++;
            return true;
        }

        private string Word(int index)
        {
            return index >= 0 && index < _wordsSnapshot.Count ? _wordsSnapshot[index] : string.Empty;
        }

        private string Format(DateTime now, string format, string fallback)
        {
            try
            {
                return now.ToString(string.IsNullOrEmpty(format) ? fallback : format, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                FormatError = $"Invalid date/time format \"{format}\"; showing {fallback}.";
                return now.ToString(fallback, CultureInfo.InvariantCulture);
            }
        }

        private static bool Assign(ref string field, string value)
        {
            if (string.Equals(field, value, StringComparison.Ordinal)) return false;

            field = value;
            return true;
        }
    }
}
