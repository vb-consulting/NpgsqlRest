namespace NpgsqlRest;

public static class Formatter
{
    public static ReadOnlySpan<char> FormatString(ReadOnlySpan<char> input, Dictionary<string, string> replacements)
    {
        if (replacements is null || replacements.Count == 0)
        {
            return input;
        }

        int inputLength = input.Length;

        if (inputLength == 0)
        {
            return input;
        }

        return FormatString(input, replacements.GetAlternateLookup<ReadOnlySpan<char>>());
    }

    public static ReadOnlySpan<char> FormatString(ReadOnlySpan<char> input, Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> lookup)
    {
        int inputLength = input.Length;

        if (inputLength == 0)
        {
            return input;
        }

        int resultLength = 0;
        bool inside = false;
        int startIndex = 0;
        for (int i = 0; i < inputLength; i++)
        {
            var ch = input[i];
            if (ch == Consts.OpenBrace)
            {
                if (inside is true)
                {
                    resultLength += input[startIndex..i].Length;
                }
                inside = true;
                startIndex = i;
                continue;
            }

            if (ch == Consts.CloseBrace && inside is true)
            {
                inside = false;
                if (lookup.TryGetValue(input[(startIndex + 1)..i], out var value))
                {
                    resultLength += value.Length;
                }
                else
                {
                    resultLength += input[startIndex..i].Length + 1;
                }
                continue;
            }

            if (inside is false)
            {
                resultLength += 1;
            }
        }
        if (inside is true)
        {
            resultLength += input[startIndex..].Length;
        }

        char[] resultArray = new char[resultLength];
        Span<char> result = resultArray;
        int resultPos = 0;

        inside = false;
        startIndex = 0;
        for (int i = 0; i < inputLength; i++)
        {
            var ch = input[i];
            if (ch == Consts.OpenBrace)
            {
                if (inside is true)
                {
                    input[startIndex..i].CopyTo(result[resultPos..]);
                    resultPos += input[startIndex..i].Length;
                }
                inside = true;
                startIndex = i;
                continue;
            }

            if (ch == Consts.CloseBrace && inside is true)
            {
                inside = false;
                if (lookup.TryGetValue(input[(startIndex + 1)..i], out var value))
                {
                    value.AsSpan().CopyTo(result[resultPos..]);
                    resultPos += value.Length;
                }
                else
                {
                    input[startIndex..i].CopyTo(result[resultPos..]);
                    resultPos += input[startIndex..i].Length;
                    result[resultPos] = ch;
                    resultPos++;
                }
                continue;
            }

            if (inside is false)
            {
                result[resultPos] = ch;
                resultPos++;
            }
        }
        if (inside is true)
        {
            input[startIndex..].CopyTo(result[resultPos..]);
        }

        return resultArray;
    }
}