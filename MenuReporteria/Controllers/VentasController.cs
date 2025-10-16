using Microsoft.AspNetCore.Mvc;
using MenuReporteria.Models;
using MenuReporteria.Services;
using System.Linq;

namespace MenuReporteria.Controllers
{
    public class VentasController : Controller
    {
        private readonly ReporteVentasService _service;

        public VentasController(ReporteVentasService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Cargar la vista inicial con filtros por defecto
            var modelo = new ResultadoReporteVentas
            {
                Filtros = new FiltroVentas()
            };

            return View("ReporteVentas", modelo);
        }

        [HttpPost]
        public IActionResult Generar([FromForm] FiltroVentas filtros)
        {
            try
            {
                var ventas = _service.ObtenerVentasPorFiltro(filtros);

                var resultado = new
                {
                    success = true,
                    data = new
                    {
                        ventas
                    },
                    totalFacturas = ventas.Count,
                    totalChasis = ventas.Count, // temporal, puedes cambiarlo
                    valorTotal = ventas.Sum(v => v.MontoBruto).ToString("N2")
                };

                return Json(resultado);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
