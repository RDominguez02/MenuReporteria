// Variables globales para el modal
let todosLosProductos = [];
let paginaModalActual = 1;
let registrosPorPaginaModal = 5;

// Funciones para el modal de factura
function abrirModalFactura(facturaNumero) {
    const modal = document.getElementById('modalFactura');
    document.body.style.overflow = 'hidden';

    // Mostrar spinner mientras cargamos el partial
    modal.innerHTML = `
        <div class="modal-factura-content" style="display:flex;align-items:center;justify-content:center;height:200px;">
            <div class="spinner"></div>
        </div>
    `;
    modal.classList.add('active');

    // Cargar el partial Razor desde el controlador
    fetch(`/Ventas/CargarModalDetalleFactura?factura=${encodeURIComponent(facturaNumero)}`)
        .then(response => {
            if (!response.ok) throw new Error('Respuesta no OK');
            return response.text();
        })
        .then(html => {
            modal.innerHTML = html;
            // Cargar datos reales después de insertar el HTML
            cargarDetalleFactura(facturaNumero);
        })
        .catch(err => {
            console.error('Error cargando modal:', err);
            modal.innerHTML = `
                <div class="modal-factura-content" style="padding:40px;text-align:center;">
                    <h3 style="color:#e74c3c;">⚠️ Error al cargar la vista</h3>
                    <p>No se pudo cargar el detalle de la factura.</p>
                    <div style="margin-top:20px;">
                        <button class="btn-modal btn-modal-secondary" onclick="cerrarModalFactura()">Cerrar</button>
                    </div>
                </div>
            `;
        });
}

function cerrarModalFactura() {
    const modal = document.getElementById('modalFactura');
    modal.classList.remove('active');
    document.body.style.overflow = 'auto';

    // Limpiar variables
    todosLosProductos = [];
    paginaModalActual = 1;
}

