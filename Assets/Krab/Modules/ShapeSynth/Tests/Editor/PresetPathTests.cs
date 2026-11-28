using Krab.ShapeSynth.Core.Presets;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class PresetPathTests
    {
        [TestCase("fx/sample", "fx", "sample")]
        [TestCase("fx/drum/kick", "fx", "drum/kick")]
        [TestCase("sample", "", "sample")]
        [TestCase(" fx / sample ", "fx", "sample")]
        [TestCase("/fx//sample/", "fx", "sample")]
        [TestCase("fx/", "", "fx")]
        [TestCase("", "", "")]
        [TestCase(null, "", "")]
        public void SplitsTheFirstSegmentAsTheGroup(string name, string group, string title)
        {
            var path = PresetPath.Parse(name);

            Assert.That(path.Group, Is.EqualTo(group));
            Assert.That(path.Title, Is.EqualTo(title));
            Assert.That(path.HasGroup, Is.EqualTo(group.Length > 0));
        }

        [TestCase(" fx // drum / kick ", "fx/drum/kick")]
        public void ToStringIsTheNormalizedPath(string name, string expected)
        {
            Assert.That(PresetPath.Parse(name).ToString(), Is.EqualTo(expected));
        }
    }
}
