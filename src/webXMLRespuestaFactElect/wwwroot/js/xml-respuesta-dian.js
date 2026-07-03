/*
 * Vista "XML Respuesta Dian" (SPEC-003).
 *
 * Contrato de datos confirmado:
 *   - ObtenerEmpresas          -> [{ codigo, nombre }]
 *   - ObtenerTipoDocumentos    -> [{ codigo, descripcion }]  (CodDocumento/NombreDocumento)
 *   - Buscar (POST)            -> body { empresa, tipoDoc, prefijo, noDocumento }
 *                                  (deben coincidir con BuscarXmlRequest: Empresa/TipoDoc/Prefijo/NoDocumento)
 */
(function () {
  "use strict";

  var urls = {
    empresas: "/XmlRespuestaDian/ObtenerEmpresas",
    tipoDocumentos: "/XmlRespuestaDian/ObtenerTipoDocumentos",
    buscar: "/XmlRespuestaDian/Buscar"
  };

  var selEmpresa = document.getElementById("selEmpresa");
  var txtNombreEmpresa = document.getElementById("txtNombreEmpresa");
  var selTipoDoc = document.getElementById("selTipoDoc");
  var txtTipoDocumento = document.getElementById("txtTipoDocumento");
  var txtPrefijo = document.getElementById("txtPrefijo");
  var txtNoDocumento = document.getElementById("txtNoDocumento");
  var frmBuscarXml = document.getElementById("frmBuscarXml");
  var btnBuscar = document.getElementById("btnBuscar");
  var btnDescargar = document.getElementById("btnDescargar");
  var selEmpresaHelp = document.getElementById("selEmpresaHelp");
  var selTipoDocHelp = document.getElementById("selTipoDocHelp");
  var panelResultado = document.getElementById("panelResultado");
  var estados = {
    inicial: document.getElementById("estadoInicial"),
    cargando: document.getElementById("estadoCargando"),
    sinResultados: document.getElementById("estadoSinResultados"),
    error: document.getElementById("estadoError"),
    conResultados: document.getElementById("estadoConResultados")
  };
  var estadoErrorMensaje = document.getElementById("estadoErrorMensaje");
  var estadoSinResultadosDetalle = document.getElementById("estadoSinResultadosDetalle");
  var cuerpoTablaLog = document.getElementById("cuerpoTablaLog");
  var txtVisorXml = document.getElementById("txtVisorXml");

  // Crea overlay si no existe
  var panelResultadoOverlay = document.getElementById("panelResultadoOverlay");
  if (!panelResultadoOverlay && panelResultado) {
    panelResultadoOverlay = document.createElement("div");
    panelResultadoOverlay.id = "panelResultadoOverlay";
    panelResultado.style.position = "relative";
    panelResultado.insertBefore(panelResultadoOverlay, panelResultado.firstChild);
  }
  if (panelResultadoOverlay) {
    panelResultadoOverlay.style.cssText =
      "position: absolute; top: 0; left: 0; width: 100%; height: 100%;" +
      "background: rgba(255,255,255,0.85); display: none;" +
      "align-items: center; justify-content: center; flex-direction: column;" +
      "z-index: 9999; border-radius: 0.375rem; pointer-events: none;";
    panelResultadoOverlay.innerHTML =
      '<div class="spinner-border text-primary" style="width: 3rem; height: 3rem;" role="status">' +
        '<span class="visually-hidden">En progreso...</span>' +
      '</div>' +
      '<div class="mt-3 text-primary fw-bold">En progreso...</div>';
  }

  var BTON_BUSCAR_NORMAL_HTML =
    '<span class="d-inline-flex align-items-center">' +
      '<i class="bi bi-search me-1" aria-hidden="true"></i>' +
      '<span>Buscar</span>' +
    '</span>';
  var BTON_BUSCAR_CARGANDO_HTML =
    '<span class="d-inline-flex align-items-center">' +
      '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>' +
      '<span>En progreso...</span>' +
    '</span>';

  var registrosActuales = [];
  var filaSeleccionada = null;
  var ultimaBusquedaExitosa = null;

  // -- Utilidades ----------------------------------------------

  function slug(texto) {
    if (!texto) return "";
    return texto.toString().normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/[^a-zA-Z0-9]+/g, "_")
      .replace(/^_|_$/g, "");
  }

  // -- Estado UI ----------------------------------------------

  function mostrarEstado(nombreEstado) {
    Object.keys(estados).forEach(function (clave) {
      if (estados[clave]) {
        estados[clave].classList.toggle("d-none", clave !== nombreEstado);
      }
    });
    if (btnDescargar) {
      btnDescargar.classList.toggle("d-none", nombreEstado !== "conResultados");
    }
  }

  function actualizarDisponibilidadBuscar() {
    btnBuscar.disabled = !cumpleCriteriosCompletos();
  }

  function cumpleCriteriosCompletos() {
    return !!txtNombreEmpresa.value.trim()
      && !!txtTipoDocumento.value.trim()
      && !!txtPrefijo.value.trim()
      && !!txtNoDocumento.value.trim();
  }

  function marcarInvalido(campo, esInvalido) {
    if (campo) campo.classList.toggle("is-invalid", !!esInvalido);
  }

  function validarCamposObligatorios() {
    var nombreEmpresaVacio = !txtNombreEmpresa.value.trim();
    var tipoDocumentoVacio = !txtTipoDocumento.value.trim();
    var prefijoVacio = !txtPrefijo.value.trim();
    var noDocumentoVacio = !txtNoDocumento.value.trim();

    marcarInvalido(selEmpresa, nombreEmpresaVacio);
    marcarInvalido(selTipoDoc, tipoDocumentoVacio);
    marcarInvalido(txtPrefijo, prefijoVacio);
    marcarInvalido(txtNoDocumento, noDocumentoVacio);

    return !nombreEmpresaVacio && !tipoDocumentoVacio && !prefijoVacio && !noDocumentoVacio;
  }

  function alCambiarFiltro() {
    actualizarDisponibilidadBuscar();
    if (!estados.inicial.classList.contains("d-none")) return;
    mostrarEstado("inicial");
  }

  function mostrarSpinner(mostrar) {
    if (mostrar) {
      if (panelResultadoOverlay) panelResultadoOverlay.style.display = "flex";
      if (btnBuscar) {
        btnBuscar.disabled = true;
        btnBuscar.innerHTML = BTON_BUSCAR_CARGANDO_HTML;
      }
      [selEmpresa, selTipoDoc, txtPrefijo, txtNoDocumento].forEach(function (c) {
        if (c) c.disabled = true;
      });
    } else {
      if (panelResultadoOverlay) panelResultadoOverlay.style.display = "none";
      if (btnBuscar) {
        btnBuscar.disabled = !cumpleCriteriosCompletos();
        btnBuscar.innerHTML = BTON_BUSCAR_NORMAL_HTML;
      }
      [selEmpresa, selTipoDoc, txtPrefijo, txtNoDocumento].forEach(function (c) {
        if (c) c.disabled = false;
      });
    }
  }

  // -- Cargas iniciales ---------------------------------------

  function poblarSelectEmpresas(empresas) {
    selEmpresa.innerHTML = "";

    var optPlaceholder = document.createElement("option");
    optPlaceholder.value = "";
    optPlaceholder.selected = true;
    optPlaceholder.disabled = true;
    optPlaceholder.textContent = "Seleccione una empresa...";
    selEmpresa.appendChild(optPlaceholder);

    (empresas || []).forEach(function (empresa) {
      var option = document.createElement("option");
      option.value = empresa.codigo || "";
      option.textContent = empresa.nombre || "";
      option.setAttribute("data-nombre", empresa.nombre || "");
      selEmpresa.appendChild(option);
    });
  }

  function poblarSelectTipoDocumentos(tipos) {
    selTipoDoc.innerHTML = "";

    var optPlaceholder = document.createElement("option");
    optPlaceholder.value = "";
    optPlaceholder.selected = true;
    optPlaceholder.disabled = true;
    optPlaceholder.textContent = "Seleccione un tipo...";
    selTipoDoc.appendChild(optPlaceholder);

    (tipos || []).forEach(function (tipo) {
      var option = document.createElement("option");
      option.value = tipo.codigo || "";
      // Formato requerido: "CodDocumento - NombreDocumento" (ej. "FA - FACTURA ESCULAPIO")
      option.textContent = (tipo.codigo || "") + " - " + (tipo.descripcion || "");
      selTipoDoc.appendChild(option);
    });
  }

  function cargarEmpresas() {
    selEmpresaHelp.classList.add("d-none");
    return fetch(urls.empresas, { headers: { Accept: "application/json" } })
      .then(function (r) { if (!r.ok) throw new Error("Status " + r.status); return r.json(); })
      .then(function (empresas) {
        poblarSelectEmpresas(empresas);
      })
      .catch(function (err) {
        console.error("[EMPRESAS] error:", err);
        selEmpresaHelp.classList.remove("d-none");
      });
  }

  function cargarTipoDocumentos(empresa) {
    selTipoDocHelp.classList.add("d-none");
    var url = urls.tipoDocumentos;
    if (empresa) url = url + "?codEmpresa=" + encodeURIComponent(empresa);

    return fetch(url, { headers: { Accept: "application/json" } })
      .then(function (r) { if (!r.ok) throw new Error("Status " + r.status); return r.json(); })
      .then(function (tipos) {
        poblarSelectTipoDocumentos(tipos);
        actualizarDisponibilidadBuscar();
      })
      .catch(function (err) {
        console.error("[TIPOS_DOC] error:", err);
        selTipoDocHelp.classList.remove("d-none");
      });
  }

  function alSeleccionarEmpresa() {
    var op = selEmpresa.options[selEmpresa.selectedIndex];
    txtNombreEmpresa.value = (op && op.value) ? (op.getAttribute("data-nombre") || "") : "";
    marcarInvalido(selEmpresa, false);

    selTipoDoc.value = "";
    txtTipoDocumento.value = "";
    cargarTipoDocumentos(selEmpresa.value || null);
    alCambiarFiltro();
  }

  function alSeleccionarTipoDoc() {
    txtTipoDocumento.value = selTipoDoc.value || "";
    marcarInvalido(selTipoDoc, false);
    alCambiarFiltro();
  }

  // -- Render del grid -----------------------------------------

  function formatearFechaHora(valorIso) {
    if (!valorIso) return "";
    var fecha = new Date(valorIso);
    return isNaN(fecha.getTime()) ? valorIso : fecha.toLocaleString("es-CO");
  }

  function truncarTexto(texto, longitudMaxima) {
    if (!texto) return "";
    return texto.length > longitudMaxima ? texto.slice(0, longitudMaxima) + "..." : texto;
  }

  function limpiarSeleccion() {
    filaSeleccionada = null;
    txtVisorXml.value = "";
    if (btnDescargar) btnDescargar.disabled = true;
  }

  function seleccionarFila(indice) {
    var filas = cuerpoTablaLog.querySelectorAll("tr");
    filas.forEach(function (f, i) { f.classList.toggle("table-active", i === indice); });
    filaSeleccionada = registrosActuales[indice];
    txtVisorXml.value = filaSeleccionada.respuestaXml || "";
    if (btnDescargar) btnDescargar.disabled = false;
  }

  function renderizarRegistros(registros) {
    registrosActuales = registros;
    cuerpoTablaLog.innerHTML = "";
    limpiarSeleccion();

    registros.forEach(function (reg, indice) {
      var fila = document.createElement("tr");
      fila.setAttribute("role", "button");
      fila.setAttribute("tabindex", "0");

      var tdFecha = document.createElement("td");
      tdFecha.textContent = formatearFechaHora(reg.fechaHoraLog);
      var tdMetodo = document.createElement("td");
      tdMetodo.textContent = reg.metodoWs || "";
      var tdXml = document.createElement("td");
      tdXml.className = "log-columna-xml";
      tdXml.title = reg.respuestaXml || "";
      tdXml.textContent = truncarTexto(reg.respuestaXml, 80);

      fila.appendChild(tdFecha);
      fila.appendChild(tdMetodo);
      fila.appendChild(tdXml);
      fila.addEventListener("click", function () { seleccionarFila(indice); });
      cuerpoTablaLog.appendChild(fila);
    });

    if (registros.length > 0) seleccionarFila(0);
  }

  // -- Buscar -----------------------------------------------

  function alEnviarFormulario(evento) {
    evento.preventDefault();

    if (!validarCamposObligatorios()) {
      estadoErrorMensaje.textContent =
        "Complete empresa, tipo de documento, prefijo y numero de documento antes de buscar.";
      mostrarEstado("error");
      return;
    }

    var parametros = {
      empresa: selEmpresa.value,
      tipoDoc: txtTipoDocumento.value.trim(),
      prefijo: txtPrefijo.value.trim(),
      noDocumento: txtNoDocumento.value.trim()
    };

    var tiempoInicio = Date.now();
    var MIN_DURACION_MS = 500;

    mostrarSpinner(true);
    mostrarEstado("cargando");
    void btnBuscar.offsetHeight;

    fetch(urls.buscar, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify(parametros)
    })
      .then(function (r) {
        return r.text().then(function (txt) {
          var d; try { d = JSON.parse(txt); } catch (e) { d = {}; }
          return { ok: r.ok, datos: d };
        });
      })
      .then(function (resultado) {
        var datos = resultado.datos || {};
        if (!resultado.ok || datos.error) {
          estadoErrorMensaje.textContent = datos.mensajeError || "Error en la consulta.";
          mostrarEstado("error");
          return;
        }
        if (!datos.registros || datos.registros.length === 0) {
          if (estadoSinResultadosDetalle) {
            estadoSinResultadosDetalle.textContent =
              "Empresa: " + parametros.empresa + " | " +
              "Tipo: " + parametros.tipoDoc + " | " +
              "Prefijo: " + parametros.prefijo + " | " +
              "No. documento: " + parametros.noDocumento;
          }
          mostrarEstado("sinResultados");
          return;
        }
        renderizarRegistros(datos.registros);
        ultimaBusquedaExitosa = parametros;
        mostrarEstado("conResultados");
      })
      .catch(function (err) {
        console.error("[BUSCAR] error:", err);
        estadoErrorMensaje.textContent = "Error: " + (err.message || err);
        mostrarEstado("error");
      })
      .finally(function () {
        var d = Date.now() - tiempoInicio;
        setTimeout(function () { mostrarSpinner(false); }, Math.max(0, MIN_DURACION_MS - d));
      });
  }

  // -- Descarga ----------------------------------------------

  function alDescargar() {
    if (!filaSeleccionada || !ultimaBusquedaExitosa) return;
    var blob = new Blob([filaSeleccionada.respuestaXml || ""], { type: "application/xml" });
    var urlBlob = URL.createObjectURL(blob);

    var nombreEmpresa = txtNombreEmpresa.value || "";
    var nombreArchivo =
      "RespuestaDian_" +
      (nombreEmpresa ? slug(nombreEmpresa) + "_" : "") +
      (ultimaBusquedaExitosa.tipoDoc || "") +
      "_" +
      (ultimaBusquedaExitosa.prefijo || "") +
      "-" +
      (ultimaBusquedaExitosa.noDocumento || "") +
      ".xml";
    var enlace = document.createElement("a");
    enlace.href = urlBlob;
    enlace.download = nombreArchivo;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    URL.revokeObjectURL(urlBlob);
  }

  // -- Eventos ------------------------------------------------

  selEmpresa.addEventListener("change", alSeleccionarEmpresa);
  selTipoDoc.addEventListener("change", alSeleccionarTipoDoc);
  txtPrefijo.addEventListener("input", alCambiarFiltro);
  txtNoDocumento.addEventListener("input", alCambiarFiltro);
  frmBuscarXml.addEventListener("submit", alEnviarFormulario);
  if (btnDescargar) btnDescargar.addEventListener("click", alDescargar);

  // -- Arranque ------------------------------------------------

  if (btnDescargar) btnDescargar.disabled = true;
  if (btnBuscar) btnBuscar.innerHTML = BTON_BUSCAR_NORMAL_HTML;
  cargarEmpresas();
  cargarTipoDocumentos(null);
  actualizarDisponibilidadBuscar();
  mostrarEstado("inicial");
})();