function cargarDetalleFactura(facturaNumero) {
    // Llamada AJAX al backend
    fetch(`/Ventas/ObtenerDetalleFactura?factura=${facturaNumero}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                poblarModalConDatos(data.data);
            } else {
                mostrarErrorModal(data.message || 'Error al cargar la factura');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            mostrarErrorModal('Error al conectar con el servidor');
        });
}

function poblarModalConDatos(datos) {
    // Helper para establecer texto seguro
    const setTextoSeguro = (id, valor, valorPorDefecto = '-') => {
        const elemento = document.getElementById(id);
        if (elemento) {
            elemento.textContent = valor || valorPorDefecto;
        }
    };

    // Información General
    setTextoSeguro('detalle-factura', datos.factura);
    if (datos.fecha) {
        setTextoSeguro('detalle-fecha', formatearFecha(datos.fecha));
    }
    setTextoSeguro('detalle-ncf', datos.ncf);
    setTextoSeguro('detalle-usuario', datos.usuario);
    setTextoSeguro('detalle-turno', datos.turno);
    setTextoSeguro('detalle-control', datos.ultimoControl);
    setTextoSeguro('detalle-caja', datos.caja);
    setTextoSeguro('detalle-vendedor', datos.vendedor);
    setTextoSeguro('detalle-moneda', datos.moneda);
    setTextoSeguro('detalle-tasa', datos.tasa);

    // Información del Cliente
    setTextoSeguro('detalle-cliente-codigo', datos.clienteCodigo);
    setTextoSeguro('detalle-cliente-nombre', datos.clienteNombre);
    setTextoSeguro('detalle-cliente-rnc', datos.clienteRnc);
    setTextoSeguro('detalle-cliente-direccion', datos.clienteDireccion);
    setTextoSeguro('detalle-cliente-telefono', datos.clienteTelefono);

    // Datos del primer vehículo (si existe)
    if (datos.productos && datos.productos.length > 0) {
        const primerProducto = datos.productos[0];
        cargarDatosVehiculo(primerProducto);
    }

    // Guardar TODOS los productos y mostrar la primera página
    todosLosProductos = datos.productos || [];
    paginaModalActual = 1;
    mostrarPaginaModal(1);

    // Totales con valores seguros
    const setMonedaSegura = (id, valor) => {
        const elemento = document.getElementById(id);
        if (elemento) {
            elemento.textContent = formatearMoneda(valor);
        }
    };

    setMonedaSegura('detalle-monto-bruto', datos.montoBruto);
    setMonedaSegura('detalle-impuesto17', datos.impuesto17);
    setMonedaSegura('detalle-itbis18', datos.itbis18);
    setMonedaSegura('detalle-descuento', datos.descuento);
    setMonedaSegura('detalle-subtotal', datos.subtotal);
    setMonedaSegura('detalle-total-itbis', datos.totalItbis);
    setMonedaSegura('detalle-monto-neto', datos.montoNeto);
}

// Función para cargar los datos del vehículo en la sección superior
function cargarDatosVehiculo(producto) {
    const setTextoSeguro = (id, valor, valorPorDefecto = '-') => {
        const elemento = document.getElementById(id);
        if (elemento) {
            elemento.textContent = valor || valorPorDefecto;
            // Agregar efecto visual de actualización
            elemento.classList.add('campo-actualizado');
            setTimeout(() => {
                elemento.classList.remove('campo-actualizado');
            }, 1000);
        }
    };

    // Cargar TODA la información del producto/vehículo
    setTextoSeguro('detalle-codigo-ficha', producto.codigoFicha);
    setTextoSeguro('detalle-descripcion-vehiculo', producto.descripcion);
    setTextoSeguro('detalle-chasis', producto.chasis);
    setTextoSeguro('detalle-ano', producto.ano);
    setTextoSeguro('detalle-motor', producto.motor);
    setTextoSeguro('detalle-modelo', producto.modelo);
    setTextoSeguro('detalle-color', producto.color);
    setTextoSeguro('detalle-placa', producto.placa);
    setTextoSeguro('detalle-matricula', producto.matricula);
}

// Función para mostrar página de productos (CON PAGINACIÓN REAL)
function mostrarPaginaModal(pagina) {
    paginaModalActual = pagina;

    const tbody = document.getElementById('productos-tbody');
    if (!tbody) return;

    if (!todosLosProductos || todosLosProductos.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="9" style="text-align: center; padding: 40px; color: #95a5a6;">
                    No hay productos registrados
                </td>
            </tr>
        `;
        actualizarPaginacionModal();
        return;
    }

    // CALCULAR QUÉ PRODUCTOS MOSTRAR EN ESTA PÁGINA
    const inicio = (pagina - 1) * registrosPorPaginaModal;
    const fin = inicio + registrosPorPaginaModal;
    const productosPagina = todosLosProductos.slice(inicio, fin);

    let html = '';
    productosPagina.forEach((producto, indexLocal) => {
        const indiceGlobal = inicio + indexLocal;
        const tieneVehiculo = producto.chasis && producto.chasis.trim() !== '';

        html += `
            <tr>
                <td>${indiceGlobal + 1}</td>
                <td>${producto.codigoFicha || '-'}</td>
                <td>${producto.marca || '-'}</td>
                <td>${producto.modelo || '-'}</td>
                <td>${producto.color || '-'}</td>
                <td>${producto.ano || '-'}</td>
                <td>${producto.motor || '-'}</td>
                <td>${producto.placa || '-'}</td>
                <td>${producto.matricula || '-'}</td>
                <td class="text-right">${formatearMoneda(producto.precioUnitario)}</td>
                <td class="text-right">${formatearMoneda(producto.itbis)}</td>
                <td class="text-right">${formatearMoneda(producto.total)}</td>

            </tr>
        `;
    });

    tbody.innerHTML = html;
    actualizarPaginacionModal();
}

//<td style="text-align: center;">
//    ${tieneVehiculo ? `
//                        <button class="btn-seleccionar-chasis" onclick="seleccionarVehiculo(${indiceGlobal})" title="Ver datos completos del vehículo">
//                            🚗 Ver Datos
//                        </button>
//                    ` : '<span style="color: #95a5a6;">-</span>'}
//</td>

