using Quitar.Core.Registry;

namespace Quitar.Core.Leftovers;

public enum LeftoverKind
{
    Folder,
    File,
    Shortcut,
    RegistryKey,
}

public enum LeftoverRisk
{
    Low,
    Medium,

    /// <summary>Llega desmarcado: el usuario tiene que decidir a conciencia.</summary>
    High,
}

public enum LeftoverState
{
    Found,
    Deleted,
    Failed,
    Skipped,
}

/// <summary>Un resto encontrado tras desinstalar. Es una clase con propiedades para poder guardarla y cambiar su estado.</summary>
public sealed class LeftoverItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public LeftoverKind Kind { get; set; }

    /// <summary>Ruta en disco o ruta de registro (HKLM\...), tal como se muestra.</summary>
    public string Path { get; set; } = "";

    /// <summary>Solo para <see cref="LeftoverKind.RegistryKey"/>.</summary>
    public RegistryKeyRef? Key { get; set; }

    public LeftoverRisk Risk { get; set; }

    /// <summary>Por qué se considera un resto. Se le muestra al usuario.</summary>
    public string Reason { get; set; } = "";

    public bool Selected { get; set; }

    public LeftoverState State { get; set; } = LeftoverState.Found;

    /// <summary>Qué pasó al borrarlo (error o motivo de omisión).</summary>
    public string? Message { get; set; }
}
