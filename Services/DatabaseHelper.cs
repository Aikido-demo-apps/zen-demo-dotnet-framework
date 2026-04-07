using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using ZenDemo.DotNetFramework.Models;

namespace ZenDemo.DotNetFramework.Services
{
    public sealed class DatabaseHelper
    {
        private static readonly Lazy<DatabaseHelper> LazyInstance = new Lazy<DatabaseHelper>(() => new DatabaseHelper());

        private readonly string _connectionString;

        private DatabaseHelper()
        {
            _connectionString = ResolveConnectionString();
        }

        public static DatabaseHelper Instance
        {
            get { return LazyInstance.Value; }
        }

        public void Initialize()
        {
            EnsureDatabase().GetAwaiter().GetResult();
        }

        public async Task<List<Pet>> GetAllPetsAsync()
        {
            var pets = new List<Pet>();

            using (var connection = new NpgsqlConnection(_connectionString))
            using (var command = new NpgsqlCommand("SELECT \"Id\", \"Name\", \"Owner\", \"Id\" AS pet_id FROM \"Pets\" ORDER BY \"Id\"", connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        pets.Add(new Pet
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Owner = reader.IsDBNull(2) ? "Aikido Security" : reader.GetString(2),
                            pet_id = reader.GetInt32(3)
                        });
                    }
                }
            }

            return pets;
        }

        public async Task<Pet> GetPetByIdAsync(string id)
        {
            int parsedId;
            if (!int.TryParse(id, out parsedId))
            {
                return null;
            }

            using (var connection = new NpgsqlConnection(_connectionString))
            using (var command = new NpgsqlCommand("SELECT \"Id\", \"Name\", \"Owner\", \"Id\" AS pet_id FROM \"Pets\" WHERE \"Id\" = @id", connection))
            {
                command.Parameters.Add("id", NpgsqlDbType.Integer).Value = parsedId;
                await connection.OpenAsync().ConfigureAwait(false);

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    if (!await reader.ReadAsync().ConfigureAwait(false))
                    {
                        return null;
                    }

                    return new Pet
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Owner = reader.IsDBNull(2) ? "Aikido Security" : reader.GetString(2),
                        pet_id = reader.GetInt32(3)
                    };
                }
            }
        }

        public async Task<int> CreatePetByNameAsync(string name)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                // Intentionally vulnerable for the demo.
                var sql = string.Format("INSERT INTO \"Pets\" (\"Name\", \"Owner\") VALUES ('{0}', 'Aikido Security')", name);
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task ClearAllAsync()
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            using (var command = new NpgsqlCommand("DELETE FROM \"Pets\"", connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async Task EnsureDatabase()
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            using (var command = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS \"Pets\" (\"Id\" SERIAL PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Owner\" TEXT NOT NULL DEFAULT 'Aikido Security');", connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private static string ResolveConnectionString()
        {
            var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = ConfigurationManager.AppSettings["DATABASE_URL"];
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var configuredConnection = ConfigurationManager.ConnectionStrings["DefaultConnection"];
                connectionString = configuredConnection != null ? configuredConnection.ConnectionString : null;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("DATABASE_URL environment variable is not set");
            }

            if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(connectionString);
                var userInfo = uri.UserInfo.Split(':');
                var builder = new NpgsqlConnectionStringBuilder
                {
                    Host = uri.Host,
                    Port = uri.Port,
                    Database = uri.AbsolutePath.TrimStart('/'),
                    Username = userInfo[0],
                    Password = userInfo.Length > 1 ? userInfo[1] : string.Empty,
                    SslMode = connectionString.IndexOf("sslmode=disable", StringComparison.OrdinalIgnoreCase) >= 0
                        ? SslMode.Disable
                        : SslMode.Prefer
                };

                return builder.ConnectionString;
            }

            return connectionString;
        }
    }
}
