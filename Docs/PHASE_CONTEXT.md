# Contexto Operativo y Handoff de Fases — Sistema Gimnasio

> [!NOTE]
> **Propósito del documento:** Este archivo sirve como marco de referencia y contexto operativo de transición (*handoff*) para futuros agentes (**Sol**, **Gemini**, **Luna**) y desarrolladores humanos. **No constituye una aceptación final ni una declaración de GREEN**, facultad que corresponde exclusivamente al usuario humano.

---

## 1. Guardrails Operativos y Metodología (Sol / Gemini / Luna)

Para garantizar la integridad técnica y arquitectónica del repositorio, todo ciclo de desarrollo debe respetar estrictamente los siguientes roles y restricciones:

- **Sol (Planificador / Arquitecto):**
  - Define alcances, identifica *seams* (costuras de prueba), evalúa riesgos y redacta planes de trabajo.
  - **No codifica ni aplica modificaciones directas** en el repositorio de código.
- **Gemini (Implementador):**
  - Modifica código, configuraciones y documentación respetando de forma estricta los límites del alcance acordado.
  - **No amplía el alcance** de forma unilateral ni altera archivos no involucrados sin previa instrucción.
- **Luna (Auditora / Revisora de Calidad):**
  - Opera en modalidad estrictamente **Read-Only**.
  - Valida reproducibilidad, integridad de compilación, adherencia a estándares y detecta anomalías.
  - Emite veredictos de revisión técnica (`READY` o `NOT READY`), pero **nunca declara GREEN**.
- **Usuario Humano:**
  - Es la **única autoridad** con potestad para declarar el hito **GREEN** y autorizar el avance entre fases o consolidación de cambios.
  - **Los agentes tienen terminantemente prohibido declarar GREEN.**

---

## 2. Estado Actual del Repositorio en el Handoff

| Componente / Fase | Estado de Revisión | Estado en Git | Detalle Operativo |
| :--- | :--- | :--- | :--- |
| **Fase 0** | **READY** (por Luna) | **Staged** | Saneamiento de artefactos rastreados, `.gitignore` y referencias relativas a `libs/`. |
| **Fase 1** | **READY** (por Luna) | **No Staged** (Working Tree) | Saneamiento de `SistemaGimnasio.sql`, paridad `App.config`, corrección en `Miembros_Conexion.cs` y script de validación. |
| **Fase 2** | **READY** (por Luna) | **No Staged** (Working Tree) | Optimización N+1, desacoplamiento UI en Core, atomicidad en RegistrarPago, vigencia acumulativa y corrección de reportes financieros. |
| **Fase 3** | **READY** (por Luna / Hardware físico probado) | **No Staged** (Working Tree) | Optimización 1:N con BiometricCache en memoria, eliminación de recargas SQL y deserialización XML por lectura, supresión de conexiones SQL compartidas, ciclo de vida robusto y tolerancia a desconexión del lector. |
| **Fase 4** | **Implementada** (pendiente auditoría Luna) | **No Staged** (Working Tree) | Manuales integrales de despliegue, operaciones y troubleshooting en Docs/, scripts seguros de respaldo/restore drill, automatización de retención y validación determinista en test-phase4.ps1. |
| **Aceptación Global**| **Pendiente** | N/A | El usuario **no ha declarado GREEN** aún. |
| **Artefacto Binario**| N/A | **Untracked** | `Docs/Images.zip` debe permanecer **no rastreado** (untracked). |

### Resumen de Cambios Realizados

