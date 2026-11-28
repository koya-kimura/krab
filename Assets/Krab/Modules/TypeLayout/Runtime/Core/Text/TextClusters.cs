using System;
using System.Collections.Generic;
using System.Globalization;

namespace Krab.TypeLayout.Core.Text
{
    public static class TextClusters
    {
        /// <summary>
        /// Splits into user-perceived characters, so combining marks and surrogate pairs stay with their base.
        /// </summary>
        public static string[] Split(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<string>();

            var clusters = new List<string>(text.Length);
            var enumerator = StringInfo.GetTextElementEnumerator(text);
            while (enumerator.MoveNext())
                clusters.Add(enumerator.GetTextElement());

            return clusters.ToArray();
        }
    }
}
