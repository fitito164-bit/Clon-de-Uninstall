using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quitar.Core.Removal;

public interface IRunStore
{
    void Save(RemovalRun run);

    /// <summary>Las corridas guardadas, la más reciente primero. Los archivos ilegibles se omiten.</summary>
    IReadOnlyList<RemovalRun> List();

    /// <summary>
    /// Al abrir la aplicación: lo que quedó a medias porque se cerró durante una corrida se marca como
    /// fallido (si estaba desinstalando o borrando) o cancelado (si estaba en revisión).
    /// </summary>
    void MarkInterrupted();
}

/// <summary>Un archivo JSON por corrida, escrito de forma atómica (archivo temporal y reemplazo).</summary>
public sealed class JsonRunStore : IRunStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _directory;

    public JsonRunStore(string directory) => _directory = directory;

    public void Save(RemovalRun run)
    {
        Directory.CreateDirectory(_directory);
        var final = Path.Combine(_directory, run.Id + ".json");
        var temp = final + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(run, Options));
        File.Move(temp, final, overwrite: true);
    }

    public IReadOnlyList<RemovalRun> List()
    {
        if (!Directory.Exists(_directory))
        {
            return [];
        }

        var runs = new List<RemovalRun>();
        foreach (var file in Directory.EnumerateFiles(_directory, "*.json"))
        {
            try
            {
                var run = JsonSerializer.Deserialize<RemovalRun>(File.ReadAllText(file), Options);
                if (run is not null)
                {
                    runs.Add(run);
                }
            }
            catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
            {
                // un archivo dañado no debe impedir ver el resto del historial
            }
        }

        return runs.OrderByDescending(r => r.StartedAt).ToList();
    }

    public void MarkInterrupted()
    {
        foreach (var run in List())
        {
            switch (run.Status)
            {
                case RunStatus.Uninstalling or RunStatus.Deleting:
                    run.AddLog("interrupted", run.ProgramName, LogResult.Failed, "La aplicación se cerró durante la operación.");
                    run.Finish(RunStatus.Failed, "Se interrumpió porque la aplicación se cerró.");
                    Save(run);
                    break;
                case RunStatus.Reviewing:
                    run.Finish(RunStatus.Cancelled, "Se cerró la aplicación antes de borrar los restos.");
                    Save(run);
                    break;
            }
        }
    }
}
