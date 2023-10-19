using System.Data;
using Dapper;

namespace AspNetCore.API.Database;

public readonly record struct TdsQueryBuilder(DatabaseTds DatabaseTds,
    string Sql,
    object? Params = null,
    int Timeout = 60,
    CommandType? CommandType = null,
    CommandFlags CommandFlags = CommandFlags.Buffered,
    bool KeepTransactionAlive = false)
{
    public CommandDefinition CreateCommandDefinition(CancellationToken token) => new(Sql, Params, DatabaseTds.Transaction, Timeout, CommandType, CommandFlags);

    public TdsQueryBuilder WithParams(object? @params) => this with { Params = @params };
    public TdsQueryBuilder WithTimeout(int timeout) => this with { Timeout = timeout };
    public TdsQueryBuilder WithCommandType(CommandType? commandType) => this with { CommandType = commandType };
    public TdsQueryBuilder WithCommandFlags(CommandFlags commandFlags) => this with { CommandFlags = commandFlags };
    public TdsQueryBuilder KeepAlive() => this with { KeepTransactionAlive = true };
    public TdsQueryBuilder Commit() => this with { KeepTransactionAlive = false };

    public async Task<List<T>> QueryToListAsync<T>(CancellationToken token)
    {
        IEnumerable<T> res = await DatabaseTds.Connection.QueryAsync<T>(CreateCommandDefinition(token));
        if (!KeepTransactionAlive) DatabaseTds.Commit();
        return res.ToList();
    }
}