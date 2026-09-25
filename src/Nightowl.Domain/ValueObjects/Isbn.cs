using System.Text.RegularExpressions;
using Nightowl.Domain.Exceptions;

namespace Nightowl.Domain.ValueObjects;

public sealed record Isbn
{
    private static readonly Regex CleanRegex = new(@"[\s\-]", RegexOptions.Compiled);

    public string Value { get; }
    public string? Isbn10 { get; }
    public string Isbn13 { get; }

    public Isbn(string rawIsbn)
    {
        if (string.IsNullOrWhiteSpace(rawIsbn))
        {
            throw new InvalidIsbnException(rawIsbn ?? string.Empty, "ISBN cannot be empty.");
        }

        var cleaned = CleanRegex.Replace(rawIsbn.Trim(), string.Empty).ToUpperInvariant();

        if (cleaned.Length == 10)
        {
            if (!IsValidIsbn10(cleaned))
            {
                throw new InvalidIsbnException(rawIsbn, "Invalid ISBN-10 checksum or format.");
            }

            Isbn10 = cleaned;
            Isbn13 = ConvertIsbn10To13(cleaned);
            Value = Isbn13;
        }
        else if (cleaned.Length == 13)
        {
            if (!IsValidIsbn13(cleaned))
            {
                throw new InvalidIsbnException(rawIsbn, "Invalid ISBN-13 checksum or format.");
            }

            Isbn13 = cleaned;
            Isbn10 = TryConvertIsbn13To10(cleaned);
            Value = Isbn13;
        }
        else
        {
            throw new InvalidIsbnException(rawIsbn, $"Expected 10 or 13 digits, but got {cleaned.Length}.");
        }
    }

    public static bool TryCreate(string? rawIsbn, out Isbn? isbn)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawIsbn))
            {
                isbn = null;
                return false;
            }

            isbn = new Isbn(rawIsbn);
            return true;
        }
        catch
        {
            isbn = null;
            return false;
        }
    }

    public static bool IsValidIsbn10(string isbn10)
    {
        if (string.IsNullOrEmpty(isbn10) || isbn10.Length != 10)
            return false;

        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            if (!char.IsDigit(isbn10[i]))
                return false;
            sum += (isbn10[i] - '0') * (10 - i);
        }

        char lastChar = isbn10[9];
        if (lastChar == 'X')
        {
            sum += 10;
        }
        else if (char.IsDigit(lastChar))
        {
            sum += (lastChar - '0');
        }
        else
        {
            return false;
        }

        return sum % 11 == 0;
    }

    public static bool IsValidIsbn13(string isbn13)
    {
        if (string.IsNullOrEmpty(isbn13) || isbn13.Length != 13)
            return false;

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            if (!char.IsDigit(isbn13[i]))
                return false;
            int digit = isbn13[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        if (!char.IsDigit(isbn13[12]))
            return false;

        int checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit == (isbn13[12] - '0');
    }

    public static string ConvertIsbn10To13(string isbn10)
    {
        var core = "978" + isbn10.Substring(0, 9);
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = core[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }
        int checkDigit = (10 - (sum % 10)) % 10;
        return core + checkDigit;
    }

    public static string? TryConvertIsbn13To10(string isbn13)
    {
        if (!isbn13.StartsWith("978"))
            return null;

        var core = isbn13.Substring(3, 9);
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += (core[i] - '0') * (10 - i);
        }

        int remainder = (11 - (sum % 11)) % 11;
        char checkChar = remainder == 10 ? 'X' : (char)('0' + remainder);
        return core + checkChar;
    }

    public override string ToString() => Value;
}
