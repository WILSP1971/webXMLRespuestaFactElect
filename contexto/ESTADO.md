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

## Estado actual (2026-07-04)

Commits en `main`, sincronizado con `origin/main` (push confirmado hasta
`3570495`):

1. `fba6952 avance antes de cambiar de auth` — funcionalidad F-1..F-8 completa
   (búsqueda de un único XML).
2. `e07967b Agregar autenticacion Windows/AD integrada (revision de S-6)` —
   autenticación Windows/AD integrada implementada.
3. `2c4e3eb Reemplazar el visor de un solo XML por un grid + textarea
   (historial completo)` — cambio de "visor de un solo XML" a "grid +
   textarea" (ver detalle más abajo), ya commiteado.
4. `5e334f1 prueba de sync`.
5. `e19842b Corregir dropdown de tipo de documento y flujo de busqueda` —
   ver detalle en "Implementado — correcciones dropdown/búsqueda" más abajo.
6. `3570495 Corregir dropdown de Empresa y unificar txtEmpresa para toda la
   busqueda` — ver detalle en "Implementado — dropdown de Empresa y
   txtEmpresa" más abajo. **Pusheado a origin/main.**

### Implementado — base funcional (commit `fba6952`)

- `HomeController` (mínimo) + `XmlRespuestaDianController`.
- `ObtenerEmpresas` (F-1) / `ObtenerTipoDocumentos` (F-3) — dropdowns.
- Capa de datos: `IFactElectronicaRepository` / `FactElectronicaRepository`
  (ADO.NET puro), mapeadores testables: `CatalogosQuery.cs`,
  `GetLogWebServiceQuery.cs` (mapeo tolerante case-insensitive de columnas).
- `XmlFormatter` — prettify del XML antes de mostrar (F-7).
- Layout con navbar de 2 ítems; ítem "Otros" es **placeholder** ("Próximamente").
- **CHECKPOINT C3** (secreto de conexión): `ConnectionStrings:CadenaConexionDB`,
  User Secrets (dev) / variable de entorno `ConnectionStrings__CadenaConexionDB`
  (IIS). `appsettings.example.json` versionado solo con placeholders.
- **CHECKPOINT C7** (pruebas): suite xUnit en
  `tests/webXMLRespuestaFactElect.Tests/`.
- `web.config` de referencia para IIS/ANCM in-process.

### Implementado — autenticación Windows/AD (commit `e07967b`)

- `Program.cs`: `AddAuthentication(NegotiateDefaults...).AddNegotiate()` +
  `AddAuthorization` con `FallbackPolicy = RequireAuthenticatedUser()` (toda la
  app exige login; no se usa `[AllowAnonymous]` en ningún lado hoy) +
  `app.UseAuthentication()` antes de `UseAuthorization()`.
- `web.config`: `<anonymousAuthentication enabled="false" />`,
  `<windowsAuthentication enabled="true" />`, `forwardWindowsAuthToken="true"`
  en `<aspNetCore>`. Requiere el feature "Windows Authentication" instalado en
  IIS (no viene por defecto); comando `appcmd unlock config` documentado en el
  propio `web.config` y en el README si la sección es rechazada.
- `Properties/launchSettings.json` (nuevo): perfil IIS Express con
  `windowsAuthentication: true` / `anonymousAuthentication: false`; perfil
  Kestrel (`dotnet run`) usa SSPI directo (solo funciona en Windows
  domain-joined; en Linux/Mac de dev da 401 esperado, no es bug).
- `_Layout.cshtml`: navbar muestra `dominio\usuario` autenticado.
- README: sección "Autenticación Windows/AD integrada".
- **No probado end-to-end todavía** — este sandbox no tiene `dotnet` (ver
  "Notas de entorno"). Pendiente que el usuario lo pruebe en un PC Windows.

### Implementado — grid de resultados (sin commitear aún, en esta sesión)

Cambio pedido por el usuario: la búsqueda ya no muestra un único XML, sino
**todas las filas** que devuelve `Get_LogWebService` para los mismos criterios
(el SP puede loguear varias llamadas de webservice por documento), en un grid.

