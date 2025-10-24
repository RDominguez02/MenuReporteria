using Azure.Core;
using MenuReporteria.Models;
using MenuReporteria.Services;
using Microsoft.AspNetCore.Mvc;

public class VentasController : Controller
{
    private readonly ReporteVentasService _reporteService;

    public VentasController(ReporteVentasService reporteService)
    {
        _reporteService = reporteService;
    }

    public IActionResult Index()
    {
        var modelo = new ResultadoReporteVentas
        {
            Filtros = new FiltroVentas
            {
                FechaDesde = DateTime.Now.AddMonths(-1),
                FechaHasta = DateTime.Now,
                // CARGAR LISTAS PARA DROPDOWNS
                VendedoresDisponibles = _reporteService.ObtenerVendedores(),
                CajasDisponibles = _reporteService.ObtenerCajas(),
                MonedasDisponibles = _reporteService.ObtenerMonedas(),
                SucursalesDisponibles = _reporteService.ObtenerSucursales()
            }
        };

        return View("ReporteVentas",modelo);
    }

    [HttpPost]
    public IActionResult Generar(FiltroVentas filtros)
    {
        try
        {
            // CARGAR LISTAS (necesarias para la vista)
            filtros.VendedoresDisponibles = _reporteService.ObtenerVendedores();
            filtros.CajasDisponibles = _reporteService.ObtenerCajas();
            filtros.MonedasDisponibles = _reporteService.ObtenerMonedas();
            filtros.SucursalesDisponibles = _reporteService.ObtenerSucursales();

            var ventas = _reporteService.ObtenerVentasPorFiltro(filtros);

            var resultado = new ResultadoReporteVentas
            {
                Ventas = ventas,
                TotalFacturas = ventas.Count,
                TotalChasis = (int)ventas.Sum(v => v.TotalChasis),
                ValorTotal = ventas.Sum(v => v.MontoNeto),
                Filtros = filtros
            };

            // SI ES AJAX
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ventas = ventas,
                        totalFacturas = resultado.TotalFacturas,
                        totalChasis = resultado.TotalChasis,
                        valorTotal = resultado.ValorTotal.ToString("N2")
                    },
                    totalFacturas = resultado.TotalFacturas,
                    totalChasis = resultado.TotalChasis,
                    valorTotal = resultado.ValorTotal.ToString("N2")
                });
            }

            return View("ReporteVentas", resultado);
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }

            ModelState.AddModelError("", ex.Message);
            return View("ReporteVentas");
        }
    }

    [HttpGet]
    [HttpGet]
    public IActionResult ObtenerDetalleFactura(string factura)
    {
        try
        {
            // Obtener el detalle real desde la base de datos
            var detalle = _reporteService.ObtenerDetalleFactura(factura);

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

    [HttpGet]
    public IActionResult CargarModalDetalleFactura(string factura)
    {
        // Puedes pasar un modelo con datos mínimos (mock) al partial
        var model = new DetalleFacturaViewModel
        {
            Factura = factura,
            // deja los demás campos vacíos; el JS (cargarDetalleFactura) los completará
        };

        return PartialView("_ModalDetalleFactura", model);
    }

    [HttpGet]
    public IActionResult ObtenerChasisFactura(string factura)
    {
        try
        {
            var chasis = _reporteService.ObtenerChasisPorFactura(factura);
            return Json(new { success = true, data = chasis });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}