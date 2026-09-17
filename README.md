# Gym System

Sistema de administración para gimnasios desarrollado en C# y .NET Framework 4.8.

## Características

- Registro de miembros
- Gestión de membresías
- Control de acceso
- Registro biométrico
- Verificación mediante huella dactilar DigitalPersona
- Integración con base de datos SQL Server

## Tecnologías y Componentes

- C#
- .NET Framework 4.8
- Windows Presentation Foundation (WPF) / Windows Forms
- Microsoft SQL Server / SQL Server Express
- DigitalPersona One Touch / U.are.U SDK (DPUruNet)
- Paquetes NuGet (MahApps.Metro.IconPacks, NodaTime, System.Drawing.Common, etc.)

## Requisitos de Entorno y Hardware

- **IDE:** Visual Studio 2022 (con carga de trabajo de desarrollo de escritorio de .NET)
- **Framework:** .NET Framework 4.8 Developer Pack / Runtime
- **Motor de Base de Datos:** SQL Server Express (instancia local recomendada, ej. `.\SQLEXPRESS01`)
- **Hardware biométrico:** Lector de huellas dactilares DigitalPersona U.are.U 4500 (requerido para funciones biométricas en tiempo de ejecución)
- **Drivers/SDK en tiempo de ejecución:** DigitalPersona U.are.U SDK / drivers instalados en el sistema operativo para la comunicación con el dispositivo físico.
- **Resolución de referencias:** Los proyectos (`Sistema_Gimnasio.csproj` y `BiometricApp.csproj`) resuelven las dependencias del SDK (`DPCtlUruNet`, `DPCtlXUru`, `DPUruNet`, `DPXUru`) de forma relativa desde la carpeta `libs/` del repositorio, desacoplando la resolución de referencias de rutas externas a `Program Files`.

> **Nota de ejecución y dependencias:** Para compilar desde línea de comandos, el comando recomendado es `msbuild Sistema_Gimnasio.sln /p:Configuration=Debug`. En esta estación de trabajo se verificó la restauración y compilación durante Fase 0/1 (resolviendo dependencias desde `libs/` y NuGet sin hardware conectado), registrándose como validación local y no como garantía universal para otros entornos. La inicialización, comunicación con el lector y captura de huellas requieren el dispositivo físico y sus controladores/runtime de DigitalPersona instalados en el sistema operativo.

## Instalación y Puesta en Marcha

### 1. Base de datos
1. Abrir SQL Server Management Studio (SSMS) o la terminal con `sqlcmd`.
2. Conectarse a la instancia local de SQL Server Express.
3. Ejecutar el script SQL ubicado en el repositorio:
   ```text
   database/SistemaGimnasio.sql
   ```
   Este script creará la base de datos `SistemaGimnasio` y las tablas necesarias.

### 2. Configuración de Conexión
En el archivo `Sistema_Gimnasio/App.config`, verificar o ajustar la cadena de conexión según su instancia de SQL Server:
```xml
<connectionStrings>
    <add name="GymDbConnection"
         connectionString="Data Source=.\SQLEXPRESS01;Initial Catalog=SistemaGimnasio;Integrated Security=True"
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

> **Nota para BiometricApp standalone:** Si el proyecto `BiometricApp` se ejecuta o prueba de forma independiente (fuera de la aplicación principal `Sistema_Gimnasio`), este requiere su propia cadena `GymDbConnection` en `BiometricApp/BiometricApp/BiometricApp/App.config`. Ambos archivos deben mantenerse sincronizados con la misma cadena de conexión hacia la base de datos.

### 3. Restauración de Paquetes NuGet
Antes de compilar, restaurar las dependencias de paquetes NuGet:
- **Desde Visual Studio:** Clic derecho sobre la solución `Sistema_Gimnasio.sln` > **Restaurar paquetes NuGet**.
- **Desde línea de comandos (MSBuild):**
  ```shell
  msbuild Sistema_Gimnasio.sln /t:restore /p:RestorePackagesConfig=true
  ```

### 4. Compilación
Abrir `Sistema_Gimnasio.sln` en Visual Studio 2022 y compilar en configuración `Debug` o `Release` (Any CPU), o ejecutar vía MSBuild:
```shell
msbuild Sistema_Gimnasio.sln /p:Configuration=Debug
```

### 5. Verificación de Reproducibilidad y Consistencia
El repositorio incluye un script PowerShell de solo lectura (`scripts/verify-reproducibility.ps1`) para validar estáticamente la configuración y consistencia del entorno:
- Presencia y coherencia (igualdad) de `GymDbConnection` en ambos archivos `App.config` (`Sistema_Gimnasio` y `BiometricApp`).
- Ausencia de cadenas de conexión SQL hardcodeadas en código C# fuera de `App.config`.
- Comprobación de que las referencias del SDK apunten a `libs/` y que las DLLs existan físicamente en el repositorio.
- Validación de codificación UTF-8 estricta (con decoder exception fallback) y ausencia de bytes NUL, además de verificación de tablas (`Membresias`, `Miembros`, `Pagos`, `Visitas`) en `database/SistemaGimnasio.sql`.
- Verificación de que no existan artefactos compilados rastreados fuera de `libs/`.

> **Verificación local:** Durante Fase 0 y Fase 1 se ejecutó localmente en esta estación la compilación con MSBuild y la ejecución de `scripts/verify-reproducibility.ps1` como comprobación previa de integridad.

Para ejecutar la verificación:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify-reproducibility.ps1
```

