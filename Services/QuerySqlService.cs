using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace eCertify.Services
{
    public class QuerySqlService
    {
        private readonly IConfiguration _configuration;

        public QuerySqlService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ObtenerDescriUnidad(string ID)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("CadenaConexion");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $"SELECT Abreviatura FROM UnidadesMedida WHERE ID = {ID}";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        return result?.ToString() ?? string.Empty;
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
