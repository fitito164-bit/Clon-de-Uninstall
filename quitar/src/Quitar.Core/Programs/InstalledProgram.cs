namespace Quitar.Core.Programs;

/// <summary>Grupo en el que se muestra un programa (S02 del mapa de pantallas).</summary>
public enum ProgramGroup
{
    Apps,
    System,
    Hidden,
    /// <summary>Reservado para el hito de Microsoft Store; todavía no se llena.</summary>
    Store,
}

public enum RegistryScope
{
    Machine,
    User,
}

public enum ProgramArchitecture
{
    X64,
    X86,
}

/// <summary>Un programa tal como aparece en las claves de desinstalación de Windows.</summary>
public sealed record InstalledProgram
{
    /// <summary>Identificador estable: ámbito, arquitectura y nombre de la subclave.</summary>
    public required string Id { get; init; }

    public required string KeyName { get; init; }

    public required string Name { get; init; }

    public string? Publisher { get; init; }

    public string? Version { get; init; }

    public DateOnly? InstallDate { get; init; }

    public long? SizeBytes { get; init; }

    public string? InstallLocation { get; init; }

    /// <summary>Texto crudo del registro, sin expandir variables de entorno (se expanden al ejecutar).</summary>
    public string? UninstallString { get; init; }

    public string? QuietUninstallString { get; init; }

    public ProgramArchitecture Architecture { get; init; }

    public RegistryScope Scope { get; init; }

    public ProgramGroup Group { get; init; }

    public bool IsUpdate { get; init; }

    public bool IsWindowsInstaller { get; init; }

    public string? ParentKeyName { get; init; }
}
