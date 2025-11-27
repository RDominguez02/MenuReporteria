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

        // --- NUEVO MÉTODO PARA RELACIÓN DE PAGOS ---
        public List<PagoCxCItem> ObtenerRelacionPagos(string clienteCodigo)
        {
            var pagos = new List<PagoCxCItem>();
            using (var connection = new SqlConnection(_connectionString))
            {
                // Consultamos la tabla de recibos PRBDHERE filtrando por cliente
                var query = @"SELECT 
                                HE_FECHA,
                                HE_DOCUM,
                                HE_OBSERV,
                                MO_CODIGO,
                                HE_MONTO 
                              FROM PRBDHERE 
                              WHERE CL_CODIGO = @Cliente 
                              ORDER BY HE_FECHA DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Cliente", clienteCodigo);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            pagos.Add(new PagoCxCItem
                            {
                                Fecha = reader["HE_FECHA"] != DBNull.Value ? Convert.ToDateTime(reader["HE_FECHA"]) : DateTime.MinValue,
                                Documento = reader["HE_DOCUM"]?.ToString() ?? "",
                                Observacion = reader["HE_OBSERV"]?.ToString() ?? "",
                                Moneda = reader["MO_CODIGO"]?.ToString() ?? "",
                                Monto = reader["HE_MONTO"] != DBNull.Value ? Convert.ToDecimal(reader["HE_MONTO"]) : 0
                            });
                        }
                    }
                }
            }
            return pagos;
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
                // CAMBIO: Consultamos prbdvend en lugar de prbdheco
                // Obtenemos Código y Nombre, asegurando que no vengan nulos
                var query = @"SELECT VE_CODIGO, ISNULL(VE_NOMBRE, '') AS VE_NOMBRE
                        FROM prbdvend
                        WHERE VE_CODIGO IS NOT NULL 
                          AND VE_CODIGO <> ''
                        ORDER BY VE_CODIGO"; // Puedes cambiar a ORDER BY VE_NOMBRE si prefieres orden alfabético

                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var codigo = reader["VE_CODIGO"].ToString().Trim();
                            var nombre = reader["VE_NOMBRE"].ToString().Trim();

                            if (!string.IsNullOrWhiteSpace(codigo))
                            {
                                vendedores.Add(new FiltroOpcion
                                {
                                    Valor = codigo,
                                    // CAMBIO: Formato "CODIGO - NOMBRE" para mejor visualización
                                    Texto = string.IsNullOrWhiteSpace(nombre) ? codigo : $"{nombre}"
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

                // --- INICIO CORRECCIÓN CLIENTE ---
                if (!string.IsNullOrWhiteSpace(filtros.Cliente))
                {
                    // Busca coincidencia parcial en Código O Nombre
                    queryBuilder.AppendLine(" AND (a.cl_codigo LIKE @Cliente OR b.cl_nombre LIKE @Cliente)");
                }
                // --- FIN CORRECCIÓN CLIENTE ---

                // Manejo de Vendedores Múltiples
                if (!string.IsNullOrWhiteSpace(filtros.Vendedor))
                {
                    // Separamos los códigos por coma
                    var listaVendedores = filtros.Vendedor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                          .Select(v => v.Trim())
                                                          .ToList();

                    if (listaVendedores.Count > 0)
                    {
                        var parametrosIn = new List<string>();
                        for (int i = 0; i < listaVendedores.Count; i++)
                        {
                            parametrosIn.Add($"@Vendedor{i}");
                        }

                        // Genera: AND c.ve_codigo IN (@Vendedor0, @Vendedor1, @Vendedor2)
                        queryBuilder.AppendLine($" AND c.ve_codigo IN ({string.Join(",", parametrosIn)})");
                    }
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

                    // --- INICIO CORRECCIÓN PARAMETRO ---
                    if (!string.IsNullOrEmpty(filtros.Cliente))
                        command.Parameters.AddWithValue("@Cliente", "%" + filtros.Cliente.Trim() + "%");
                    // --- FIN CORRECCIÓN PARAMETRO ---

                    if (!string.IsNullOrEmpty(filtros.Vendedor))
                    {
                        var listaVendedores = filtros.Vendedor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                              .Select(v => v.Trim())
                                                              .ToList();
                        for (int i = 0; i < listaVendedores.Count; i++)
                        {
                            command.Parameters.AddWithValue($"@Vendedor{i}", listaVendedores[i]);
                        }
                    }
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

                        // ... (El resto de tus funciones auxiliares ObtenerDecimal, etc. se mantienen igual) ...
                        // Para simplificar la respuesta, asumo que tienes el resto del código de lectura igual.
                        // Aquí solo incluyo las funciones para que el código sea válido si copias todo.

                        decimal ObtenerDecimal(string columna)
                        {
                            if (!columnas.Contains(columna) || reader[columna] == DBNull.Value)
                                return 0m;
                            // ... (Misma lógica existente) ...
                            var valor = reader[columna];
                            if (valor is decimal d) return d;
                            if (valor is double db) return (decimal)db;
                            if (valor is float f) return (decimal)f;
                            if (valor is int i) return i;
                            if (valor is long l) return l;
                            return decimal.TryParse(valor.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 0m;
                        }

                        int ObtenerEntero(string columna)
                        {
                            if (!columnas.Contains(columna) || reader[columna] == DBNull.Value) return 0;
                            var valor = reader[columna];
                            if (valor is int i) return i;
                            if (valor is short s) return s;
                            return int.TryParse(valor.ToString(), out var r) ? r : 0;
                        }

                        DateTime? ObtenerFecha(string columna)
                        {
                            if (!columnas.Contains(columna) || reader[columna] == DBNull.Value) return null;
                            return Convert.ToDateTime(reader[columna]);
                        }

                        string ObtenerTexto(string columna)
                        {
                            if (!columnas.Contains(columna) || reader[columna] == DBNull.Value) return string.Empty;
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

                            if (capital == 0 && capitalInicial > 0) capital = capitalInicial;
                            if (interes == 0 && interesInicial > 0) interes = interesInicial;

                            var totalSaldo = ObtenerDecimal("HI_SALDO");
                            if (totalSaldo == 0) totalSaldo = capital + interes + comision + mora;

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

        public ResultadoClientesPaginados ObtenerClientesPaginados(string filtro, int pagina, int registrosPorPagina)
        {
            var resultado = new ResultadoClientesPaginados();
            filtro = filtro?.Trim() ?? "";

            using (var connection = new SqlConnection(_connectionString))
            {
                // Primero obtener el total de registros
                var queryTotal = @"
            SELECT COUNT(*) as total
            FROM CCBDCLIE
            WHERE (cl_codigo LIKE @Filtro OR cl_nombre LIKE @Filtro)
              AND cl_codigo IS NOT NULL 
              AND cl_codigo <> ''
              AND cl_nombre IS NOT NULL
              AND cl_nombre <> ''
        ";

                using (var command = new SqlCommand(queryTotal, connection))
                {
                    command.Parameters.AddWithValue("@Filtro", "%" + filtro + "%");
                    connection.Open();
                    resultado.Total = (int)command.ExecuteScalar();
                    connection.Close();
                }

                // Calcular total de páginas
                resultado.TotalPaginas = (int)Math.Ceiling((double)resultado.Total / registrosPorPagina);

                // Validar página
                if (pagina < 1) pagina = 1;
                if (pagina > resultado.TotalPaginas && resultado.TotalPaginas > 0) pagina = resultado.TotalPaginas;

                // Obtener registros paginados
                int offset = (pagina - 1) * registrosPorPagina;

                var query = @"
            SELECT 
                cl_codigo as codigo,
                cl_nombre as nombre
            FROM CCBDCLIE
            WHERE (cl_codigo LIKE @Filtro OR cl_nombre LIKE @Filtro)
              AND cl_codigo IS NOT NULL 
              AND cl_codigo <> ''
              AND cl_nombre IS NOT NULL
              AND cl_nombre <> ''
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
                Tasa = 1m,

                ClienteCodigo = detalleCxC.ClienteCodigo,
                ClienteNombre = detalleCxC.ClienteNombre,
                ClienteRnc = detalleCxC.ClienteRnc,
                ClienteDireccion = detalleCxC.ClienteDireccion,
                ClienteTelefono = detalleCxC.ClienteTelefono
            };

            // Obtener productos reales del contrato de CxC
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var queryProductos = @"
            SELECT 
                A.DE_CANTID,
                A.DE_UNIDAD,
                A.AR_CODIGO,
                A.DE_DESCRI,
                A.DE_PRECIO,
                A.DE_ITBIS18,
                A.DE_ITBIS08,
                (A.DE_CANTID * A.DE_PRECIO) as Total,
                A.ar_chasis,
                A.ar_ano,
                A.ar_motor,
                A.ar_modelo,
                A.ar_color,
                A.ar_placa,
                A.ar_matri,
                A.ar_marca,
                ISNULL(B.AR_DESCRI, '') as AR_DESCRI
            FROM IVBDDEPE A
            LEFT JOIN orlando.dbo.IVBDVEHICULO B ON A.AR_CODIGO = B.AR_CODIGO
            WHERE A.DE_FACTURA = @Factura
            ORDER BY A.de_id
        ";

                using (var cmdProductos = new SqlCommand(queryProductos, connection))
                {
                    cmdProductos.Parameters.AddWithValue("@Factura", numeroContrato);

                    using (var reader = cmdProductos.ExecuteReader())
                    {
                        decimal montoBruto = 0m;
                        decimal totalItbis = 0m;

                        while (reader.Read())
                        {
                            var producto = new ProductoFacturaItem
                            {
                                CodigoFicha = reader["AR_CODIGO"]?.ToString() ?? "",
                                Cantidad = reader["DE_CANTID"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["DE_CANTID"])
                                    : 0,
                                UnidadMedida = reader["DE_UNIDAD"]?.ToString() ?? "UD",
                                Descripcion = reader["DE_DESCRI"]?.ToString() ??
                                             reader["AR_DESCRI"]?.ToString() ?? "",
                                PrecioUnitario = reader["DE_PRECIO"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["DE_PRECIO"])
                                    : 0,
                                Itbis = reader["DE_ITBIS18"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["DE_ITBIS18"])
                                    : (reader["DE_ITBIS08"] != DBNull.Value
                                        ? Convert.ToDecimal(reader["DE_ITBIS08"])
                                        : 0),
                                Total = reader["Total"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["Total"])
                                    : 0,
                                // Datos del chasis
                                Chasis = reader["ar_chasis"]?.ToString() ?? "",
                                Ano = reader["ar_ano"]?.ToString() ?? "",
                                Motor = reader["ar_motor"]?.ToString() ?? "",
                                Modelo = reader["ar_modelo"]?.ToString() ?? "",
                                Color = reader["ar_color"]?.ToString() ?? "",
                                Placa = reader["ar_placa"]?.ToString() ?? "",
                                Matricula = reader["ar_matri"]?.ToString() ?? "",
                                Marca = reader["ar_marca"]?.ToString() ?? ""
                            };

                            vm.Productos.Add(producto);
                            montoBruto += producto.Total;
                            totalItbis += producto.Itbis;
                        }

                        // Calcular totales
                        vm.MontoBruto = montoBruto;
                        vm.Impuesto17 = 0;
                        vm.Itbis18 = totalItbis;
                        vm.Descuento = 0;
                        vm.Subtotal = montoBruto;
                        vm.TotalItbis = totalItbis;
                        vm.MontoNeto = montoBruto + totalItbis;
                    }
                }
            }

            return vm;
        }
    }
}

// Agregar estos métodos a ReporteVentasService.cs

/// <summary>
/// Clase auxiliar para resultados paginados de clientes
/// </summary>
public class ResultadoClientesPaginados
{
    public List<dynamic> Clientes { get; set; } = new List<dynamic>();
    public int Total { get; set; }
    public int TotalPaginas { get; set; }
}

// --- CLASES AUXILIARES ---
public class PagoCxCItem
{
    public DateTime Fecha { get; set; }
    public string Documento { get; set; }
    public string Observacion { get; set; }
    public string Moneda { get; set; }
    public decimal Monto { get; set; }
}