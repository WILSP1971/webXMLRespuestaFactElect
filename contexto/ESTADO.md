# Estado del proyecto — webXMLRespuestaFactElect

> Este archivo es la fuente de verdad para "retomar" el proyecto. Antes de explorar
> el repo o pedir contexto, **lee este archivo primero**. Actualízalo al cerrar cada
> sesión de trabajo relevante (nuevas fases, decisiones, pendientes).

## Referencia externa

- SPEC: `SPEC-003` — vive en el repo del enjambre, NO en este repo:
  `/home/swarm/sistema-agentico/.swarm/specs/SPEC-003.md`
- Diseño UX: `/home/swarm/sistema-agentico/.swarm/design/SPEC-003-ux.md`
- ADR asociado: `ADR-003` (mismo repo del enjambre, `.swarm/adrs/`)

## Qué es

App web interna (LAN), **solo lectura**, para consultar y descargar el XML de
respuesta de la DIAN (`RespuestaXML`) asociado a un documento de facturación
electrónica. Stack: ASP.NET Core MVC (C#, net8.0), Razor + Bootstrap 5
(self-hosted), acceso a datos por Stored Procedures vía `Microsoft.Data.SqlClient`
(ADO.NET). Hosting objetivo: IIS (in-process, ANCM).

## Estado actual (2026-07-03)

Un único commit inicial en `main`: `fba6952 avance antes de cambiar de auth`
(2026-07-03 16:52). Working tree limpio, sincronizado con `origin/main`.

### Implementado

- **Funcionalidad completa F-1 a F-8** (según nombres en el código):
  - `HomeController` (mínimo) + `XmlRespuestaDianController` con las acciones:
    - `Index` — vista principal.
    - `ObtenerEmpresas` (F-1) — dropdown Empresas desde `GetEmpresas`.
    - `ObtenerTipoDocumentos` (F-3) — dropdown Tipo de Documento desde
      `Get_TipoDocumentosFactElect`.
    - `Buscar` (F-6/F-7) — invoca `Get_LogWebService` con los 4 parámetros
      (`@Empresa`, `@TipoDoc`, `@Prefijo`, `@NoDocumento`), formatea el XML
      (`XmlFormatter.Formatear`) y devuelve JSON (éxito / sin resultados / error).
    - `Descargar` (F-8) — re-ejecuta la misma consulta y devuelve el `.xml` como
      archivo descargable (sin guardar estado de sesión en servidor).
  - Capa de datos: `IFactElectronicaRepository` / `FactElectronicaRepository`
    (ADO.NET puro), con mapeadores testables separados: `CatalogosQuery.cs`,
    `GetLogWebServiceQuery.cs` (mapeo tolerante case-insensitive de columnas).
  - `XmlFormatter` — prettify del XML antes de mostrar (F-7).
  - Vista `XmlRespuestaDian/Index.cshtml` + JS (`xml-respuesta-dian.js`) que puebla
    los dropdowns vía fetch y maneja búsqueda/descarga/errores controlados (AC-M2:
    mensajes controlados si falla la carga de catálogos, no crash).
  - Layout con navbar de 2 ítems; ítem "Otros" es **placeholder** ("Próximamente"),
    sin funcionalidad (fuera de alcance de SPEC-003).
  - **CHECKPOINT C3** (secreto de conexión): resuelto vía
    `ConnectionStrings:CadenaConexionDB`, con instrucciones para User Secrets (dev)
    y variables de entorno `ConnectionStrings__CadenaConexionDB` en IIS (prod).
    `appsettings.example.json` versionado solo con placeholders;
    `appsettings*.json` reales excluidos por `.gitignore`.
  - **CHECKPOINT C7** (pruebas): suite xUnit en
    `tests/webXMLRespuestaFactElect.Tests/` cubriendo armado de parámetros del SP,
    mapeo tolerante de `RespuestaXML` (incluye "sin resultados"/`DBNull`), mapeo de
    `GetEmpresas`/`Get_TipoDocumentosFactElect`, y el prettify del XML.
  - `web.config` de referencia para IIS/ANCM in-process.
  - **Sin autenticación de usuarios** (S-6 del SPEC): se asumió acceso restringido
    solo por red LAN interna. **Esto es lo que está a punto de cambiar** (ver
    "Próximo paso").

### Seguridad — pendiente de acción del Lead (no es código)

- La contraseña real de BD del proyecto original quedó expuesta en texto plano y
  **debe rotarse en SQL Server**. El código nuevo no la contiene, pero la rotación
  sigue pendiente y es responsabilidad del Lead.

### Supuestos sin confirmar por el Lead (SPEC-003 §9)

- **S-1 / S-2:** nombres exactos de columnas de `GetEmpresas` y
  `Get_TipoDocumentosFactElect` (mapeo actual tolerante a variantes comunes).
- **S-3:** nombre exacto de columna de salida de `Get_LogWebService` (se asume
  `RespuestaXML`, con tolerancia a variantes).
- **S-4:** `GetFacturaElectronica` documentado pero **no cableado** a esta versión
  de la UI.

## Próximo paso (en curso / anunciado)

El mensaje del último commit ("avance antes de cambiar de auth") indica que el
siguiente trabajo es **incorporar autenticación de usuarios**, lo cual revisa la
decisión S-6 (antes: "sin auth, solo LAN"). Aún no se ha decidido/confirmado con
el usuario:

- Mecanismo de auth (Windows/AD integrado vía IIS, formulario con usuarios locales,
  algún proveedor externo, etc.).
- Si esto requiere un nuevo SPEC/ADR en el repo del enjambre o es una extensión de
  SPEC-003/ADR-003.
- Alcance: ¿toda la app requiere login, o solo ciertas acciones?

**Antes de implementar, preguntar al usuario estos puntos** — no asumir el
mecanismo de autenticación.

## Notas de entorno

- El entorno de este sandbox **no tiene `dotnet` instalado** (`dotnet --version`
  falla con "command not found"). No se puede compilar/testear localmente aquí;
  cualquier verificación de build debe hacerse donde sí haya el SDK, o advertir
  explícitamente esta limitación al usuario.
