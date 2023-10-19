using System.Data;
using System.Data.SqlClient;

namespace AspNetCore.API.Database;

public class DatabaseTds : IDisposable
{
    public readonly SqlConnection Connection;
    public IDbTransaction? Transaction;

    public DatabaseTds(IConfiguration configuration)
    {
        Connection = new SqlConnection(configuration.GetConnectionString("Test"));
        Connection.Open();
    }

    public TdsQueryBuilder Sql(string sql, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        Transaction = Connection.BeginTransaction(isolationLevel);
        return new TdsQueryBuilder(this, sql);
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

    public void Dispose()
    {
        Connection.Close();
        Connection.Dispose();
    }
}