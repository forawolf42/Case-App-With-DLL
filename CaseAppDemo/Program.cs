using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
using CaseAppDemo;
using MySql.Data.MySqlClient;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string dllPath = "PluginDLL.dll";
            IPluginLoader pluginLoader = new PluginLoader(dllPath);
            IPlugin plugin;

            try
            {
                plugin = pluginLoader.LoadPlugin();
                Console.WriteLine("Plugin başarıyla yüklendi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Plugin yüklenirken hata oluştu: " + ex.Message);
                return;
            }

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
                    try
                    {
                        plugin = pluginLoader.LoadPlugin();
                        Console.WriteLine("DLL yeniden yüklendi.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Plugin yeniden yüklenirken hata oluştu: " + ex.Message);
                    }
                    continue;
                }

                try
                {
                    using (IDbConnection cnn = connectionProvider.GetConnection())
                    {
                        plugin.Run(cnn, input);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Hata: " + ex.Message);
                }
            }
        }
    }
}
