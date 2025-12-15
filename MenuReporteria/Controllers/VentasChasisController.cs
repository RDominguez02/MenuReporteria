using MenuReporteria.Models;
using MenuReporteria.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace MenuReporteria.Controllers
{
    public class VentasChasisController : Controller
    {
        private readonly ReporteVentasChasisService _reporteService;
        private readonly ReporteVentasService _ventasService; // Agregar esta dependencia

        public VentasChasisController(ReporteVentasChasisService reporteService, ReporteVentasService ventasService)
        {
            _reporteService = reporteService;
            _ventasService = ventasService;
        }

        public IActionResult Index()
        {
            var modelo = new ResultadoReporteVentasChasis
            {
                Filtros = new FiltroVentasChasis
                {
                    FechaDesde = DateTime.Now.AddMonths(-1),
                    FechaHasta = DateTime.Now,
                    VendedoresDisponibles = _reporteService.ObtenerVendedores(),
                    AlmacenesDisponibles = _reporteService.ObtenerAlmacenes(),
                    CajasDisponibles = _reporteService.ObtenerCajas()
                }
            };

            return View("ReporteVentasChasis", modelo);
        }

        [HttpPost]
        public IActionResult Generar(FiltroVentasChasis filtros)
        {
            try
            {
                filtros.VendedoresDisponibles = _reporteService.ObtenerVendedores();
                filtros.AlmacenesDisponibles = _reporteService.ObtenerAlmacenes();
                filtros.CajasDisponibles = _reporteService.ObtenerCajas();

                var ventas = _reporteService.ObtenerVentasChasisPorFiltro(filtros);

                var resultado = new ResultadoReporteVentasChasis
                {
                    Ventas = ventas,
                    TotalRegistros = ventas.Count,
                    TotalChasis = ventas.Count,
                    ValorTotal = ventas.Sum(v => v.Precio),
                    Filtros = filtros
                };

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            ventas = ventas,
                            totalRegistros = resultado.TotalRegistros,
                            totalChasis = resultado.TotalChasis,
                            valorTotal = resultado.ValorTotal.ToString("N2")
                        },
                        totalRegistros = resultado.TotalRegistros,
                        totalChasis = resultado.TotalChasis,
                        valorTotal = resultado.ValorTotal.ToString("N2")
                    });
                }

                return View("ReporteVentasChasis", resultado);
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = ex.Message });
                }

                ModelState.AddModelError("", ex.Message);
                return View("ReporteVentasChasis");
            }
        }

        [HttpGet]
        public IActionResult ObtenerClientes(string filtro = "", int pagina = 1, int registrosPorPagina = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filtro) || filtro.Length < 2)
                {
                    return Json(new
                    {
                        clientes = new System.Collections.Generic.List<object>(),
                        total = 0,
                        pagina = pagina,
                        totalPaginas = 0,
                        mensaje = "Ingresa al menos 2 caracteres para buscar"
                    });
                }

                var resultado = _reporteService.ObtenerClientesPaginados(filtro, pagina, registrosPorPagina);

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

        // ===== NUEVOS MÉTODOS PARA VER FACTURA =====

        [HttpGet]
        public IActionResult CargarModalDetalleFactura(string factura)
        {
            var model = new DetalleFacturaViewModel
            {
                Factura = factura
            };
            return PartialView("~/Views/Shared/_ModalDetalleFactura.cshtml", model);
        }

        [HttpGet]
        public IActionResult ObtenerDetalleFactura(string factura)
        {
            try
            {
                // Obtener el detalle real desde la base de datos usando el servicio de Ventas
                var detalle = _ventasService.ObtenerDetalleFactura(factura);

                if (detalle == null || string.IsNullOrEmpty(detalle.Factura))
                {
                    return Json(new
                    {
                        success = false,
                        message = "No se encontró la factura especificada"
                    });
                }

                return Json(new { success = true, data = detalle });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error al obtener el detalle: {ex.Message}"
                });
            }
        }
    }
}