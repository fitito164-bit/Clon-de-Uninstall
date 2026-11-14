# Recon: Uninstall Tool (CrystalIDEA)

Fecha: 2026-11-10 · Plataforma objetivo: **Windows** · Alcance: **núcleo (lista + desinstalar + restos)** · Uso: **proyecto propio para publicar**

> Método: fuentes públicas solamente (sitio, ayuda, blog, reseñas). No se vio la app instalada ni se leyeron capturas: las pantallas y estados de abajo se infieren del texto de las páginas y los atajos documentados. Lo que es inferencia está marcado.

## 0. Reglas de esta réplica (importante para publicar)

- Se reconstruye **funcionalidad y patrones de UX**, no nada de lo que el original posee.
- Nombre, logo, iconos, textos, capturas y traducciones: **todo propio**.
- «Uninstall Wizard™» y «Smart Search™» figuran como marcas del original: no reutilizar esos nombres.
- No copiar ni descompilar su binario ni su driver. El repositorio de GitHub de CrystalIDEA se presenta como seguimiento de incidencias; no asumir que hay código fuente reutilizable.
- Antes de publicar: revisar la EULA del original y buscar el nombre elegido en registros de marcas.

## 1. Fuentes

| fuente | qué aporta | URL |
| --- | --- | --- |
| Inicio | propuesta de valor, 4 grupos de programas | https://crystalidea.com/uninstall-tool |
| Uninstall Wizard | flujo de restos, log, procesos, borrado diferido | https://crystalidea.com/uninstall-tool/uninstall-wizard |
| Force Removal | quitar sin el desinstalador propio | https://crystalidea.com/uninstall-tool/force-removal |
| Batch | selección múltiple, 3 modos por lotes | https://crystalidea.com/uninstall-tool/batch-uninstaller-batch-programs-removal |
| Install Tracker | monitor de instalación (fuera del primer corte) | https://crystalidea.com/uninstall-tool/install-tracker-installation-monitor |
| Startup Manager | inicio de Windows (fuera del primer corte) | https://crystalidea.com/uninstall-tool/startup-programs |
| Lista de programas | columnas, sub-items | https://crystalidea.com/uninstall-tool/programs-list |
| Smart Search | filtro al escribir | https://crystalidea.com/uninstall-tool/smart-search |
| Preferencias | todos los ajustes | https://crystalidea.com/uninstall-tool/preferences |
| Atajos | teclado | https://crystalidea.com/uninstall-tool/keyboard-shortcuts |
| Línea de comandos | parámetros | https://crystalidea.com/uninstall-tool/command-line |
| Soporte | requisitos (Windows 7 a 11, 32/64 bits, requiere administrador) | https://crystalidea.com/uninstall-tool/support |
| Precios | $24.95 estándar, $39.95 portable, prueba de 30 días | https://crystalidea.com/uninstall-tool/buy |
| Reseña independiente | comparación con Geek Uninstaller | https://www.sflorg.com/2026/02/rev02012601.html |

Pendientes de leer (no se pudieron abrir): FAQ, release notes, Portable mode.

## 2. Inventario de pantallas

Primer corte = núcleo. «Después» = fuera del alcance elegido.