- **Modelo nuevo** `Models/LogWebServiceViewModel.cs`: `FechaHoraLog`
  (`DateTime?`), `MetodoWs` (`string`), `RespuestaXml` (`string`).
- `Services/GetLogWebServiceQuery.cs`: nuevo `MapearFila(IDataRecord)` →
  `LogWebServiceViewModel` (reutiliza `MapearRespuestaXml` existente para la
  columna XML; nombres de columna `FechaHoraLog`/`MetodoWs` tal como los dio el
  usuario, case-insensitive vía `BuscarIndiceColumna`).
- `IFactElectronicaRepository` / `FactElectronicaRepository`: el método
  `ObtenerRespuestaXmlAsync` (devolvía `string?`, solo la primera fila) fue
  **reemplazado** por `ObtenerHistorialLogAsync` (devuelve
  `IReadOnlyList<LogWebServiceViewModel>`, lee TODAS las filas con
  `while (await lector.ReadAsync(ct))`, igual patrón que `ObtenerEmpresasAsync`).
- `Models/BuscarXmlResponse.cs`: simplificado a `Error` / `MensajeError` /
  `Registros` (lista vacía = "sin resultados", ya no hay flag `Encontrado`).
- `XmlRespuestaDianController.Buscar`: formatea el XML de **cada** fila
  (`XmlFormatter.Formatear`) antes de devolver el JSON.
- **Acción `Descargar` (GET) eliminada del controlador.** Con múltiples filas
  por documento no había una forma limpia de identificar "cuál" descargar sin
  agregar un parámetro/llave nueva; en vez de eso, la descarga ahora es
  **100% client-side**: `xml-respuesta-dian.js` arma un `Blob` con el XML ya
  mostrado en el textarea (`filaSeleccionada.respuestaXml`) y dispara la
  descarga con un `<a download>` temporal. Sigue cumpliendo AC-8 (el archivo es
  exactamente lo que se ve en pantalla) sin round-trip adicional al servidor.
- Vista `Views/XmlRespuestaDian/Index.cshtml`: el bloque `estadoConXml` (un
  `<pre>`) fue reemplazado por `estadoConResultados` con dos columnas: tabla
  `#tablaLog` (thead Fecha y hora / Método WS / Respuesta XML, `tbody
  #cuerpoTablaLog` poblado por JS) + `#txtVisorXml` (`<textarea readonly>`) al
  lado.
- `wwwroot/js/xml-respuesta-dian.js`: reescrito — `renderizarRegistros()` pinta
  las filas del grid, `seleccionarFila(indice)` resalta la fila
  (`table-active`) y vuelca `respuestaXml` en el textarea; clic o
  Enter/Espacio en una fila selecciona; la primera fila se autoselecciona tras
  buscar.
- `wwwroot/css/site.css`: `.log-tabla-contenedor` (scroll, max-height 60vh),
  `#tablaLog tbody tr { cursor: pointer; }`, `.log-columna-xml` (truncado con
  ellipsis + `title` con el XML completo para hover).
- Tests: `GetLogWebServiceQueryTests.cs` — agregadas
  `MapearFila_MapeaFechaHoraLogMetodoWsYRespuestaXml_ParaElGridDeHistorial` y
  `MapearFila_DevuelveValoresPorDefecto_CuandoLasColumnasNoExistenOSonNulas`.
  No se tocó `FakeDataRecord.cs` (el mapeo usa `GetValue`, no `GetDateTime`).
- Verificado por grep que no quedan referencias colgantes a
  `ObtenerRespuestaXmlAsync`, `BuscarXmlResponse.ConXml/SinResultados`,
  `estadoConXml`, `visorXml`, ni al endpoint `/XmlRespuestaDian/Descargar`.
- **No compilado ni probado** (sin `dotnet` en este sandbox) — pendiente
  `dotnet build` / `dotnet test` / prueba manual en el PC Windows del usuario.

