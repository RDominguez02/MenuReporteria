using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MenuReporteria.Models;
using System.Linq;

namespace MenuReporteria.Services
{
    public class ReporteVentasService
    {
        private readonly string _connectionString;

        public ReporteVentasService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // MÉTODO PARA OBTENER VENDEDORES
        public List<string> ObtenerVendedores()
        {
            var vendedores = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT DISTINCT ve_codigo FROM IVBDHEPE ORDER BY VE_CODIGO";
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

        // MÉTODO PARA OBTENER MONEDAS
        public List<string> ObtenerMonedas()
        {
            var monedas = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                // Ajusta el nombre de la tabla según tu base de datos
                var query = "SELECT DISTINCT mo_codigo FROM IVBDHEPE WHERE mo_codigo IS NOT NULL ORDER BY mo_codigo";
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

        // MÉTODO PARA OBTENER SUCURSALES
        public List<string> ObtenerSucursales()
        {
            var sucursales = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT DISTINCT su_codigo FROM IVBDHEPE WHERE su_codigo IS NOT NULL ORDER BY su_codigo";
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sucursales.Add(reader["su_codigo"].ToString());
                        }
                    }
                }
            }
            return sucursales;
        }

        // MÉTODO PRINCIPAL CON TODOS LOS FILTROS
        public List<VentaItem> ObtenerVentasPorFiltro(FiltroVentas filtros)
        {
            var ventas = new List<VentaItem>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"
                SELECT
                    he_fecha, he_tipo, 'F' AS tipo, he_factura, cl_codigo, he_nombre, he_monto, 
                    he_horamod, he_turno, he_paypal, he_valdesc, he_itbis, he_neto, he_flete, 
                    he_desc, he_tipdes, he_ncf, he_rotura, he_ogas, he_usuario, he_Caja,
                    he_rnc, su_codigo, ve_codigo, HE_CREGEN, HE_VCREDI, HE_APARTA, HE_CONGEN, 
                    HE_ECHEQUE, HE_ETARJE, HE_BONIFI, HE_VCONSUMO, HE_PUNTOS, he_itbis18, 
                    he_itbis08, IM_CODIGO, he_conduce, HE_ORDEN, mo_codigo, he_tasa, 
                    he_personas, (HE_TASA * he_neto) as totalR
                FROM IVBDHEPE
                WHERE CAST(he_fecha AS DATE) BETWEEN @FechaDesde AND @FechaHasta
            ";

                // FILTROS BÁSICOS
                if (!string.IsNullOrEmpty(filtros.Cliente))
                    query += " AND he_nombre LIKE '%' + @Cliente + '%'";

                if (!string.IsNullOrEmpty(filtros.Vendedor))
                    query += " AND ve_codigo = @Vendedor";

                if (filtros.ValorDesde.HasValue)
                    query += " AND he_monto >= @ValorDesde";

                if (filtros.ValorHasta.HasValue)
                    query += " AND he_monto <= @ValorHasta";

                if (!string.IsNullOrEmpty(filtros.Ncf))
                    query += " AND he_ncf LIKE @Ncf + '%'";

                if (!string.IsNullOrEmpty(filtros.FacturaDesde))
                    query += " AND he_factura LIKE '%' + @FacturaDesde + '%'";

                if (!string.IsNullOrEmpty(filtros.Caja))
                    query += " AND he_Caja = @Caja";

                // NUEVOS FILTROS
                if (!string.IsNullOrEmpty(filtros.Moneda))
                    query += " AND mo_codigo = @Moneda";

                if (!string.IsNullOrEmpty(filtros.Sucursal))
                    query += " AND su_codigo = @Sucursal";

                // FILTRO TIPO DE FACTURA (Contado/Crédito)
                if (filtros.TipoFactura == "Contado")
                    query += " AND he_tipo = 1"; // Ajusta según tu lógica
                else if (filtros.TipoFactura == "Crédito")
                    query += " AND he_tipo = 2"; // Ajusta según tu lógica

                // FILTRO MODO DE FACTURAS
                if (filtros.ModoFacturas == "Mayor")
                    query += " AND he_tipof = '2'"; // Ajusta según tu campo
                else if (filtros.ModoFacturas == "Detalle")
                    query += " AND he_tipof = '1'"; // Ajusta según tu campo

                // FILTRO OPCIONES
                switch (filtros.Opciones)
                {
                    case "Normales":
                        query += " AND he_progra=0"; // Ajusta según tu lógica
                        break;
                    case "SoloNCF":
                        query += " AND he_progra=1 and he_rotura=0";
                        break;
                    case "Editadas":
                        query += " AND he_progra=5 and he_rotura=0 "; // Ajusta según tu campo de edición
                        break;
                    case "Repuestos":
                        query += " he_progra=2 and he_rotura=0"; // Ajusta según tu lógica
                        break;
                    case "Placa":
                        query += " AND he_progra=1 and he_rotura=1"; // Ajusta según tu lógica
                        break;
                        // "Todas" no agrega filtro
                }

                // ORDENAMIENTO
                if (filtros.OrdenadoPor == "FACTURA")
                    query += " ORDER BY he_factura";
                else // FECHA_FACTURA por defecto
                    query += " ORDER BY he_fecha, he_factura";

                using (var command = new SqlCommand(query, connection))
                {
                    // PARÁMETROS BÁSICOS
                    command.Parameters.AddWithValue("@FechaDesde", filtros.FechaDesde.Date);
                    command.Parameters.AddWithValue("@FechaHasta", filtros.FechaHasta.Date);

                    if (!string.IsNullOrEmpty(filtros.Cliente))
                        command.Parameters.AddWithValue("@Cliente", filtros.Cliente);
                    if (!string.IsNullOrEmpty(filtros.Vendedor))
                        command.Parameters.AddWithValue("@Vendedor", filtros.Vendedor);
                    if (filtros.ValorDesde.HasValue)
                        command.Parameters.AddWithValue("@ValorDesde", filtros.ValorDesde.Value);
                    if (filtros.ValorHasta.HasValue)
                        command.Parameters.AddWithValue("@ValorHasta", filtros.ValorHasta.Value);
                    if (!string.IsNullOrEmpty(filtros.Ncf))
                        command.Parameters.AddWithValue("@Ncf", filtros.Ncf);
                    if (!string.IsNullOrEmpty(filtros.FacturaDesde))
                        command.Parameters.AddWithValue("@FacturaDesde", filtros.FacturaDesde);
                    if (!string.IsNullOrEmpty(filtros.Caja))
                        command.Parameters.AddWithValue("@Caja", filtros.Caja);

                    // NUEVOS PARÁMETROS
                    if (!string.IsNullOrEmpty(filtros.Moneda))
                        command.Parameters.AddWithValue("@Moneda", filtros.Moneda);
                    if (!string.IsNullOrEmpty(filtros.Sucursal))
                        command.Parameters.AddWithValue("@Sucursal", filtros.Sucursal);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ventas.Add(new VentaItem
                            {
                                Fecha = reader.GetDateTime(reader.GetOrdinal("he_fecha")),
                                Factura = reader["he_factura"].ToString(),
                                Ncf = reader["he_ncf"].ToString(),
                                CodigoCliente = reader["cl_codigo"].ToString(),
                                Cliente = reader["he_nombre"].ToString(),
                                Vendedor = reader["ve_codigo"].ToString(),
                                Caja = reader["he_Caja"].ToString(),
                                Turno = Convert.ToInt32(reader["he_turno"]),
                                MontoBruto = Convert.ToDecimal(reader["he_monto"]),
                                Itbis = Convert.ToDecimal(reader["he_itbis"]),
                                CIF = Convert.ToDecimal(reader["he_itbis08"]),
                                MontoNeto = Convert.ToDecimal(reader["he_neto"]),
                                Moneda = reader["mo_codigo"].ToString(),
                                Tasa = Convert.ToDecimal(reader["he_tasa"]),
                                TotalR = Convert.ToDecimal(reader["totalR"]),
                                TotalChasis = Convert.ToDecimal(reader["he_personas"])
                            });
                        }
                    }
                }
            }

            return ventas;
        }
        // Agregar este método a la clase ReporteVentasService

        /// <summary>
        /// Obtiene el detalle completo de una factura incluyendo encabezado, cliente, productos y chasis
        /// </summary>
        public DetalleFacturaViewModel ObtenerDetalleFactura(string numeroFactura)
        {
            var detalle = new DetalleFacturaViewModel();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // 1. OBTENER DATOS DEL ENCABEZADO (IVBDHEPE)
                var queryEncabezado = @"
            SELECT TOP 1
                he_factura, he_fecha, he_ncf, he_usuario, he_turno, 
                he_orden as ultimoControl, he_Caja, ve_codigo, mo_codigo, he_tasa,
                cl_codigo, he_nombre, he_rnc, he_direc1, he_telef,
                he_monto, he_itbis, he_itbis18, he_valdesc, 
                he_neto, he_itbis08
            FROM IVBDHEPE
            WHERE he_factura = @Factura
        ";

                using (var cmdEncabezado = new SqlCommand(queryEncabezado, connection))
                {
                    cmdEncabezado.Parameters.AddWithValue("@Factura", numeroFactura);

                    using (var reader = cmdEncabezado.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Información General
                            detalle.Factura = reader["he_factura"]?.ToString() ?? "";
                            detalle.Fecha = reader["he_fecha"] != DBNull.Value
                                ? Convert.ToDateTime(reader["he_fecha"])
                                : DateTime.Now;
                            detalle.Ncf = reader["he_ncf"]?.ToString() ?? "";
                            detalle.Usuario = reader["he_usuario"]?.ToString() ?? "";
                            detalle.Turno = reader["he_turno"] != DBNull.Value
                                ? Convert.ToInt32(reader["he_turno"])
                                : 0;
                            detalle.UltimoControl = reader["ultimoControl"]?.ToString() ?? "";
                            detalle.Caja = reader["he_Caja"]?.ToString() ?? "";
                            detalle.Vendedor = reader["ve_codigo"]?.ToString() ?? "";
                            detalle.Moneda = reader["mo_codigo"]?.ToString() ?? "";
                            detalle.Tasa = reader["he_tasa"] != DBNull.Value
                                ? Convert.ToDecimal(reader["he_tasa"])
                                : 0;
                            // Información del Cliente
                            detalle.ClienteCodigo = reader["cl_codigo"]?.ToString() ?? "";
                            detalle.ClienteNombre = reader["he_nombre"]?.ToString() ?? "";
                            detalle.ClienteRnc = reader["he_rnc"]?.ToString() ?? "";
                            detalle.ClienteDireccion = reader["he_direc1"]?.ToString() ?? "";
                            detalle.ClienteTelefono = reader["he_telef"]?.ToString() ?? "";

                            // Totales
                            detalle.MontoBruto = reader["he_monto"] != DBNull.Value
                                ? Convert.ToDecimal(reader["he_monto"])
                                : 0;
                            detalle.Impuesto17 = reader["he_itbis"] != DBNull.Value
                                ? Convert.ToDecimal(reader["he_itbis"])
                                : 0;
                            detalle.Itbis18 = reader["he_itbis18"] != DBNull.Value
                                ? Convert.ToDecimal(reader["he_itbis18"])
                                : 0;
                            detalle.Descuento = reader["he_valdesc"] != DBNull.Value
                                ? Convert.ToDecimal(reader["he_valdesc"])
                                : 0;
                            detalle.MontoNeto = reader["he_neto"] != DBNull.Value
                                ? Convert.ToDecimal(reader["he_neto"])
                                : 0;
                            detalle.TotalItbis = (detalle.Impuesto17 + detalle.Itbis18);
                            detalle.Subtotal = detalle.MontoNeto - detalle.TotalItbis;
                        }
                    }
                }

                // 2. OBTENER PRODUCTOS Y DATOS DE CHASIS (IVBDDEPE + IVBDVEHICULO)
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
                    cmdProductos.Parameters.AddWithValue("@Factura", numeroFactura);

                    using (var reader = cmdProductos.ExecuteReader())
                    {
                        bool primerProducto = true;

                        while (reader.Read())
                        {
                            // Agregar producto a la lista con sus datos de chasis
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
                                // Datos del chasis de este producto
                                Chasis = reader["ar_chasis"]?.ToString() ?? "",
                                Ano = reader["ar_ano"]?.ToString() ?? "",
                                Motor = reader["ar_motor"]?.ToString() ?? "",
                                Modelo = reader["ar_modelo"]?.ToString() ?? "",
                                Color = reader["ar_color"]?.ToString() ?? "",
                                Placa = reader["ar_placa"]?.ToString() ?? "",
                                Matricula = reader["ar_matri"]?.ToString() ?? "",
                                Marca = reader["ar_marca"]?.ToString() ?? ""

                            };

                            detalle.Productos.Add(producto);

                            // Si es el primer producto con datos de chasis, llenar la sección principal
                            if (primerProducto)
                            {
                                var chasis = reader["ar_chasis"]?.ToString();
                                if (!string.IsNullOrEmpty(chasis))
                                {
                                    detalle.Chasis = chasis;
                                    detalle.Ano = reader["ar_ano"]?.ToString() ?? "";
                                    detalle.Motor = reader["ar_motor"]?.ToString() ?? "";
                                    detalle.Modelo = reader["ar_modelo"]?.ToString() ?? "";
                                    detalle.Color = reader["ar_color"]?.ToString() ?? "";
                                    detalle.Placa = reader["ar_placa"]?.ToString() ?? "";
                                    detalle.Matricula = reader["ar_matri"]?.ToString() ?? "";
                                    detalle.Marca = reader["ar_marca"]?.ToString() ?? "";
                                    primerProducto = false;
                                }
                            }
                        }
                    }
                }
            }

            return detalle;
        }

        /// <summary>
        /// Obtiene todos los chasis asociados a una factura (para mostrar múltiples si existen)
        /// </summary>
        public List<ChasisInfo> ObtenerChasisPorFactura(string numeroFactura)
        {
            var chasisList = new List<ChasisInfo>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"
            SELECT DISTINCT
                ar_chasis,
                ar_ano,
                ar_motor,
                ar_modelo,
                ar_color,
                ar_placa,
                ar_matri,
                AR_CODIGO,
                AR_MARCA,
                DE_DESCRI
            FROM IVBDDEPE
            WHERE DE_FACTURA = @Factura
              AND ar_chasis IS NOT NULL
              AND ar_chasis <> ''
            ORDER BY de_id
        ";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Factura", numeroFactura);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            chasisList.Add(new ChasisInfo
                            {
                                Chasis = reader["ar_chasis"]?.ToString() ?? "",
                                Ano = reader["ar_ano"]?.ToString() ?? "",
                                Motor = reader["ar_motor"]?.ToString() ?? "",
                                Modelo = reader["ar_modelo"]?.ToString() ?? "",
                                Color = reader["ar_color"]?.ToString() ?? "",
                                Placa = reader["ar_placa"]?.ToString() ?? "",
                                Matricula = reader["ar_matri"]?.ToString() ?? "",
                                Marca = reader["ar_marca"]?.ToString() ?? "",
                                CodigoArticulo = reader["AR_CODIGO"]?.ToString() ?? "",
                                Descripcion = reader["DE_DESCRI"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return chasisList;
        }

        // Clase auxiliar para múltiples chasis
        public class ChasisInfo
        {
            public string Chasis { get; set; }
            public string Ano { get; set; }
            public string Motor { get; set; }
            public string Modelo { get; set; }
            public string Color { get; set; }
            public string Placa { get; set; }
            public string Matricula { get; set; }
            public string Marca { get; set; }
            public string CodigoArticulo { get; set; }
            public string Descripcion { get; set; }
        }
    }
}