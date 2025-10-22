// Models/ReporteVentas.cs
using System;
using System.Collections.Generic;

namespace MenuReporteria.Models
{
    /// <summary>
    /// Modelo para un registro de venta individual
    /// </summary>
    public class VentaItem
    {
        public DateTime Fecha { get; set; }
        public string Factura { get; set; }
        public string Ncf { get; set; }
        public string Cliente { get; set; }
        public string Vendedor { get; set; }
        public string Caja { get; set; }
        public int Turno { get; set; }
        public decimal MontoBruto { get; set; }
        public decimal Itbis { get; set; }
        public decimal CIF { get; set; }
        public decimal MontoNeto { get; set; }
        public string Moneda { get; set; }
        public decimal Tasa { get; set; }
        public decimal TotalR { get; set; }
        public decimal TotalChasis { get; set; }
    }

    /// <summary>
    /// Modelo para el filtro de búsqueda
    /// </summary>
public class FiltroVentas
{
    public DateTime FechaDesde { get; set; } = DateTime.Now.AddMonths(-1);
    public DateTime FechaHasta { get; set; } = DateTime.Now;
    public string Cliente { get; set; }
    public string Vendedor { get; set; }
    public decimal? ValorDesde { get; set; }
    public decimal? ValorHasta { get; set; }
    public string Ncf { get; set; }
    public string FacturaDesde { get; set; }
    public string Caja { get; set; }
    
    // NUEVOS FILTROS
    public string EntradaLibre { get; set; }
    public string Moneda { get; set; }
    public string Sucursal { get; set; }
    public string TipoFactura { get; set; } = "Todas"; // Contado, Crédito, Todas
    public string OrdenadoPor { get; set; } = "FECHA_FACTURA"; // FECHA_FACTURA, FACTURA
    public string ModoFacturas { get; set; } = "Todas"; // Mayor, Detalle, Todas
    public string Opciones { get; set; } = "Todas"; // Normales, SoloNCF, Editadas, Repuestos, Placa, Todas, Archivo
    
    // LISTAS PARA DROPDOWNS
    public List<string> CajasDisponibles { get; set; }
    public List<string> VendedoresDisponibles { get; set; }
    public List<string> MonedasDisponibles { get; set; }
    public List<string> SucursalesDisponibles { get; set; }
    public List<string> EntradasLibresDisponibles { get; set; }
}

    /// <summary>
    /// Modelo para el resultado del reporte
    /// </summary>
    public class ResultadoReporteVentas
    {
        public List<VentaItem> Ventas { get; set; } = new List<VentaItem>();
        public int TotalFacturas { get; set; }
        public int TotalChasis { get; set; }
        public decimal ValorTotal { get; set; }
        public FiltroVentas Filtros { get; set; }
    }

    /// <summary>
    /// Modelo para vendedor
    /// </summary>
    public class Vendedor
    {
        public string VeCodigo { get; set; }
        public string VeNombre { get; set; }
    }

    /// <summary>
    /// Modelo para caja
    /// </summary>
    public class Caja
    {
        public string CajaCodigo { get; set; }
    }
}