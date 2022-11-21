using System.Text.Json.Serialization;

namespace PluginBase
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }

        int Execute();

        EventData _eventData { get; }
    }

    public class EventData
    {
        public EventBody Body { get; set; }
    }

    public class EventBody
    {
        [JsonPropertyName("correlationId")]
        public string CorrelationId { get; set; } = "776C7C4C-5D6E-4B04-B404-DD4042C81199";
    }
}
