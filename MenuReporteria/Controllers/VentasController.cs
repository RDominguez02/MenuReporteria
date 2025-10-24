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
    public IActionResult ObtenerDetalleFactura(string factura)
    {
        try
        {
            // Por ahora devolvemos datos de prueba
            // Luego conectaremos con el servicio real
            var detalle = new DetalleFacturaViewModel
            {
                Factura = factura,
                Fecha = DateTime.Now,
                Ncf = "B0100000001",
                Usuario = "ADMIN",
                Turno = 1,
                UltimoControl = "5806",
                Caja = "C",
                Vendedor = "VE001",

                ClienteCodigo = "CL1-01",
                ClienteNombre = "COMERCIAL LA ISABELA SRL",
                ClienteRnc = "131-12345-6",
                ClienteDireccion = "PADRE CASTELLANOS #61 ENSANCHE ESPAILLAT",
                ClienteTelefono = "809-123-4567",

                Productos = new List<ProductoFacturaItem>
            {
                new ProductoFacturaItem
                {
                    CodigoFicha = "LZRKTF00BS1010183",
                    Cantidad = 1,
                    UnidadMedida = "UD",
                    Descripcion = "TAURO LEAD 125 BLANCO 2025",
                    PrecioUnitario = 1090.00m,
                    Itbis = 152.20m,
                    Total = 1090.00m
                }
            },

                MontoBruto = 22894.02m,
                Impuesto17 = 2385.05m,
                Itbis18 = 4120.93m,
                Descuento = 0.00m,
                Subtotal = 0.00m,
                TotalItbis = 4120.93m,
                MontoNeto = 29400.00m
            };

            return Json(new { success = true, data = detalle });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
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
}