| ID | pantalla | cómo se llega | propósito | componentes clave | estados |
| --- | --- | --- | --- | --- | --- |
| S01 | Ventana principal: lista de programas | al abrir | ver, filtrar y elegir programas | lista multicolumna, buscador, barra inferior, barra de herramientas, sub-items de actualizaciones | vacía, cargando, con filtro sin resultados, con selección múltiple, lista compacta |
| S02 | Selector de grupos | barra inferior de S01 | cambiar entre Apps, Sistema, Ocultos y Apps de Microsoft Store | conmutador de 4 grupos | un estado por grupo |
| S03 | Menú contextual de un programa (inferido) | clic derecho en S01 | acciones sobre un programa | desinstalar, quitar a la fuerza, abrir carpeta, abrir web, buscar en Google, ir al registro, quitar entrada | multi-selección cambia las opciones |
| S04 | Pregunta «usar el asistente de restos» | al desinstalar | elegir si se buscan restos (por defecto pregunta cada vez) | diálogo con opciones siempre/nunca/preguntar | |
| S05 | Procesos en ejecución | antes de desinstalar | ofrecer cerrar procesos del programa | lista de procesos, botón terminar | sin procesos (no aparece), fallo al terminar |
| S06 | Escaneo de restos | tras el desinstalador o en quitar a la fuerza | buscar archivos y registro sobrantes | barra de progreso, cancelar | cancelado, sin resultados |
| S07 | Revisión de restos | fin del escaneo | revisar y desmarcar antes de borrar | árbol de elementos con casillas, exportar a XML | muchos elementos, ninguno marcado |
| S08 | Borrado en curso | confirmar en S07 | ejecutar la limpieza | progreso | error en un elemento |
| S09 | Aviso de borrado diferido | elementos en uso | programar borrado en el próximo reinicio | diálogo informativo, botón reiniciar | |
| S10 | Registro de borrado | fin de S08 | ver o guardar cada operación y sus errores | tabla/texto, guardar | con errores, sin errores |
| S11 | Confirmación de quitar a la fuerza | atajo Shift+Supr | evitar borrados accidentales | diálogo | |
| S12 | Revisión combinada por lotes | quitar varios a la fuerza | lista unificada de restos de varios programas, con revisión precisa | igual que S07 | |
| S13 | Confirmación de quitar entradas | lote «Quitar entradas» | borrar solo la entrada de la lista | diálogo de advertencia | |
| S14 | Preferencias | F9 | ajustes | 3 pestañas: General, Interfaz, Desinstalador | |
| S15 | Barra de recientes y aviso de programas nuevos | tras instalar algo | mostrar instalados recientemente | barra secundaria, ventana emergente | |
| S16 | Aviso de nueva versión | al iniciar | informar actualizaciones | notificación abajo a la derecha | |
| S17 | Licencia y activación | primer arranque o menú | prueba de 30 días, activar clave | diálogo | prueba vencida |
| S18 | Gestor de inicio (después) | Ctrl+Tab | ver, activar, desactivar, borrar apps de arranque | lista con origen, estado, ruta, editor | entradas inválidas |
| S19 | Agregar programa de inicio (después) | desde S18 | crear entrada | tipo, ruta, argumentos, «máximos privilegios» | |
| S20 | Monitor de instalación (después) | menú contextual de un instalador, barra o bandeja | registrar cambios | registro en vivo, detener, revertir | varias instalaciones embebidas |

## 3. Flujos

```
F01 Desinstalar con limpieza de restos
    S01 -> S05 (si hay procesos) -> desinstalador propio del programa -> S04 -> S06 -> S07 -> S08 -> (S09) -> S10
    edge: el desinstalador falla, archivo en uso, programa se vuelve a abrir, cancelar en S07
    clics en el camino feliz: seleccionar, desinstalar, confirmar restos, borrar (4 a 5)

F02 Quitar a la fuerza (sin desinstalador)
    S01 -> S11 -> S06 -> S07 -> S08 -> (S09) -> S10
    edge: instalación corrupta, sin datos de rastreo, restos en uso

F03 Quitar varios a la fuerza
    S01 (Ctrl/Shift) -> S11 -> S06 -> S12 -> S08 -> S10

F04 Quitar solo entradas de la lista
    S01 (selección) -> S13 -> la entrada desaparece, no se toca ningún archivo

F05 Buscar un programa
    S01, escribir directo -> la lista se filtra al instante -> Esc limpia el filtro
    edge: sub-items que coinciden expanden a su padre

F06 Cambiar de grupo y ver Microsoft Store
    S01 -> S02 -> lista del grupo; desinstalar por lotes funciona solo para Store

F07 Exportar la lista
    Ctrl+S -> archivo HTML

F08 Ajustes
    F9 -> S14 -> guardar

F09 Instalar y rastrear (después)   S03/S20 -> instalador -> parar -> programa marcado «Traced»
F10 Gestionar inicio (después)      Ctrl+Tab -> S18 -> S19
```

