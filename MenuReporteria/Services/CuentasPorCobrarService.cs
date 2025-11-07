using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.Extensions.Configuration;
using MenuReporteria.Models;

namespace MenuReporteria.Services
{
    public class CuentasPorCobrarService
    {
        private readonly string _connectionString;

        public CuentasPorCobrarService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public List<string> ObtenerZonas()
        {
            var zonas = new List<string>();
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
                            zonas.Add(reader["zo_codigo"].ToString());
                        }
                    }
                }
            }
            return zonas;
        }

        public List<string> ObtenerClientes()
        {
            var clientes = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT DISTINCT cl_codigo FROM prbdclie WHERE cl_codigo IS NOT NULL ORDER BY cl_codigo";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            clientes.Add(reader["cl_codigo"].ToString());
                        }
                    }
                }
            }
            return clientes;
        }

        public List<string> ObtenerVendedores()
        {
            var vendedores = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT DISTINCT ve_codigo FROM prbdheco WHERE ve_codigo IS NOT NULL ORDER BY ve_codigo";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vendedores.Add(reader["ve_codigo"].ToString());
                        }
                    }
                }
            }
            return vendedores;
        }

        public List<string> ObtenerMonedas()
        {
            var monedas = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT DISTINCT mo_codigo FROM prbdheco WHERE mo_codigo IS NOT NULL ORDER BY mo_codigo";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            monedas.Add(reader["mo_codigo"].ToString());
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
                var query = @"
                SELECT 
                    a.hi_facafec,
                    a.cl_codigo,
                    a.hi_contra,
                    a.hi_fecha,
                    a.hi_fecpag,
                    a.HI_CAPITAL,
                    a.HI_INTERES,
                    a.HI_COMISI,
                    a.hi_mora,
                    ISNULL(b.cl_nombre, '') as cl_nombre,
                    ISNULL(b.cl_direc1, '') as cl_direc1,
                    ISNULL(b.zo_codigo, '') as zo_codigo,
                    ISNULL(a.MO_CODIGO, '') as mo_codigo,
                    ISNULL(a.ve_codigo, '') as ve_codigo,
                    DATEDIFF(DAY, a.hi_fecha, GETDATE()) as dias,
                    (ISNULL(a.HI_CAPITAL, 0) + ISNULL(a.HI_INTERES, 0) + ISNULL(a.HI_COMISI, 0) + ISNULL(a.hi_mora, 0)) as total_saldo
                FROM prbdhis as a
                LEFT JOIN prbdclie as b ON a.cl_codigo = b.cl_codigo
                WHERE 1=1
            ";

                if (filtros.FechaDesde.HasValue)
                    query += " AND CAST(a.hi_fecha AS DATE) >= @FechaDesde";

                if (filtros.FechaHasta.HasValue)
                    query += " AND CAST(a.hi_fecha AS DATE) <= @FechaHasta";

                if (!string.IsNullOrEmpty(filtros.Zona))
                    query += " AND b.zo_codigo = @Zona";

                if (!string.IsNullOrEmpty(filtros.Cliente))
                    query += " AND a.cl_codigo = @Cliente";

                if (!string.IsNullOrEmpty(filtros.Vendedor))
                    query += " AND c.ve_codigo = @Vendedor";

                if (!string.IsNullOrEmpty(filtros.FacturaDesde))
                    query += " AND a.hi_contra >= @FacturaDesde";

                if (!string.IsNullOrEmpty(filtros.FacturaHasta))
                    query += " AND a.hi_contra <= @FacturaHasta";

                if (filtros.CuotaDesde.HasValue)
                    query += @" AND CONVERT(INT, REPLACE(RTRIM(a.hi_facafec), '/' + RIGHT(RTRIM(a.hi_facafec), 
                        LEN(LTRIM(RTRIM(STR(ISNULL(c.co_canpag, '0')))))), '')) >= @CuotaDesde";

                if (filtros.CuotaHasta.HasValue)
                    query += @" AND CONVERT(INT, REPLACE(RTRIM(a.hi_facafec), '/' + RIGHT(RTRIM(a.hi_facafec), 
                        LEN(LTRIM(RTRIM(STR(ISNULL(c.co_canpag, '0')))))), '')) <= @CuotaHasta";

                if (!string.IsNullOrEmpty(filtros.Moneda))
                    query += " AND a.mo_codigo = @Moneda";

                if (filtros.OrdenMoneda == "opcion1")
                    query += " ORDER BY a.mo_codigo, a.hi_fecha, a.hi_contra";
                else if (filtros.OrdenMoneda == "opcion2")
                    query += " ORDER BY a.mo_codigo, a.hi_contra, a.hi_fecha";
                else
                    query += " ORDER BY b.cl_nombre, a.hi_fecha, a.hi_contra";

                using (var command = new SqlCommand(query, connection))
                {
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
                        while (reader.Read())
                        {
                            cuentas.Add(new CxCItem
                            {
                                Cuota = reader["hi_facafec"]?.ToString() ?? "",
                                CodigoCliente = reader["cl_codigo"]?.ToString() ?? "",
                                Contrato = reader["hi_contra"]?.ToString() ?? "",
                                FechaFactura = reader["hi_fecha"] != DBNull.Value
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
                                NombreCliente = reader["cl_nombre"]?.ToString() ?? "",
                                Direccion = reader["cl_direc1"]?.ToString() ?? "",
                                Zona = reader["zo_codigo"]?.ToString() ?? "",
                                Moneda = reader["mo_codigo"]?.ToString() ?? "RD",
                                Vendedor = reader["ve_codigo"]?.ToString() ?? "",
                                DiasVencimiento = reader["dias"] != DBNull.Value
                                    ? Convert.ToInt32(reader["dias"])
                                    : 0,
                                TotalR = reader["total_saldo"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["total_saldo"])
                                    : 0,
                                Fecha = reader["hi_fecha"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["hi_fecha"])
                                    : DateTime.Now,
                                Factura = reader["hi_contra"]?.ToString() ?? ""
                            });
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
                        c.co_valor,
                        c.co_interes,
                        b.cl_codigo,
                        b.cl_nombre,
                        b.cl_rnc,
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
                            detalle.ValorContrato = reader["co_valor"] != DBNull.Value
                                ? Convert.ToDecimal(reader["co_valor"])
                                : 0;
                            detalle.InteresTotal = reader["co_interes"] != DBNull.Value
                                ? Convert.ToDecimal(reader["co_interes"])
                                : 0;

                            detalle.ClienteCodigo = reader["cl_codigo"]?.ToString() ?? "";
                            detalle.ClienteNombre = reader["cl_nombre"]?.ToString() ?? "";
                            detalle.ClienteRnc = reader["cl_rnc"]?.ToString() ?? "";
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
    }
}