#### Fase 0: Saneamiento de Repositorio e Independencia de Rutas (Staged)
- **Purga de artefactos compilados:** Eliminación del seguimiento en Git de paquetes NuGet históricos en `packages/`, directorios de compilación `bin/` y `obj/`, y archivos de entorno IDE en `.vs/`.
- **Reglas de exclusión:** Actualización de `.gitignore` para prevenir reintroducción de binarios, paquetes y temporales.
- **Desacoplamiento de rutas del SDK:** Sustitución de rutas absolutas dependientes de `Program Files` por rutas relativas hacia la carpeta versionada `libs/` (`DPCtlUruNet.dll`, `DPCtlXUru.dll`, `DPUruNet.dll`, `DPXUru.dll`) en:
  - `Sistema_Gimnasio/Sistema_Gimnasio.csproj`
  - `BiometricApp/BiometricApp/BiometricApp/BiometricApp.csproj`
- **Documentación base:** Reestructuración inicial del `README.md`.

#### Fase 1: Consistencia de Base de Datos, Configuración y Reproducibilidad (No Staged)
- **Limpieza de base de datos (`database/SistemaGimnasio.sql`):** Normalización de codificación a UTF-8 estricto sin BOM, remoción de bytes nulos (`0x00`) corruptos y verificación de definición de tablas (`Membresias`, `Miembros`, `Pagos`, `Visitas`).
- **Paridad de conexión:** Incorporación de la cadena centralizada `GymDbConnection` en `BiometricApp/BiometricApp/BiometricApp/App.config` idéntica a la de `Sistema_Gimnasio/App.config`.
- **Corrección en `Gym_System.Core/Gym_System.Core/Miembros_Conexion.cs`:**
  - Supresión de cadena de conexión SQL hardcodeada en `ObtenerHuella()`, adoptando `connectionString` centralizado.
  - Corrección de tipo de retorno de `byte[]` a `string`, alineado con el almacenamiento de la plantilla biométrica como `NVARCHAR(MAX)` (XML de DigitalPersona).
  - Corrección de parámetro de consulta SQL a `@id` acorde al esquema de la tabla `Miembros`.
- **Herramienta de verificación:** Creación de `scripts/verify-reproducibility.ps1` para validación estática automatizada de configuración, codificación y consistencia.
- **Documentación:** Actualización de `README.md` documentando comandos y notas de verificación local.

#### Fase 2: Bugs Funcionales Críticos, Pagos y Estado de Membresía (No Staged)
- **Optimización N+1 de estados:** Reemplazo de bucles iterativos por el método masivo `ActualizarEstadosMasivo()` en `Miembro_Conexion`, ejecutando una única consulta SQL `UPDATE` con expresión condicional `CASE`. Supresión de `MessageBox.Show` repetitivo en actualizaciones automáticas de `MiembrosView.xaml.cs`.
- **Desacoplamiento total de UI en Core:** Eliminación de `using System.Windows.Forms;`, de todas las llamadas a `MessageBox.Show` en `Gym_System.Core` y eliminación definitiva de la referencia `<Reference Include="System.Windows.Forms" />` en `Gym_System.Core.csproj`.
- **Prevención de condición de carrera y atomicidad en `RegistrarPago`:** Lectura de vigencia previa con bloqueo de fila (`UPDLOCK, ROWLOCK`), lectura de membresía, cálculo determinista con `VigenciaCalculador`, actualización a `Estado = 'Activo'` e inserción en `Pagos` consolidados dentro de la misma `SqlTransaction`. `PagoButWindow.xaml.cs` delega atómicamente a esta transacción.
- **Validación de filas afectadas:** `RegistrarPago` valida explícitamente `rowsAffected > 0` tanto en actualización como en inserción; si el miembro o membresía no existe, ejecuta rollback inmediato y retorna `false`.
- **Saneamiento de reportes y rangos de fecha (`Pagos_conexion.cs`):**
  - Supresión del producto cartesiano generado por `LEFT JOIN Pagos` superfluo en `ObtenerReporteMembresiasActivos`.
  - Tratamiento inclusivo de fechas mediante rangos semiabiertos `< FinExclusivo` en `ObtenerPagosPorSemanaDelMesActual`, `ObtenerPagosSemanaActual` y `ObtenerTotalesVisitasPorFecha`.
  - Corrección semántica en `ObtenerPagosRecientes`: eliminación del `JOIN` espurio `p.ID = m.id`, ajustando la consulta y la vista `InicioView.xaml` al esquema real de `Pagos`.
