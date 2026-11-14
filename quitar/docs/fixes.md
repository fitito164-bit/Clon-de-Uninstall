# Qué odian, qué falta y qué está sin resolver en Uninstall Tool

Fecha: 2026-11-10 · Base: `replica/reviews.csv` (36 filas) y `replica/feedback.md`

## Tamaño y límites de la muestra (leer primero)

- **36 comentarios de 6 fuentes:** FileForum (13), foro de CrystalIDEA (10), incidencias de GitHub (9), Trustpilot (2), gHacks (1) y un benchmark de un competidor (1). Solo 2 tienen estrellas.
- **Es poco y es viejo.** La mayoría es de 2006 a 2017. Es una dirección, no un ranking. No apuestes el plan sobre esto sin juntar más.
- **Sesgo de fuente:** el foro y GitHub son canales de soporte, así que casi todo lo que hay ahí es un problema. Los elogios que aparecen en el sitio del producto, en tiendas que revenden licencias y en la web del propio fabricante no se cuentan: son publicidad.
- **No llegué a** Reddit, Hacker News, G2 ni Capterra: las búsquedas no devolvieron nada sobre este producto. Un hilo de MalwareTips lo leí, pero no pude extraer texto textual, así que no lo cito. Sí vi que ahí un usuario duda en comprarlo por ser un pago único con clave vitalicia (resumen mío, no cita).
- **Trustpilot** muestra 3 reseñas (promedio 3.9) y una de 1 estrella cuyo texto no pude leer.
- **Valoraciones sueltas:** FileForum 3.0 sobre 106 votos, SnapFiles 4.1 sobre 11 reseñas de usuarios, y ShouldIRemoveIt dice que el 78% lo conserva. Son fuentes distintas, no se pueden promediar.
- **Reseñas recientes:** solo 2 de 2026 (una de Trustpilot y el benchmark). Muchas quejas son de versiones viejas y pueden estar arregladas.

## 1. Lo que odian

Ordenado por evidencia. Cada cita es textual y tiene su enlace.

