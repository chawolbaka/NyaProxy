using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NyaProxy.API;
using NyaProxy.API.Config;

namespace NyaProxy.Configs
{

    public class Manifest : IManifest
    {
        [JsonProperty("UniqueId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string UniqueId { get; set; }

        [JsonProperty("Name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("Author", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Author { get; set; }

        [JsonProperty("Description", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("EntryDll", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string EntryDll { get; set; }

        [JsonProperty("Version", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(VersionConverter))]
        public Version Version { get; set; }

        [JsonProperty("MinimumApiVersion", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(VersionConverter))]
        public Version MinimumApiVersion { get; set; }

        [JsonProperty("Checksum", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Checksum { get; set; }

        public Manifest() { }

        private class VersionConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Version);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.ValueType == typeof(string))
                {
                    if (Version.TryParse(reader.Value.ToString(), out Version result))
                    {
                        return result;
                    }
                }
                return reader.Value;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value.GetType() == typeof(Version))
                {
                    writer.WriteValue(value.ToString());
                }
            }
        }
    }

}
