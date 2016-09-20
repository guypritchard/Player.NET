using System;
using System.IO;
using System.Xml.Serialization;

namespace DJPad.Core.Utils
{
    [XmlRoot("Configuration")]
    public class PlayerConfiguration : ApplicationConfiguration
    {
        public const string ApplicationName = "Player.Net.2";
        public const string Name = "State.xml";

        public string PlaylistName { get; set; }

        public string LastFile { get; set; }

        public int Volume { get; set; }

        public int Mode { get; set; }
        
        public int Visualisation { get; set; }

        public bool Randomise { get; set; }

        public bool PlaylistVisible { get; set; }

        public string WatchDirectory { get; set; }

        [XmlIgnore]
        public static string DefaultLocation
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ApplicationName, 
                    Name);
            }
        }

        public void Save()
        {
            this.Save(PlayerConfiguration.DefaultLocation);
        }
    }

    public class ApplicationConfiguration
    {
        public static T Load<T>(string fileName) where T : ApplicationConfiguration, new()
        {
            var result = new T();

            if (File.Exists(fileName))
            {
                using (FileStream stream = File.OpenRead(fileName))
                {
                    try
                    {
                        result = new XmlSerializer(typeof(T)).Deserialize(stream) as T;
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }

            return result;
        }

        protected void Save(string fileName)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                new XmlSerializer(this.GetType()).Serialize(stream, this);
            }
        }
    }
}