### Implementado — correcciones dropdown/búsqueda (commit `e19842b`, pusheado)

Correcciones pedidas por el usuario tras confirmar la firma real de
`Get_TipoDocumentosFactElect` (recibe `@Empresa varchar(6)` obligatorio y
devuelve columnas `Empresa, CodDocumento, NombreDocumento, TipoDocumento`).

- **S-2 CONFIRMADO** (ya no es supuesto): `Services/CatalogosQuery.cs` ahora
  incluye `CodDocumento`/`NombreDocumento` en las listas de nombres de columna
  candidatos para `MapearTipoDocumento` (con las variantes previas como
  tolerancia adicional). `Models/TipoDocumentoViewModel.cs` actualizado para
  reflejar que el supuesto quedó confirmado.
- Vista `Views/XmlRespuestaDian/Index.cshtml`: agregado
  `<input type="hidden" id="txtTipoDocumento">` dentro del bloque de
  `selTipoDoc`, para guardar el `CodDocumento` de la opción seleccionada.
- `wwwroot/js/xml-respuesta-dian.js` — **reescrito**, eliminando el modo
  "diagnóstico" (heurística genérica sobre nombres de campo) que traía desde
  la sesión anterior:
  - El dropdown de tipo de documento ahora se pinta como
    `CodDocumento - NombreDocumento` (ej. `FA - FACTURA ESCULAPIO`); `value`
    del `<option>` es el `CodDocumento`.
  - Al cambiar `selTipoDoc`, el código se copia a `txtTipoDocumento` (oculto).
  - El botón Buscar valida, al hacer click, que `txtNombreEmpresa`,
    `txtTipoDocumento`, `txtPrefijo` y `txtNoDocumento` no estén vacíos,
    marcando `is-invalid` en los controles visibles correspondientes
    (`selEmpresa`, `selTipoDoc`, `txtPrefijo`, `txtNoDocumento`) cuando falta
    algo.
  - **Bug corregido**: el JS enviaba al POST `/XmlRespuestaDian/Buscar` las
    claves `codEmpresa`/`tipoDocumento`, pero `Models/BuscarXmlRequest.cs`
    espera `Empresa`/`TipoDoc` (el resto del binding es case-insensitive, pero
    los nombres no coincidían en absoluto) — esos dos parámetros nunca
    llegaban bien al controlador. Ahora el payload usa
    `{ empresa, tipoDoc, prefijo, noDocumento }`.
  - El texto del spinner (botón Buscar + overlay del panel) cambió a
    "En progreso...".
  - El botón Descargar ahora queda `disabled` hasta que se selecciona una
    fila del grid (antes solo dependía de mostrar el estado "con resultados").
- **No compilado ni probado** (mismo motivo: sin `dotnet` en este sandbox) —
  pendiente verificación end-to-end contra la BD real (dropdown, validación,
  búsqueda con los parámetros corregidos, descarga) en el entorno del usuario.

### Implementado — dropdown de Empresa y txtEmpresa (commit `3570495`, pusheado)

Correcciones pedidas por el usuario tras confirmar la firma real de
`Getempresas` (columnas `empresa, NombreEmpresa, Nit, CodCiudad,
NombreCiudad, CodDepartamento, NombreDepartamento, CodPais, NombrePais,
Direccion, Telefonos, Estado, PaginaWeb, LogoEmpresa, TipoLogo`).

- **S-1 CONFIRMADO** (ya no es supuesto): las columnas reales `empresa`
  (código) y `NombreEmpresa` ya estaban cubiertas por la lista de nombres
  candidatos de `Services/CatalogosQuery.cs` (coincidencia case-insensitive);
  se reordenaron como primera opción y se actualizaron los comentarios /
  `Models/EmpresaViewModel.cs`. También se corrigió el nombre exacto del SP
  a `Getempresas` (antes `GetEmpresas`; SQL Server resuelve esto
  case-insensitive por defecto, pero se alineó por claridad).
- Vista `Views/XmlRespuestaDian/Index.cshtml`: agregado
  `<input type="hidden" id="txtEmpresa">` dentro del bloque de `selEmpresa`,
  para guardar el código de empresa seleccionado.