**1. Fallos y cosas que no funcionan.** 9 filas, 3 fuentes. Casi todo Install Tracker y versiones viejas.
- "Install and Trace: Error 193" ([foro](https://forums.crystalidea.com/viewforum.php?f=1&start=50), hilo de 60 respuestas)
- "Regression in 3.5.9: recent apps not working, not highlighted" ([GitHub](https://issues.ecosyste.ms/hosts/GitHub/repositories/crystalidea%2Funinstall-tool/issues?page=2))
- "It was not possible to test Uninstall Tool, because it crashed every time when it was opened." ([benchmark de abril de 2026](https://uninstalr.com/blog/windows-uninstaller-performance-comparison-2026/)). Ojo: lo escribe el autor de un competidor, lo probó en una sola VM con la versión 3.8.1 y avisa él mismo que hay que tomarlo con pinzas. Puede ser un problema de ese entorno.
- El original ya arregló uno de los casos: no entendía variables como `%systemroot%` en la cadena de desinstalación (arreglado en la 3.4 según su desarrollador). No cuenta como ventaja, sí como caso de prueba.

**2. Se salta restos o borra de más.** 5 filas, 2 fuentes.
- "Uninstall Tool does not scan / clean up trash in HKCR" ([foro](https://forums.crystalidea.com/viewforum.php?f=1&start=50))
- "i install adobe acrobat & adobe photoshop cs6 with trace in my computer but when i uninstall adobe acrobat, photoshop cs6 deleted" ([foro](https://forums.crystalidea.com/viewtopic.php?t=527)). La respuesta del desarrollador admite el problema: "The best practice for such case is too be careful when removing items in Uninstall Wizard, but we're working on a solution."
- "The tool cannot identify the program files of Tencent correctly" ([GitHub](https://issues.ecosyste.ms/hosts/GitHub/repositories/crystalidea%2Funinstall-tool/issues?page=2))
- "False positives problem, report" ([GitHub](https://issues.ecosyste.ms/hosts/GitHub/repositories/crystalidea%2Funinstall-tool/issues?page=2)). El título no aclara si habla de restos mal marcados o del antivirus; no lo cuento en ninguno de los dos con seguridad.

**3. El antivirus lo marca o lo borra.** 3 filas claras, 2 fuentes.
- "Uninstall Tool getting blocked as PUA (false-positive)" ([GitHub](https://issues.ecosyste.ms/hosts/GitHub/repositories/crystalidea%2Funinstall-tool/issues?page=2))
- "Uninstall Tool Portable Removed by Norton 360" ([foro](https://forums.crystalidea.com/viewforum.php?f=1&start=50))
- "False positive with Norton Internet Security, Latest Update (Uninstall Tool 3.5.5.5580)" ([GitHub](https://issues.ecosyste.ms/hosts/GitHub/repositories/crystalidea%2Funinstall-tool/issues?page=2)), todavía abierta.

**4. Licencia, activación y soporte.** 4 quejas claras, 3 fuentes, casi todas de 2013 a 2016. El ranking automático cuenta 5 porque incluye un elogio de Trustpilot al servicio.
- "Every time Uninstall Tool is run, it starts as being unregistered." ([foro](https://forums.crystalidea.com/viewtopic.php?p=4557))
- "I've purchased the program but still unregistered version." ([GitHub](https://issues.ecosyste.ms/hosts/GitHub/repositories/crystalidea%2Funinstall-tool/issues?page=2))
- "Most worse customer care." ([FileForum](https://fileforum.com/detail/Uninstall-Tool/1141150595/1)), por una clave que dejó de servir al pasar de la beta a la versión final.

**5. Precio: no vale lo que cuesta.** *Fina: 8 filas pero una sola fuente, de 2006 a 2013.*
- "$19.95 for what?" y "Save $20 bucks" ([FileForum](https://fileforum.com/detail/Uninstall-Tool/1141150595/1)). Un comentario de 2008 lo contradice: "Without doubt the best app uninstaller. 20 bucks is the only minus."

**6. Peor que las gratis.** *Fina: 3 filas, una fuente.* "why pay for such tool when you have NirSoft uninstaller or Safarp that do the same job for free in less than 100KB portable tool ?" ([FileForum](https://fileforum.com/detail/Uninstall-Tool/1141150595/1))

**7. La prueba no sirve para probar.** *Fina: 1 fila.* "You won't be able to fully test it out (in other words you won't know if it removed everything installed) because the trial version does everything up to but not including the uninstall" ([FileForum, 2011](https://fileforum.com/detail/Uninstall-Tool/1141150595/1))

**Lo que sí les gusta (para no romperlo):** la velocidad ("there's nothing faster than this program") y que limpia bien ("extremely good at removing all remnants of old software", [gHacks](https://www.ghacks.net/2017/06/08/advanced-uninstaller-pro-review/), con la pega de que "doesn’t look very cool").

## 2. Lo que falta

*Delgada: dos pedidos, una fuente cada uno.*
- Punto de recuperación automático antes de desinstalar: "Creating automatic recovery point before uninstall" ([GitHub](https://issues.ecosyste.ms/hosts/GitHub/repositories/crystalidea%2Funinstall-tool/issues?page=2)), pedido todavía abierto.
- Rastrear instalaciones sin acordarse de pedirlo: "Would be nice if it would ask user if they want to have program monitor all installations automatically" ([FileForum, 2011](https://fileforum.com/detail/Uninstall-Tool/1141150595/1)). Es fase 2 en este plan.

## 3. Lo que nadie resuelve

Todo sale de un solo benchmark hecho por el autor de un competidor, así que es una pista, no una prueba.
- **Quitar un programa cuyo desinstalador está roto.** En su prueba 3 solo 2 de 10 herramientas lo lograron, una de ellas la suya. Uninstall Tool figura como "No". Y quitar a la fuerza es una de sus funciones estrella.
- **Programas portables y restos de programas ya desinstalados.** En su prueba 1 ninguna herramienta detectó Notepad++ portable, Brave portable ni los restos de CCleaner, salvo la del autor.

## 4. Plan de arreglos

Ordenado por evidencia y por lo barato que es arreglarlo. Cada fila ya está en `features.csv` con `original = no`.

| # | qué construir | tamaño | quién lo hace | evidencia |
| --- | --- | --- | --- | --- |
| 1 | **Red de seguridad antes de borrar:** respaldo del registro (ya está en el plan) más punto de restauración de Windows automático, y deshacer la última limpieza | S | `/replica-build`, `/replica-backend` | pedido en GitHub, y los casos de borrado de más |
| 2 | **Vista previa de dependencias y elementos compartidos:** mostrar qué otros programas comparten cada carpeta o clave antes de borrar, y dejarlos desmarcados | M | `/replica-build` | el caso de Adobe CS6, 5 filas de restos mal tratados |
| 3 | **Quitar a la fuerza cuando el desinstalador está roto**, con una prueba de aceptación: borrar los archivos de setup de un programa, forzar y comprobar que su instalador ya no lo detecta | M | `/replica-build`, `/replica-test` | benchmark (fuente única, de competidor) |
| 4 | **Distribución amiga del antivirus:** firma de código, sin driver de kernel, hashes publicados y envío a los proveedores para quitar falsos positivos | S | `/replica-deploy` | 3 filas claras, una incidencia sigue abierta |
| 5 | **Estabilidad de arranque:** que la app nunca se cierre al abrir, tests en Windows 10 y 11 limpios, cadenas de desinstalación con variables de entorno, informe de errores local | S | `/replica-test` | 9 filas, aunque varias viejas |
| 6 | **Licencia que no se pierde:** clave firmada que se verifica sin conexión, reactivación sin soporte, política clara para versiones mayores | S | `/replica-launch` | 4 quejas de 2013 a 2016 |
| 7 | **Prueba completa por tiempo o nivel gratuito básico** | S | `/replica-launch` | una queja de 2011 y las de precio; fina |
| 8 | **Detectar programas portables y restos huérfanos** | L | `/replica-build` | solo el benchmark; opcional |

Las quejas de precio y licencia van a `/replica-launch`, no a la construcción.

## 5. El ángulo de venta

**A. Para quien tiene un programa que no se deja desinstalar.**
Para quien se encuentra con un programa cuyo desinstalador está roto, [tu app] lo saca igual y comprueba que quedó fuera.
Evidencia: benchmark de un solo competidor, 10 herramientas, 2 lo lograron. Fuente única.

**B. Para quien teme que un limpiador borre lo que no debe.**
Para quien teme que un limpiador de programas se lleve algo ajeno, [tu app] muestra qué es compartido, hace respaldo y punto de restauración antes de tocar nada, y deja deshacer.
Evidencia: restos mal tratados, 5 filas de 2 fuentes; el pedido de punto de recuperación; 3 filas de antivirus.

**C. Para quien odia pagar antes de saber si funciona.**
Para quien no quiere pagar antes de comprobar que limpia de verdad, [tu app] deja probar todo durante el periodo de prueba.
Evidencia: una reseña de 2011 y 8 de precio de una sola fuente. La más débil.

**Recomiendo B.** Tiene más filas y más fuentes, es barata de construir (el respaldo ya está en el esquema y el punto de restauración es S), responde al mayor riesgo técnico del propio plan y la puedes demostrar en una captura. A es el diferencial más fuerte si se confirma, pero cuesta M o más y se apoya en una sola prueba ajena: déjala como función estrella de la segunda versión, después de verificarla tú en tu propia VM.

Esto alimenta el nombre y la voz de `/replica-brand` y el titular de `/replica-launch`.

## Reglas para publicar

- No pongas el nombre del original en el nombre de tu app, en anuncios ni en la ficha de las tiendas. Una página de comparación es una consulta para un abogado de tu país.
- Las palabras de estos usuarios son investigación. No las uses como testimonios en tu página.
- No fabriques reseñas, ni para tu app ni contra la del original.

Siguiente paso: `/replica-brand`.
