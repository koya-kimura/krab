using System;
using System.Globalization;
using System.Text;

namespace Krab.TypeLayout.Core.Text
{
    /// <summary>
    /// Replaces <c>{name}</c> tokens through a resolver. <c>{name:upper}</c> and <c>{name:lower}</c> change the case.
    /// Tokens the resolver does not know (it returns null) and unclosed braces stay as written.
    /// </summary>
    public static class TextTemplate
    {
        /// <summary>Returns the same string instance when there is nothing to replace.</summary>
        public static string Expand(string template, Func<string, string> resolve)
        {
            Require.NotNull(resolve, nameof(resolve));
            if (string.IsNullOrEmpty(template) || template.IndexOf('{') < 0) return template;

            var builder = new StringBuilder(template.Length + 16);
            var index = 0;
            while (index < template.Length)
            {
                var open = template.IndexOf('{', index);
                var close = open < 0 ? -1 : template.IndexOf('}', open + 1);
                if (close < 0)
                {
                    builder.Append(template, index, template.Length - index);
                    break;
                }

                builder.Append(template, index, open - index);
                var token = template.Substring(open + 1, close - open - 1);
                builder.Append(Resolve(token, resolve) ?? template.Substring(open, close - open + 1));
                index = close + 1;
            }

            return builder.ToString();
        }

        private static string Resolve(string token, Func<string, string> resolve)
        {
            var separator = token.IndexOf(':');
            var name = separator < 0 ? token : token.Substring(0, separator);
            var modifier = separator < 0 ? null : token.Substring(separator + 1);
            var value = resolve(name.Trim());
            if (value == null) return null;

            return modifier?.Trim() switch
            {
                null => value,
                "upper" => value.ToUpper(CultureInfo.InvariantCulture),
                "lower" => value.ToLower(CultureInfo.InvariantCulture),
                _ => null
            };
        }
    }
}
