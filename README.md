# 🏋️ Gym Management System with Biometric Authentication

![Platform](https://img.shields.io/badge/.NET_Framework-4.x-blue)
![Language](https://img.shields.io/badge/C%23-WinForms%20%7C%20WPF-239120)
![Database](https://img.shields.io/badge/Database-SQL_Server-red)
![Biometrics](https://img.shields.io/badge/Biometrics-DigitalPersona-success)
![License](https://img.shields.io/badge/License-MIT-green)

Sistema de administración para gimnasios desarrollado en **C# (.NET Framework)** que integra **WinForms**, **WPF**, **SQL Server** y autenticación biométrica mediante lectores **DigitalPersona U.are.U**.

El proyecto permite administrar miembros, membresías, pagos y controlar el acceso mediante reconocimiento de huella digital.

---

#  Características

-  Registro y administración de miembros.
-  Enrolamiento biométrico mediante DigitalPersona.
-  Verificación de identidad por huella digital.
-  Administración de membresías.
-  Registro e historial de pagos.
-  Registro de visitas.
-  Base de datos SQL Server.
-  Arquitectura modular separando la lógica biométrica del sistema principal.

---

# 🛠 Tecnologías

| Tecnología | Descripción |
|------------|-------------|
| C# | Lenguaje principal |
| .NET Framework | Plataforma de desarrollo |
| WinForms | Interfaces administrativas |
| WPF | Sistema de control de acceso |
| SQL Server | Base de datos |
| ADO.NET | Acceso a datos |
| DPUruNet SDK | Integración biométrica DigitalPersona |

---

#  Requisitos

- Visual Studio 2022
- SQL Server Express (o superior)
- .NET Framework
- DigitalPersona SDK
- Lector DigitalPersona U.are.U 4500

---

#  Base de datos

El proyecto incluye el respaldo de la base de datos.

```
Database/
└── SistemaGimnasio.bak
```

Una vez restaurada la base de datos, actualiza la cadena de conexión correspondiente en:

```
App.config
```

---

#  DigitalPersona SDK

Por motivos de licencia **las DLL del SDK no se incluyen en este repositorio**.

Para ejecutar el proyecto es necesario instalar el **DigitalPersona U.are.U SDK** y agregar las referencias correspondientes.

---

#  Capturas del sistema

##  Dashboard

<p align="center">
<img src="Docs/Images/CapInicio.png" width="900">
</p>

Pantalla principal desde donde se administran todos los módulos del sistema.

---

##  Gestión de miembros

<p align="center">
<img src="Docs/Images/CapMiembros.png" width="900">
</p>

Administración completa de los miembros registrados.

---

##  Registro de miembros

<p align="center">
<img src="Docs/Images/CapRegistrarMiembro.png" width="48%">
<img src="Docs/Images/CapRegistrarMiembro2.png" width="48%">
</p>

Proceso de registro dividido en dos etapas:

- Información personal.
- Enrolamiento biométrico mediante lector DigitalPersona.

---

## 💳 Gestión de membresías

<p align="center">
<img src="Docs/Images/CapMembresias.png" width="900">
</p>

Creación, modificación y administración de los distintos tipos de membresías.

---

## Gestión de pagos

<p align="center">
<img src="Docs/Images/CapPagos.png" width="900">
</p>

Consulta del historial de pagos registrados.

---

## Registro de pagos

Registro de nuevos pagos y actualización automática de la vigencia de la membresía.

---

## Gestión de visitas

<p align="center">
<img src="Docs/Images/CapVisitas.png" width="900">
</p>

Registro y control de visitantes.

---

# Arquitectura del proyecto

```
GymManagementSystem/
│
├── BiometricApp/
│   ├── Fingerprint Enrollment
│   ├── Fingerprint Verification
│   └── DigitalPersona Integration
│
├── Sistema_Gimnasio/
│   ├── Member Management
│   ├── Memberships
│   ├── Payments
│   ├── Visits
│   └── Access Control
│
├── Database/
│
├── Docs/
│   └── Images/
│
└── README.md
```

---

# 📚 Referencias

Dashboard (WPF)

https://youtu.be/mlmyFXJy8gQ?si=VXKKKBo_pvUkGP_0

DigitalPersona U.are.U SDK

https://youtu.be/j-h5GsKoi1g?si=ed1JnvJ3HHH0ZS2A

---

#  Estado del proyecto

No esta en desarrollo activo por el momento

Características implementadas:

- ✔ Gestión de miembros
- ✔ Gestión de membresías
- ✔ Registro de pagos
- ✔ Registro de visitas
- ✔ Enrolamiento biométrico
- ✔ Verificación biométrica
- ✔ Control de acceso mediante huella digital

---

#  Licencia

Este proyecto está distribuido bajo la licencia **MIT**.

Consulta el archivo **LICENSE** para más información.
