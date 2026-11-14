using System.Diagnostics;
using System.Runtime.Versioning;

namespace Quitar.Core.Uninstall;

/// <summary>Ejecuta un desinstalador y espera a que termine. Existe para probar el flujo sin lanzar procesos.</summary>
public interface IUninstallerHost
{
    /// <returns>El código de salida del proceso que se lanzó.</returns>
    Task<int> RunAsync(UninstallCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Lanza el desinstalador del programa con el shell de Windows (admite .msi, .bat y los que piden
/// su propia elevación). Cancelar solo deja de esperar: nunca mata al desinstalador.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ProcessUninstallerHost : IUninstallerHost
{
    public async Task<int> RunAsync(UninstallCommand command, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo(command.FileName, command.Arguments)
        {
            UseShellExecute = true,
        };

        using var process = Process.Start(info)
            ?? throw new InvalidOperationException("Windows no devolvió el proceso del desinstalador.");

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }
}
