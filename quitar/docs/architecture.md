# Arquitectura del clon de Uninstall Tool

Base: `replica/recon.md` y `replica/features.csv`. Plataforma: Windows. Primer corte: núcleo (lista, desinstalar, restos, quitar a la fuerza, lotes).

## 1. Diferencia clave con una app web

Esto es una utilidad de escritorio que toca el registro, el sistema de archivos y servicios de la máquina del usuario. No necesita servidor, cuentas ni base de datos en la nube para funcionar. El «backend» es código local con permisos de administrador. La única parte en línea es la venta (licencias) y la página de lanzamiento, y vienen después.

## 2. Stack

| capa | elección | por qué | alternativa |
| --- | --- | --- | --- |
| lenguaje y runtime | C# con .NET 10 | acceso directo y maduro a registro, servicios, procesos, WMI y elevación; un ejecutable autónomo | Rust con Tauri |
| interfaz | WPF con una librería de estilo Fluent (WPF-UI) | listas con árbol y columnas de calidad, tema claro y oscuro, estable | WinUI 3 (más nuevo, más fricción) |
| núcleo de lógica | biblioteca `Core` separada de la interfaz | se prueba sin abrir ventanas; el motor de restos es lo más delicado | |
| datos locales | SQLite (`Microsoft.Data.Sqlite`) + ajustes en la misma base | un archivo, sin instalar nada; modo portable casi gratis | JSON suelto |
| elevación | manifiesto `requireAdministrator` | el original también exige administrador | tarea programada para evitar UAC (opcional, después) |
| instalador y actualizaciones | Inno Setup o WiX para instalar, Velopack para actualizar | estándar en Windows | MSIX |
| firma | certificado de firma de código | sin firma, SmartScreen y los antivirus marcan una herramienta que borra cosas del sistema | |
| pruebas | xUnit + registro y disco falsos + máquina virtual de Windows con instantáneas | borrar de verdad solo se prueba en una VM descartable | |
| licencias (fase comercial) | Paddle o Lemon Squeezy como vendedor oficial + clave firmada con Ed25519 verificable sin conexión | cobran y calculan impuestos por vos | Stripe |
| página y descargas | Next.js en Vercel (lo arma `/replica-launch`) | | |

Decisión de compatibilidad: **Windows 10 (1607) o superior**. El original llega hasta Windows 7, pero .NET 10 no corre ahí. Para un producto nuevo es una pérdida razonable.

Necesitás una máquina o VM con Windows para compilar y probar. No hay forma de validar este tipo de app sin Windows real.

## 3. Estructura de proyectos

```
src/
  Core/            motor sin interfaz
    Programs/      lectura de programas instalados (registro 64/32 bits, HKCU, Store)
    Uninstall/     ejecutar desinstaladores, esperar procesos hijos, terminar procesos
    Leftovers/     escáner por tipo, reglas de riesgo, lista de rutas protegidas
    Removal/       borrado, respaldo .reg, borrado diferido, registro de operaciones
    Data/          SQLite, migraciones, repositorios
  App/             WPF: ventanas, vistas, tema
  Cli/             parámetros de línea de comandos
tests/
  Core.Tests/      reglas del escáner contra datos falsos
  Vm/              guiones para la VM
```

## 4. Esquema de datos

Archivo: `replica/schema.sql` (SQLite). Fase 1: 7 tablas.

| tabla | para qué |
| --- | --- |
| `settings` | preferencias (S14) |
| `removal_runs` | una ejecución de limpieza: programa, modo, estado |
| `leftover_items` | restos encontrados, con tipo, riesgo, motivo y estado |
| `removal_log_entries` | registro de cada operación con código de error (S10), solo se agrega |
| `pending_deletes` | borrados programados para el próximo reinicio (S09) |
| `registry_backups` | claves exportadas a `.reg` antes de borrar |
| `license` | prueba de 30 días y clave (una fila) |

Decisiones:

- **Los programas no se guardan.** Se leen del registro al iniciar y al pulsar F5; así nunca hay datos viejos.
- **Sin `user_id`.** Es una base local de un solo usuario. Las reglas de acceso son de otro tipo: la app corre como administrador y la protección está en el código (punto siguiente).
- **Reglas de seguridad en el código, no en la base:** lista de rutas y claves protegidas que jamás se ofrecen para borrar (Windows, Program Files raíz, perfiles de usuario, claves de sistema), nivel de riesgo por elemento, y los elementos de riesgo alto llegan desmarcados.
- **Unicidad:** `(run_id, kind, path, value_name)` evita duplicados. `value_name` es texto vacío en vez de nulo para que la restricción funcione en SQLite.
- **Borrado en cascada** de restos, registro y respaldos al borrar una ejecución; `pending_deletes` se conserva (`set null`) porque el archivo sigue existiendo hasta reiniciar.
- **Tiempos** en UTC, texto ISO 8601.

## 5. «API»: servicios internos y línea de comandos

No hay HTTP. Son interfaces del `Core` que la interfaz llama.

