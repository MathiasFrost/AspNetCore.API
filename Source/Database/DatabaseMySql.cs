using System.Data;
using MySqlConnector;

namespace AspNetCore.API.Database;

public abstract class DatabaseMySql : IDisposable
{
    public readonly MySqlConnection Connection;
    public IDbTransaction? Transaction;

    public DatabaseMySql(IConfiguration configuration)
    {
        Connection = new MySqlConnection(configuration.GetConnectionString("Test"));
        Connection.Open();
    }

    public void Dispose()
    {
        Connection.Close();
        Connection.Dispose();
    }

    public MySqlQueryBuilder Sql(string sql, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        Transaction = Connection.BeginTransaction(isolationLevel);
        return new MySqlQueryBuilder(this, sql);
    }

    public void Rollback()
    {
        try
        {
            Transaction?.Rollback();
        }
        finally
        {
            Transaction?.Dispose();
            Transaction = null;
        }
    }

    public void Commit()
    {
        try
        {
            Transaction?.Commit();
        }
        finally
        {
            Transaction?.Dispose();
            Transaction = null;
        }
    }
}