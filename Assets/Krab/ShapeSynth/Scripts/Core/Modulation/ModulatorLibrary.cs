using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;

namespace Krab.ShapeSynth.Core.Modulation
{
    /// <summary>An immutable named modulator definition.</summary>
    public sealed class ModulatorEntry
    {
        public string Id { get; }
        public string Name { get; }
        public AdsrDefinition Adsr { get; }
        public LfoDefinition Lfo { get; }
        public ModulatorSlotDefinition Slot { get; }

        public ModulatorEntry(string id, string name, AdsrDefinition definition)
        {
            ValidateIdentity(id, name);
            Id = id;
            Name = name;
            Adsr = definition ?? throw new ArgumentNullException(nameof(definition));
            Slot = new ModulatorSlotDefinition(Adsr);
        }

        public ModulatorEntry(string id, string name, LfoDefinition definition)
        {
            ValidateIdentity(id, name);
            Id = id;
            Name = name;
            Lfo = definition ?? throw new ArgumentNullException(nameof(definition));
            Slot = new ModulatorSlotDefinition(Lfo);
        }

        private static void ValidateIdentity(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An ID is required.", nameof(id));
            if (name == null) throw new ArgumentNullException(nameof(name));
        }
    }

    /// <summary>An immutable snapshot of reusable modulators, with one slot per entry.</summary>
    public sealed class ModulatorLibrary
    {
        private readonly List<ModulatorEntry> _entries = new();
        private readonly Dictionary<string, ModulatorEntry> _byId = new(StringComparer.Ordinal);
        private readonly Dictionary<ModulatorEntry, ModulatorSlotDefinition> _slots = new();
        private readonly IReadOnlyList<ModulatorEntry> _readOnlyEntries;

        public IReadOnlyList<ModulatorEntry> Entries => _readOnlyEntries;

        public ModulatorLibrary(IEnumerable<ModulatorEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            foreach (var entry in entries)
            {
                if (entry == null)
                    throw new ArgumentException("The entry collection cannot contain null.", nameof(entries));
                if (_byId.ContainsKey(entry.Id))
                    throw new ArgumentException($"Duplicate modulator ID '{entry.Id}'.", nameof(entries));
                _entries.Add(entry);
                _byId.Add(entry.Id, entry);
                _slots.Add(entry, entry.Slot);
            }

            _readOnlyEntries = _entries.AsReadOnly();
        }

        public ModulatorEntry Get(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            if (_byId.TryGetValue(id, out var entry)) return entry;

            throw new ArgumentException($"No modulator with ID '{id}' exists in this library.", nameof(id));
        }

        public ModulatorSlotDefinition[] ResolveSlots(params string[] ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));

            var slots = new ModulatorSlotDefinition[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                var entry = Get(ids[i]);
                slots[i] = _slots[entry];
            }

            return slots;
        }
    }
}
