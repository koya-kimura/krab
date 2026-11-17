using System.Text;

namespace Krab.TypeLayout.Performance
{
    /// <summary>Text helpers for the settings UI, whose labels never wrap on their own.</summary>
    public static class UiText
    {
        /// <summary>
        /// Breaks at spaces so no line exceeds <paramref name="maxLineLength"/> characters (long words stay whole).
        /// </summary>
        public static string Wrap(string text, int maxLineLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLineLength) return text;

            var builder = new StringBuilder(text.Length + 8);
            var lineLength = 0;
            foreach (var word in text.Split(' '))
            {
                if (lineLength > 0 && lineLength + 1 + word.Length > maxLineLength)
                {
                    builder.Append('\n');
                    lineLength = 0;
                }
                else if (lineLength > 0)
                {
                    builder.Append(' ');
                    lineLength++;
                }

                builder.Append(word);
                lineLength += word.Length;
            }

            return builder.ToString();
        }
    }
}
