using System.Windows;

namespace Quitar.Desktop;

public partial class App : Application
{
    public static AppServices Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        Services = AppServices.Create();

        try
        {
            // Lo que quedó a medias porque la aplicación se cerró se marca como interrumpido.
            Services.Store.MarkInterrupted();
        }
        catch (Exception)
        {
            // El historial es secundario: si no se puede leer, la aplicación abre igual.
        }

        base.OnStartup(e);
    }
}