- **Robustez en scripts de verificación (`scripts/`):**
  - Compilación dinámica en memoria de `VigenciaCalculador.cs` vía Roslyn `csc.exe` en `test-phase2.ps1` y `verify-reproducibility.ps1` para garantizar ejecución contra el código fuente actual y evitar DLL stale.
  - Adición de checks automáticos para verificar la ausencia de `System.Windows.Forms` en el `.csproj`, validación de filas afectadas en `RegistrarPago`, rangos semiabiertos y ausencia de joins inválidos.
  - Saneamiento completo de espacios en blanco finales (`git diff --check`).

#### Fase 3: Robustez Biométrica, Caché Versionada y Desacoplamiento (No Staged)
- **Caché en memoria de plantillas (`BiometricCache.cs`):** Implementación de la clase `BiometricCache` con almacenamiento en RAM de registros deserializados `DPUruNet.Fmd`. La verificación 1:N consulta la caché precargada sin ejecutar consultas SQL a la tabla completa ni deserializaciones repetitivas de XML en cada lectura física. Opera sobre un snapshot inmutable copy-on-write para garantizar atomicidad y seguridad multihilo.
- **Invalidación sincronizada y versionada (Anti-Race Condition):** `Invalidar()` incrementa un contador `_version` bajo sincronización thread-safe. `CargarPlantillas()` serializa recargas concurrentes con `_reloadLock`, captura la versión antes de leer datos y verifica que coincida antes de publicar; si ocurrió una invalidación concurrente durante la lectura, descarta la publicación obsoleta, mantiene la caché sucia y recarga automáticamente.
- **Estado de error, backoff y cooldown ante caídas de SQL:** `BiometricCache` implementa `EnCooldown`, `CooldownFalloSegundos` y `UltimoErrorCarga`. Ante fallos de conexión o lectura, entra en un periodo de enfriamiento que omite consultas SQL repetitivas en cada escaneo, previniendo bloqueos y saturación del callback de captura, reportando error de forma segura.
- **Desacoplamiento arquitectónico estricto de `Gym_System.Core`:** Eliminación definitiva de la referencia a `BiometricApp.csproj` y supresión de llamadas directas a `BiometricCache` o bloques `catch {}` opacos en `Miembros_Conexion.cs`. Introducción del seam neutral `NotificadorCambioMiembro` en Core. Las invalidaciones se realizan desde adaptadores/UI (`FormAgregar.cs`, `FormEditar.cs`, `MiembrosView.xaml.cs`, `PagoButWindow.xaml.cs`) y el cableado del seam se realiza al inicio en `App.xaml.cs`.
- **Supresión de conexiones SQL compartidas:** Eliminación de los campos `conn` (`SqlConnection`) de larga vida en `UCVerifyFingerprint.cs` y `frmDBEnrollment.cs`, eliminando condiciones de carrera y colisiones multihilo. En `BiometricCache`, la carga desde base de datos utiliza conexiones de ámbito local estrictamente gestionadas con bloques `using`.
- **Ruta segura garantizada en `FingerprintManager.ProcesarVerificacion`:** Envoltura del evento `HuellaParaVerificar` en un bloque `try-catch-finally`, garantizando la ejecución incondicional de `IniciarCaptura()` en `finally` aun cuando los suscriptores lancen excepciones, y notificando `ErrorLector` de forma segura sin tragar errores silenciosamente.
- **Ciclo de vida y liberación de recursos de hardware:**
  - `SistemaAcceso.xaml.cs`: Desuscripción explícita del evento `HuellaVerificada`, invocación de `verificador.DetenerLector()`, desvinculación de `winFormsHost.Child = null` y llamada a `verificador.Dispose()` en `OnClosed` y reinicio.
  - `UCVerifyFingerprint.cs`: Desuscripción de eventos al detener o destruir el control, liberación de `Image` en `PictureBox` para prevenir fugas de GDI+, y eliminación del campo compartido `conn`.
  - `frmDBEnrollment.cs`: Desuscripción completa de eventos en `FormClosing` y `Dispose()`, liberación de bitmaps y supresión de `conn`.
  - `FormAgregar.cs`: Limpieza y cierre seguro del formulario secundario `frmDBEnrollment` al cerrar o cambiar de pestaña.
  - `FingerprintManager.cs`: Supresión de manejadores de eventos, cancelación de capturas y llamada a `_reader.Dispose()` en `Dispose()`, limpiando la instancia singleton.
