using System.Data;
using Dapper;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;

namespace AspNetCore.API.Database;

[PublicAPI]
public readonly record struct IoQueryBuilder(
    DatabaseIo DatabaseIo,
    string Sql,
    object? Params = null,
    int Timeout = 60,
    CommandType? CommandType = null,
    CommandFlags CommandFlags = CommandFlags.Buffered,
    bool KeepTransactionAlive = false)
{
    public CommandDefinition CreateCommandDefinition(CancellationToken token) =>
        new(Sql, Params, DatabaseIo.Transaction, Timeout, CommandType, CommandFlags, token);

    public IoQueryBuilder WithParams(object? @params) => this with { Params = @params };
    public IoQueryBuilder WithTimeout(int timeout) => this with { Timeout = timeout };
    public IoQueryBuilder WithCommandType(CommandType? commandType) => this with { CommandType = commandType };
    public IoQueryBuilder WithCommandFlags(CommandFlags commandFlags) => this with { CommandFlags = commandFlags };
    public IoQueryBuilder KeepAlive() => this with { KeepTransactionAlive = true };
    public IoQueryBuilder Commit() => this with { KeepTransactionAlive = false };

    public async Task<T> QueryFirst<T>(CancellationToken token)
    {
        return await InternalQuery(static tuple => tuple.Item1.QueryFirstAsync<T>(tuple.Item2), token);
    }

    public async Task<IEnumerable<T>> Query<T>(CancellationToken token) => await InternalQuery(static tuple => tuple.Item1.QueryAsync<T>(tuple.Item2), token);

    public async Task<int> Execute(CancellationToken token) => await InternalQuery(static tuple => tuple.Item1.ExecuteAsync(tuple.Item2), token);

    public async Task<T> InternalQuery<T>(Func<(SqliteConnection, CommandDefinition), Task<T>> query, CancellationToken token)
    {
        T res = await query((DatabaseIo.Connection, CreateCommandDefinition(token)));
        if (!KeepTransactionAlive) DatabaseIo.Commit();
        return res;
    }
}