| servicio.método | qué hace | quién | entrada | salida | flujo |
| --- | --- | --- | --- | --- | --- |
| `Programs.List(group)` | lee programas del grupo y arma sub-items de actualizaciones | UI | grupo | lista de programas | F05 F06 |
| `Programs.Export(path)` | guarda la lista a HTML | UI | ruta | archivo | F07 |
| `Uninstall.FindRunningProcesses(program)` | procesos asociados a la ruta de instalación | UI | programa | lista | F01 |
| `Uninstall.Terminate(pids)` | cierra procesos | UI | pids | resultado por proceso | F01 |
| `Uninstall.Run(program)` | lanza el desinstalador y espera al árbol de procesos | UI | programa | código de salida | F01 |
| `Leftovers.Scan(run)` | escanea archivos, registro, servicios, COM, asociaciones, accesos directos | UI | ejecución | restos con riesgo y motivo | F01 F02 F03 |
| `Leftovers.ExportXml(run, path)` | exporta la revisión | UI | ejecución, ruta | XML | F01 |
| `Removal.Execute(run)` | respalda el registro, borra los restos marcados, programa los que están en uso | UI | ejecución | resumen | F01 F02 F03 |
| `Removal.RemoveEntry(programs)` | borra solo la entrada de la lista | UI | programas | resultado | F04 |
| `Removal.GetLog(run)` | registro de operaciones | UI | ejecución | entradas | F01 |
| `Backups.Restore(backup)` | reimporta un `.reg` | UI | respaldo | resultado | mejora propia |
| `Pending.Resolve()` | al iniciar, marca como resueltos los borrados ya ocurridos | arranque | | | |
| `Settings.Get/Set` | preferencias | UI | clave, valor | valor | F08 |
| `License.Activate(key)` | verifica la firma sin conexión | UI | clave | estado | S17 |

Línea de comandos (de `recon.md`): `/startup`, `/store_apps` abren en esa vista. `/install` se pospone a la fase 2.

Trabajos en segundo plano: vigilar cambios en las claves de desinstalación para la barra de recientes (S15), comprobar actualizaciones al iniciar (S16), resolver borrados pendientes al arrancar.

Servicios externos solo en la fase comercial: webhook del vendedor de licencias y el canal de actualizaciones de Velopack.

## 6. Lo que muerde

- **Falsos positivos al borrar:** el riesgo número uno. Mitigación: lista de protegidos, riesgo por elemento, revisión obligatoria, respaldo `.reg` antes de borrar, nada marcado por defecto en riesgo alto.
- **Registro de 32 y 64 bits:** hay que leer ambas vistas (`WOW6432Node`) más `HKCU` y los perfiles de otros usuarios.
- **Cadenas de desinstalación:** vienen con comillas y argumentos de todo tipo; los MSI usan el código de producto con `msiexec`.
- **Esperar al desinstalador:** muchos lanzan un proceso hijo y salen enseguida. Hay que seguir el árbol de procesos, no solo el primero.
- **Borrado en uso:** programar para el reinicio (`MoveFileEx` con retraso) y avisar en S09.
- **Apps de Microsoft Store:** se quitan por la API de paquetes o PowerShell, no por el registro. En lote solo funciona para Store en el original.
- **Elevación:** la app corre como administrador; las acciones peligrosas siempre piden confirmación explícita.
- **Antivirus y SmartScreen:** una herramienta que borra el sistema se ve sospechosa. Firma de código y publicar siempre desde el mismo editor.
- **Idempotencia:** si la app se cierra a mitad de un borrado, al reabrir `removal_runs` en estado `deleting` se retoma o se marca como fallida.
- **Rendimiento:** listas de cientos de programas; leer el registro en segundo plano y mostrar la lista de a poco.
- **Textos:** todos los textos en archivos de recursos desde el día uno (el original ofrece 5 idiomas en su sitio).

## 7. Orden de construcción

1. **Corte vertical (feo, pero de punta a punta).** Leer programas (S01), desinstalar con el desinstalador (F01 parcial), escanear solo archivos y claves de registro con el nombre del programa, mostrar la revisión (S07) y borrar con respaldo. Tablas: `removal_runs`, `leftover_items`, `removal_log_entries`, `registry_backups`. Prueba en VM.
2. **Obligatorias** (`must` de `features.csv`), por área:
   - Lista: columnas, orden, grupos, búsqueda instantánea (S01, S02).
   - Desinstalación: procesos en ejecución (S05), pregunta del asistente (S04).
   - Limpieza: borrado diferido (S09, `pending_deletes`), registro de operaciones completo (S10), quitar a la fuerza (S11).
   - Seguridad: lista de protegidos, riesgo, respaldo y restauración.
3. **Recomendadas** (`should`): sub-items, columnas configurables, lotes (S12, S13), servicios, COM, asociaciones, preferencias (S14), tema, atajos, licencia (S17).
4. **Opcionales** (`could`): barra de recientes, exportar HTML, idioma, línea de comandos, y la fase 2 (gestor de inicio, rastreador sin driver).
5. **Las mejoras** que encuentre `/replica-entrepreneur` leyendo las quejas de usuarios del original.

## Nota del hito 2: persistencia en JSON, no SQLite (todavía)

`removal_runs`, `leftover_items`, `removal_log_entries` y `registry_backups` de `schema.sql` se guardan por ahora como **un archivo JSON por corrida** (`%LOCALAPPDATA%\Quitar\runs\<id>.json`, escritura atómica). Motivo: sin dependencias y verificable sin Windows. El modelo (`RemovalRun`) es 1 a 1 con esas tablas, así que pasar a SQLite es cambiar `IRunStore`. `pending_deletes` y `settings` llegan con sus hitos.

Registro: todas las claves se expresan en la vista de 64 bits (`SOFTWARE\WOW6432Node\...` para 32 bits). Exportar e importar usan `reg.exe`.
