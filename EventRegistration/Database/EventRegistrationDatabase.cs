using System.Data;
using EventRegistration.Api.Interfaces;
using MySqlConnector;

namespace EventRegistration.Api.Database;

public class EventRegistrationDatabase : IEventRegistrationDatabase
{
    private readonly string _connectionString;

    public EventRegistrationDatabase(IConfiguration configuration)
    {
        _connectionString = configuration["DB_CONNECTION_STRING"]
            ?? throw new InvalidOperationException("DB_CONNECTION_STRING is not configured.");
    }

    public IDbConnection Open()
    {
        var connection = new MySqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}