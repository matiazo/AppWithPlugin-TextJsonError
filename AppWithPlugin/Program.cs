using Azure.Messaging.EventGrid;
using PluginBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
                            Console.WriteLine("Checking property is deserialized correctly, TestEvent is in DefaultContext");
                            var testEvent = new EventGridEvent("Test", "Type", "1.0", new TestEvent() { CorrelationId = "1-2-3-4" });
                            var testEventJson = testEvent.Data.ToString();
                            if (testEventJson.Contains("correlationId") == false)
                            {
                                Console.WriteLine($"***unable to find \"correlationId\"***");

                            }
                            Console.WriteLine($"Actual testEventJson: {testEventJson}");

                            Console.WriteLine();

                            Console.WriteLine("check property is deserialized correctly, EventData is different context");
                            var eventDataBody = new EventGridEvent("Test", "Type", "1.0", command._eventData.Body);
                            var eventDataBodyJson = eventDataBody.Data.ToString();
                            if (testEventJson.Contains("myCustomProperty") == false)
                            {
                                Console.WriteLine($"***unable to find \"myCustomProperty\"***");
                            }
                            Console.WriteLine($"Actual EventData Json: {eventDataBodyJson}");


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
