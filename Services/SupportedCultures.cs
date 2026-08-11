using System.Globalization;

namespace FishFarmManager.Services;

public static class SupportedCultures
{
    public const string Arabic = "ar-SA";
    public const string English = "en-GB";
    public const string Default = Arabic;

    public static IReadOnlyList<string> All { get; } = new[] { Arabic, English };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Default;

        var normalized = value.Trim();
        if (normalized.Equals(English, StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("en", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("English", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("الإنجليزية", StringComparison.OrdinalIgnoreCase))
        {
            return English;
        }

        if (normalized.Equals(Arabic, StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("ar", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Arabic", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("العربية", StringComparison.OrdinalIgnoreCase))
        {
            return Arabic;
        }

        return Default;
    }

    public static CultureInfo Create(string? value)
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(Normalize(value)).Clone();
        culture.DateTimeFormat.Calendar = new GregorianCalendar();
        culture.DateTimeFormat.DateSeparator = "/";
        return culture;
    }

    public static bool IsRightToLeft(string? value) => Create(value).TextInfo.IsRightToLeft;
}
