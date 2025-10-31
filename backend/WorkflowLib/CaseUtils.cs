using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkflowLib;

public static class CaseUtils
{
    private static readonly char[] _separator = [' ', '-', '_'];
    
    public static Dictionary<string, object?> ToCamelCase(this IDictionary<string, object?> dictionary)
    {
        return dictionary.ToDictionary(kvp => kvp.Key.ToCamelCase(), kvp => ConvertValueToCamelCase(kvp.Value));
    }

    public static Dictionary<string, object?> ToPascalCase(this IDictionary<string, object?> dictionary)
    {
        return dictionary.ToDictionary(kvp => kvp.Key.ToPascalCase(), kvp => ConvertValueToPascalCase(kvp.Value));
    }

    public static string ToPascalCase(this string input)
    {
        string[] words = input.Split(_separator, StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();
        foreach (string word in words)
        {
            result.Append(char.ToUpper(word[0]));
            result.Append(word[1..]);
        }

        return result.ToString();
    }

    public static string ToCamelCase(this string input)
    {
        string pascal = ToPascalCase(input);
        if (string.IsNullOrEmpty(pascal))
        {
            return pascal;
        }

        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    private static object? ConvertValueToCamelCase(object? value)
    {
        return value switch
        {
            IDictionary<string, object?> nestedDict => nestedDict.ToCamelCase(),
            IEnumerable<object?> list => list.Select(ConvertValueToCamelCase).ToList(),
            _ => value
        };
    }

    private static object? ConvertValueToPascalCase(object? value)
    {
        return value switch
        {
            IDictionary<string, object?> nestedDict => nestedDict.ToPascalCase(),
            IEnumerable<object?> list => list.Select(ConvertValueToPascalCase).ToList(),
            _ => value
        };
    }
}
