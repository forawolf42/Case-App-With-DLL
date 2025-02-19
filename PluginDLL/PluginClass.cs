using System;
using System.Data;
using Dapper;

public interface IDatabaseCommand
{
    void Execute(IDbConnection connection);
}

public class InsertCommand : IDatabaseCommand
{
    private readonly string _commandText;

    public InsertCommand(string commandText)
    {
        _commandText = commandText;
    }

    public void Execute(IDbConnection connection)
    {
        connection.Execute(_commandText);
        Console.WriteLine("Kayıt eklendi.");
    }
}

public class DeleteCommand : IDatabaseCommand
{
    private readonly string _commandText;

    public DeleteCommand(string commandText)
    {
        _commandText = commandText;
    }

    public void Execute(IDbConnection connection)
    {
        connection.Execute(_commandText);
        Console.WriteLine("Kayıt silindi.");
    }
}

public class SelectCommand : IDatabaseCommand
{
    public void Execute(IDbConnection connection)
    {
        var users = connection.Query("SELECT * FROM users");
        foreach (var user in users)
        {
            Console.WriteLine(user);
        }
    }
}

public interface IDatabaseCommandFactory
{
    IDatabaseCommand Create(string commandText);
}

public class DatabaseCommandFactory : IDatabaseCommandFactory
{
    public IDatabaseCommand Create(string commandText)
    {
        if (commandText.StartsWith("insert", StringComparison.OrdinalIgnoreCase))
        {
            return new InsertCommand(commandText);
        }
        else if (commandText.StartsWith("delete", StringComparison.OrdinalIgnoreCase))
        {
            return new DeleteCommand(commandText);
        }
        else if (commandText.StartsWith("select", StringComparison.OrdinalIgnoreCase))
        {
            return new SelectCommand();
        }
        else
        {
            throw new NotSupportedException($"Yanlış komut: {commandText}");
        }
    }
}

public class PluginClass
{
    private readonly IDatabaseCommandFactory _commandFactory;

    public PluginClass() : this(new DatabaseCommandFactory())
    {
    }

    public PluginClass(IDatabaseCommandFactory commandFactory)
    {
        _commandFactory = commandFactory;
    }

    public void Run(IDbConnection connection, string commandText)
    {
        connection.Open();
        try
        {
            var command = _commandFactory.Create(commandText);
            command.Execute(connection);
        }
        finally
        {
            connection.Close();
        }
    }
}
