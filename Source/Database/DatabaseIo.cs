using System.Data;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;

namespace AspNetCore.API.Database;

public abstract class DatabaseIo : IDisposable
{
    internal readonly SqliteConnection Connection;
    internal IDbTransaction? Transaction;

    protected DatabaseIo(string connectionString)
    {
        Connection = new SqliteConnection(connectionString);
        Connection.Open();
    }

    public void Dispose()
    {
        Connection.Close();
        Connection.Dispose();
        GC.SuppressFinalize(this);
    }

    internal IoQueryBuilder Sql(string sql, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        Transaction = Connection.BeginTransaction(isolationLevel);
        return new IoQueryBuilder(this, sql);
    }

    [PublicAPI]
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

    [PublicAPI]
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