## Librerías SDK (libs/)
Las DLLs del SDK (`DPCtlUruNet.dll`, `DPCtlXUru.dll`, `DPUruNet.dll`, `DPXUru.dll`) se encuentran versionadas en el directorio `libs/` y son referenciadas de forma relativa desde los archivos `.csproj` (`Sistema_Gimnasio.csproj` y `BiometricApp.csproj`). Esto permite resolver las referencias y compilar directamente desde el repositorio sin requerir rutas absolutas locales a `Program Files`. El runtime del SDK comercial y sus drivers deben ser provistos e instalados por el usuario para la interacción con el lector físico en ejecución.

## Documentación Operativa y Despliegue (Fase 4)

El repositorio cuenta con manuales completos y guías operativas en español ubicadas en el directorio `Docs/`:

- [Manual Integral de Despliegue (Docs/DEPLOYMENT.md)](Docs/DEPLOYMENT.md): Requisitos de hardware/sistema, puesta en marcha de SQL Server / SQL Express, ejecución de `database/SistemaGimnasio.sql`, política de permisos mínimos (*least-privilege*), configuración de `GymDbConnection`, instalación del runtime/drivers DigitalPersona U.are.U 4500 (x86/x64) y compilación.
- [Manual de Operación y Respaldos (Docs/OPERATIONS.md)](Docs/OPERATIONS.md): Estrategia de copias de seguridad, recomendación del modelo de recuperación `SIMPLE`, scripts de respaldo parametrizado (`scripts/backup-database.sql`, `scripts/Backup-Database.ps1`), política de retención, simulacros seguros de restauración aislada (`scripts/restore-drill.sql`), mantenimiento preventivo de base de datos y cuidados del hardware óptico.
- [Guía de Resolución de Problemas (Docs/TROUBLESHOOTING.md)](Docs/TROUBLESHOOTING.md): Árboles de decisión y resolución para fallos de conexión SQL (Errores 26, 40, 18456), detección del lector (`DP_DEVICE_FAILURE`, `DP_INVALID_DEVICE`), reconocimiento de huellas (prisma, piel reseca, re-enrolamiento), comportamiento y cooldown de `BiometricCache`, y resolución de problemas de compilación (MSBuild, NuGet restore).

### Scripts de Verificación Automatizada

El repositorio incluye suites de pruebas deterministas en PowerShell:
- **Verificación de Consistencia General:** `powershell -ExecutionPolicy Bypass -File scripts/verify-reproducibility.ps1`
- **Pruebas Automatizadas Fase 2:** `powershell -ExecutionPolicy Bypass -File scripts/test-phase2.ps1`
- **Pruebas Automatizadas Fase 3:** `powershell -ExecutionPolicy Bypass -File scripts/test-phase3.ps1`
- **Pruebas Automatizadas Fase 4:** `powershell -ExecutionPolicy Bypass -File scripts/test-phase4.ps1`

## Referencias
- DashBoard C# XAML: https://youtu.be/mlmyFXJy8gQ?si=VXKKKBo_pvUkGP_0
- C# U are U SDK: https://youtu.be/j-h5GsKoi1g?si=ed1JnvJ3HHH0ZS2A

## Licencia

MIT
