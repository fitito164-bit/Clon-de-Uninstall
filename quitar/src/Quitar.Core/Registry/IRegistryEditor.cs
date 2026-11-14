namespace Quitar.Core.Registry;

/// <summary>
/// Lo único que el motor necesita del registro para respaldar, escanear y borrar.
/// Existe para probar toda la lógica con un registro falso.
/// </summary>
public interface IRegistryEditor
{
    bool KeyExists(RegistryKeyRef key);

    /// <summary>Nombres de las subclaves directas; vacío si la clave no existe o no se puede leer.</summary>
    IReadOnlyList<string> GetSubKeyNames(RegistryKeyRef key);

    /// <summary>Exporta la clave y todo lo que cuelga de ella a un archivo .reg. Lanza si no pudo.</summary>
    void Export(RegistryKeyRef key, string destinationFile);

    /// <summary>Borra la clave con todo su contenido. No falla si ya no existe.</summary>
    void DeleteKeyTree(RegistryKeyRef key);

    /// <summary>Vuelve a cargar un archivo .reg exportado antes. Lanza si no pudo.</summary>
    void Import(string regFile);
}
