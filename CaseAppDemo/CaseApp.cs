using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace CaseAppDemo
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
            if (!File.Exists(_dllPath))
                throw new FileNotFoundException("DLL bulunamadı", _dllPath);

            Assembly assembly = Assembly.Load(File.ReadAllBytes(_dllPath));
            Type type = assembly.GetType("PluginClass");
            if (type == null)
                throw new Exception("PluginClass bulunamadı!");

            MethodInfo method = type.GetMethod("Run");
            if (method == null)
                throw new Exception("Run metodu bulunamadı!");

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
            var connectionStringSettings = ConfigurationManager.ConnectionStrings["MySqlConnection"];
            if (connectionStringSettings == null)
                throw new Exception("MySqlConnection connection string bulunamadı!");

            _connectionString = connectionStringSettings.ConnectionString;
        }

        public IDbConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}
