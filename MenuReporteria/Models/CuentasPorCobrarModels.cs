using System;
using System.Collections.Generic;

namespace MenuReporteria.Models
{
    /// <summary>
    /// Filtro para búsqueda de cuentas por cobrar
    /// </summary>
    public class FiltroCxC
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string Zona { get; set; }
        public string Cliente { get; set; }
        public string Vendedor { get; set; }
        public string FacturaDesde { get; set; }
        public string FacturaHasta { get; set; }
        public int? CuotaDesde { get; set; }
        public int? CuotaHasta { get; set; }
        public string Moneda { get; set; }
        public string OrdenMoneda { get; set; }
        public string MonedaTipo { get; set; }

        public List<string> ZonasDisponibles { get; set; } = new List<string>();
        public List<string> ClientesDisponibles { get; set; } = new List<string>();
        public List<string> VendedoresDisponibles { get; set; } = new List<string>();
        public List<string> MonedasDisponibles { get; set; } = new List<string>();
    }

    /// <summary>
    /// Item de cuenta por cobrar
    /// </summary>
    public class CxCItem
    {
        public string Cuota { get; set; }
        public string CodigoCliente { get; set; }
        public string Contrato { get; set; }
        public DateTime FechaFactura { get; set; }
        public DateTime? FechaPago { get; set; }
        public decimal Capital { get; set; }
        public decimal Interes { get; set; }
        public decimal Comision { get; set; }
        public decimal Mora { get; set; }
        public string NombreCliente { get; set; }
        public string Direccion { get; set; }
        public string Zona { get; set; }
        public string Moneda { get; set; }
        public string Vendedor { get; set; }
        public int DiasVencimiento { get; set; }
        public decimal TotalR { get; set; }
        public DateTime Fecha { get; set; }
        public string Factura { get; set; }
    }

    /// <summary>
    /// Detalle de cuenta por cobrar
    /// </summary>
    public class DetalleCxC
    {
        public string Contrato { get; set; }
        public DateTime FechaContrato { get; set; }
        public string Moneda { get; set; }
        public string Vendedor { get; set; }
        public string TipoContrato { get; set; }
        public int CantidadCuotas { get; set; }
        public decimal ValorContrato { get; set; }
        public decimal InteresTotal { get; set; }

        public string ClienteCodigo { get; set; }
        public string ClienteNombre { get; set; }
        public string ClienteRnc { get; set; }
        public string ClienteDireccion { get; set; }
        public string ClienteTelefono { get; set; }
        public string Zona { get; set; }

        public List<CuotaCxC> Cuotas { get; set; } = new List<CuotaCxC>();
    }

    /// <summary>
    /// Cuota de cuenta por cobrar
    /// </summary>
    public class CuotaCxC
    {
        public string Cuota { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }
        public decimal Capital { get; set; }
        public decimal Interes { get; set; }
        public decimal Comision { get; set; }
        public decimal Mora { get; set; }
        public int DiasVencimiento { get; set; }

        public decimal Total => Capital + Interes + Comision + Mora;
        public string Estado => FechaPago.HasValue ? "Pagado" : (DiasVencimiento > 0 ? "Vencido" : "Por Vencer");
    }

    /// <summary>
    /// Resultado del reporte de CxC
    /// </summary>
    public class ResultadoCxC
    {
        public List<CxCItem> Items { get; set; } = new List<CxCItem>();
        public FiltroCxC Filtros { get; set; } = new FiltroCxC();
        public int TotalRegistros { get; set; }
        public int TotalFacturas { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal MontoTotal { get; set; }
    }
}