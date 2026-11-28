using System;
using Krab.TypeLayout.Core.Text;
using NUnit.Framework;

namespace Krab.TypeLayout.Tests.Editor
{
    public sealed class TextTemplateTests
    {
        private static string Resolve(string name)
        {
            return name switch
            {
                "main" => "Kimura",
                "bpm" => "128",
                "empty" => string.Empty,
                _ => null
            };
        }

        [Test]
        public void ReplacesKnownTokensAndKeepsTheRest()
        {
            Assert.That(TextTemplate.Expand("{main} at {bpm} BPM", Resolve), Is.EqualTo("Kimura at 128 BPM"));
            Assert.That(TextTemplate.Expand("{main}{main}", Resolve), Is.EqualTo("KimuraKimura"));
            Assert.That(TextTemplate.Expand("[{empty}]", Resolve), Is.EqualTo("[]"));
            Assert.That(TextTemplate.Expand("{unknown} {main}", Resolve), Is.EqualTo("{unknown} Kimura"));
        }

        [Test]
        public void ModifiersChangeCaseAndUnknownModifiersStayLiteral()
        {
            Assert.That(TextTemplate.Expand("{main:upper}", Resolve), Is.EqualTo("KIMURA"));
            Assert.That(TextTemplate.Expand("{ main : lower }", Resolve), Is.EqualTo("kimura"));
            Assert.That(TextTemplate.Expand("{main:bold}", Resolve), Is.EqualTo("{main:bold}"));
        }

        [Test]
        public void PlainTextAndBrokenBracesPassThrough()
        {
            const string plain = "MIDDLE MODEL";
            Assert.That(TextTemplate.Expand(plain, Resolve), Is.SameAs(plain));
            Assert.That(TextTemplate.Expand("a { b", Resolve), Is.EqualTo("a { b"));
            Assert.That(TextTemplate.Expand("} {main", Resolve), Is.EqualTo("} {main"));
            Assert.That(TextTemplate.Expand(null, Resolve), Is.Null);
            Assert.Throws<ArgumentNullException>(() => TextTemplate.Expand("{main}", null));
        }
    }
}
