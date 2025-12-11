using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MenuReporteria.Models;
using System.Linq;
using System.Text;
using OfficeOpenXml;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace MenuReporteria.Services
{
    public class ReporteVentasChasisService
    {
        private readonly string _connectionString;

        public ReporteVentasChasisService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // MÉTODO PARA OBTENER VENDEDORES
        public List<string> ObtenerVendedores()
        {
            var vendedores = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT VE_CODIGO, ISNULL(VE_NOMBRE, '') AS VE_NOMBRE FROM prbdvend ORDER BY VE_CODIGO";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var codigo = reader["VE_CODIGO"].ToString().Trim();
                            var nombre = reader["VE_NOMBRE"].ToString().Trim();
                            var texto = string.IsNullOrWhiteSpace(nombre) ? codigo : $"{nombre}";
                            if (!string.IsNullOrWhiteSpace(codigo))
                            {
                                vendedores.Add(texto);
                            }
                        }
                    }
                }
            }
            return vendedores;
        }

        // MÉTODO PARA OBTENER ALMACENES
        public List<string> ObtenerAlmacenes()
        {
            var almacenes = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT DISTINCT al_codigo FROM IVBDDEPE WHERE al_codigo IS NOT NULL ORDER BY al_codigo";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            almacenes.Add(reader["al_codigo"].ToString());
                        }
                    }
                }
            }
            return almacenes;
        }

        // MÉTODO PARA OBTENER CAJAS
        public List<string> ObtenerCajas()
        {
            var cajas = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT DISTINCT he_Caja FROM IVBDHEPE WHERE he_Caja IS NOT NULL ORDER BY he_Caja";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cajas.Add(reader["he_Caja"].ToString());
                        }
                    }
                }
            }
            return cajas;
        }

        // MÉTODO PRINCIPAL CON TODOS LOS FILTROS
        public List<VentaChasisItem> ObtenerVentasChasisPorFiltro(FiltroVentasChasis filtros)
        {
            var ventas = new List<VentaChasisItem>();
            var queryBuilder = new StringBuilder();

            // Query base para FACTURAS (F)
            var queryFacturas = @"
                SELECT 
                    a.ar_codigo,
                    A.de_fecha AS Fecha,
                    'F' AS tipo,
                    A.de_tipo,
                    A.de_ncf,
                    A.de_factura,
                    A.al_codigo,
                    A.de_cantid,
                    A.DE_PRECIO,
                    a.de_itbis,
                    a.de_costocif,
                    A.ar_codigo2,
                    ISNULL(b.ar_CHASIS,'') AS AR_CHASIS,
                    ISNULL(B.AR_descri,'') AS AR_descri,
                    ISNULL(B.DE_CODIGO,'') AS DE_CODIGO,
                    ISNULL(B.MA_CODIGO,'') AS MA_CODIGO,
                    ISNULL(c.he_neto,0) AS he_neto,
                    ISNULL(b.ar_marca,'') as ar_marca,
                    ISNULL(b.ar_mODveh,'') as ar_mODELO,
                    ISNULL(b.ar_COLOR,'') as ar_COLOR,
                    ISNULL(b.ar_motor,'') as ar_motor,
                    ISNULL(b.ar_placa,'') as ar_placa,
                    ISNULL(b.ar_ANO,'') as ar_ANO,
                    ISNULL(b.ar_refer1,'') as ar_refer2,
                    ISNULL(b.ar_uso,'') as ar_uso,
                    ISNULL(b.ar_origen,'') as ar_origen,
                    ISNULL(c.he_rnc,'') as he_rnc,
                    a.CL_CODIGO,
                    ISNULL(D.CL_NOMBRE,'') CL_NOMBRE,
                    ISNULL(C.MO_CODIGO,'') MO_CODIGO,
                    ISNULL(C.HE_TASA,0) HE_TASA,
                    ISNULL(c.ve_codigo,'') ve_codigo,
                    ISNULL(c.HE_NCF,'') HE_NCF
                FROM IVBDDEPE AS A 
                LEFT JOIN orlando.dbo.IVBDVEHICULO B ON a.ar_codigo=B.AR_CODIGO
                LEFT JOIN IVBDHEPE C ON A.DE_FACTURA=C.HE_FACTURA
                LEFT JOIN PRBDCLIE D ON A.CL_CODIGO=D.CL_CODIGO AND D.COD_SUCU='1'
                WHERE 1=1 AND A.DE_PRECIO>=0 AND C.he_progra=0
            ";

            // Query para COGE (C)
            var queryCoge = @"
                SELECT 
                    a.ar_codigo,
                    A.de_fecha AS Fecha,
                    'C' AS tipo,
                    A.de_tipo,
                    A.de_ncf,
                    A.de_factura,
                    A.al_codigo,
                    A.de_cantid,
                    A.DE_PRECIO,
                    a.de_itbis,
                    a.de_costocif,
                    A.ar_codigo2,
                    ISNULL(b.ar_CHASIS,'') AS AR_CHASIS,
                    ISNULL(B.AR_descri,'') AS AR_descri,
                    ISNULL(B.DE_CODIGO,'') AS DE_CODIGO,
                    ISNULL(B.MA_CODIGO,'') AS MA_CODIGO,
                    ISNULL(c.he_neto,0) AS he_neto,
                    ISNULL(b.ar_marca,'') as ar_marca,
                    ISNULL(b.ar_mODveh,'') as ar_mODELO,
                    ISNULL(b.ar_COLOR,'') as ar_COLOR,
                    ISNULL(b.ar_motor,'') as ar_motor,
                    ISNULL(b.ar_placa,'') as ar_placa,
                    ISNULL(b.ar_ANO,'') as ar_ANO,
                    ISNULL(b.ar_refer1,'') as ar_refer2,
                    ISNULL(b.ar_uso,'') as ar_uso,
                    ISNULL(b.ar_origen,'') as ar_origen,
                    ISNULL(c.he_rnc,'') as he_rnc,
                    a.CL_CODIGO,
                    ISNULL(D.CL_NOMBRE,'') CL_NOMBRE,
                    ISNULL(C.MO_CODIGO,'') MO_CODIGO,
                    ISNULL(C.HE_TASA,0) HE_TASA,
                    ISNULL(c.ve_codigo,'') ve_codigo,
                    ISNULL(c.HE_NCF,'') HE_NCF
                FROM IVBDDCOGE AS A 
                LEFT JOIN orlando.dbo.IVBDVEHICULO B ON a.ar_codigo=B.AR_CODIGO
                LEFT JOIN IVBDHCOGE C ON A.DE_FACTURA=C.HE_FACTURA
                LEFT JOIN PRBDCLIE D ON A.CL_CODIGO=D.CL_CODIGO AND D.COD_SUCU='1'
                WHERE 1=1 AND A.DE_PRECIO>=0 AND C.he_progra=0
            ";

            // Query para PREFACTURA (P)
            var queryPreFactura = @"
                SELECT 
                    a.ar_codigo,
                    A.de_fecha AS Fecha,
                    'P' AS tipo,
                    A.de_tipo,
                    A.de_ncf,
                    A.de_factura,
                    A.al_codigo,
                    A.de_cantid,
                    A.DE_PRECIO,
                    a.de_itbis,
                    a.de_costocif,
                    A.ar_codigo2,
                    ISNULL(b.ar_CHASIS,'') AS AR_CHASIS,
                    ISNULL(B.AR_descri,'') AS AR_descri,
                    ISNULL(B.DE_CODIGO,'') AS DE_CODIGO,
                    ISNULL(B.MA_CODIGO,'') AS MA_CODIGO,
                    ISNULL(c.he_neto,0) AS he_neto,
                    ISNULL(b.ar_marca,'') as ar_marca,
                    ISNULL(b.ar_mODveh,'') as ar_mODELO,
                    ISNULL(b.ar_COLOR,'') as ar_COLOR,
                    ISNULL(b.ar_motor,'') as ar_motor,
                    ISNULL(b.ar_placa,'') as ar_placa,
                    ISNULL(b.ar_ANO,'') as ar_ANO,
                    ISNULL(b.ar_refer1,'') as ar_refer2,
                    ISNULL(b.ar_uso,'') as ar_uso,
                    ISNULL(b.ar_origen,'') as ar_origen,
                    ISNULL(c.he_rnc,'') as he_rnc,
                    a.CL_CODIGO,
                    ISNULL(D.CL_NOMBRE,'') CL_NOMBRE,
                    ISNULL(C.MO_CODIGO,'') MO_CODIGO,
                    ISNULL(C.HE_TASA,0) HE_TASA,
                    ISNULL(c.ve_codigo,'') ve_codigo,
                    ISNULL(c.HE_NCF,'') HE_NCF
                FROM IVBDDEPREFACTURA AS A 
                LEFT JOIN orlando.dbo.IVBDVEHICULO B ON a.ar_codigo=B.AR_CODIGO
                LEFT JOIN IVBDHEPREFACTURA C ON A.DE_FACTURA=C.HE_FACTURA
                LEFT JOIN PRBDCLIE D ON A.CL_CODIGO=D.CL_CODIGO AND D.COD_SUCU='1'
                WHERE 1=1 AND A.DE_PRECIO>=0
            ";

            using (var connection = new SqlConnection(_connectionString))
            {
                // Construir query según TipoDocumento
                if (filtros.TipoDocumento == "Facturas")
                    queryBuilder.Append(queryFacturas);
                else if (filtros.TipoDocumento == "Coge")
                    queryBuilder.Append(queryCoge);
                else if (filtros.TipoDocumento == "PreFactura")
                    queryBuilder.Append(queryPreFactura);
                else // Todas
                {
                    queryBuilder.Append(queryFacturas);
                    queryBuilder.Append(" UNION ALL ");
                    queryBuilder.Append(queryCoge);
                    queryBuilder.Append(" UNION ALL ");
                    queryBuilder.Append(queryPreFactura);
                }

                // Aplicar filtros
                if (filtros.FechaDesde != DateTime.MinValue)
                    queryBuilder.Append(" AND A.de_fecha >= @FechaDesde");
                if (filtros.FechaHasta != DateTime.MinValue)
                    queryBuilder.Append(" AND A.de_fecha <= @FechaHasta");
                if (!string.IsNullOrEmpty(filtros.Cliente))
                    queryBuilder.Append(" AND (A.CL_CODIGO LIKE @Cliente OR D.CL_NOMBRE LIKE @Cliente)");
                if (!string.IsNullOrEmpty(filtros.Vendedor))
                {
                    var listaVendedores = filtros.Vendedor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                          .Select(v => v.Trim())
                                                          .ToList();
                    if (listaVendedores.Count > 0)
                    {
                        var parametrosIn = new List<string>();
                        for (int i = 0; i < listaVendedores.Count; i++)
                            parametrosIn.Add($"@Vendedor{i}");
                        queryBuilder.Append($" AND c.ve_codigo IN ({string.Join(",", parametrosIn)})");
                    }
                }
                if (!string.IsNullOrEmpty(filtros.Chasis))
                    queryBuilder.Append(" AND a.ar_codigo LIKE @Chasis");
                if (!string.IsNullOrEmpty(filtros.Marca))
                    queryBuilder.Append(" AND b.ar_marca LIKE @Marca");
                if (!string.IsNullOrEmpty(filtros.Modelo))
                    queryBuilder.Append(" AND b.ar_mODveh LIKE @Modelo");
                if (!string.IsNullOrEmpty(filtros.Color))
                    queryBuilder.Append(" AND b.ar_color LIKE @Color");
                if (!string.IsNullOrEmpty(filtros.Almacen))
                    queryBuilder.Append(" AND A.al_codigo = @Almacen");
                if (!string.IsNullOrEmpty(filtros.Factura))
                    queryBuilder.Append(" AND A.DE_FACTURA = @Factura");
                if (!string.IsNullOrEmpty(filtros.NCFDesde))
                    queryBuilder.Append(" AND A.de_ncf >= @NCFDesde");
                if (!string.IsNullOrEmpty(filtros.NCFHasta))
                    queryBuilder.Append(" AND A.de_NCF <= @NCFHasta");

                // Filtro de anuladas
                if (!filtros.IncluirAnuladas)
                    queryBuilder.Append(" AND a.de_cantid > 0");
                else
                    queryBuilder.Append(" AND a.de_cantid = 0");

                // Ordenamiento
                queryBuilder.Append(" ORDER BY A.DE_FECHA, a.de_factura, ar_marca, ar_mODELO, ar_COLOR");

                using (var command = new SqlCommand(queryBuilder.ToString(), connection))
                {
                    // Agregar parámetros
                    command.Parameters.AddWithValue("@FechaDesde", filtros.FechaDesde.Date);
                    command.Parameters.AddWithValue("@FechaHasta", filtros.FechaHasta.Date);
                    if (!string.IsNullOrEmpty(filtros.Cliente))
                        command.Parameters.AddWithValue("@Cliente", "%" + filtros.Cliente + "%");
                    if (!string.IsNullOrEmpty(filtros.Vendedor))
                    {
                        var listaVendedores = filtros.Vendedor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                              .Select(v => v.Contains(" - ") ? v.Split(" - ")[0].Trim() : v)
                                                              .ToList();
                        for (int i = 0; i < listaVendedores.Count; i++)
                            command.Parameters.AddWithValue($"@Vendedor{i}", listaVendedores[i]);
                    }
                    if (!string.IsNullOrEmpty(filtros.Chasis))
                        command.Parameters.AddWithValue("@Chasis", "%" + filtros.Chasis + "%");
                    if (!string.IsNullOrEmpty(filtros.Marca))
                        command.Parameters.AddWithValue("@Marca", "%" + filtros.Marca + "%");
                    if (!string.IsNullOrEmpty(filtros.Modelo))
                        command.Parameters.AddWithValue("@Modelo", "%" + filtros.Modelo + "%");
                    if (!string.IsNullOrEmpty(filtros.Color))
                        command.Parameters.AddWithValue("@Color", "%" + filtros.Color + "%");
                    if (!string.IsNullOrEmpty(filtros.Almacen))
                        command.Parameters.AddWithValue("@Almacen", filtros.Almacen);
                    if (!string.IsNullOrEmpty(filtros.Factura))
                        command.Parameters.AddWithValue("@Factura", filtros.Factura);
                    if (!string.IsNullOrEmpty(filtros.NCFDesde))
                        command.Parameters.AddWithValue("@NCFDesde", filtros.NCFDesde);
                    if (!string.IsNullOrEmpty(filtros.NCFHasta))
                        command.Parameters.AddWithValue("@NCFHasta", filtros.NCFHasta);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ventas.Add(new VentaChasisItem
                            {
                                Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                                Tipo = reader["tipo"]?.ToString() ?? "",
                                ControlNro = reader["de_factura"]?.ToString() ?? "",
                                Almacen = reader["al_codigo"]?.ToString() ?? "",
                                Vendedor = reader["ve_codigo"]?.ToString() ?? "",
                                NumeroChasis = reader["ar_CHASIS"]?.ToString() ?? "",
                                Marca = reader["ar_marca"]?.ToString() ?? "",
                                Modelo = reader["ar_mODELO"]?.ToString() ?? "",
                                Color = reader["ar_COLOR"]?.ToString() ?? "",
                                Ano = reader["ar_ANO"]?.ToString() ?? "",
                                Precio = reader["DE_PRECIO"] != DBNull.Value ? Convert.ToDecimal(reader["DE_PRECIO"]) : 0,
                                CodigoCliente = reader["CL_CODIGO"]?.ToString() ?? "",
                                NombreCliente = reader["CL_NOMBRE"]?.ToString() ?? "",
                                Moneda = reader["MO_CODIGO"]?.ToString() ?? "",
                                Tasa = reader["HE_TASA"] != DBNull.Value ? Convert.ToDecimal(reader["HE_TASA"]) : 0,
                                NCF = reader["HE_NCF"]?.ToString() ?? "",
                                Factura = reader["de_factura"]?.ToString() ?? "",
                                Motor = reader["ar_motor"]?.ToString() ?? "",
                                Placa = reader["ar_placa"]?.ToString() ?? "",
                                Uso = reader["ar_uso"]?.ToString() ?? "",
                                Origen = reader["ar_origen"]?.ToString() ?? "",
                                RNC = reader["he_rnc"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return ventas;
        }

        // Método para búsqueda paginada de clientes (igual que los demás)
        public class ResultadoClientesPaginados
        {
            public List<dynamic> Clientes { get; set; } = new List<dynamic>();
            public int Total { get; set; }
            public int TotalPaginas { get; set; }
        }

        public ResultadoClientesPaginados ObtenerClientesPaginados(string filtro, int pagina, int registrosPorPagina)
        {
            var resultado = new ResultadoClientesPaginados();
            filtro = filtro?.Trim() ?? "";

            using (var connection = new SqlConnection(_connectionString))
            {
                var queryTotal = @"
                    SELECT COUNT(*) as total
                    FROM CCBDCLIE
                    WHERE (cl_codigo LIKE @Filtro OR cl_nombre LIKE @Filtro)
                      AND cl_codigo IS NOT NULL AND cl_codigo <> ''
                      AND cl_nombre IS NOT NULL AND cl_nombre <> ''
                ";

                using (var command = new SqlCommand(queryTotal, connection))
                {
                    command.Parameters.AddWithValue("@Filtro", "%" + filtro + "%");
                    connection.Open();
                    resultado.Total = (int)command.ExecuteScalar();
                    connection.Close();
                }

                resultado.TotalPaginas = (int)Math.Ceiling((double)resultado.Total / registrosPorPagina);
                if (pagina < 1) pagina = 1;
                if (pagina > resultado.TotalPaginas && resultado.TotalPaginas > 0) pagina = resultado.TotalPaginas;

                int offset = (pagina - 1) * registrosPorPagina;

                var query = @"
                    SELECT cl_codigo as codigo, cl_nombre as nombre
                    FROM CCBDCLIE
                    WHERE (cl_codigo LIKE @Filtro OR cl_nombre LIKE @Filtro)
                      AND cl_codigo IS NOT NULL AND cl_codigo <> ''
                      AND cl_nombre IS NOT NULL AND cl_nombre <> ''
                    ORDER BY cl_nombre
                    OFFSET @Offset ROWS
                    FETCH NEXT @RegistrosPorPagina ROWS ONLY
                ";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Filtro", "%" + filtro + "%");
                    command.Parameters.AddWithValue("@Offset", offset);
                    command.Parameters.AddWithValue("@RegistrosPorPagina", registrosPorPagina);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resultado.Clientes.Add(new
                            {
                                codigo = reader["codigo"].ToString(),
                                nombre = reader["nombre"].ToString()
                            });
                        }
                    }
                }
            }

            return resultado;
        }
    }
}