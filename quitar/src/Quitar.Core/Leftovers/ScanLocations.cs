namespace Quitar.Core.Leftovers;

/// <summary>Una carpeta donde se buscan subcarpetas con el nombre del programa.</summary>
public sealed record ScanFolder(string Path, LeftoverRisk Risk, string Reason);

/// <summary>Dónde busca el escáner. Se puede reemplazar entera en las pruebas.</summary>
public sealed record ScanLocations(
    IReadOnlyList<ScanFolder> Folders,
    /// <summary>Menú Inicio (todos los usuarios y el actual): carpetas y accesos directos.</summary>
    IReadOnlyList<string> StartMenuPrograms,
    /// <summary>Escritorios: solo accesos directos.</summary>
    IReadOnlyList<string> DesktopFolders,
    /// <summary>Carpetas donde se instalan programas: una InstallLocation dentro de ellas es de riesgo bajo.</summary>
    IReadOnlyList<string> InstallRoots)
{
    public static ScanLocations FromEnvironment()
    {
        static string Get(Environment.SpecialFolder f) => Environment.GetFolderPath(f);

        var programFiles = Get(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Get(Environment.SpecialFolder.ProgramFilesX86);
        var localAppData = Get(Environment.SpecialFolder.LocalApplicationData);
        var localPrograms = string.IsNullOrEmpty(localAppData) ? "" : Path.Combine(localAppData, "Programs");

        const string fromInstall = "Carpeta de programa con el nombre del programa";
        const string userData = "Datos o configuración del programa; puede incluir tus archivos";

        var folders = new List<ScanFolder>
        {
            new(programFiles, LeftoverRisk.Low, fromInstall),
            new(programFilesX86, LeftoverRisk.Low, fromInstall),
            new(localPrograms, LeftoverRisk.Low, fromInstall),
            new(Get(Environment.SpecialFolder.CommonApplicationData), LeftoverRisk.Medium, userData),
            new(localAppData, LeftoverRisk.Medium, userData),
            new(Get(Environment.SpecialFolder.ApplicationData), LeftoverRisk.Medium, userData),
        };

        return new ScanLocations(
            folders.Where(f => !string.IsNullOrWhiteSpace(f.Path)).ToList(),
            new[] { Get(Environment.SpecialFolder.CommonPrograms), Get(Environment.SpecialFolder.Programs) }
                .Where(p => !string.IsNullOrWhiteSpace(p)).ToList(),
            new[] { Get(Environment.SpecialFolder.CommonDesktopDirectory), Get(Environment.SpecialFolder.DesktopDirectory) }
                .Where(p => !string.IsNullOrWhiteSpace(p)).ToList(),
            new[] { programFiles, programFilesX86, localPrograms }
                .Where(p => !string.IsNullOrWhiteSpace(p)).ToList());
    }
}
