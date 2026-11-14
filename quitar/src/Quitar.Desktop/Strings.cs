namespace Quitar.Desktop;

/// <summary>
/// Todos los textos de la interfaz en un solo lugar, para poder pasarlos a archivos .resx
/// (y traducirlos) más adelante sin tocar las pantallas.
/// </summary>
public static class Strings
{
    public const string AppTitle = "Quitar (borrador)";

    public const string SearchPlaceholder = "Escribí para filtrar la lista…";
    public const string Refresh = "Actualizar (F5)";

    public const string GroupApps = "Aplicaciones";
    public const string GroupSystem = "Sistema";
    public const string GroupHidden = "Ocultos";

    public const string ColName = "Nombre";
    public const string ColPublisher = "Editor";
    public const string ColVersion = "Versión";
    public const string ColDate = "Instalado";
    public const string ColSize = "Tamaño";
    public const string ColArch = "Arq.";
    public const string ColScope = "Ámbito";

    public const string ArchX64 = "64 bits";
    public const string ArchX86 = "32 bits";
    public const string ScopeMachine = "Equipo";
    public const string ScopeUser = "Usuario";

    public const string Loading = "Leyendo programas…";
    public const string StatusFormat = "{0} en la lista · {1} en total · leído en {2} ms";
    public const string LoadError = "No se pudo leer la lista: {0}";

    // --- Hito 2: desinstalar con respaldo ---
    public const string Uninstall = "Desinstalar (Enter)";
    public const string History = "Historial";

    public const string ConfirmUninstall =
        "Se va a ejecutar el desinstalador de «{0}».\n\n" +
        "Antes se guarda una copia de su entrada en el registro, y al terminar se buscan los restos para que los revises. " +
        "No se borra nada sin que lo confirmes.";

    public const string ConfirmSystemComponent =
        "\n\nAtención: esto es un componente o actualización del sistema. Quitarlo puede afectar a otros programas o a Windows.";

    public const string NoUninstaller =
        "«{0}» no tiene un desinstalador registrado, así que no se puede desinstalar desde acá. " +
        "Quitarlo a la fuerza llega en un hito próximo.";

    public const string UninstallWindowTitle = "Desinstalar: {0}";
    public const string StopWaiting = "Dejar de esperar";
    public const string DeleteSelected = "Borrar lo marcado";
    public const string Close = "Cerrar";

    public const string ColKind = "Tipo";
    public const string ColRisk = "Riesgo";
    public const string ColPath = "Ruta";
    public const string ColReason = "Motivo";
    public const string ColResult = "Resultado";

    public const string KindFolder = "Carpeta";
    public const string KindFile = "Archivo";
    public const string KindShortcut = "Acceso directo";
    public const string KindRegistryKey = "Clave de registro";

    public const string RiskLow = "Bajo";
    public const string RiskMedium = "Medio";
    public const string RiskHigh = "Alto";

    public const string ResultDeleted = "Borrado";
    public const string ResultFailed = "Error: {0}";
    public const string ResultSkipped = "Omitido: {0}";

    public const string Starting = "Preparando…";
    public const string ReviewHint =
        "Se desinstaló «{0}». Revisá lo que quedó: lo marcado se borra (los archivos van a la Papelera y las claves " +
        "del registro se respaldan antes). Lo de riesgo alto viene sin marcar.";
    public const string DeletingStatus = "Borrando…";
    public const string DoneStatus =
        "Listo: {0} borrado(s), {1} con error, {2} omitido(s). Los archivos están en la Papelera de reciclaje; " +
        "las claves del registro se pueden restaurar desde el Historial.";
    public const string UnexpectedError = "Algo salió mal: {0}";

    public const string ConfirmDelete =
        "Se van a enviar a la Papelera {0} carpeta(s), archivo(s) o acceso(s) directo(s) y se van a borrar {1} clave(s) del registro " +
        "(cada una se respalda antes).{2}\n\n¿Continuar?";
    public const string ConfirmDeleteHighRisk =
        "\n\nHay elementos de riesgo alto marcados: revisalos bien antes de seguir.";

    public const string BusyClosing =
        "Hay una operación en curso. Si estás esperando al desinstalador, usá «Dejar de esperar».";

    public const string HistoryTitle = "Historial de desinstalaciones";
    public const string ColWhen = "Fecha";
    public const string ColProgram = "Programa";
    public const string ColStatus = "Estado";
    public const string ColBackups = "Respaldos";
    public const string RestoreRegistry = "Restaurar registro";
    public const string HistoryEmpty = "Todavía no hay desinstalaciones registradas.";
    public const string ConfirmRestore =
        "Se van a volver a cargar en el registro {0} clave(s) respaldadas de «{1}».\n\n" +
        "Los archivos no se restauran desde acá: están en la Papelera de reciclaje. ¿Continuar?";
    public const string RestoreResult =
        "Se restauraron {0} clave(s); {1} con error. Actualizá la lista principal con F5.";

    public const string StatusUninstalling = "Desinstalando";
    public const string StatusReviewing = "En revisión";
    public const string StatusDeleting = "Borrando";
    public const string StatusDone = "Terminada";
    public const string StatusCancelled = "Cancelada";
    public const string StatusFailed = "Con error";
}