- **Tolerancia a desconexión y ausencia de hardware:**
  - Manejo seguro de códigos de fallo de hardware `DP_DEVICE_FAILURE` y `DP_INVALID_DEVICE` en `IniciarCaptura()` y `OnCaptured()`, previniendo bucles infinitos o bloqueos de UI.
  - Protección de todo el callback `OnCaptured()` con bloques `try/catch` para evitar caídas del proceso por excepciones no controladas.
  - Validación preventiva de `Capabilities` y `Resolutions` antes de iniciar capturas asíncronas.
  - Manejo defensivo en interfaz gráfica verificando `IsHandleCreated` e `!IsDisposed`.
- **Seams y pruebas automatizadas robustas (`scripts/test-phase3.ps1`):**
  - Compilación dinámica en memoria mediante Roslyn `csc.exe` sin dependencias de binarios obsoletos (stale DLL fallback eliminado).
  - Pruebas unitarias de matching 1:N, eficiencia de caché (1 carga para 10 escaneos), recarga selectiva bajo demanda y mutación in-memory copy-on-write.
  - Seams deterministas para validación de carrera de versión (`SeamDuranteCarga`), cooldown/backoff de fallos (`SimularFalloCarga`, `SeamTiempoActual`), y ejecución garantizada de `IniciarCaptura` en `finally` ante excepciones de suscriptores.
  - Documentación explícita de limitaciones: pruebas automatizadas operan sobre emulación de software y mocks, sin afirmar interacción con hardware físico real.
  - Integración de validaciones de Fase 3 en `scripts/verify-reproducibility.ps1`.

#### Fase 4: Documentación Operativa y Despliegue (No Staged)
- **Manual integral de despliegue (`Docs/DEPLOYMENT.md`):** Guía en español para topologías monopuesto y en red, instalación de SQL Server Express, ejecución de `database/SistemaGimnasio.sql`, política estricta de permisos mínimos (*least-privilege* con `db_datareader`/`db_datawriter`), explicación técnica de `Integrated Security=True`, configuración paritaria de `GymDbConnection` e instalación del runtime de DigitalPersona U.are.U 4500 (x86/x64).
- **Manual de operación y respaldos (`Docs/OPERATIONS.md`):** Justificación y recomendación del modelo de recuperación `SIMPLE` para prevenir explosión del log de transacciones, scripts seguros de respaldo parametrizado con `WITH CHECKSUM` y `RESTORE VERIFYONLY`, automatización PowerShell con política de retención (`scripts/Backup-Database.ps1`), simulacro seguro de restauración aislada en `SistemaGimnasio_Drill` (`scripts/restore-drill.sql`) sin afectar producción, y mantenimiento del hardware óptico y caché in-memory.
- **Guía de troubleshooting (`Docs/TROUBLESHOOTING.md`):** Árboles de diagnóstico y mitigación para errores de red SQL (26, 40, 18456), detección física del sensor USB (`DP_DEVICE_FAILURE`, `DP_INVALID_DEVICE`), factores de rechazo en huellas (prisma, piel reseca, re-enrolamiento con 4 tomas), comportamiento del cooldown de protección en `BiometricCache` ante caídas de base de datos, y resolución de errores de build y paquetes NuGet.
- **Scripts operativos y de verificación (`scripts/`):**
  - `scripts/backup-database.sql`: Respaldo T-SQL seguro y parametrizado.
  - `scripts/restore-drill.sql`: Simulacro de restauración con reubicación `WITH MOVE` en base aislada.
  - `scripts/Backup-Database.ps1`: Automatización desatendida con retención rotativa.
  - `scripts/test-phase4.ps1`: Suite automatizada de comprobación de despliegue, referencias a archivos reales, ausencia de secretos, consistencia de configuración y calidad de formato.

