using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ConfigData
    {
        [JsonPropertyName("settings")]
        public Dictionary<string, Setting> Settings { get; set; }

        [JsonPropertyName("config")]
        public Config Config { get; set; }

        [JsonPropertyName("exclusion")]
        public Exclusion Exclusion { get; set; }
    }

    public class Setting
    {
        [JsonPropertyName("imagefilename")]
        public string ImageFilename { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("tooltip")]
        public string Tooltip { get; set; }
    }

    public class Config
    {
        [JsonPropertyName("debug")]
        public string Debug { get; set; }

        [JsonPropertyName("web3d")]
        public string Web3d { get; set; }

        [JsonPropertyName("backgroundImagePath")]
        public string Bg { get; set; }

        [JsonPropertyName("layout")]
        public string Layout { get; set; }

        [JsonPropertyName("nodesize")]
        public int Nodesize { get; set; }

        [JsonPropertyName("translator")]
        public string Translator { get; set; }

        [JsonPropertyName("shortcut")]
        public string Shortcut { get; set; }

        [JsonPropertyName("style")]
        public string Style { get; set; }

        [JsonPropertyName("definestart")]
        public string Definestart { get; set; }

        [JsonPropertyName("left")]
        public string Left { get; set; }

        [JsonPropertyName("top")]
        public string Top { get; set; }
    }

    public class Exclusion
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
