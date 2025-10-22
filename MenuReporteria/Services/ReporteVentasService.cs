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
    }
}