using Microsoft.AspNetCore.Mvc;
using MenuReporteria.Services;
using MenuReporteria.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MenuReporteria.Controllers
{
    public class CuentasPorCobrarController : Controller
    {
        private readonly CuentasPorCobrarService _cuentasService;

        public CuentasPorCobrarController(CuentasPorCobrarService cuentasService)
        {
            _cuentasService = cuentasService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var modelo = new ResultadoCxC
            {
                Filtros = new FiltroCxC
                {
                    FechaDesde = DateTime.Now.AddMonths(-1),
                    FechaHasta = DateTime.Now,
                    ZonasDisponibles = _cuentasService.ObtenerZonas(),
                    ClientesDisponibles = _cuentasService.ObtenerClientes(),
                    VendedoresDisponibles = _cuentasService.ObtenerVendedores(),
                    MonedasDisponibles = _cuentasService.ObtenerMonedas()
                }
            };

            return View("ReporteCxC", modelo);
        }

        [HttpPost]
        public IActionResult Generar(
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            string zona,
            string cliente,
            string vendedor,
            string factura,
            string facturaHasta,
            int? cuotaDesde,
            int? cuotaHasta,
            string moneda,
            string ordenMoneda)
        {
            try
            {
                var filtros = new FiltroCxC
                {
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,
                    Zona = zona,
                    Cliente = cliente,
                    Vendedor = vendedor,
                    FacturaDesde = factura,
                    FacturaHasta = facturaHasta,
                    CuotaDesde = cuotaDesde,
                    CuotaHasta = cuotaHasta,
                    Moneda = moneda,
                    OrdenMoneda = ordenMoneda
                };

                var cuentas = _cuentasService.ObtenerCuentasPorFiltro(filtros);

                var totalMonto = 0m;
                foreach (var cuenta in cuentas)
                {
                    totalMonto += cuenta.TotalR;
                }

                return Json(new
                {
                    success = true,
                    totalFacturas = cuentas.Count,
                    valorTotal = totalMonto,
                    data = new
                    {
                        cuentas
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al generar el reporte: " + ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult ObtenerClientes(string filtro = "", int pagina = 1, int registrosPorPagina = 20)
        {
            try
            {
                // Validar que el filtro tenga al menos 2 caracteres
                if (string.IsNullOrWhiteSpace(filtro) || filtro.Length < 2)
                {
                    return Json(new
                    {
                        clientes = new List<object>(),
                        total = 0,
                        pagina = pagina,
                        totalPaginas = 0,
                        mensaje = "Ingresa al menos 2 caracteres para buscar"
                    });
                }

                var resultado = _cuentasService.ObtenerClientesPaginados(filtro, pagina, registrosPorPagina);

                return Json(new
                {
                    clientes = resultado.Clientes,
                    total = resultado.Total,
                    pagina = pagina,
                    totalPaginas = resultado.TotalPaginas,
                    mensaje = resultado.Total == 0 ? "No se encontraron clientes" : null
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult GenerarPDF(
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            string zona,
            string cliente,
            string vendedor,
            string factura,
            string facturaHasta,
            int? cuotaDesde,
            int? cuotaHasta,
            string moneda,
            string ordenMoneda)
        {
            try
            {
                var filtros = new FiltroCxC
                {
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,
                    Zona = zona,
                    Cliente = cliente,
                    Vendedor = vendedor,
                    FacturaDesde = factura,
                    FacturaHasta = facturaHasta,
                    CuotaDesde = cuotaDesde,
                    CuotaHasta = cuotaHasta,
                    Moneda = moneda,
                    OrdenMoneda = ordenMoneda
                };

                var cuentas = _cuentasService.ObtenerCuentasPorFiltro(filtros);

                // TODO: Implementar generación de PDF con iTextSharp
                var pdfBytes = new byte[0];

                return File(pdfBytes, "application/pdf", $"ReporteCxC_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest("Error al generar PDF: " + ex.Message);
            }
        }

        [HttpPost]
        public IActionResult ExportarExcel(
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            string zona,
            string cliente,
            string vendedor,
            string factura,
            string facturaHasta,
            int? cuotaDesde,
            int? cuotaHasta,
            string moneda,
            string ordenMoneda)
        {
            try
            {
                var filtros = new FiltroCxC
                {
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,
                    Zona = zona,
                    Cliente = cliente,
                    Vendedor = vendedor,
                    FacturaDesde = factura,
                    FacturaHasta = facturaHasta,
                    CuotaDesde = cuotaDesde,
                    CuotaHasta = cuotaHasta,
                    Moneda = moneda,
                    OrdenMoneda = ordenMoneda
                };

                var cuentas = _cuentasService.ObtenerCuentasPorFiltro(filtros);

                // TODO: Implementar generación de Excel con EPPlus
                var excelBytes = new byte[0];

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ReporteCxC_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest("Error al exportar Excel: " + ex.Message);
            }
        }

        // Modal de detalle (CxC) - usa el mismo partial que Ventas
        [HttpGet]
        public IActionResult CargarModalDetalleFactura(string contrato, string cliente)
        {
            var model = new DetalleFacturaViewModel
            {
                Factura = contrato
            };
            return PartialView("~/Views/Shared/_ModalDetalleFactura.cshtml", model);
        }

        [HttpGet]
        public IActionResult ObtenerRelacionPagos(string cliente)
        {
            try
            {
                var pagos = _cuentasService.ObtenerRelacionPagos(cliente);
                return Json(new { success = true, pagos = pagos });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Datos del detalle para el modal
        [HttpGet]
        public IActionResult ObtenerDetalleFactura(string contrato, string cliente)
        {
            try
            {
                var detalle = _cuentasService.ObtenerDetalleFacturaCxC(contrato, cliente);
                if (detalle == null || string.IsNullOrEmpty(detalle.Factura))
                {
                    return Json(new { success = false, message = "No se encontró el contrato especificado." });
                }

                return Json(new { success = true, data = detalle });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al obtener el detalle: {ex.Message}" });
            }
        }
    }
}