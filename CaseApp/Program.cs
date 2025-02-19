using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
namespace CaseApp
{
    public interface IPlugin
    {
        void Run(IDbConnection connection, string command);
    }

    public interface IPluginLoader
    {
        IPlugin LoadPlugin();
    }
    public class ReflectionPlugin : IPlugin
    {
        private readonly object _pluginInstance;
        private readonly MethodInfo _runMethod;

        public ReflectionPlugin(object pluginInstance, MethodInfo runMethod)
        {
            _pluginInstance = pluginInstance;
            _runMethod = runMethod;
        }

        public void Run(IDbConnection connection, string command)
        {
            _runMethod.Invoke(_pluginInstance, new object[] { connection, command });
        }
    }
    public class PluginLoader : IPluginLoader
    {
        private readonly string _dllPath;

        public PluginLoader(string dllPath)
        {
            _dllPath = dllPath;
        }

        public IPlugin LoadPlugin()
        {
            Assembly assembly = Assembly.Load(File.ReadAllBytes(_dllPath));
            Type type = assembly.GetType("PluginClass");
            if (type == null)
            {
                throw new Exception("Yanlış tip!");
            }
            MethodInfo method = type.GetMethod("Run");
            if (method == null)
            {
                throw new Exception("Run metodu yok!");
            }
            object instance = Activator.CreateInstance(type);
            return new ReflectionPlugin(instance, method);
        }
    }
    public interface IDatabaseConnectionProvider
    {
        IDbConnection GetConnection();
    }
    public class DatabaseConnectionProvider : IDatabaseConnectionProvider
    {
        private readonly string _connectionString;

        public DatabaseConnectionProvider()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;
        }

        public IDbConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            string dllPath = "PluginDLL.dll";

            IPluginLoader pluginLoader = new PluginLoader(dllPath);
            IPlugin plugin = pluginLoader.LoadPlugin();
            IDatabaseConnectionProvider connectionProvider = new DatabaseConnectionProvider();

            bool exit = false;
            while (!exit)
            {
                Console.Write("SQL Komutunu Girin: ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    exit = true;
                    continue;
                }

                if (input.Equals("reload", StringComparison.OrdinalIgnoreCase))
                {
                    plugin = pluginLoader.LoadPlugin();
                    Console.WriteLine("DLL yeniden yüklendi.");
                    continue;
                }
                using (IDbConnection cnn = connectionProvider.GetConnection())
                {
                    cnn.Open();
                    plugin.Run(cnn, input);
                    cnn.Close();
                }
            }
        }
    }
}
