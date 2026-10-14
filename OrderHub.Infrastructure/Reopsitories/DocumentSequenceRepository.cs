using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal sealed class DocumentSequenceRepository : IDocumentSequenceRepository
{
    private readonly AppDbContext _context;

    public DocumentSequenceRepository(AppDbContext context) => _context = context;

    public async Task<int> ReserveNextNumberAsync(
        DocumentType documentType,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO document_sequences (document_type, year, month, last_number)
                VALUES (@document_type, @year, @month, 1)
                ON CONFLICT (document_type, year, month)
                DO UPDATE SET last_number = document_sequences.last_number + 1
                RETURNING last_number;
                """;
            if (_context.Database.CurrentTransaction is { } transaction)
                command.Transaction = transaction.GetDbTransaction();

            AddParameter(command, "document_type", (int)documentType);
            AddParameter(command, "year", year);
            AddParameter(command, "month", month);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
        finally
        {
            if (closeConnection)
                await connection.CloseAsync();
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
