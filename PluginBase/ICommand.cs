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
        public object Body { get; set; }
    }
}
