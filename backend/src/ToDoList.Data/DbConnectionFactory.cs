using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using ToDoList.Core.Interfaces;

namespace ToDoList.Data
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string connectionString;

        public DbConnectionFactory(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' not found in configuration.");
        }

        public IDbConnection CreateConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}
