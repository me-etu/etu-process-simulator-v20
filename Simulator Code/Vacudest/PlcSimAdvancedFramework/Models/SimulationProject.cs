using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;

namespace PlcSimAdvancedFramework.Models
{
    [XmlRoot("SimulationProject")]
    public class SimulationProject
    {
        public string Name { get; set; }

        [XmlArray("Tags")]
        [XmlArrayItem("Tag")]
        public ObservableCollection<TagDefinition> Tags { get; set; } = new ObservableCollection<TagDefinition>();

        public static SimulationProject Load(string path)
        {
            var serializer = new XmlSerializer(typeof(SimulationProject));
            using (var stream = File.OpenRead(path))
            {
                return (SimulationProject)serializer.Deserialize(stream);
            }
        }

        public void Save(string path)
        {
            var serializer = new XmlSerializer(typeof(SimulationProject));
            using (var stream = File.Create(path))
            {
                serializer.Serialize(stream, this);
            }
        }
    }
}
