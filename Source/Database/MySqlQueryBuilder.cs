using System.Data;
using Dapper;

namespace AspNetCore.API.Database;

public readonly record struct MySqlQueryBuilder(DatabaseMySql DatabaseMySql,
    string Sql,
    object? Params = null,
    int Timeout = 60,
    CommandType? CommandType = null,
    CommandFlags CommandFlags = CommandFlags.Buffered,
    bool KeepTransactionAlive = false)
{
    public CommandDefinition CreateCommandDefinition(CancellationToken token) => new(Sql, Params, DatabaseMySql.Transaction, Timeout, CommandType, CommandFlags);

    public MySqlQueryBuilder WithParams(object? @params) => this with { Params = @params };
    public MySqlQueryBuilder WithTimeout(int timeout) => this with { Timeout = timeout };
    public MySqlQueryBuilder WithCommandType(CommandType? commandType) => this with { CommandType = commandType };
    public MySqlQueryBuilder WithCommandFlags(CommandFlags commandFlags) => this with { CommandFlags = commandFlags };
    public MySqlQueryBuilder KeepAlive() => this with { KeepTransactionAlive = true };
    public MySqlQueryBuilder Commit() => this with { KeepTransactionAlive = false };

    public async Task<List<T>> QueryToListAsync<T>(CancellationToken token)
    {
        IEnumerable<T> res = await DatabaseMySql.Connection.QueryAsync<T>(CreateCommandDefinition(token));
        if (!KeepTransactionAlive) DatabaseMySql.Commit();
        return res.ToList();
    }
}