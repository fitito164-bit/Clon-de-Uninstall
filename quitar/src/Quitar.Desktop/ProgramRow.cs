using System.Globalization;
using Quitar.Core.Formatting;
using Quitar.Core.Programs;

namespace Quitar.Desktop;

/// <summary>Una fila de la lista: el programa más sus textos ya listos para mostrar.</summary>
public sealed class ProgramRow
{
    public ProgramRow(InstalledProgram program)
    {
        Program = program;
        Name = program.Name;
        Publisher = program.Publisher ?? "";
        Version = program.Version ?? "";
        InstallDate = program.InstallDate;
        InstallDateText = program.InstallDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        SizeBytes = program.SizeBytes;
        SizeText = SizeFormatter.Format(program.SizeBytes);
        ArchText = program.Architecture == ProgramArchitecture.X64 ? Strings.ArchX64 : Strings.ArchX86;
        ScopeText = program.Scope == RegistryScope.Machine ? Strings.ScopeMachine : Strings.ScopeUser;
    }

    public InstalledProgram Program { get; }

    public string Name { get; }

    public string Publisher { get; }

    public string Version { get; }

    /// <summary>Se usa para ordenar la columna; el texto de pantalla es <see cref="InstallDateText"/>.</summary>
    public DateOnly? InstallDate { get; }

    public string InstallDateText { get; }

    /// <summary>Se usa para ordenar la columna; el texto de pantalla es <see cref="SizeText"/>.</summary>
    public long? SizeBytes { get; }

    public string SizeText { get; }

    public string ArchText { get; }

    public string ScopeText { get; }
}
