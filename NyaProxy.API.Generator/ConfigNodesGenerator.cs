using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace NyaProxy.API.Generator
{
    [Generator]
    public class ConfigNodesGenerator : ISourceGenerator
    {
        private static StringBuilder ReaderPartial;
        private static StringBuilder WriterPartial;
        private static StringBuilder ConfigNodePartial;
        private static Dictionary<string,string> sources = new Dictionary<string, string>();
        private void Setup()
        {
            Dictionary<string, string> types = new Dictionary<string, string>
            {
                ["Boolean"] = "bool",
                ["Number"] = "long",
                ["Float"] = "float",
                ["Double"] = "double",
                ["DateTime"] = "DateTime",
                ["String"] = "string"
            };
            Dictionary<string, string> readTypes = new Dictionary<string, string>
            {
                ["BooleanNode"] = "bool",
                ["NumberNode"] = "long",
                ["FloatNode"] = "float",
                ["DoubleNode"] = "double",
                ["DateTimeNode"] = "DateTime",
                ["StringNode"] = "string",
                ["ArrayNode"] = "NULL",
                ["ObjectNode"] = "NULL"

            };
            
            WriterPartial = new StringBuilder(@"using System;

namespace NyaProxy.API
{
    public abstract partial class ConfigWriter
    {");
            foreach (var type in types)
            {

                WriterPartial.AppendLine($"        public virtual ConfigWriter WriteProperty(string key, {type.Key}Node node) => WriteProperty(key, (ConfigNode)node);");
                WriterPartial.AppendLine($"        public virtual ConfigWriter WriteProperty(string key, {type.Value} value) => WriteProperty(key, (ConfigNode)new {type.Key}Node(value));");
            }

            WriterPartial.AppendLine(@"    }
}");

            ReaderPartial = new StringBuilder(@"using System;

namespace NyaProxy.API
{
    public abstract partial class ConfigReader
    {

");
            foreach (var type in readTypes)
            {
                ReaderPartial.AppendLine($"        public virtual {type.Key} Read{type.Key.Replace("Node", "")}Property(string key) => ({type.Key})ReadProperty(key);");
                ReaderPartial.AppendLine($@"        public virtual bool TryRead{type.Key.Replace("Node", "")}(string key, out {type.Key} result)
        {{
            result = null;
            if (!ContainsKey(key))
                return false;

            result = ReadProperty(key) as {type.Key};
            return result != null;
        }}");
                if (type.Value != "NULL")
                {
                    ReaderPartial.AppendLine($@"
        public virtual bool TryRead{type.Key.Replace("Node", "")}(string key, out {type.Value} result)
        {{
            result = default;
            if (!ContainsKey(key))
                return false;

            ConfigNode node = ReadProperty(key);
            if (node is {type.Key})
            {{
                result = (({type.Key})node).Value;
                return true;
            }}
            else
            {{
                return false;
            }}
        }}");
                }

            }
            ReaderPartial.AppendLine(@"    }
}");


            ConfigNodePartial = new StringBuilder($@"using System;

namespace NyaProxy.API
{{
    public abstract partial class ConfigNode
    {{

");

            foreach (var type in types)
            {

                ConfigNodePartial.AppendLine($"        public static explicit operator {type.Value}(ConfigNode node) => (({type.Key}Node)node).Value;");


                sources.Add($"{type.Key}Node.cs", $@"using System;
namespace NyaProxy.API
{{

    public class {type.Key}Node : ConfigNode
    {{
        public virtual {type.Value} Value {{ get; set; }}

        public {type.Key}Node({type.Value} value)
        {{
            Value = value;
        }}
        
        public {type.Key}Node({type.Value} value, string precedingComment)
        {{
            if (!string.IsNullOrWhiteSpace(precedingComment))
                Comment = new ConfigComment(precedingComment);
            Value = value;
        }}
        public {type.Key}Node({type.Value} value, string precedingComment, string inlineComment)
        {{
            if (!string.IsNullOrWhiteSpace(precedingComment) && string.IsNullOrWhiteSpace(inlineComment))
                Comment = new ConfigComment(precedingComment, inlineComment);
            Value = value;
        }}
        public {type.Key}Node({type.Value} value, ConfigComment comment)
        {{
            if (comment!=null)
                Comment = comment;
            Value = value;
        }}

        public static implicit operator {type.Value}({type.Key}Node node) => node.Value;

        public override string ToString()
        {{
            return Value.ToString();
        }}
    }}
}}
");
            }


            ConfigNodePartial.AppendLine(@"    }
}");
        }


        public void Execute(GeneratorExecutionContext context)
        {
            if (sources.Count == 0)
                Setup();


            foreach (var source in sources)
            {
                context.AddSource(source.Key, source.Value);
            }
            context.AddSource("ConfigWriter.Partial.cs", WriterPartial.ToString());
            context.AddSource("ConfigReader.Partial.cs", ReaderPartial.ToString());
            context.AddSource("ConfigNode.Explicit.cs", ConfigNodePartial.ToString());
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
