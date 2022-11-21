using PluginBase;
using System;
using System.Text.Json.Serialization;

namespace HelloPlugin
{
    public class HelloCommand : ICommand
    {
        public string Name { get => "hello"; }
        public string Description { get => "Displays hello message."; }

        public EventData _eventData => new EventData() { Body = new EventBody_Custom() { SomeProp = "TestProp" } };

        public int Execute()
        {
            Console.WriteLine("Hello !!!");
            return 0;
        }
    }

    public class EventBody_Custom : EventBody
    {
        [JsonPropertyName("myCustomProperty")]
        public string SomeProp { get; set; } = "Test";
    }
}
