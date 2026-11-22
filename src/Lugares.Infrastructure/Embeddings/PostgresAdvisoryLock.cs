using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Lugares.Infrastructure.Embeddings;

public class PostgresAdvisoryLock : IEmbeddingBackfillLock
{
    private const long LockKey = 2026112101;

    private readonly string _connectionString;

    public PostgresAdvisoryLock(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No se configuró la cadena de conexión para el backfill de embeddings.");
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(
                "SELECT pg_try_advisory_lock(@key)",
                connection);
            command.Parameters.AddWithValue("key", LockKey);

            var acquired = await command.ExecuteScalarAsync(cancellationToken);
            if (acquired is not true)
            {
                await connection.DisposeAsync();
                return null;
            }

            return new LockHandle(connection, LockKey);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private sealed class LockHandle : IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly long _key;

        public LockHandle(NpgsqlConnection connection, long key)
        {
            _connection = connection;
            _key = key;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await using var command = new NpgsqlCommand(
                        "SELECT pg_advisory_unlock(@key)",
                        _connection);
                    command.Parameters.AddWithValue("key", _key);
                    await command.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
