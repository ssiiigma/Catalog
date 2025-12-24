using System.Data;

namespace Catalog.Application.Interfaces;

public interface IDapperContext
{
    IDbConnection CreateConnection();
}