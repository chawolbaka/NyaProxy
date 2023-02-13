using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NyaProxy.API.Generator
{
    [Generator]
    public class ConfigNodesGenerator : ISourceGenerator
    {
        private static Dictionary<string,string> types = new Dictionary<string, string>
        {
            ["Boolean"] = "bool",
            ["Number"] = "long",
            ["Float"] = "float",
            ["Double"] = "double",
            ["DateTime"] = "DateTime",
            ["String"] = "string"
        };
        public void Execute(GeneratorExecutionContext context)
        {


            StringBuilder configNodePartial = new StringBuilder($@"using System;

namespace NyaProxy.API
{{
    public abstract partial class ConfigNode
    {{

");
            
            foreach (var type in types)
            {

                configNodePartial.AppendLine($"        public static explicit operator {type.Value}(ConfigNode node) => (({type.Key}Node)node).Value;");


                context.AddSource($"{type.Key}Node.cs", $@"using System;
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

            configNodePartial.AppendLine(@"    }
}");
            context.AddSource("ConfigNode.Explicit.cs", configNodePartial.ToString());
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
