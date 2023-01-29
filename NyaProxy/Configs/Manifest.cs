using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NyaProxy.API.Config;

namespace NyaProxy.Configs
{

    public class Manifest : IManifest
    {
        [JsonPropertyName("UniqueId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string UniqueId { get; set; }

        [JsonPropertyName("Name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Name { get; set; }

        [JsonPropertyName("Author"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Author { get; set; }

        [JsonPropertyName("Description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Description { get; set; }

        [JsonPropertyName("EntryDll"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string EntryDll { get; set; }

        [JsonPropertyName("Version"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonConverter(typeof(VersionConverter))]
        public Version Version { get; set; }

        [JsonPropertyName("MinimumApiVersion"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonConverter(typeof(VersionConverter))]
        public Version MinimumApiVersion { get; set; }

        [JsonPropertyName("Checksum"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Checksum { get; set; }

        public Manifest() { }

        private class VersionConverter : JsonConverter<Version>
        {
            public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (Version.TryParse(reader.GetString(), out Version result))
                {
                    return result;
                }
                return null;
            }

            public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }

}
