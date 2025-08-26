using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Simulador.Api
{
    public class SqlConnectionFactory
    {
        private readonly string _cs;
        public SqlConnectionFactory(IConfiguration cfg)
            => _cs = cfg.GetConnectionString("SqlServer")
               ?? throw new InvalidOperationException("ConnectionStrings:SqlServer não configurada.");

        // Retorna SqlConnection (não IDbConnection) para permitir métodos async
        public SqlConnection Create() => new SqlConnection(_cs);
    }
}