---

## 3. Comandos de Verificación Existentes

Antes de iniciar cualquier trabajo o al auditar cambios, ejecutar los siguientes comandos desde la raíz del repositorio (`D:\Proyecto Gym\repos\Sistema_Gimnasio(+)`):

### 1. Verificación Estática y Consistencia (PowerShell)
```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify-reproducibility.ps1
```
*Comprueba:*
- Presencia y consistencia de `GymDbConnection` en ambos `App.config`.
- Ausencia de cadenas de conexión SQL hardcodeadas en archivos `.cs`.
- Referencias del SDK DigitalPersona apuntando a `libs/` y existencia de binarios.
- Formato UTF-8 estricto y ausencia de bytes nulos en `database/SistemaGimnasio.sql`.
- Inexistencia de binarios rastreados indebidamente fuera de `libs/`.
- Ausencia de MessageBox y WinForms en `Gym_System.Core`.
- Optimización masiva de estados y prevención N+1.
- Atomicidad en transacciones de pagos.
- Inexistencia de productos cartesianos y rangos semiabiertos en reportes financieros.
- Inexistencia de conexiones SQL compartidas en controles biométricos.
- Uso de caché biométrica sin recarga SQL ni deserialización XML por lectura.
- Tolerancia a desconexión de hardware y ciclo de vida de recursos.

### 2. Pruebas Automatizadas de Fase 2 (PowerShell)
```powershell
powershell -ExecutionPolicy Bypass -File scripts/test-phase2.ps1
```

### 3. Pruebas Automatizadas de Fase 3 (PowerShell)
```powershell
powershell -ExecutionPolicy Bypass -File scripts/test-phase3.ps1
```

### 4. Pruebas Automatizadas de Fase 4 (PowerShell)
```powershell
powershell -ExecutionPolicy Bypass -File scripts/test-phase4.ps1
```

### 5. Compilación de la Solución (MSBuild)
Desde Developer PowerShell / símbolo del sistema de Visual Studio:
```powershell
msbuild Sistema_Gimnasio.sln /p:Configuration=Debug
```
O invocando la ruta directa de MSBuild:
```powershell
& "${env:ProgramFiles}\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" Sistema_Gimnasio.sln /p:Configuration=Debug
```
*Criterio de éxito:* `0 Errores`, compilación completa de `BiometricApp`, `Gym_System.Core` y `Sistema_Gimnasio`.

---

## 4. Definición de Próximas Fases Sugeridas

### Fase 2: Bugs Funcionales Críticos, Pagos y Estado de Membresía

