using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Npgsql;

namespace NpgsqlRestClient;

public class DbDataProtection(string? connectionString, string getCommand, string storeCommand) 
    : IXmlRepository
{
    private readonly string? _connectionString = connectionString;
    private readonly string _getCommand = getCommand;
    private readonly string _storeCommand = storeCommand;

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var elements = new List<XElement>();

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            using var cmd = new NpgsqlCommand(_getCommand, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                elements.Add(XElement.Parse(reader.GetString(0)));
            }
        }

        return elements;
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var cmd = new NpgsqlCommand();
        cmd.Connection = connection;
        cmd.CommandText = _storeCommand;
        cmd.Parameters.Add(new NpgsqlParameter() { Value = friendlyName }); // $1
        cmd.Parameters.Add(new NpgsqlParameter() { Value = element.ToString(SaveOptions.DisableFormatting) }); // $2
        cmd.ExecuteNonQuery();
    }
}
