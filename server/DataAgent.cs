using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using YMS.Server.Data;

namespace YMS.Server;

public class DataAgent
{
    private readonly AppDbContext _context;

    public DataAgent(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DataTable> Fill(string sql, List<DataParameter> parameters)
    {
        var connection = _context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var param in parameters)
        {
            var dbParam = command.CreateParameter();
            dbParam.ParameterName = ":" + param.Name;
            dbParam.Value = param.Value ?? DBNull.Value;
            command.Parameters.Add(dbParam);
        }

        var dataTable = new DataTable();
        using var reader = await command.ExecuteReaderAsync();
        dataTable.Load(reader);

        return dataTable;
    }

    public async Task<int> Execute(string sql, List<DataParameter> parameters)
    {
        var connection = _context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var param in parameters)
        {
            var dbParam = command.CreateParameter();
            dbParam.ParameterName = ":" + param.Name;
            dbParam.Value = param.Value ?? DBNull.Value;
            command.Parameters.Add(dbParam);
        }

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<object?> ExecuteScalar(string sql, List<DataParameter> parameters)
    {
        var connection = _context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var param in parameters)
        {
            var dbParam = command.CreateParameter();
            dbParam.ParameterName = ":" + param.Name;
            dbParam.Value = param.Value ?? DBNull.Value;
            command.Parameters.Add(dbParam);
        }

        return await command.ExecuteScalarAsync();
    }
}
