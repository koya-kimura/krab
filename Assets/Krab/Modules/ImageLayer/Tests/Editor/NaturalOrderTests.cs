using System.Collections.Generic;
using Krab.ImageLayer.Core.Library;
using NUnit.Framework;

namespace Krab.ImageLayer.Tests.Editor
{
    public sealed class NaturalOrderTests
    {
        [Test]
        public void DigitRunsCompareByValue()
        {
            var names = new List<string> { "10.png", "2.png", "1.png", "100.png", "20.png" };
            names.Sort(NaturalOrder.Instance);
            Assert.That(names, Is.EqualTo(new[] { "1.png", "2.png", "10.png", "20.png", "100.png" }));
        }

        [Test]
        public void ZeroPaddedAndPlainNumbersInterleaveByValue()
        {
            var names = new List<string> { "010.png", "9.png", "002.png", "1.png" };
            names.Sort(NaturalOrder.Instance);
            Assert.That(names, Is.EqualTo(new[] { "1.png", "002.png", "9.png", "010.png" }));
        }

        [Test]
        public void EqualValuesAreStillOrderedSoTheSortIsTotal()
        {
            Assert.That(NaturalOrder.Instance.Compare("1.png", "01.png"), Is.LessThan(0));
            Assert.That(NaturalOrder.Instance.Compare("01.png", "1.png"), Is.GreaterThan(0));
            Assert.That(NaturalOrder.Instance.Compare("a.png", "A.png"), Is.Not.EqualTo(0));
            Assert.That(NaturalOrder.Instance.Compare("walk", "walk"), Is.EqualTo(0));
        }

        [Test]
        public void TextComparesIgnoringCaseAndNumbersInsideNames()
        {
            var names = new List<string> { "Walk10", "walk2", "dance", "Walk", "walk2b" };
            names.Sort(NaturalOrder.Instance);
            Assert.That(names, Is.EqualTo(new[] { "dance", "Walk", "walk2", "walk2b", "Walk10" }));
        }

        [Test]
        public void VeryLongNumbersDoNotOverflow()
        {
            Assert.That(NaturalOrder.Instance.Compare("99999999999999999999", "100000000000000000000"),
                Is.LessThan(0));
        }
    }
}
