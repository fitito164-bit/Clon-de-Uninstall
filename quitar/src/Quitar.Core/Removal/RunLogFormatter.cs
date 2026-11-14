using System.Globalization;
using System.Text;

namespace Quitar.Core.Removal;

/// <summary>El registro de operaciones de una corrida como texto, para mostrarlo o guardarlo.</summary>
public static class RunLogFormatter
{
    public static string Format(RemovalRun run)
    {
        var sb = new StringBuilder();
        foreach (var e in run.Log)
        {
            sb.Append(e.Time.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture))
              .Append("  ").Append(e.Result.ToUpperInvariant().PadRight(7))
              .Append(' ').Append(e.Operation)
              .Append("  ").Append(e.Target);
            if (!string.IsNullOrWhiteSpace(e.Message))
            {
                sb.Append("  —  ").Append(e.Message);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