- `wwwroot/js/xml-respuesta-dian.js`:
  - El dropdown de Empresa ahora se pinta como `Codigo - NombreEmpresa`
    (ej. `07 - Fundacion Campobell`).
  - Al cambiar `selEmpresa`, el código se copia a `txtEmpresa` (oculto);
    `txtNombreEmpresa` se mantiene solo para mostrar el nombre.
  - `txtEmpresa` es ahora la única fuente para: la validación del botón
    Buscar (reemplaza a `txtNombreEmpresa` en `cumpleCriteriosCompletos` /
    `validarCamposObligatorios`), la carga en cascada de
    `ObtenerTipoDocumentos` (`cargarTipoDocumentos(txtEmpresa.value)`) y el
    parámetro `empresa` del payload enviado a `/XmlRespuestaDian/Buscar`
    (antes se leía `selEmpresa.value` directamente en los tres casos).
- **No compilado ni probado** (sin `dotnet` en este sandbox) — pendiente
  verificación end-to-end en el entorno del usuario.

### Seguridad — pendiente de acción del Lead (no es código)

- La contraseña real de BD del proyecto original quedó expuesta en texto plano y
  **debe rotarse en SQL Server**. El código nuevo no la contiene.
- El remoto `origin` (`git remote -v`) tiene un **GitHub Personal Access Token
  embebido en texto plano en la URL** (visible en `.git/config`). Se le avisó al
  usuario; recomendado rotarlo y reconfigurar el remoto sin el token embebido.
  Estado: avisado, no resuelto.

### Supuestos sin confirmar por el Lead (SPEC-003 §9)

- ~~**S-1**~~ **CONFIRMADO** (2026-07-04): `Getempresas` devuelve, entre
  otras, las columnas `empresa` (código) y `NombreEmpresa`. Ya reflejado en
  `CatalogosQuery.cs` (ver "Implementado — dropdown de Empresa y txtEmpresa").
- ~~**S-2**~~ **CONFIRMADO** (2026-07-04): `Get_TipoDocumentosFactElect`
  recibe `@Empresa varchar(6)` obligatorio y devuelve columnas `Empresa,
  CodDocumento, NombreDocumento, TipoDocumento`. Ya reflejado en
  `CatalogosQuery.cs` (ver "Implementado — correcciones dropdown/búsqueda").
- **S-3:** nombre exacto de columna de salida de `Get_LogWebService` (se asume
  `RespuestaXML`, con tolerancia a variantes).
- **S-4:** `GetFacturaElectronica` documentado pero **no cableado** a esta
  versión de la UI.
- Las columnas `FechaHoraLog` y `MetodoWs` del grid se asumen con esos nombres
  exactos (dados por el usuario), con matching case-insensitive pero **sin**
  lista de variantes alternativas (a diferencia de S-1/S-2/S-3). Si el Lead
  confirma nombres distintos, ajustar
  `PosiblesNombresColumnaFechaHoraLog`/`PosiblesNombresColumnaMetodoWs` en
  `GetLogWebServiceQuery.cs`.

## Próximo paso

1. Probar en el PC Windows (build + auth Windows integrada + grid + dropdown
   de Empresa + dropdown de tipo de documento + validación de Buscar +
   descarga) — este sandbox no puede compilar/ejecutar la app.
2. Rotar el PAT de GitHub expuesto en el remoto (pendiente, avisado al
   usuario).
3. Seguir pendientes ya conocidos: rotación de contraseña de BD, confirmación
   de supuestos S-3 y S-4 con el Lead (S-1 y S-2 ya quedaron confirmados).

## Notas de entorno

- El entorno de este sandbox **no tiene `dotnet` instalado** (`dotnet --version`
  falla con "command not found"). No se puede compilar/testear localmente aquí;
  cualquier verificación de build debe hacerse donde sí haya el SDK (p. ej. el
  PC Windows del usuario), o advertir explícitamente esta limitación.
