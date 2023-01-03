using System;

namespace MuteAppBG;

public static class StringExtensions
{
    public static string TrimEnd(this string input, string suffixToRemove,
        StringComparison comparisonType = StringComparison.CurrentCulture)
    {
        return input.EndsWith(suffixToRemove, comparisonType) ? input[..^suffixToRemove.Length] : input;
    }
}