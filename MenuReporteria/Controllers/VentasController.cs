using Azure.Core;
using MenuReporteria.Models;
using MenuReporteria.Services;
using Microsoft.AspNetCore.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace MenuReporteria.Controllers
{
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

            return View("ReporteVentas", modelo);
        }

        public IActionResult Index2()
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

            return View("ReporteVentas2", modelo);
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
                Factura = factura
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

        // Actualizar método en VentasController.cs

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

        [HttpPost]
        public IActionResult GenerarPDF(FiltroVentas filtros)
        {
            try
            {
                // CARGAR LISTAS
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

                // Generar PDF
                byte[] pdfBytes = GenerarReportePDF(resultado);

                // Retornar como descarga
                return File(pdfBytes, "application/pdf", $"ReporteVentas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al generar PDF: {ex.Message}" });
            }
        }

        private byte[] GenerarReportePDF(ResultadoReporteVentas resultado)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Crear documento PDF en HORIZONTAL (Landscape)
                Document doc = new Document(PageSize.A4.Rotate(), 10, 10, 20, 20);
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // FUENTES
                Font fontTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                Font fontSubtitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                Font fontNormal = FontFactory.GetFont(FontFactory.HELVETICA, 8);
                Font fontEncabezado = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);

                // TÍTULO
                Paragraph titulo = new Paragraph("REPORTE DE VENTAS", fontTitulo);
                titulo.Alignment = Element.ALIGN_CENTER;
                doc.Add(titulo);

                // INFORMACIÓN DEL REPORTE
                Paragraph info = new Paragraph();
                info.Add(new Chunk("Reporte de Ventas\n", fontSubtitulo));
                info.Add(new Chunk($"Desde la Fecha: {resultado.Filtros.FechaDesde:dd-MM-yyyy}   ", fontNormal));
                info.Add(new Chunk($"Hasta la fecha: {resultado.Filtros.FechaHasta:dd-MM-yyyy}", fontNormal));
                if (!string.IsNullOrEmpty(resultado.Filtros.Sucursal))
                {
                    info.Add(new Chunk($"   / SUCURSAL: {resultado.Filtros.Sucursal}", fontNormal));
                }
                info.Alignment = Element.ALIGN_LEFT;
                doc.Add(info);
                doc.Add(new Paragraph(" "));

                // TABLA
                PdfPTable tabla = new PdfPTable(12);
                tabla.WidthPercentage = 100;
                tabla.SetWidths(new float[] { 8, 10, 8, 8, 20, 6, 6, 12, 8, 12, 12, 12 });

                // Encabezados
                string[] encabezados = { "FECHA", "DOCUMENTO", "NCF", "CÓDIGO", "NOMBRE", "VEND.", "SUCU.", "VALOR", "MONEDA", "TASA", "TOTAL RD$", "CHASIS" };

                foreach (string encabezado in encabezados)
                {
                    PdfPCell celda = new PdfPCell(new Phrase(encabezado, fontEncabezado));
                    celda.BackgroundColor = new BaseColor(100, 150, 200);
                    celda.BorderWidth = 0;
                    celda.HorizontalAlignment = Element.ALIGN_CENTER;
                    celda.VerticalAlignment = Element.ALIGN_MIDDLE;
                    celda.Padding = 5;
                    celda.PaddingTop = 6;
                    celda.PaddingBottom = 6;

                    // Cambiar color del texto a blanco
                    Phrase frase = new Phrase(encabezado, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, Font.NORMAL, new BaseColor(255, 255, 255)));
                    celda.Phrase = frase;

                    tabla.AddCell(celda);
                }

                // FILAS
                bool alternado = false;
                foreach (var venta in resultado.Ventas)
                {
                    BaseColor colorFondo = alternado ? new BaseColor(240, 245, 250) : new BaseColor(255, 255, 255);

                    // FECHA
                    PdfPCell celdaFecha = new PdfPCell(new Phrase(venta.Fecha.ToString("dd-MM-yyyy"), fontNormal));
                    celdaFecha.BackgroundColor = colorFondo;
                    celdaFecha.BorderWidth = 0;
                    celdaFecha.HorizontalAlignment = Element.ALIGN_CENTER;
                    celdaFecha.Padding = 4;
                    tabla.AddCell(celdaFecha);

                    // DOCUMENTO
                    PdfPCell celdaDocumento = new PdfPCell(new Phrase(venta.Factura, fontNormal));
                    celdaDocumento.BackgroundColor = colorFondo;
                    celdaDocumento.BorderWidth = 0;
                    celdaDocumento.HorizontalAlignment = Element.ALIGN_CENTER;
                    celdaDocumento.Padding = 4;
                    tabla.AddCell(celdaDocumento);

                    // CÓDIGO (NCF)
                    PdfPCell celdaNCF = new PdfPCell(new Phrase(venta.Ncf ?? "", fontNormal));
                    celdaNCF.BackgroundColor = colorFondo;
                    celdaNCF.BorderWidth = 0;
                    celdaNCF.HorizontalAlignment = Element.ALIGN_CENTER;
                    celdaNCF.Padding = 4;
                    tabla.AddCell(celdaNCF);

                    // CÓDIGO (Cliente)
                    PdfPCell celdaCodigo = new PdfPCell(new Phrase(venta.CodigoCliente ?? "", fontNormal));
                    celdaCodigo.BackgroundColor = colorFondo;
                    celdaCodigo.BorderWidth = 0;
                    celdaCodigo.HorizontalAlignment = Element.ALIGN_CENTER;
                    celdaCodigo.Padding = 4;
                    tabla.AddCell(celdaCodigo);

                    // NOMBRE
                    PdfPCell celdaNombre = new PdfPCell(new Phrase(venta.Cliente, fontNormal));
                    celdaNombre.BackgroundColor = colorFondo;
                    celdaNombre.BorderWidth = 0;
                    celdaNombre.HorizontalAlignment = Element.ALIGN_LEFT;
                    celdaNombre.Padding = 4;
                    tabla.AddCell(celdaNombre);

                    // VENDEDOR
                    PdfPCell celdaVendedor = new PdfPCell(new Phrase(venta.Vendedor ?? "", fontNormal));
                    celdaVendedor.BackgroundColor = colorFondo;
                    celdaVendedor.BorderWidth = 0;
                    celdaVendedor.HorizontalAlignment = Element.ALIGN_CENTER;
                    celdaVendedor.Padding = 4;
                    tabla.AddCell(celdaVendedor);

                    // SUCURSAL
                    PdfPCell celdaSucursal = new PdfPCell(new Phrase("02", fontNormal));
                    celdaSucursal.BackgroundColor = colorFondo;
                    celdaSucursal.BorderWidth = 0;
                    celdaSucursal.HorizontalAlignment = Element.ALIGN_CENTER;
                    celdaSucursal.Padding = 4;
                    tabla.AddCell(celdaSucursal);

                    // VALOR
                    PdfPCell celdaValor = new PdfPCell(new Phrase($"{venta.MontoNeto:N2} {venta.Moneda}", fontNormal));
                    celdaValor.BackgroundColor = colorFondo;
                    celdaValor.BorderWidth = 0;
                    celdaValor.HorizontalAlignment = Element.ALIGN_RIGHT;
                    celdaValor.Padding = 4;
                    tabla.AddCell(celdaValor);

                    // MONEDA
                    PdfPCell celdaMoneda = new PdfPCell(new Phrase(venta.Moneda ?? "", fontNormal));
                    celdaMoneda.BackgroundColor = colorFondo;
                    celdaMoneda.BorderWidth = 0;
                    celdaMoneda.HorizontalAlignment = Element.ALIGN_CENTER;
                    celdaMoneda.Padding = 4;
                    tabla.AddCell(celdaMoneda);

                    // TASA
                    PdfPCell celdaTasa = new PdfPCell(new Phrase(venta.Tasa.ToString("N2"), fontNormal));
                    celdaTasa.BackgroundColor = colorFondo;
                    celdaTasa.BorderWidth = 0;
                    celdaTasa.HorizontalAlignment = Element.ALIGN_RIGHT;
                    celdaTasa.Padding = 4;
                    tabla.AddCell(celdaTasa);

                    // TOTAL RD$
                    PdfPCell celdaTotal = new PdfPCell(new Phrase(venta.TotalR.ToString("N2"), fontNormal));
                    celdaTotal.BackgroundColor = colorFondo;
                    celdaTotal.BorderWidth = 0;
                    celdaTotal.HorizontalAlignment = Element.ALIGN_RIGHT;
                    celdaTotal.Padding = 4;
                    tabla.AddCell(celdaTotal);

                    // TOTAL Chasis
                    PdfPCell celdaChasis = new PdfPCell(new Phrase(venta.TotalChasis.ToString("N2"), fontNormal));
                    celdaChasis.BackgroundColor = colorFondo;
                    celdaChasis.BorderWidth = 0;
                    celdaChasis.HorizontalAlignment = Element.ALIGN_RIGHT;
                    celdaChasis.Padding = 4;
                    tabla.AddCell(celdaChasis);

                    alternado = !alternado;
                }

                doc.Add(tabla);
                doc.Add(new Paragraph(" "));

                // RESUMEN
                PdfPTable resumenTabla = new PdfPTable(3);
                resumenTabla.WidthPercentage = 100;

                PdfPCell celdaResumen(string label, string valor)
                {
                    var celda = new PdfPCell(new Phrase($"{label}: {valor}", fontNormal));
                    celda.Padding = 6;
                    celda.BackgroundColor = new BaseColor(100, 150, 200);
                    celda.BorderWidth = 0;
                    celda.HorizontalAlignment = Element.ALIGN_CENTER;

                    Phrase fraseResumen = new Phrase($"{label}: {valor}", FontFactory.GetFont(FontFactory.HELVETICA, 9, Font.NORMAL, new BaseColor(255, 255, 255)));
                    celda.Phrase = fraseResumen;

                    return celda;
                }

                resumenTabla.AddCell(celdaResumen("Total Facturas", resultado.TotalFacturas.ToString()));
                resumenTabla.AddCell(celdaResumen("Total Chasis", resultado.TotalChasis.ToString()));
                resumenTabla.AddCell(celdaResumen("Valor Total", $"RD$ {resultado.ValorTotal:N2}"));

                doc.Add(resumenTabla);

                doc.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        [HttpPost]
        public IActionResult ExportarExcel(FiltroVentas filtros)
        {
            try
            {
                // Establecer licencia de EPPlus (requerido en versiones recientes)
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // CARGAR LISTAS
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

                // Generar Excel
                byte[] excelBytes = GenerarReporteExcel(resultado);

                // Retornar como descarga
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ReporteVentas_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al generar Excel: {ex.Message}" });
            }
        }

        private byte[] GenerarReporteExcel(ResultadoReporteVentas resultado)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Reporte Ventas");

                // ESTILOS
                var fillColor = System.Drawing.Color.FromArgb(100, 150, 200);

                // TÍTULO
                worksheet.Cells["A1"].Value = "REPORTE DE VENTAS";
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1:I1"].Merge = true;
                worksheet.Cells["A1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                // INFORMACIÓN DEL REPORTE
                worksheet.Cells["A3"].Value = "Reporte de Ventas";
                worksheet.Cells["A3"].Style.Font.Bold = true;
                worksheet.Cells["A3"].Style.Font.Size = 11;

                worksheet.Cells["A4"].Value = $"Desde la Fecha: {resultado.Filtros.FechaDesde:dd-MM-yyyy}";
                worksheet.Cells["B4"].Value = $"Hasta la fecha: {resultado.Filtros.FechaHasta:dd-MM/yyyy}";

                if (!string.IsNullOrEmpty(resultado.Filtros.Sucursal))
                {
                    worksheet.Cells["C4"].Value = $"SUCURSAL: {resultado.Filtros.Sucursal}";
                }

                // ENCABEZADOS DE TABLA
                int fila = 6;
                string[] encabezados = { "FECHA", "DOCUMENTO", "NCF", "CÓDIGO", "NOMBRE", "VENDEDOR", "SUCU.", "VALOR", "MONEDA", "TASA", "TOTAL RD$", "CHASIS" };

                for (int i = 0; i < encabezados.Length; i++)
                {
                    var cell = worksheet.Cells[fila, i + 1];
                    cell.Value = encabezados[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Size = 12;
                    cell.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(fillColor);
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    cell.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Medium;
                }

                // DATOS
                fila = 7;
                int colorAlternado = 0;

                foreach (var venta in resultado.Ventas)
                {
                    // Asignar color alternado a las filas
                    System.Drawing.Color colorFila = colorAlternado % 2 == 0
                        ? System.Drawing.Color.White
                        : System.Drawing.Color.FromArgb(240, 245, 250);

                    worksheet.Cells[fila, 1].Value = venta.Fecha.ToString("dd-MM-yyyy");
                    worksheet.Cells[fila, 2].Value = venta.Factura;
                    worksheet.Cells[fila, 3].Value = venta.Ncf;
                    worksheet.Cells[fila, 4].Value = venta.CodigoCliente;
                    worksheet.Cells[fila, 5].Value = venta.Cliente;
                    worksheet.Cells[fila, 6].Value = venta.Vendedor;
                    worksheet.Cells[fila, 7].Value = "02";
                    worksheet.Cells[fila, 8].Value = $"{venta.MontoNeto:N2}";
                    worksheet.Cells[fila, 9].Value = venta.Moneda;
                    worksheet.Cells[fila, 10].Value = venta.Tasa;
                    worksheet.Cells[fila, 11].Value = venta.TotalR;
                    worksheet.Cells[fila, 12].Value = venta.TotalChasis;

                    // Aplicar formato y color
                    for (int i = 1; i <= 12; i++)
                    {
                        var cell = worksheet.Cells[fila, i];
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(colorFila);

                        // Alinear números a la derecha
                        if (i >= 7)
                        {
                            cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                            cell.Style.Numberformat.Format = "#,##0.00";
                        }
                        else if (i == 1 || i == 2 || i == 3 || i == 6)
                        {
                            cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        }

                        // Asegurar que el color de fuente de las celdas de datos sea negro
                        cell.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                    }

                    fila++;
                    colorAlternado++;
                }

                // RESUMEN
                int filaResumen = fila + 2;
                worksheet.Cells[filaResumen, 1].Value = "RESUMEN";
                worksheet.Cells[filaResumen, 1].Style.Font.Bold = true;
                worksheet.Cells[filaResumen, 1].Style.Font.Size = 11;

                filaResumen++;

                // Total Facturas
                worksheet.Cells[filaResumen, 1].Value = "Total Facturas:";
                worksheet.Cells[filaResumen, 2].Value = resultado.TotalFacturas;
                worksheet.Cells[filaResumen, 1].Style.Font.Bold = true;
                worksheet.Cells[filaResumen, 1].Style.Font.Size = 11;
                worksheet.Cells[filaResumen, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[filaResumen, 1].Style.Fill.BackgroundColor.SetColor(fillColor);
                worksheet.Cells[filaResumen, 1].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                worksheet.Cells[filaResumen, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[filaResumen, 2].Style.Fill.BackgroundColor.SetColor(fillColor);
                worksheet.Cells[filaResumen, 2].Style.Font.Color.SetColor(System.Drawing.Color.Black);

                filaResumen++;

                // Total Chasis
                worksheet.Cells[filaResumen, 1].Value = "Total Chasis:";
                worksheet.Cells[filaResumen, 2].Value = resultado.TotalChasis;
                worksheet.Cells[filaResumen, 1].Style.Font.Bold = true;
                worksheet.Cells[filaResumen, 1].Style.Font.Size = 11;
                worksheet.Cells[filaResumen, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[filaResumen, 1].Style.Fill.BackgroundColor.SetColor(fillColor);
                worksheet.Cells[filaResumen, 1].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                worksheet.Cells[filaResumen, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[filaResumen, 2].Style.Fill.BackgroundColor.SetColor(fillColor);
                worksheet.Cells[filaResumen, 2].Style.Font.Color.SetColor(System.Drawing.Color.Black);

                filaResumen++;

                // Valor Total
                worksheet.Cells[filaResumen, 1].Value = "Valor Total:";
                worksheet.Cells[filaResumen, 2].Value = resultado.ValorTotal;
                worksheet.Cells[filaResumen, 1].Style.Font.Bold = true;
                worksheet.Cells[filaResumen, 1].Style.Font.Size = 11;
                worksheet.Cells[filaResumen, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[filaResumen, 1].Style.Fill.BackgroundColor.SetColor(fillColor);
                worksheet.Cells[filaResumen, 1].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                worksheet.Cells[filaResumen, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[filaResumen, 2].Style.Fill.BackgroundColor.SetColor(fillColor);
                worksheet.Cells[filaResumen, 2].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                worksheet.Cells[filaResumen, 2].Style.Numberformat.Format = "#,##0.00";

                // AJUSTAR ANCHO DE COLUMNAS
                worksheet.Column(1).Width = 12;
                worksheet.Column(2).Width = 12;
                worksheet.Column(3).Width = 12;
                worksheet.Column(4).Width = 12;
                worksheet.Column(5).Width = 25;
                worksheet.Column(6).Width = 12;
                worksheet.Column(7).Width = 8;
                worksheet.Column(8).Width = 18;
                worksheet.Column(9).Width = 12;
                worksheet.Column(10).Width = 18;
                worksheet.Column(11).Width = 18;
                worksheet.Column(12).Width = 18;

                return package.GetAsByteArray();
            }
        }
    }

}