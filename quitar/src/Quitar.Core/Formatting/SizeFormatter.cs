using System.Globalization;

namespace Quitar.Core.Formatting;

public static class SizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    /// <summary>Tamaño legible. Vacío si se desconoce.</summary>
    public static string Format(long? bytes, IFormatProvider? provider = null)
    {
        if (bytes is null or <= 0)
        {
            return "";
        }

        provider ??= CultureInfo.CurrentCulture;

        double value = bytes.Value;
        var unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        var number = value.ToString(unit <= 1 ? "0" : "0.0", provider);
        return $"{number} {Units[unit]}";
    }
}
