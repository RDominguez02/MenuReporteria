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

        public List<VentaItem> ObtenerVentasPorFiltro(FiltroVentas filtros)
        {
            var ventas = new List<VentaItem>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"
                    SELECT
                        he_fecha, he_tipo, 'F' AS tipo, he_factura, cl_codigo, he_nombre, he_monto, he_horamod, he_turno, he_paypal,
                        he_valdesc, he_itbis, he_neto, he_flete, he_desc, he_tipdes, he_ncf, he_rotura, he_ogas, he_usuario, he_Caja,
                        he_rnc, su_codigo, ve_codigo, HE_CREGEN, HE_VCREDI, HE_APARTA, HE_CONGEN, HE_ECHEQUE, HE_ETARJE, HE_BONIFI,
                        HE_VCONSUMO, HE_PUNTOS, he_itbis18, he_itbis08, IM_CODIGO, he_conduce, HE_ORDEN, mo_codigo, he_tasa, he_personas
                    FROM IVBDHEPE
                    WHERE CAST(he_fecha AS DATE) BETWEEN @FechaDesde AND @FechaHasta
                ";

                // Filtros adicionales dinámicos
                if (!string.IsNullOrEmpty(filtros.Cliente))
                    query += " AND he_nombre LIKE '%' + @Cliente + '%'";

                if (!string.IsNullOrEmpty(filtros.Vendedor))
                    query += " AND ve_codigo = @Vendedor";

                if (filtros.ValorDesde.HasValue)
                    query += " AND he_monto >= @ValorDesde";

                if (filtros.ValorHasta.HasValue)
                    query += " AND he_monto <= @ValorHasta";

                if (!string.IsNullOrEmpty(filtros.Ncf))
                    query += " AND he_ncf LIKE '%' + @Ncf + '%'";

                if (filtros.FacturaDesde.HasValue)
                    query += " AND he_factura >= @FacturaDesde";

                if (filtros.FacturaHasta.HasValue)
                    query += " AND he_factura <= @FacturaHasta";

                if (!string.IsNullOrEmpty(filtros.Caja))
                    query += " AND he_Caja = @Caja";

                using (var command = new SqlCommand(query, connection))
                {
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
                    if (filtros.FacturaDesde.HasValue)
                        command.Parameters.AddWithValue("@FacturaDesde", filtros.FacturaDesde.Value);
                    if (filtros.FacturaHasta.HasValue)
                        command.Parameters.AddWithValue("@FacturaHasta", filtros.FacturaHasta.Value);
                    if (!string.IsNullOrEmpty(filtros.Caja))
                        command.Parameters.AddWithValue("@Caja", filtros.Caja);

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
                                Itbis = Convert.ToDecimal(reader["he_itbis"])
                            });
                        }
                    }
                }
            }

            return ventas;
        }
    }
}
