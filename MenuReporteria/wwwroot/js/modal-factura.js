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
}

function limpiarModalFactura() {
    // Limpiar todos los campos
    const campos = [
        'detalle-factura', 'detalle-fecha', 'detalle-ncf', 'detalle-usuario',
        'detalle-turno', 'detalle-control', 'detalle-caja', 'detalle-vendedor',
        'detalle-cliente-codigo', 'detalle-cliente-nombre', 'detalle-cliente-rnc',
        'detalle-cliente-direccion', 'detalle-cliente-telefono',
        'detalle-chasis', 'detalle-ano', 'detalle-motor', 'detalle-modelo',
        'detalle-color', 'detalle-placa', 'detalle-matricula'
    ];

    campos.forEach(campo => {
        const elemento = document.getElementById(campo);
        if (elemento) elemento.textContent = '-';
    });

    // Limpiar totales
    const totales = [
        'detalle-monto-bruto', 'detalle-impuesto17', 'detalle-itbis18',
        'detalle-descuento', 'detalle-subtotal', 'detalle-total-itbis', 'detalle-monto-neto'
    ];

    totales.forEach(total => {
        const elemento = document.getElementById(total);
        if (elemento) elemento.textContent = 'RD$ 0.00';
    });

    // Limpiar tabla de productos
    const tbody = document.getElementById('productos-tbody');
    if (tbody) {
        tbody.innerHTML = `
            <tr>
                <td colspan="8" style="text-align: center; padding: 40px; color: #95a5a6;">
                    <div class="spinner" style="margin: 0 auto 15px;"></div>
                    Cargando productos...
                </td>
            </tr>
        `;
    }
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

    // Información del Cliente
    setTextoSeguro('detalle-cliente-codigo', datos.clienteCodigo);
    setTextoSeguro('detalle-cliente-nombre', datos.clienteNombre);
    setTextoSeguro('detalle-cliente-rnc', datos.clienteRnc);
    setTextoSeguro('detalle-cliente-direccion', datos.clienteDireccion);
    setTextoSeguro('detalle-cliente-telefono', datos.clienteTelefono);

    // Datos del Chasis
    setTextoSeguro('detalle-chasis', datos.chasis);
    setTextoSeguro('detalle-ano', datos.ano);
    setTextoSeguro('detalle-motor', datos.motor);
    setTextoSeguro('detalle-modelo', datos.modelo);
    setTextoSeguro('detalle-color', datos.color);
    setTextoSeguro('detalle-placa', datos.placa);
    setTextoSeguro('detalle-matricula', datos.matricula);

    // Productos
    const tbody = document.getElementById('productos-tbody');
    if (tbody) {
        if (datos.productos && datos.productos.length > 0) {
            let html = '';

            datos.productos.forEach((producto, index) => {
                html += `
                    <tr>
                        <td>${index + 1}</td>
                        <td>${producto.codigoFicha || '-'}</td>
                        <td>${producto.cantidad || '0'}</td>
                        <td>${producto.unidadMedida || 'UD'}</td>
                        <td>${producto.descripcion || '-'}</td>
                        <td class="text-right">${formatearMoneda(producto.precioUnitario)}</td>
                        <td class="text-right">${formatearMoneda(producto.itbis)}</td>
                        <td class="text-right">${formatearMoneda(producto.total)}</td>
                    </tr>
                `;
            });

            tbody.innerHTML = html;
        } else {
            tbody.innerHTML = `
                <tr>
                    <td colspan="8" style="text-align: center; padding: 40px; color: #95a5a6;">
                        No hay productos registrados
                    </td>
                </tr>
            `;
        }
    }

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

function mostrarErrorModal(mensaje) {
    const tbody = document.getElementById('productos-tbody');
    if (tbody) {
        tbody.innerHTML = `
            <tr>
                <td colspan="8" style="text-align: center; padding: 40px; color: #e74c3c;">
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
        return 'RD$ ' + num.toLocaleString('es-DO', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    } catch (e) {
        return 'RD$ 0.00';
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