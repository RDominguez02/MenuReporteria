// Models/DetalleFacturaViewModel.cs
using System;
using System.Collections.Generic;

namespace MenuReporteria.Models
{
    /// <summary>
    /// Modelo para el detalle completo de una factura
    /// </summary>
    public class DetalleFacturaViewModel
    {
        // Información General
        public string Factura { get; set; }
        public DateTime Fecha { get; set; }
        public string Ncf { get; set; }
        public string Usuario { get; set; }
        public int Turno { get; set; }
        public string UltimoControl { get; set; }
        public string Caja { get; set; }
        public string Vendedor { get; set; }

        // Información del Cliente
        public string ClienteCodigo { get; set; }
        public string ClienteNombre { get; set; }
        public string ClienteRnc { get; set; }
        public string ClienteDireccion { get; set; }
        public string ClienteTelefono { get; set; }

        // Datos del Chasis
        public string Chasis { get; set; }
        public string Ano { get; set; }
        public string Motor { get; set; }
        public string Modelo { get; set; }
        public string Color { get; set; }
        public string Placa { get; set; }
        public string Matricula { get; set; }

        // Productos
        public List<ProductoFacturaItem> Productos { get; set; } = new List<ProductoFacturaItem>();

        // Totales
        public decimal MontoBruto { get; set; }
        public decimal Impuesto17 { get; set; }
        public decimal Itbis18 { get; set; }
        public decimal Descuento { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal MontoNeto { get; set; }
    }

    /// <summary>
    /// Modelo para un producto dentro de la factura
    /// </summary>
    public class ProductoFacturaItem
    {
        public string CodigoFicha { get; set; }
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Itbis { get; set; }
        public decimal Total { get; set; }
    }
}