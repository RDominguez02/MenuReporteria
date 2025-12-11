// Models/ReporteVentasChasis.cs
using System;
using System.Collections.Generic;

namespace MenuReporteria.Models
{
    /// <summary>
    /// Modelo para un registro de venta de chasis individual
    /// </summary>
    public class VentaChasisItem
    {
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; }
        public string ControlNro { get; set; }
        public string Almacen { get; set; }
        public string Vendedor { get; set; }
        public string NumeroChasis { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public string Color { get; set; }
        public string Ano { get; set; }
        public decimal Precio { get; set; }
        public string CodigoCliente { get; set; }
        public string NombreCliente { get; set; }
        public string Moneda { get; set; }
        public decimal Tasa { get; set; }
        public string NCF { get; set; }
        public string Factura { get; set; }
        public string Motor { get; set; }
        public string Placa { get; set; }
        public string Uso { get; set; }
        public string Origen { get; set; }
        public string RNC { get; set; }
    }

    /// <summary>
    /// Modelo para el filtro de búsqueda de chasis
    /// </summary>
    public class FiltroVentasChasis
    {
        public DateTime FechaDesde { get; set; } = DateTime.Now.AddMonths(-1);
        public DateTime FechaHasta { get; set; } = DateTime.Now;
        public string Cliente { get; set; }
        public string Vendedor { get; set; }
        public string Chasis { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public string Color { get; set; }
        public string Almacen { get; set; }
        public string NCFDesde { get; set; }
        public string NCFHasta { get; set; }
        public string Factura { get; set; }
        public string Turno { get; set; }
        public string Caja { get; set; }
        public string Departamento { get; set; }
        public string MarcaProducto { get; set; }
        public string TipoDocumento { get; set; } = "Todas"; // Contado(1), Crédito(2), Nota(3), Orden(4), Traslado(5), Todas
        public string TipoFactura { get; set; } = "Todas"; // Mayor, Detalle, Todas
        public bool IncluirAnuladas { get; set; } = false;

        // Listas para dropdowns
        public List<string> VendedoresDisponibles { get; set; }
        public List<string> AlmacenesDisponibles { get; set; }
        public List<string> CajasDisponibles { get; set; }
    }

    /// <summary>
    /// Modelo para el resultado del reporte de chasis
    /// </summary>
    public class ResultadoReporteVentasChasis
    {
        public List<VentaChasisItem> Ventas { get; set; } = new List<VentaChasisItem>();
        public int TotalRegistros { get; set; }
        public int TotalChasis { get; set; }
        public decimal ValorTotal { get; set; }
        public FiltroVentasChasis Filtros { get; set; }
    }
}