using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MenuReporteria.Models;
using System.Text;
using System.Linq;
using System.Globalization;

namespace MenuReporteria.Services
{
    public class CuentasPorCobrarService
    {
        private readonly string _connectionString;

        public CuentasPorCobrarService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        }

        public List<FiltroOpcion> ObtenerZonas()
        {
            var zonas = new List<FiltroOpcion>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT DISTINCT zo_codigo FROM prbdclie WHERE zo_codigo IS NOT NULL ORDER BY zo_codigo";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var codigo = reader["zo_codigo"].ToString();
                            if (!string.IsNullOrWhiteSpace(codigo))
                            {
                                zonas.Add(new FiltroOpcion
                                {
                                    Valor = codigo,
                                    Texto = codigo
                                });
                            }
                        }
                    }
                }
            }
            return zonas;
        }

        public List<FiltroOpcion> ObtenerClientes()
        {
            var clientes = new List<FiltroOpcion>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"SELECT DISTINCT cl_codigo, ISNULL(cl_nombre, '') AS cl_nombre
                               FROM prbdclie
                               WHERE cl_codigo IS NOT NULL
                               ORDER BY cl_nombre, cl_codigo";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var codigo = reader["cl_codigo"].ToString();
                            if (!string.IsNullOrWhiteSpace(codigo))
                            {
                                var nombre = reader["cl_nombre"].ToString();
                                clientes.Add(new FiltroOpcion
                                {
                                    Valor = codigo,
                                    Texto = string.IsNullOrWhiteSpace(nombre) ? codigo : $"{codigo} - {nombre.Trim()}"
                                });
                            }
                        }
                    }
                }
            }
            return clientes;
        }

        public List<FiltroOpcion> ObtenerVendedores()
        {
            var vendedores = new List<FiltroOpcion>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"SELECT DISTINCT ve_codigo
                               FROM prbdheco
                               WHERE ve_codigo IS NOT NULL
                               ORDER BY ve_codigo";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var codigo = reader["ve_codigo"].ToString();
                            if (!string.IsNullOrWhiteSpace(codigo))
                            {
                                vendedores.Add(new FiltroOpcion
                                {
                                    Valor = codigo,
                                    Texto = codigo
                                });
                            }
                        }
                    }
                }
            }
            return vendedores;
        }

        public List<FiltroOpcion> ObtenerMonedas()
        {
            var monedas = new List<FiltroOpcion>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"SELECT DISTINCT mo_codigo
                               FROM prbdheco
                               WHERE mo_codigo IS NOT NULL
                               ORDER BY mo_codigo";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var codigo = reader["mo_codigo"].ToString();
                            if (!string.IsNullOrWhiteSpace(codigo))
                            {
                                monedas.Add(new FiltroOpcion
                                {
                                    Valor = codigo,
                                    Texto = codigo
                                });
                            }
                        }
                    }
                }
            }
            return monedas;
        }

        public List<CxCItem> ObtenerCuentasPorFiltro(FiltroCxC filtros)
        {
            var cuentas = new List<CxCItem>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var fechaFiltro = filtros.FechaHasta ?? filtros.FechaDesde ?? DateTime.Now;
                var fechaFiltroParametro = fechaFiltro.ToString("yyyyMMdd");

                var queryBuilder = new StringBuilder(@"
                SELECT
                    a.*,
                    ISNULL(b.cl_nombre,'') AS cl_nombre,
                    ISNULL(b.cl_tele,'') AS cl_tele,
                    ISNULL(b.cl_BEEP,'') AS cl_BEEP,
                    ISNULL(b.cl_CELU,'') AS cl_CELU,
                    ISNULL(b.cl_direc1,'') AS cl_direc1,
                    ISNULL(b.cl_direc2,'') AS cl_direc2,
                    ISNULL(b.zo_codigo,'') AS zo_codigo,
                    ISNULL(c.ti_codigo,'') AS ti_codigo,
                    ISNULL(d.ti_descri,'') AS ti_descri,
                    ISNULL(c.MO_CODIGO,'') AS MO_CODIGO,
                    ISNULL(c.co_fecha,'') AS co_fecha,
                    ISNULL(c.ve_codigo,'') AS ve_codigo,
                    ISNULL(e.hi_capital,0) AS CAPITAL_INI,
                    ISNULL(e.hi_interes,0) AS INTERES_INI
                FROM (
                    SELECT a.*, DATEDIFF(DAY, a.hi_FECha, GETDATE()) AS dias
                    FROM fun_pagare(@FechaFiltro) AS a
                    LEFT JOIN PRBDHECO AS bh ON a.hi_contra = bh.CO_CONTRA
                    UNION ALL
                    SELECT a.*, DATEDIFF(DAY, a.hi_FECha, GETDATE()) AS dias
                    FROM fun_nodistri(@FechaFiltro) AS a
                    LEFT JOIN PRBDHECO AS nh ON a.hi_contra = nh.CO_CONTRA
                ) AS a
                LEFT JOIN prbdclie AS b ON a.cl_codigo = b.cl_codigo AND a.COD_SUCU = b.COD_SUCU
                LEFT JOIN PRBDHECO AS c ON a.hi_contra = c.CO_CONTRA AND a.COD_SUCU = c.COD_SUCU
                LEFT JOIN prbdtipocontrato AS d ON c.ti_codigo = d.ti_codigo
                LEFT JOIN prbdhis AS e ON a.HI_CONTRA = e.HI_CONTRA AND a.HI_FACAFEC = e.HI_FACAFEC AND a.CL_CODIGO = c.CL_CODIGO AND e.HI_TIPO = 'F'
                WHERE 1 = 1
                ");

                if (filtros.FechaDesde.HasValue)
                {
                    queryBuilder.AppendLine(" AND CAST(a.hi_fecha AS DATE) >= @FechaDesde");
                }

                if (filtros.FechaHasta.HasValue)
                {
                    queryBuilder.AppendLine(" AND CAST(a.hi_fecha AS DATE) <= @FechaHasta");
                }

                if (!string.IsNullOrWhiteSpace(filtros.Zona))
                {
                    queryBuilder.AppendLine(" AND b.zo_codigo = @Zona");
                }

                if (!string.IsNullOrWhiteSpace(filtros.Cliente))
                {
                    queryBuilder.AppendLine(" AND a.cl_codigo = @Cliente");
                }

                if (!string.IsNullOrWhiteSpace(filtros.Vendedor))
                {
                    queryBuilder.AppendLine(" AND c.ve_codigo = @Vendedor");
                }

                if (!string.IsNullOrWhiteSpace(filtros.FacturaDesde))
                {
                    queryBuilder.AppendLine(" AND a.hi_contra >= @FacturaDesde");
                }

                if (!string.IsNullOrWhiteSpace(filtros.FacturaHasta))
                {
                    queryBuilder.AppendLine(" AND a.hi_contra <= @FacturaHasta");
                }

                if (filtros.CuotaDesde.HasValue)
                {
                    queryBuilder.AppendLine(" AND TRY_CAST(LEFT(a.hi_facafec, CHARINDEX('/', a.hi_facafec + '/') - 1) AS INT) >= @CuotaDesde");
                }

                if (filtros.CuotaHasta.HasValue)
                {
                    queryBuilder.AppendLine(" AND TRY_CAST(LEFT(a.hi_facafec, CHARINDEX('/', a.hi_facafec + '/') - 1) AS INT) <= @CuotaHasta");
                }

                if (!string.IsNullOrWhiteSpace(filtros.Moneda))
                {
                    queryBuilder.AppendLine(" AND c.MO_CODIGO = @Moneda");
                }

                if (filtros.OrdenMoneda == "opcion1")
                {
                    queryBuilder.AppendLine(" ORDER BY c.MO_CODIGO, a.hi_fecha, a.hi_contra");
                }
                else if (filtros.OrdenMoneda == "opcion2")
                {
                    queryBuilder.AppendLine(" ORDER BY c.MO_CODIGO, a.hi_contra, a.hi_fecha");
                }
                else
                {
                    queryBuilder.AppendLine(" ORDER BY b.cl_nombre, a.hi_fecha, a.hi_contra");
                }

                using (var command = new SqlCommand(queryBuilder.ToString(), connection))
                {
                    command.Parameters.AddWithValue("@FechaFiltro", fechaFiltroParametro);
                    if (filtros.FechaDesde.HasValue)
                        command.Parameters.AddWithValue("@FechaDesde", filtros.FechaDesde.Value.Date);
                    if (filtros.FechaHasta.HasValue)
                        command.Parameters.AddWithValue("@FechaHasta", filtros.FechaHasta.Value.Date);
                    if (!string.IsNullOrEmpty(filtros.Zona))
                        command.Parameters.AddWithValue("@Zona", filtros.Zona);
                    if (!string.IsNullOrEmpty(filtros.Cliente))
                        command.Parameters.AddWithValue("@Cliente", filtros.Cliente);
                    if (!string.IsNullOrEmpty(filtros.Vendedor))
                        command.Parameters.AddWithValue("@Vendedor", filtros.Vendedor);
                    if (!string.IsNullOrEmpty(filtros.FacturaDesde))
                        command.Parameters.AddWithValue("@FacturaDesde", filtros.FacturaDesde);
                    if (!string.IsNullOrEmpty(filtros.FacturaHasta))
                        command.Parameters.AddWithValue("@FacturaHasta", filtros.FacturaHasta);
                    if (filtros.CuotaDesde.HasValue)
                        command.Parameters.AddWithValue("@CuotaDesde", filtros.CuotaDesde.Value);
                    if (filtros.CuotaHasta.HasValue)
                        command.Parameters.AddWithValue("@CuotaHasta", filtros.CuotaHasta.Value);
                    if (!string.IsNullOrEmpty(filtros.Moneda))
                        command.Parameters.AddWithValue("@Moneda", filtros.Moneda);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        var schemaTable = reader.GetSchemaTable();
                        var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        if (schemaTable != null)
                        {
                            foreach (DataRow row in schemaTable.Rows)
                            {
                                var nombre = row["ColumnName"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(nombre))
                                {
                                    columnas.Add(nombre.Trim());
                                }
                            }
                        }

                        decimal ObtenerDecimal(string columna)
                        {
                            if (!columnas.Contains(columna) || reader[columna] == DBNull.Value)
                                return 0m;

                            var valor = reader[columna];

                            switch (valor)
                            {
                                case decimal decValor:
                                    return decValor;
                                case double doubleValor:
                                    return Convert.ToDecimal(doubleValor);
                                case float floatValor:
                                    return Convert.ToDecimal(floatValor);
                                case int intValor:
                                    return intValor;
                                case long longValor:
                                    return longValor;
                                case short shortValor:
                                    return shortValor;
                            }

                            var texto = valor.ToString();
                            if (decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultadoInv))
                            {
                                return resultadoInv;
                            }

                            return decimal.TryParse(texto, NumberStyles.Any, CultureInfo.CurrentCulture, out var resultadoActual)
                                ? resultadoActual
                                : 0m;
                        }

                        int ObtenerEntero(string columna)
                        {
                            if (!columnas.Contains(columna) || reader[columna] == DBNull.Value)
                                return 0;

                            var valor = reader[columna];

                            switch (valor)
                            {
                                case int intValor:
                                    return intValor;
                                case short shortValor:
                                    return shortValor;
                                case long longValor:
                                    return Convert.ToInt32(longValor);
                            }

                            var texto = valor.ToString();
                            return int.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultadoInv)
                                ? resultadoInv
                                : int.TryParse(texto, NumberStyles.Any, CultureInfo.CurrentCulture, out var resultadoActual)
                                    ? resultadoActual
                                    : 0;
                        }

                        DateTime? ObtenerFecha(string columna)
                        {
                            if (!columnas.Contains(columna) || reader[columna] == DBNull.Value)
                                return null;
                            return Convert.ToDateTime(reader[columna]);
                        }

                        string ObtenerTexto(string columna)
                        {
                            if (!columnas.Contains(columna) || reader[columna] == DBNull.Value)
                                return string.Empty;
                            return reader[columna].ToString()?.Trim() ?? string.Empty;
                        }

                        while (reader.Read())
                        {
                            var capital = ObtenerDecimal("HI_CAPITAL");
                            var interes = ObtenerDecimal("HI_INTERES");
                            var comision = ObtenerDecimal("HI_COMISI");
                            var mora = ObtenerDecimal("HI_MORA");
                            var capitalInicial = ObtenerDecimal("CAPITAL_INI");
                            var interesInicial = ObtenerDecimal("INTERES_INI");

                            if (capital == 0 && capitalInicial > 0)
                            {
                                capital = capitalInicial;
                            }

                            if (interes == 0 && interesInicial > 0)
                            {
                                interes = interesInicial;
                            }

                            var totalSaldo = ObtenerDecimal("HI_SALDO");
                            if (totalSaldo == 0)
                            {
                                totalSaldo = capital + interes + comision + mora;
                            }

                            var fechaFactura = ObtenerFecha("hi_fecha") ?? DateTime.Now;

                            var cuenta = new CxCItem
                            {
                                Cuota = ObtenerTexto("hi_facafec"),
                                CodigoCliente = ObtenerTexto("cl_codigo"),
                                Contrato = ObtenerTexto("hi_contra"),
                                FechaFactura = fechaFactura,
                                FechaPago = ObtenerFecha("hi_fecpag"),
                                Capital = capital,
                                Interes = interes,
                                Comision = comision,
                                Mora = mora,
                                NombreCliente = ObtenerTexto("cl_nombre"),
                                Direccion = ObtenerTexto("cl_direc1"),
                                Zona = ObtenerTexto("zo_codigo"),
                                Moneda = ObtenerTexto("MO_CODIGO"),
                                Vendedor = ObtenerTexto("ve_codigo"),
                                DiasVencimiento = ObtenerEntero("dias"),
                                TotalR = totalSaldo,
                                Fecha = fechaFactura,
                                Factura = ObtenerTexto("hi_contra")
                            };

                            cuentas.Add(cuenta);
                        }
                    }
                }
            }

            return cuentas;
        }

        public DetalleCxC ObtenerDetalleCuenta(string numeroContrato, string codigoCliente)
        {
            var detalle = new DetalleCxC();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var queryEncabezado = @"
                    SELECT TOP 1
                        c.co_contra,
                        c.co_fecha,
                        c.mo_codigo,
                        c.ve_codigo,
                        c.ti_codigo,
                        c.co_canpag,
                        b.cl_codigo,
                        b.cl_nombre,
                        b.cl_direc1,
                        b.cl_tele,
                        b.zo_codigo,
                        d.ti_descri
                    FROM prbdheco as c
                    LEFT JOIN prbdclie as b ON c.cl_codigo = b.cl_codigo
                    LEFT JOIN prbdtipocontrato as d ON c.ti_codigo = d.ti_codigo
                    WHERE c.co_contra = @Contrato AND b.cl_codigo = @Cliente
                ";

                using (var cmdEncabezado = new SqlCommand(queryEncabezado, connection))
                {
                    cmdEncabezado.Parameters.AddWithValue("@Contrato", numeroContrato);
                    cmdEncabezado.Parameters.AddWithValue("@Cliente", codigoCliente);

                    using (var reader = cmdEncabezado.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            detalle.Contrato = reader["co_contra"]?.ToString() ?? "";
                            detalle.FechaContrato = reader["co_fecha"] != DBNull.Value
                                ? Convert.ToDateTime(reader["co_fecha"])
                                : DateTime.Now;
                            detalle.Moneda = reader["mo_codigo"]?.ToString() ?? "";
                            detalle.Vendedor = reader["ve_codigo"]?.ToString() ?? "";
                            detalle.TipoContrato = reader["ti_descri"]?.ToString() ?? "";
                            detalle.CantidadCuotas = reader["co_canpag"] != DBNull.Value
                                ? Convert.ToInt32(reader["co_canpag"])
                                : 0;
                            // Valores de contrato no disponibles en este esquema actual
                            detalle.ValorContrato = 0;
                            detalle.InteresTotal = 0;

                            detalle.ClienteCodigo = reader["cl_codigo"]?.ToString() ?? "";
                            detalle.ClienteNombre = reader["cl_nombre"]?.ToString() ?? "";
                            detalle.ClienteRnc = "";
                            detalle.ClienteDireccion = reader["cl_direc1"]?.ToString() ?? "";
                            detalle.ClienteTelefono = reader["cl_tele"]?.ToString() ?? "";
                            detalle.Zona = reader["zo_codigo"]?.ToString() ?? "";
                        }
                    }
                }

                var queryHistorial = @"
                    SELECT 
                        a.hi_facafec,
                        a.hi_fecha,
                        a.hi_fecpag,
                        a.HI_CAPITAL,
                        a.HI_INTERES,
                        a.HI_COMISI,
                        a.hi_mora,
                        DATEDIFF(DAY, a.hi_fecha, GETDATE()) as dias_vencimiento
                    FROM prbdhis as a
                    WHERE a.hi_contra = @Contrato
                    ORDER BY a.hi_facafec
                ";

                using (var cmdHistorial = new SqlCommand(queryHistorial, connection))
                {
                    cmdHistorial.Parameters.AddWithValue("@Contrato", numeroContrato);

                    using (var reader = cmdHistorial.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var cuota = new CuotaCxC
                            {
                                Cuota = reader["hi_facafec"]?.ToString() ?? "",
                                FechaVencimiento = reader["hi_fecha"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["hi_fecha"])
                                    : DateTime.Now,
                                FechaPago = reader["hi_fecpag"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["hi_fecpag"])
                                    : (DateTime?)null,
                                Capital = reader["HI_CAPITAL"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["HI_CAPITAL"])
                                    : 0,
                                Interes = reader["HI_INTERES"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["HI_INTERES"])
                                    : 0,
                                Comision = reader["HI_COMISI"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["HI_COMISI"])
                                    : 0,
                                Mora = reader["hi_mora"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["hi_mora"])
                                    : 0,
                                DiasVencimiento = reader["dias_vencimiento"] != DBNull.Value
                                    ? Convert.ToInt32(reader["dias_vencimiento"])
                                    : 0
                            };

                            detalle.Cuotas.Add(cuota);
                        }
                    }
                }
            }

            return detalle;
        }

        public DetalleFacturaViewModel ObtenerDetalleFacturaCxC(string numeroContrato, string codigoCliente)
        {
            // Reutilizamos el detalle ya existente y lo mapeamos al layout del modal de ventas
            var detalleCxC = ObtenerDetalleCuenta(numeroContrato, codigoCliente);

            var vm = new DetalleFacturaViewModel
            {
                Factura = detalleCxC.Contrato,
                Fecha = detalleCxC.FechaContrato,
                Vendedor = detalleCxC.Vendedor,
                Moneda = detalleCxC.Moneda,
                Tasa = 1m, // Si no hay tasa disponible para CxC, dejamos 1

                ClienteCodigo = detalleCxC.ClienteCodigo,
                ClienteNombre = detalleCxC.ClienteNombre,
                ClienteRnc = detalleCxC.ClienteRnc,
                ClienteDireccion = detalleCxC.ClienteDireccion,
                ClienteTelefono = detalleCxC.ClienteTelefono
            };

            // Mapear cuotas como "productos" para aprovechar el mismo modal
            decimal montoBruto = 0m;
            foreach (var cuota in detalleCxC.Cuotas)
            {
                var totalCuota = cuota.Total;
                montoBruto += totalCuota;

                vm.Productos.Add(new ProductoFacturaItem
                {
                    CodigoFicha = cuota.Cuota,
                    Cantidad = 1,
                    UnidadMedida = "CUOTA",
                    Descripcion = $"Cuota {cuota.Cuota} - Vence: {cuota.FechaVencimiento:dd/MM/yyyy}",
                    PrecioUnitario = totalCuota,
                    Itbis = 0,
                    Total = totalCuota
                });
            }

            vm.MontoBruto = montoBruto;
            vm.Impuesto17 = 0;
            vm.Itbis18 = 0;
            vm.Descuento = 0;
            vm.Subtotal = montoBruto;
            vm.TotalItbis = 0;
            vm.MontoNeto = montoBruto;

            return vm;
        }
    }
}