## 4. Componentes

- Lista multicolumna: orden por clic, columnas mostrar/ocultar con clic derecho, ancho y posición recordados, filas de 32 px o compactas de 16 px, marca «32-bit», resalte de recientes.
- Filas con sub-items (actualizaciones) expandibles con las flechas izquierda y derecha. Modos: inicialmente colapsado, expandido, siempre expandido.
- Buscador siempre activo: escribir desde el arranque filtra.
- Barra de herramientas con o sin textos; barra inferior con grupos.
- Árbol con casillas (restos), diálogos de confirmación, avisos tipo toast, icono de bandeja, tema claro/oscuro.

## 5. Modelo de datos inferido

```
Program        id, name, publisher, version, installDate, size, installLocation,
               uninstallString, quietUninstallString, group (app|system|hidden|store),
               arch (32|64), registryKey, isTraced, parentId (para actualizaciones)
               evidencia: columnas de S01, sub-items, grupos. confianza: alta
               (en Windows esto sale de las claves Uninstall del registro, conocimiento estándar)
Leftover       id, runId, kind (file|folder|regKey|regValue|service|driver|comObject|
               fileAssoc|installerComponent|shortcut), path, selected, inUse, risk?
               evidencia: páginas de Wizard y Force Removal. confianza: media (el campo risk es propuesta propia)
RemovalRun     id, programId, mode (uninstall|force|batchForce|removeEntry), startedAt,
               endedAt, rebootPending
RemovalLogEntry runId, ts, operation, target, result, errorCode, message
               evidencia: log con código de error y explicación. confianza: alta
PendingDelete  path, scheduledFor=nextReboot. confianza: media (mecanismo de Windows para esto es estándar)
Preferences    uacBypass, explorerContextMenu, checkUpdates, controlPanelIcon, language,
               toolbarCaptions, compactList, mark32bit, colorScheme, initialSort,
               showUpdates, highlightRecent, recentPopup, recentToolbar, wizardUsage
               evidencia: página de preferencias. confianza: alta
-- después --
TraceSession / TraceEntry   registro de archivos y claves creados por un instalador, programa principal y dependientes
StartupEntry                source (HKLM Run, HKLM RunOnce, HKCU Run, HKCU RunOnce, carpeta Inicio todos, carpeta Inicio usuario, tarea programada), path, args, enabled, valid
```

## 6. Qué no se puede (ni conviene) clonar

| elemento | motivo |
| --- | --- |
| Nombre, logo, iconos, capturas, textos, traducciones | pertenecen al original |
| Marcas «Uninstall Wizard™», «Smart Search™» | marcas del original |
| Base de reseñas, clientes y reputación | no se copia |
| Driver de kernel del Install Tracker | difícil, requiere firma de controladores y es riesgoso; se propone alternativa más simple en la fase 2 |
| Modo portable con licencia por usuario | decisión comercial propia |

## 7. Tamaño

- Pantallas: 17 en el primer corte (20 con las posteriores). Flujos: 8 del primer corte.
- Entidades: 5 del primer corte.
- **Tamaño: M** para el núcleo (algunas semanas). El Install Tracker con driver de kernel sería **XL**; en versión simplificada por comparación de instantáneas, M adicional.
- Partes difíciles:
  1. **Heurística de restos sin falsos positivos**: borrar algo ajeno es el peor error posible. Requiere puntuación de riesgo, revisión obligatoria y copia de respaldo del registro antes de borrar.
  2. **Elevación (UAC), borrado diferido y servicios/drivers/COM**: cada tipo de resto se elimina distinto.
  3. **Apps de Microsoft Store y componentes de sistema**: otro mecanismo de desinstalación y riesgo de romper Windows.
- Siguiente paso: `/replica-architect`.
