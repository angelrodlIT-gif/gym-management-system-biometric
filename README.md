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

> **Nota de reproducibilidad y ejecución:** Se probó la restauración de paquetes y compilación vía MSBuild en configuración `Debug`, resolviendo dependencias desde `libs/` y NuGet sin hardware conectado. La inicialización, comunicación con el lector y captura de huellas requieren el dispositivo físico y sus controladores/runtime de DigitalPersona instalados en el sistema operativo.

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

## Librerías SDK (libs/)
Las DLLs del SDK (`DPCtlUruNet.dll`, `DPCtlXUru.dll`, `DPUruNet.dll`, `DPXUru.dll`) se encuentran versionadas en el directorio `libs/` y son referenciadas de forma relativa desde los archivos `.csproj` (`Sistema_Gimnasio.csproj` y `BiometricApp.csproj`). Esto permite resolver las referencias y compilar directamente desde el repositorio sin requerir rutas absolutas locales a `Program Files`. El runtime del SDK comercial y sus drivers deben ser provistos e instalados por el usuario para la interacción con el lector físico en ejecución.

## Referencias
- DashBoard C# XAML: https://youtu.be/mlmyFXJy8gQ?si=VXKKKBo_pvUkGP_0
- C# U are U SDK: https://youtu.be/j-h5GsKoi1g?si=ed1JnvJ3HHH0ZS2A

## Licencia

MIT
