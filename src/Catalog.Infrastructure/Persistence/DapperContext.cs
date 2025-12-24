using MySqlConnector;
using System.Data;
using Catalog.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Catalog.Infrastructure.Persistence;

public class DapperContext : IDapperContext
{
    private readonly string _connectionString;
    
    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }
    
    public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
}