# webXMLRespuestaFactElect — XML Respuesta Dian

Aplicación web interna (LAN), de **solo lectura**, para consultar y descargar el XML de
respuesta de la DIAN (`RespuestaXML`) asociado a un documento de facturación electrónica.

- SPEC: `.swarm/specs/SPEC-003.md` (repo del enjambre) · ADR: `ADR-003`
- Stack: **ASP.NET Core MVC (C#), net8.0**, Razor + Bootstrap 5 (self-hosted), acceso a
  datos por Stored Procedures vía `Microsoft.Data.SqlClient` (ADO.NET).
- Hosting objetivo: **IIS** (Windows) con el módulo ASP.NET Core (ANCM), **in-process**.

## ⚠️ Seguridad — leer antes de nada

El objetivo original de esta app traía la contraseña real de la base de datos en texto
plano. **Esa contraseña ya quedó expuesta y debe ROTARSE** en SQL Server por el Lead lo
antes posible, independientemente de que el código nuevo no la contenga.

Este repositorio **no contiene ninguna credencial real**. Solo se versiona
`src/webXMLRespuestaFactElect/appsettings.example.json` con placeholders. Cualquier
`appsettings.json` / `appsettings.*.json` con datos reales está excluido por
`.gitignore` y **nunca debe forzarse** a `git add`.

## Requisitos

- .NET 8 SDK (`dotnet --version` ≥ 8.0).
- SQL Server accesible en `192.168.2.20\SIESA`, catálogo `FactElectronicaDB` (solo
  alcanzable desde la red interna/el entorno del Lead).
- IIS con el **ASP.NET Core Hosting Bundle** instalado (incluye ANCM), para publicar.

## Estructura del repositorio

```
webXMLRespuestaFactElect.sln
src/webXMLRespuestaFactElect/        Proyecto web MVC (net8.0)
  Controllers/                       HomeController, XmlRespuestaDianController
  Models/                            ViewModels/DTOs (Empresa, TipoDocumento, requests/responses)
  Services/                          IFactElectronicaRepository + implementación ADO.NET,
                                      mapeadores testables (GetLogWebServiceQuery, CatalogosQuery),
                                      XmlFormatter (prettify)
  Views/                             Layout (navbar 2 ítems), vista "XML Respuesta Dian"
  wwwroot/                           Bootstrap 5 + Bootstrap Icons self-hosted, CSS/JS propios
  appsettings.example.json           PLANTILLA versionada (placeholders, sin secretos)
  web.config                         Config de referencia para IIS/ANCM in-process
tests/webXMLRespuestaFactElect.Tests/  Pruebas xUnit (armado de parámetros y mapeo de resultados)
```

## Configurar el secreto de conexión de forma segura (CHECKPOINT C3)

La cadena de conexión se lee de configuración bajo la clave
**`ConnectionStrings:CadenaConexionDB`**. Nunca se hardcodea en el código ni se
versiona con un valor real.

### Desarrollo local — ASP.NET Core User Secrets (recomendado)

```bash
cd src/webXMLRespuestaFactElect
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:CadenaConexionDB" "Data Source=192.168.2.20\SIESA;Initial Catalog=FactElectronicaDB;User Id=<usuario>;Password=<password-rotado>;TrustServerCertificate=True"
```

Esto guarda el secreto **fuera del repositorio** (perfil del usuario de Windows/Linux),
identificado por el `UserSecretsId` ya declarado en el `.csproj`.

Alternativa rápida sin User Secrets: copiar `appsettings.example.json` a
`appsettings.Development.json` (que está en `.gitignore`) y reemplazar los
placeholders. **No renombrar a `appsettings.json` ni versionar este archivo.**

### Producción — IIS (variables de entorno)

ASP.NET Core traduce automáticamente variables de entorno con `__` (doble guion bajo)
como separador de jerarquía. En el Administrador de IIS, sobre el **Application Pool**
o el sitio, configurar:

```
Nombre:  ConnectionStrings__CadenaConexionDB
Valor:   Data Source=192.168.2.20\SIESA;Initial Catalog=FactElectronicaDB;User Id=<usuario>;Password=<password-rotado>;TrustServerCertificate=True
```

(Panel de IIS: sitio → *Configuration Editor* → sección `system.webServer/aspNetCore` →
`environmentVariables`, o Application Pool → *Advanced Settings* según la versión de
IIS/ANCM. También es válida una variable de entorno de sistema del servidor si se
prefiere gestionar el secreto con un secret manager externo.)

Otras claves opcionales (no secretas) en `FactElectronicaDb`:
`ComandoTimeoutSegundos`, `ConexionTimeoutSegundos` (NF-5).

## Compilar y ejecutar

```bash
cd /ruta/al/repo
dotnet restore
dotnet build
dotnet run --project src/webXMLRespuestaFactElect
```

Sin BD configurada, la vista carga igual: los dropdowns muestran un mensaje controlado
("No fue posible cargar las empresas / los tipos de documento. Intente de nuevo.") en
vez de fallar (AC-M2).

## Ejecutar las pruebas (CHECKPOINT C7)

```bash
dotnet test tests/webXMLRespuestaFactElect.Tests
```

Las pruebas verifican, **sin base de datos real** (dobles de `IDataRecord` y de los
parámetros de `Microsoft.Data.SqlClient`):

- El armado de los 4 parámetros de `Get_LogWebService` (`@Empresa`, `@TipoDoc`,
  `@Prefijo`, `@NoDocumento`) con el orden y tipo esperados.
- El mapeo tolerante (case-insensitive) de la columna `RespuestaXML` a partir de una
  fila simulada, incluyendo los casos "sin resultados" / `DBNull`.
- El mapeo de `GetEmpresas` / `Get_TipoDocumentosFactElect`.
- El formateo (prettify) del XML antes de mostrarse (F-7).

## Publicar en IIS (in-process ANCM)

1. Instalar en el servidor el **.NET 8 Hosting Bundle** (incluye ANCM V2).
2. Publicar:
   ```bash
   dotnet publish src/webXMLRespuestaFactElect -c Release -o ./publish
   ```
3. Copiar el contenido de `./publish` a la carpeta del sitio en IIS.
4. Crear el sitio/aplicación en IIS apuntando a esa carpeta; el `web.config` generado
   por `dotnet publish` (basado en `src/webXMLRespuestaFactElect/web.config`) ya
   declara `hostingModel="InProcess"`.
5. Configurar `ConnectionStrings__CadenaConexionDB` como variable de entorno del sitio
   (ver sección de seguridad arriba). **No copiar ningún `appsettings.json` con datos
   reales dentro del repositorio versionado**; puede colocarse directamente en la
   carpeta publicada del servidor (fuera de Git) o preferirse la variable de entorno.
6. Verificar que el Application Pool usa **"No Managed Code"** (requerido por ANCM).
7. Confirmar conectividad de red del servidor IIS hacia `192.168.2.20\SIESA` (S-7).

## Autenticación Windows/AD integrada

La app dejó de asumir "sin login, solo LAN" (S-6 original, ver `contexto/ESTADO.md`).
Ahora **toda la app exige una sesión de dominio autenticada** vía Windows
Authentication (Negotiate), integrada con IIS. No hay pantalla de login propia:
IIS/el navegador negocian las credenciales de Windows de forma transparente; el
navbar muestra el usuario autenticado (`dominio\usuario`).

### Requisitos en el servidor IIS

1. Instalar el **rol/feature "Windows Authentication"** de IIS (no viene activado
   por defecto): *Server Manager → Add Roles and Features → Web Server (IIS) →
   Security → Windows Authentication*.
2. El `web.config` versionado ya declara, dentro de `<system.webServer>`:
   - `<anonymousAuthentication enabled="false" />`
   - `<windowsAuthentication enabled="true" />`
   - `forwardWindowsAuthToken="true"` en `<aspNetCore>` (requerido para que ANCM
     reenvíe el token de Windows al proceso in-process).
3. Si IIS rechaza estas secciones del `web.config` ("This configuration section
   cannot be used at this path..."), desbloquear una vez en el servidor:
   ```
   %windir%\system32\inetsrv\appcmd.exe unlock config /section:windowsAuthentication
   %windir%\system32\inetsrv\appcmd.exe unlock config /section:anonymousAuthentication
   ```
4. Confirmar que el Application Pool corre con una identidad con acceso al dominio
   (normalmente basta con `ApplicationPoolIdentity` en un servidor unido al dominio).

### Desarrollo local

- `Properties/launchSettings.json` habilita `windowsAuthentication` / deshabilita
  `anonymousAuthentication` para el perfil **IIS Express** (Windows).
- Con el perfil Kestrel (`dotnet run`) Negotiate usa SSPI directamente; solo
  funciona en una máquina Windows unida al dominio (o con Kerberos configurado).
  En Linux/macOS de desarrollo, las peticiones no autenticadas recibirán `401`
  esperado — no es un bug, es la ausencia de un KDC/SSPI local.

## Supuestos a confirmar por el Lead (ver SPEC-003 §9)

- **S-1 / S-2:** nombres exactos de columnas de `GetEmpresas` y
  `Get_TipoDocumentosFactElect` (el mapeo actual es tolerante a varias variantes
  comunes, ver `Services/CatalogosQuery.cs`).
- **S-3:** nombre exacto de la columna de salida de `Get_LogWebService`
  (se asume `RespuestaXML`, con tolerancia a variantes, ver
  `Services/GetLogWebServiceQuery.cs`).
- **S-4:** `GetFacturaElectronica` queda documentado pero **no está cableado** a esta
  versión de la UI.

## Alcance y limitaciones (por diseño, ver SPEC-003)

- Solo lectura: no hay inserciones, actualizaciones ni borrados.
- Requiere autenticación Windows/AD integrada (revisión de S-6; ver sección
  "Autenticación Windows/AD integrada" arriba) — no hay acceso anónimo.
- El ítem de menú "Otros" es un **placeholder** sin funcionalidad ("Próximamente").