- **Objetivos:**
  1. **Optimización de consultas de estado:** Investigar y corregir si se confirma un posible patrón de consultas N+1 en `MiembrosView.xaml.cs` (`ActualizarEstados_Click` y `ActualizarEstados_Automatico`), evaluando sustituir iteraciones individuales por una consulta SQL en lote o procedimiento eficiente.
  2. **Desacoplamiento de UI en Core:** Investigar y corregir si se confirman llamadas a `System.Windows.Forms.MessageBox.Show` en la capa de datos `Gym_System.Core` (`Miembros_Conexion.cs` en `RegistrarPago` y `RegistrarPagoHistorial`), desacoplando la lógica de datos de la interfaz de usuario.
  3. **Atomicidad en Pagos:** Asegurar que al ejecutar `RegistrarPago`, además de actualizar fechas (`FechaInicio`, `FechaFin`) e insertar en la tabla `Pagos`, el estado del miembro se actualice explícitamente a `'Activo'` dentro de la misma transacción SQL.
  4. **Revisión de cálculos de vigencia:** En `PagoButWindow.xaml.cs`, investigar y corregir si se confirma la necesidad de contemplar días activos restantes para sumar la duración a partir de la vigencia restante en lugar de reiniciar desde `DateTime.Now`.
  5. **Revisión de reportes financieros:** Investigar y corregir si se confirman productos cartesianos o inconsistencias en las consultas de `Pagos_conexion.cs` (`ObtenerReporteMembresiasActivos`, `ReportePagosConNombreMembresia`) derivadas de la relación entre `Membresias`, `Miembros` y `Pagos`.
- **Riesgos:**
  - Alteración involuntaria de reportes contables o históricos de membresías.
  - Bloqueo de registros en base de datos si las transacciones no se manejan con tiempos de espera adecuados.
  - Modificación de estados que afecte el acceso biométrico inmediato.
- **Archivos Relevantes:**
  - `Gym_System.Core/Gym_System.Core/Miembros_Conexion.cs`
  - `Gym_System.Core/Gym_System.Core/Pagos_conexion.cs`
  - `Gym_System.Core/Gym_System.Core/Pagos_agregar.cs`
  - `Sistema_Gimnasio/MiembrosView.xaml.cs`
  - `Sistema_Gimnasio/PagoButWindow.xaml.cs`
  - `Sistema_Gimnasio/PagosView.xaml.cs`
  - `database/SistemaGimnasio.sql`
- **Checks / Seams Sugeridos:**
  - Creación de seam de prueba para simular actualización masiva de estados sin interfaz gráfica.
  - Prueba de rollback: forzar error en inserción de `Pagos` y verificar que el miembro no modifica su vigencia.
- **Criterios de Salida:**
  - Cero referencias a `MessageBox` en `Gym_System.Core` (de confirmarse su presencia).
  - Actualización masiva de estados en una sola operación sin mensajes emergentes repetitivos.
  - Transacciones de pago atómicas y consistentes.
  - Compilación sin errores y verificación estática con checks aprobados.

---

### Fase 3: Robustez Biométrica y Rendimiento

- **Objetivos:**
  1. **Optimización de verificación 1:N:** En `UCVerifyFingerprint.cs`, investigar y corregir si se confirma la recarga completa de la tabla de miembros desde la base de datos y la deserialización XML de huellas en cada lectura (`VerificarEnBaseDeDatos`).
  2. **Caché en memoria de plantillas FMD:** Implementar un almacén en memoria de plantillas biométricas (`DPUruNet.Fmd`) previamente deserializadas, con mecanismo de invalidación o recarga selectiva ante altas, bajas o modificaciones de miembros.
  3. **Seguridad multihilo en conexiones SQL:** Investigar y corregir si se confirman condiciones de carrera por conexiones SQL compartidas (como `conn` en `UCVerifyFingerprint.cs`), adoptando conexiones con ámbito local (`using`).
  4. **Ciclo de vida y liberación de recursos:** Asegurar que `FingerprintManager` y los controles de captura liberen adecuadamente manejadores de hardware (`Dispose`, `Reader.Dispose()`, desuscripción de eventos) al navegar entre vistas o cerrar la aplicación (`SistemaAcceso.xaml.cs`).
  5. **Tolerancia a desconexión de hardware:** Gestionar la reconexión en caliente y la ausencia del dispositivo biométrico sin provocar excepciones no controladas ni bloqueos de interfaz.
- **Riesgos:**
  - Modificar los umbrales o el pipeline de comparación (`Comparison.Compare`) podría inducir falsos positivos o rechazos indebidos.
  - Fugas de memoria si los manejadores de eventos o las plantillas en caché no se liberan en la recarga.
