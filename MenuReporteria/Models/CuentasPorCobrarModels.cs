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
        public string? Zona { get; set; }
        public string? Cliente { get; set; }
        public string? Vendedor { get; set; }
        public string? FacturaDesde { get; set; }
        public string? FacturaHasta { get; set; }
        public int? CuotaDesde { get; set; }
        public int? CuotaHasta { get; set; }
        public string? Moneda { get; set; }
        public string? OrdenMoneda { get; set; }
        public string? MonedaTipo { get; set; }

        public List<FiltroOpcion> ZonasDisponibles { get; set; } = new List<FiltroOpcion>();
        public List<FiltroOpcion> ClientesDisponibles { get; set; } = new List<FiltroOpcion>();
        public List<FiltroOpcion> VendedoresDisponibles { get; set; } = new List<FiltroOpcion>();
        public List<FiltroOpcion> MonedasDisponibles { get; set; } = new List<FiltroOpcion>();
    }

    /// <summary>
    /// Item de cuenta por cobrar
    /// </summary>
    public class CxCItem
    {
        public string Cuota { get; set; } = string.Empty;
        public string CodigoCliente { get; set; } = string.Empty;
        public string Contrato { get; set; } = string.Empty;
        public DateTime FechaFactura { get; set; }
        public DateTime? FechaPago { get; set; }
        public decimal Capital { get; set; }
        public decimal Interes { get; set; }
        public decimal Comision { get; set; }
        public decimal Mora { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Zona { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public string Vendedor { get; set; } = string.Empty;
        public int DiasVencimiento { get; set; }
        public decimal TotalR { get; set; }
        public DateTime Fecha { get; set; }
        public string Factura { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detalle de cuenta por cobrar
    /// </summary>
    public class DetalleCxC
    {
        public string Contrato { get; set; } = string.Empty;
        public DateTime FechaContrato { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public string Vendedor { get; set; } = string.Empty;
        public string TipoContrato { get; set; } = string.Empty;
        public int CantidadCuotas { get; set; }
        public decimal ValorContrato { get; set; }
        public decimal InteresTotal { get; set; }

        public string ClienteCodigo { get; set; } = string.Empty;
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteRnc { get; set; } = string.Empty;
        public string ClienteDireccion { get; set; } = string.Empty;
        public string ClienteTelefono { get; set; } = string.Empty;
        public string Zona { get; set; } = string.Empty;

        public List<CuotaCxC> Cuotas { get; set; } = new List<CuotaCxC>();
    }

    /// <summary>
    /// Cuota de cuenta por cobrar
    /// </summary>
    public class CuotaCxC
    {
        public string Cuota { get; set; } = string.Empty;
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

    /// <summary>
    /// Representa una opción disponible para filtros desplegables
    /// </summary>
    public class FiltroOpcion
    {
        public string Valor { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
    }


}