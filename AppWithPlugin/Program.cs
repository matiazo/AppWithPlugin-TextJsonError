using Azure.Messaging.EventGrid;
using PluginBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Serialization;

namespace AppWithPlugin
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 1 && args[0] == "/d")
                {
                    Console.WriteLine("Waiting for any key...");
                    Console.ReadLine();
                }

                string[] pluginPaths = new string[]
                {
                    @"HelloPlugin\bin\Debug\net6\HelloPlugin.dll",
                    @"JsonPlugin\bin\Debug\net6\JsonPlugin.dll",
                    @"OldJsonPlugin\bin\Debug\net6\OldJsonPlugin.dll",
                    @"FrenchPlugin\bin\Debug\net6\FrenchPlugin.dll",
                    @"UVPlugin\bin\Debug\net6\UVPlugin.dll",
                };

                IEnumerable<ICommand> commands = pluginPaths.SelectMany(pluginPath =>
                {
                    Assembly pluginAssembly = LoadPlugin(pluginPath);
                    return CreateCommands(pluginAssembly);
                }).ToList();

                if (args.Length == 0)
                {
                    Console.WriteLine("Commands: ");
                    foreach (ICommand command in commands)
                    {
                        Console.WriteLine($"{command.Name}\t - {command.Description}");
                    }
                }
                else
                {
                    var testEvent = new TestEvent() { CorrelationId = "1-2-3-4" };
                    var defaultContext = AssemblyLoadContext.GetLoadContext(testEvent.GetType().Assembly);
                    Console.WriteLine("***");
                    Console.WriteLine($"Checking property is deserialized correctly, TestEvent context '{defaultContext.ToString()}'");
                    var testEventEvGrid = new EventGridEvent("Test", "Type", "1.0", testEvent);
                    var testEventJson = testEventEvGrid.Data.ToString();
                    if (testEventJson.Contains("correlationId") == false)
                    {
                        Console.WriteLine($"***unable to find \"correlationId\"***");
                    }
                    Console.WriteLine($"Actual testEventJson: {testEventJson}");
                    PrintAssemblies(AssemblyLoadContext.Default.Assemblies.Where(x => x.FullName.Contains("Json")));
                    Console.WriteLine("***");
                    Console.WriteLine();

                    foreach (string commandName in args)
                    {
                        Console.WriteLine($"-- {commandName} --");
                        ICommand command = commands.FirstOrDefault(c => c.Name == commandName);
                        if (command == null)
                        {
                            Console.WriteLine("No such command is known.");
                            return;
                        }

                        command.Execute();

                        if (command._eventData != null)
                        {
                            Console.WriteLine("");
                            Console.WriteLine("***");
                            var pluginContext = AssemblyLoadContext.GetLoadContext(command.GetType().Assembly);
                            Console.WriteLine($"Checking property is deserialized correctly, EventData context '{pluginContext.ToString()}'");
                            var eventDataBody = new EventGridEvent("Test", "Type", "1.0", command._eventData.Body);
                            var eventDataBodyJson = eventDataBody.Data.ToString();
                            if (eventDataBodyJson.Contains("myCustomProperty") == false)
                            {
                                Console.WriteLine($"***unable to find \"myCustomProperty\", Json deserialized with property name instead***");
                            }
                            Console.WriteLine($"Actual EventData Json: {eventDataBodyJson}");
                            PrintAssemblies(AssemblyLoadContext.GetLoadContext(command.GetType().Assembly).Assemblies);
                            Console.WriteLine("***");
                            Console.WriteLine();
                        }

                        Console.WriteLine();
                    }

                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void PrintAssemblies(IEnumerable<Assembly> assemblies)
        {
            Console.WriteLine();
            foreach (var assembly in assemblies)
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fileVersionInfo.ProductVersion;

                Console.WriteLine($"   {assembly.GetName().FullName} - {version}");
            }
            Console.WriteLine();
        }

        static Assembly LoadPlugin(string relativePath)
        {
            // Navigate up to the solution root
            string root = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(
                        Path.GetDirectoryName(
                            Path.GetDirectoryName(
                                Path.GetDirectoryName(typeof(Program).Assembly.Location)))))));

            string pluginLocation = Path.GetFullPath(Path.Combine(root, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
            Console.WriteLine($"Loading commands from: {pluginLocation}");
            PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(pluginLocation));
        }

        static IEnumerable<ICommand> CreateCommands(Assembly assembly)
        {
            int count = 0;

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(ICommand).IsAssignableFrom(type))
                {
                    ICommand result = Activator.CreateInstance(type) as ICommand;
                    if (result != null)
                    {
                        count++;
                        yield return result;
                    }
                }
            }

            if (count == 0)
            {
                string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
                throw new ApplicationException(
                    $"Can't find any type which implements ICommand in {assembly} from {assembly.Location}.\n" +
                    $"Available types: {availableTypes}");
            }
        }
    }

    public class TestEvent
    {
        [JsonPropertyName("correlationId")]
        public string CorrelationId { get; set; }
    }
}
