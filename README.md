# eCertify

Sistema web para la gestión y emisión de **comprobantes fiscales electrónicos (e-CF)** conforme a las normativas de la **DGII (Dirección General de Impuestos Internos)** de la República Dominicana.

---

## ¿Qué es eCertify?

eCertify es una plataforma que permite a empresas registradas gestionar su proceso de facturación electrónica de forma segura y eficiente. El sistema actúa como intermediario entre los contribuyentes y la DGII, facilitando:

- Registro y autenticación de usuarios y empresas
- Emisión y firma de comprobantes fiscales electrónicos (e-CF)
- Consulta y seguimiento de comprobantes emitidos
- Gestión de RNC y datos fiscales de empresas

---

## Stack tecnológico

| Capa | Tecnología |
|---|---|
| Frontend / BFF | ASP.NET Core Razor Pages |
| API Backend | ASP.NET Core Web API |
| ORM | Entity Framework Core |
| Base de datos | SQL Server |
| Autenticación | Cookie (sesión web) + JWT (API) |

---

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local o remoto)
- Visual Studio 2022 o VS Code

---

## Configuración local

1. Clona el repositorio:
   ```bash
   git clone https://github.com/Luis-Pichardo/ECertify.git
   cd ECertify
   ```

2. Copia el archivo de configuración de ejemplo y llena tus valores:
   ```bash
   cp appsettings.json appsettings.Development.json
   ```

3. Edita `appsettings.Development.json` con tus datos reales:
   ```json
   {
     "ConnectionStrings": {
       "CadenaConexion": "Server=TU_SERVIDOR;Database=TU_BD;Trusted_Connection=True;TrustServerCertificate=True;"
     },
     "ClaveAccesoEmpresaProd": "TU_CLAVE_DE_ACCESO",
     "Jwt": {
       "Key": "UNA_CLAVE_SECRETA_DE_AL_MENOS_32_CARACTERES"
     }
   }
   ```

4. Aplica las migraciones de base de datos (si aplica) y corre el proyecto:
   ```bash
   dotnet run
   ```

---

## Estructura del proyecto

```
eCertify/
├── Controllers/       # API controllers (JWT auth)
├── Pages/             # Razor Pages (cookie auth)
├── Services/          # Lógica de negocio
├── Models/            # Entidades de base de datos
├── DTOs/              # Objetos de transferencia de datos
├── Interfaces/        # Contratos de servicios
├── Utils/             # Helpers y utilidades compartidas
├── Data/              # DbContext y configuración EF Core
└── wwwroot/           # Archivos estáticos (CSS, JS, assets)
```

---

## Seguridad

- Los secretos (JWT key, cadena de conexión, clave de acceso) **nunca se suben al repositorio**.
- Usa `appsettings.Development.json` localmente (está en `.gitignore`).
- En producción, usa variables de entorno o un gestor de secretos.

---

## Licencia

Proyecto privado — todos los derechos reservados.
