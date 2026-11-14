# Quitar (nombre provisional)

Desinstalador para Windows.

- **Hito 1:** lista de programas instalados con búsqueda instantánea (solo lectura).
- **Hito 2 (este): desinstalar con respaldo.** Selecciona un programa → **Desinstalar** (o Enter) → Quitar guarda una copia de su entrada del registro, ejecuta el desinstalador del propio programa, busca lo que quedó y te lo muestra para revisar. Lo marcado se borra: los archivos van a la **Papelera de reciclaje** y cada clave del registro se exporta a `.reg` **antes** de borrarla. Si no se pudo respaldar, no se borra. **Historial** muestra el registro de operaciones de cada corrida y permite **restaurar el registro**.

Desde este hito la app pide administrador (aviso de UAC al abrirla).

### Cómo funciona la limpieza (y qué no hace todavía)
- Busca por **nombre exacto** (sin versión, arquitectura ni signos): carpeta de instalación registrada, carpetas en Program Files / ProgramData / AppData con ese nombre, accesos directos y claves `Software\Nombre` o `Software\Editor\Nombre`. Nunca ofrece la carpeta o clave del *editor*.
- **Lista de protegidos** (Windows, raíces de Program Files / AppData / perfil, claves de sistema, etc.): se aplica al escanear y otra vez justo antes de borrar.
- Riesgo **alto** (p. ej. carpeta directamente bajo la raíz de un disco) llega **desmarcado**.
- Los componentes de Microsoft solo se limpian por su carpeta de instalación, no por nombre.
- Los datos se guardan en `%LOCALAPPDATA%\Quitar` (`runs\*.json`, `backups\<corrida>\*.reg`).
- **Todavía no:** procesos en ejecución, archivos en uso con borrado al reiniciar, servicios/drivers/COM, quitar a la fuerza (sin desinstalador), punto de restauración de Windows. Un programa sin desinstalador registrado se rechaza con un aviso.
- Si el programa sigue en el registro tras el desinstalador (lo cancelaste o falló), no se ofrece borrar nada.
- Los archivos **no se restauran desde Quitar**: se recuperan desde la Papelera. (Si algo es más grande que la Papelera, Windows lo borra directamente.)

## Cómo obtener el .exe

Este código se escribió sin poder compilar la parte de WPF, así que la primera compilación real la hacés vos. Dos caminos:

### A) GitHub Actions (recomendado, no instalás nada)
1. Creá un repositorio en GitHub (puede ser privado) y subí esta carpeta.
2. Entrá a la pestaña **Actions** → workflow **build** (corre solo con cada push; también con "Run workflow").
3. Cuando termine en verde, abajo en **Artifacts** descargá:
   - `Quitar-instalador` → `Quitar-Setup-0.2.N.exe`
   - `Quitar-portable` → `Quitar.exe` (sin instalar)
4. Si falla, copiá el error del paso rojo y pasámelo: lo corrijo.

### B) En tu Windows
1. Instalá el SDK de .NET 10: https://dotnet.microsoft.com/download/dotnet/10.0
2. (Opcional, para el instalador) Inno Setup 6: https://jrsoftware.org/isinfo.php
3. En PowerShell, dentro de esta carpeta:
   `powershell -ExecutionPolicy Bypass -File .\build.ps1`
4. Resultado: `publish\Quitar.exe` y, si tenés Inno Setup, `installer\Output\Quitar-Setup-0.2.0.exe`.

## Atajos
F5 actualiza · Esc limpia el filtro · escribir en cualquier lado va al buscador.

## Estructura
- `src/Quitar.Core`: lectura del registro, clasificación, filtro, desinstalación, escáner de restos, respaldo y borrado (sin dependencias de WPF).
- `src/Quitar.Desktop`: interfaz WPF.
- `tests/Quitar.Core.Tests`: pruebas xUnit.
- `installer/quitar.iss`: instalador Inno Setup.

## Nota
El instalador no está firmado: SmartScreen puede avisar al abrirlo. La firma de código se resuelve antes de publicar.

## Probar con cuidado
Esta app **borra cosas de verdad**. La primera prueba conviene hacerla en una **máquina virtual con instantánea** o con un programa que no te importe (uno portable de prueba, por ejemplo). Después de desinstalar, mirá el **Historial** y probá **Restaurar registro** una vez.

## Estado de la verificación (hito 2)
- El núcleo (`Quitar.Core`) se compiló y sus pruebas corrieron (140 pasaron), con un registro y un disco falsos. También se comprobó que las pruebas detectan dos errores introducidos a propósito (regla de protección y borrado sin respaldo).
- **No se pudo probar en Windows real** ni compilar la parte WPF: `WindowsRegistryEditor` (reg.exe), `ProcessUninstallerHost`, `RecycleBinFileRemover` y las ventanas son la primera compilación real que hacés vos. Si falla, pasame el error del paso rojo.
