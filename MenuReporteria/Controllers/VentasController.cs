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
}