using System;
using System.Linq;
using Krab.ShapeSynth.Core.Presets;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class PresetPathGroupsTests
    {
        [Test]
        public void NamedGroupsComeInOrdinalOrderAndUngroupedLast()
        {
            var groups = PresetPathGroups.Of(new[] { "solo", "fx/b", "adsr/a", "other" }, name => name);

            Assert.That(groups.Select(group => group.Group), Is.EqualTo(new[] { "adsr", "fx", "" }));
            Assert.That(groups[2].Entries.Select(entry => entry.Path.Title), Is.EqualTo(new[] { "other", "solo" }));
        }

        [Test]
        public void EntriesAreSortedByTitleAndKeepTheItem()
        {
            var groups = PresetPathGroups.Of(new[] { "fx/zeta", "fx/alpha", " fx / mid " }, name => name);

            Assert.That(groups, Has.Count.EqualTo(1));
            Assert.That(groups[0].Entries.Select(entry => entry.Path.Title), Is.EqualTo(new[] { "alpha", "mid", "zeta" }));
            Assert.That(groups[0].Entries[1].Item, Is.EqualTo(" fx / mid "));
        }

        [Test]
        public void SameTitlesKeepTheGivenOrder()
        {
            var items = new[] { (Id: 1, Name: "fx/a"), (Id: 2, Name: "fx/a"), (Id: 3, Name: "fx/a") };

            var groups = PresetPathGroups.Of(items, item => item.Name);

            Assert.That(groups[0].Entries.Select(entry => entry.Item.Id), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void NoItemsGiveNoGroups()
        {
            Assert.That(PresetPathGroups.Of(Array.Empty<string>(), name => name), Is.Empty);
        }

        [Test]
        public void RejectsMissingArguments()
        {
            Assert.Throws<ArgumentNullException>(() => PresetPathGroups.Of<string>(null, name => name));
            Assert.Throws<ArgumentNullException>(() => PresetPathGroups.Of(new[] { "a" }, null));
        }
    }
}
