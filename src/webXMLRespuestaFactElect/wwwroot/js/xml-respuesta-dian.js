/*
 * Interaccion de la vista "XML Respuesta Dian" (SPEC-003 / F-1..F-8).
 * Sin dependencias externas (solo Bootstrap ya cargado en el layout).
 * Toda la comunicacion con el servidor es via fetch/AJAX (no hay postbacks completos).
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
  var txtPrefijo = document.getElementById("txtPrefijo");
  var txtNoDocumento = document.getElementById("txtNoDocumento");
  var frmBuscarXml = document.getElementById("frmBuscarXml");
  var btnBuscar = document.getElementById("btnBuscar");
  var btnBuscarSpinner = document.getElementById("btnBuscarSpinner");
  var btnDescargar = document.getElementById("btnDescargar");

  var selEmpresaHelp = document.getElementById("selEmpresaHelp");
  var selTipoDocHelp = document.getElementById("selTipoDocHelp");

  var estados = {
    inicial: document.getElementById("estadoInicial"),
    cargando: document.getElementById("estadoCargando"),
    sinResultados: document.getElementById("estadoSinResultados"),
    error: document.getElementById("estadoError"),
    conResultados: document.getElementById("estadoConResultados")
  };
  var estadoErrorMensaje = document.getElementById("estadoErrorMensaje");
  var cuerpoTablaLog = document.getElementById("cuerpoTablaLog");
  var txtVisorXml = document.getElementById("txtVisorXml");

  // Registros de la ultima busqueda (para el grid) y fila actualmente seleccionada
  // (para el visor y la descarga). Parametros de la ultima busqueda exitosa, para
  // construir el nombre del archivo descargado (AC-8).
  var registrosActuales = [];
  var filaSeleccionada = null;
  var ultimaBusquedaExitosa = null;

  function mostrarEstado(nombreEstado) {
    Object.keys(estados).forEach(function (clave) {
      estados[clave].classList.toggle("d-none", clave !== nombreEstado);
    });
    btnDescargar.classList.toggle("d-none", nombreEstado !== "conResultados");
  }

  function actualizarDisponibilidadBuscar() {
    var completo =
      !!selEmpresa.value && !!selTipoDoc.value && !!txtPrefijo.value.trim() && !!txtNoDocumento.value.trim();
    btnBuscar.disabled = !completo;
  }

  function alCambiarFiltro() {
    actualizarDisponibilidadBuscar();
    // Si ya habia un resultado visible y el usuario vuelve a tocar los filtros,
    // se vuelve al estado inicial hasta la proxima busqueda explicita (evita mostrar
    // un XML/mensaje que ya no corresponde a los criterios actuales).
    if (!estados.inicial.classList.contains("d-none")) {
      return;
    }
    mostrarEstado("inicial");
  }

  function poblarSelect(select, opciones, valorClave, textoClave, placeholder) {
    select.innerHTML = "";

    var optPlaceholder = document.createElement("option");
    optPlaceholder.value = "";
    optPlaceholder.selected = true;
    optPlaceholder.disabled = true;
    optPlaceholder.textContent = placeholder;
    select.appendChild(optPlaceholder);

    (opciones || []).forEach(function (item) {
      var option = document.createElement("option");
      option.value = item[valorClave];
      option.textContent = item[textoClave];
      select.appendChild(option);
    });
  }

  function cargarEmpresas() {
    selEmpresaHelp.classList.add("d-none");
    return fetch(urls.empresas, { headers: { Accept: "application/json" } })
      .then(function (respuesta) {
        if (!respuesta.ok) {
          throw new Error("No fue posible cargar las empresas.");
        }
        return respuesta.json();
      })
      .then(function (empresas) {
        poblarSelect(selEmpresa, empresas, "codigo", "nombre", "Seleccione una empresa…");
      })
      .catch(function () {
        selEmpresaHelp.classList.remove("d-none");
      });
  }

  function cargarTipoDocumentos() {
    selTipoDocHelp.classList.add("d-none");
    return fetch(urls.tipoDocumentos, { headers: { Accept: "application/json" } })
      .then(function (respuesta) {
        if (!respuesta.ok) {
          throw new Error("No fue posible cargar los tipos de documento.");
        }
        return respuesta.json();
      })
      .then(function (tipos) {
        poblarSelect(selTipoDoc, tipos, "codigo", "descripcion", "Seleccione un tipo…");
      })
      .catch(function () {
        selTipoDocHelp.classList.remove("d-none");
      });
  }

  function alSeleccionarEmpresa() {
    var opcionSeleccionada = selEmpresa.options[selEmpresa.selectedIndex];
    txtNombreEmpresa.value = opcionSeleccionada && opcionSeleccionada.value ? opcionSeleccionada.textContent : "";
    alCambiarFiltro();
  }

  // Campos de filtro que deben congelarse mientras hay una busqueda en vuelo, para
  // evitar que el usuario los toque durante el fetch y dispare alCambiarFiltro()
  // (lo que ocultaria el estado "cargando" con un "inicial" prematuro, produciendo
  // parpadeo/estados inconsistentes). Ver FIX 1 / M-1 (DAREDEVIL).
  var camposFiltro = [selEmpresa, selTipoDoc, txtPrefijo, txtNoDocumento];

  function establecerCargando(activo) {
    btnBuscar.disabled = activo || !cumpleCriteriosCompletos();
    btnBuscarSpinner.classList.toggle("d-none", !activo);
    camposFiltro.forEach(function (campo) {
      campo.disabled = activo;
    });
  }

  function cumpleCriteriosCompletos() {
    return !!selEmpresa.value && !!selTipoDoc.value && !!txtPrefijo.value.trim() && !!txtNoDocumento.value.trim();
  }

  function formatearFechaHora(valorIso) {
    if (!valorIso) {
      return "";
    }
    var fecha = new Date(valorIso);
    return isNaN(fecha.getTime()) ? valorIso : fecha.toLocaleString("es-CO");
  }

  function truncarTexto(texto, longitudMaxima) {
    if (!texto) {
      return "";
    }
    return texto.length > longitudMaxima ? texto.slice(0, longitudMaxima) + "…" : texto;
  }

  function limpiarSeleccion() {
    filaSeleccionada = null;
    txtVisorXml.value = "";
  }

  function seleccionarFila(indice) {
    var filas = cuerpoTablaLog.querySelectorAll("tr");
    filas.forEach(function (fila, i) {
      fila.classList.toggle("table-active", i === indice);
    });

    filaSeleccionada = registrosActuales[indice];
    txtVisorXml.value = filaSeleccionada.respuestaXml || "";
  }

  function renderizarRegistros(registros) {
    registrosActuales = registros;
    cuerpoTablaLog.innerHTML = "";
    limpiarSeleccion();

    registros.forEach(function (registro, indice) {
      var fila = document.createElement("tr");
      fila.setAttribute("role", "button");
      fila.setAttribute("tabindex", "0");

      var tdFecha = document.createElement("td");
      tdFecha.textContent = formatearFechaHora(registro.fechaHoraLog);

      var tdMetodo = document.createElement("td");
      tdMetodo.textContent = registro.metodoWs;

      var tdXml = document.createElement("td");
      tdXml.className = "log-columna-xml";
      tdXml.title = registro.respuestaXml || "";
      tdXml.textContent = truncarTexto(registro.respuestaXml, 80);

      fila.appendChild(tdFecha);
      fila.appendChild(tdMetodo);
      fila.appendChild(tdXml);

      fila.addEventListener("click", function () {
        seleccionarFila(indice);
      });
      fila.addEventListener("keydown", function (evento) {
        if (evento.key === "Enter" || evento.key === " ") {
          evento.preventDefault();
          seleccionarFila(indice);
        }
      });

      cuerpoTablaLog.appendChild(fila);
    });

    if (registros.length > 0) {
      seleccionarFila(0);
    }
  }

  function alEnviarFormulario(evento) {
    evento.preventDefault();

    var parametros = {
      empresa: selEmpresa.value,
      tipoDoc: selTipoDoc.value,
      prefijo: txtPrefijo.value.trim(),
      noDocumento: txtNoDocumento.value.trim()
    };

    mostrarEstado("cargando");
    establecerCargando(true);

    fetch(urls.buscar, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify(parametros)
    })
      .then(function (respuesta) {
        return respuesta.json().then(function (datos) {
          return { ok: respuesta.ok, datos: datos };
        });
      })
      .then(function (resultado) {
        var datos = resultado.datos;

        if (!resultado.ok || datos.error) {
          estadoErrorMensaje.textContent =
            (datos && datos.mensajeError) ||
            "No fue posible completar la consulta. Intente nuevamente en unos minutos.";
          mostrarEstado("error");
          return;
        }

        if (!datos.registros || datos.registros.length === 0) {
          mostrarEstado("sinResultados");
          return;
        }

        renderizarRegistros(datos.registros);
        ultimaBusquedaExitosa = parametros;
        mostrarEstado("conResultados");
      })
      .catch(function () {
        estadoErrorMensaje.textContent =
          "No fue posible completar la consulta. Intente nuevamente en unos minutos.";
        mostrarEstado("error");
      })
      .finally(function () {
        establecerCargando(false);
      });
  }

  function alDescargar() {
    if (!filaSeleccionada || !ultimaBusquedaExitosa) {
      return;
    }

    // Descarga 100% client-side: el contenido ya fue entregado por el servidor y es
    // exactamente el que se ve en el visor (AC-8), sin necesidad de otro round-trip.
    var blob = new Blob([filaSeleccionada.respuestaXml || ""], { type: "application/xml" });
    var url = URL.createObjectURL(blob);
    var enlace = document.createElement("a");
    enlace.href = url;
    enlace.download =
      "RespuestaDian_" + ultimaBusquedaExitosa.prefijo + "-" + ultimaBusquedaExitosa.noDocumento + ".xml";
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    URL.revokeObjectURL(url);
  }

  selEmpresa.addEventListener("change", alSeleccionarEmpresa);
  selTipoDoc.addEventListener("change", alCambiarFiltro);
  txtPrefijo.addEventListener("input", alCambiarFiltro);
  txtNoDocumento.addEventListener("input", alCambiarFiltro);
  frmBuscarXml.addEventListener("submit", alEnviarFormulario);
  btnDescargar.addEventListener("click", alDescargar);

  cargarEmpresas();
  cargarTipoDocumentos();
  actualizarDisponibilidadBuscar();
  mostrarEstado("inicial");
})();
