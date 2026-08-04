using System.Text.RegularExpressions;

namespace EbayPlaywrightAutomation.Utilities
{
    /// <summary>
    /// Extracts a numeric price from raw eBay price strings.
    /// Handles formats such as:
    ///   "$12.99"  "US $12.99"  "$10.00 to $15.00"  "USD 12.99"  "12,99 $"
    /// For ranges (e.g. "$10.00 to $15.00") returns the lower bound.
    /// Returns null when no numeric value can be parsed.
    /// </summary>
    public static class PriceParser
    {
        // Matches one or more digits, optionally followed by a decimal separator and cents.
        // Handles both period (12.99) and comma (12,99) decimal separators.
        private static readonly Regex _numberPattern =
            new Regex(@"\d{1,3}(?:[,\.\s]\d{3})*(?:[,\.]\d{1,2})?|\d+",
                RegexOptions.Compiled);

        /// <summary>
        /// Parses the first valid price found in <paramref name="rawText"/>.
        /// </summary>
        public static double? Parse(string? rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return null;

            // Strip currency symbols and noise
            string cleaned = rawText
                .Replace("US", "")
                .Replace("USD", "")
                .Replace("$", "")
                .Replace("€", "")
                .Replace("£", "")
                .Trim();

            var matches = _numberPattern.Matches(cleaned);
            if (matches.Count == 0)
                return null;

            // Take the first match (lower bound for ranges)
            string first = matches[0].Value
                .Replace(" ", "")   // thousands separator (space)
                .Replace(",", "."); // normalise decimal comma → dot

            // If there are multiple dots (e.g. "1.234.56"), strip all but the last one
            int dotCount = first.Count(c => c == '.');
            if (dotCount > 1)
            {
                int lastDot = first.LastIndexOf('.');
                string intPart = first[..lastDot].Replace(".", "");
                string fracPart = first[(lastDot + 1)..];
                first = intPart + "." + fracPart;
            }

            return double.TryParse(first, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double value)
                ? value
                : null;
        }
    }
}