- **Archivos Relevantes:**
  - `BiometricApp/BiometricApp/BiometricApp/UCVerifyFingerprint.cs`
  - `BiometricApp/BiometricApp/BiometricApp/FingerprintManager.cs`
  - `BiometricApp/BiometricApp/BiometricApp/frmDBEnrollment.cs`
  - `Sistema_Gimnasio/SistemaAcceso.xaml.cs`
  - `Sistema_Gimnasio/FormAgregar.cs`
- **Checks / Seams Sugeridos:**
  - Seam de desacoplamiento para inyectar un proveedor de huellas simuladas, permitiendo validar la caché y el algoritmo de matching sin hardware físico conectado.
  - Medición de latencia de identificación biométrica (tiempo de respuesta antes vs. después de la caché).
- **Criterios de Salida:**
  - Eliminación de consultas completas a base de datos y deserializaciones repetidas por cada lectura biométrica (si se confirma su impacto).
  - Conexiones a base de datos seguras ante concurrencia.
  - Estabilidad de la aplicación ante desconexión o falla del lector.
  - Compilación limpia y pruebas de consistencia con validación sin errores.

---

### Fase 4: Documentación Operativa y Despliegue

- **Objetivos:**
  1. **Manual integral de despliegue:** Redactar guía técnica y operativa detallada para instalación en puestos de recepción y servidores locales.
  2. **Puesta en marcha de base de datos:** Procedimiento formal para la instalación de SQL Server / SQL Server Express, ejecución de `database/SistemaGimnasio.sql`, creación de usuarios con mínimos privilegios y configuración de autenticación.
  3. **Instalación de controladores y SDK:** Guía clara sobre la instalación del runtime de DigitalPersona U.are.U (drivers, servicios de Windows, dependencias de 32/64 bits).
  4. **Estrategia de respaldos (Backups):** Elaboración de scripts y procedimientos de respaldo y restauración periódica de la base de datos `SistemaGimnasio`.
  5. **Guía de resolución de problemas (Troubleshooting):** Diagnóstico de errores comunes (fallos de conexión `GymDbConnection`, lector no detectado, huella no reconocida, errores de sincronización).
- **Riesgos:**
  - Desalineación entre las versiones de Windows (10 vs 11, x64) y las versiones de los controladores de DigitalPersona.
  - Permisos insuficientes en entornos productivos restringidos.
- **Archivos Relevantes:**
  - `docs/` (archivos de manual y guías operativas)
  - `README.md`
  - `database/SistemaGimnasio.sql`
  - `Sistema_Gimnasio/App.config`
  - `BiometricApp/BiometricApp/BiometricApp/App.config`
- **Checks / Seams Sugeridos:**
  - Ejecución de prueba de despliegue en limpio sobre una máquina o máquina virtual sin dependencias preinstaladas.
  - Comprobación de restauración de base de datos a partir de backup.
- **Criterios de Salida:**
  - Documentación de instalación clara, reproducible y en español.
  - Manual de operaciones y troubleshooting completo.
  - Verificación final de integridad de todo el repositorio.

---

## 5. Protocolo de Transición para Futuros Agentes

Al asumir una nueva fase de trabajo:

1. **Inspeccionar estado:** Ejecutar `git status` para confirmar que los cambios de Fase 0 continúan *staged*, Fase 1 continúa *no staged*, y `Docs/Images.zip` se mantiene *untracked*.
2. **Validar línea base:** Ejecutar `verify-reproducibility.ps1` y compilación por MSBuild para asegurar que el entorno base esté íntegro antes de realizar cualquier cambio.
3. **Respetar delimitación:** No modificar archivos fuera del alcance estipulado para la fase asignada.
4. **Cierre de fase:** Tras implementar, solicitar auditoría a **Luna (Read-Only)** y remitir el resultado al **Usuario Humano** para la evaluación del estado **GREEN**.