// Función para seleccionar y mostrar TODOS los datos del vehículo
function seleccionarVehiculo(indice) {
    if (!todosLosProductos || !todosLosProductos[indice]) {
        alert('Error: No se pudo cargar los datos del vehículo');
        return;
    }

    const producto = todosLosProductos[indice];
    cargarDatosVehiculo(producto);

    // Feedback visual
    mostrarNotificacionModal('✓ Datos del vehículo cargados correctamente');

    // Scroll suave hacia la sección de datos del vehículo
    const seccionVehiculo = document.querySelector('.factura-section:nth-child(3)');
    if (seccionVehiculo) {
        seccionVehiculo.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

// Actualizar información de paginación del modal
function actualizarPaginacionModal() {
    const total = todosLosProductos.length;
    const totalPaginas = Math.ceil(total / registrosPorPaginaModal);

    // Actualizar info de registros
    const rangoInicio = total > 0 ? ((paginaModalActual - 1) * registrosPorPaginaModal) + 1 : 0;
    const rangoFin = Math.min(paginaModalActual * registrosPorPaginaModal, total);

    const infoElement = document.getElementById('modal-pagination-info');
    if (infoElement) {
        infoElement.innerHTML = `
            Mostrando <strong>${rangoInicio}</strong> a <strong>${rangoFin}</strong> de <strong>${total}</strong> productos
        `;
    }

    // Generar botones de paginación
    generarBotonesPaginacionModal(totalPaginas);
}

// Generar botones de paginación del modal
function generarBotonesPaginacionModal(totalPaginas) {
    const contenedor = document.getElementById('modal-pagination-controls');
    if (!contenedor) return;

    let html = '';

    // Botón anterior
    html += `<button class="pagination-btn-modal" onclick="mostrarPaginaModal(${paginaModalActual - 1})" ${paginaModalActual === 1 ? 'disabled' : ''}>
        ← Anterior
    </button>`;

    // Números de página (mostrar máximo 5 botones)
    const maxBotones = 5;
    let inicioPagina = Math.max(1, paginaModalActual - 2);
    let finPagina = Math.min(totalPaginas, inicioPagina + maxBotones - 1);

    if (finPagina - inicioPagina < maxBotones - 1) {
        inicioPagina = Math.max(1, finPagina - maxBotones + 1);
    }

    for (let i = inicioPagina; i <= finPagina; i++) {
        html += `<button class="pagination-btn-modal ${i === paginaModalActual ? 'active' : ''}" onclick="mostrarPaginaModal(${i})">
            ${i}
        </button>`;
    }

    // Botón siguiente
    html += `<button class="pagination-btn-modal" onclick="mostrarPaginaModal(${paginaModalActual + 1})" ${paginaModalActual === totalPaginas || totalPaginas === 0 ? 'disabled' : ''}>
        Siguiente →
    </button>`;

    contenedor.innerHTML = html;
}

// Cambiar registros por página en el modal
function cambiarRegistrosPorPaginaModal() {
    const select = document.getElementById('modal-records-per-page');
    if (select) {
        registrosPorPaginaModal = parseInt(select.value);
        paginaModalActual = 1;
        mostrarPaginaModal(1);
    }
}

// Mostrar notificación en el modal
function mostrarNotificacionModal(mensaje) {
    const notif = document.createElement('div');
    notif.className = 'modal-notification';
    notif.textContent = mensaje;
    document.body.appendChild(notif);

    setTimeout(() => {
        notif.classList.add('show');
    }, 10);

    setTimeout(() => {
        notif.classList.remove('show');
        setTimeout(() => {
            document.body.removeChild(notif);
        }, 300);
    }, 2000);
}

function mostrarErrorModal(mensaje) {
    const tbody = document.getElementById('productos-tbody');
    if (tbody) {
        tbody.innerHTML = `
            <tr>
                <td colspan="9" style="text-align: center; padding: 40px; color: #e74c3c;">
                    <div style="font-size: 48px; margin-bottom: 15px;">⚠️</div>
                    <strong>${mensaje}</strong>
                </td>
            </tr>
        `;
    }
}

function formatearFecha(fecha) {
    try {
        const date = new Date(fecha);
        return date.toLocaleDateString('es-DO', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    } catch (e) {
        return '-';
    }
}

function formatearMoneda(valor) {
    try {
        const num = parseFloat(valor || 0);
        return num.toLocaleString('es-DO', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    } catch (e) {
        return '0.00';
    }
}

function imprimirFactura() {
    window.print();
}

// Cerrar modal al hacer clic fuera
window.addEventListener('click', function (event) {
    const modal = document.getElementById('modalFactura');
    if (event.target === modal) {
        cerrarModalFactura();
    }
});

// Cerrar modal con tecla ESC
document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        const modal = document.getElementById('modalFactura');
        if (modal && modal.classList.contains('active')) {
            cerrarModalFactura();
        }
    }
});