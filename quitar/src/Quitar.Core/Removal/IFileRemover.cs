using System.Runtime.Versioning;
using Microsoft.VisualBasic.FileIO;

namespace Quitar.Core.Removal;

/// <summary>Quita archivos y carpetas de forma recuperable. Existe para probar sin tocar el disco.</summary>
public interface IFileRemover
{
    /// <exception cref="IOException">Si no se pudo (en uso, sin permiso, etc.).</exception>
    void Remove(string path, bool isDirectory);
}

/// <summary>
/// Manda a la Papelera de reciclaje en vez de borrar para siempre: es el respaldo de los archivos.
/// (Si algo es más grande que la Papelera, Windows lo borra directamente.)
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class RecycleBinFileRemover : IFileRemover
{
    public void Remove(string path, bool isDirectory)
    {
        if (isDirectory)
        {
            FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
        }
        else
        {
            FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
        }
    }
}
