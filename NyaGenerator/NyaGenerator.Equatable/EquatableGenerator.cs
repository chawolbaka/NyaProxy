using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace NyaGenerator.Equatable
{
    [Generator]
    public class EquatableGenerator : ISourceGenerator
    {
        private const string EquatableAttributeText = @"
using System;
namespace NyaGenerator.Equatable
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class EquatableAttribute : Attribute
    {

    }
}
";
        private const string IgnoreEqualityAttributeText = @"
using System;
namespace NyaGenerator.Equatable
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class IgnoreEqualityAttribute : Attribute
    {

    }
}
";

        private readonly HashSet<string> baseTypes = new HashSet<string>() { "bool", "byte", "sbyte", "short", "ushort", "uint", "int", "ulong", "long", "float", "double", "DateTime", "Decimal" };

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver && receiver.Pairs.Count > 0))
                return;

            foreach (var pair in receiver.Pairs)
            {
                if (pair.Value.Count == 0)
                    continue;

                string className = pair.Key.Identifier.ValueText;

                if (pair.Key.TypeParameterList != null)
                {
                    className += $"<{string.Join(", ", pair.Key.TypeParameterList.Parameters)}>";
                }

                StringBuilder getHashCode = new StringBuilder(@"
        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();
");
                StringBuilder source = new StringBuilder($@"using System;
using System.Diagnostics.CodeAnalysis;

namespace {(pair.Key.Parent as NamespaceDeclarationSyntax).Name}
{{

    public partial class {className} : IEquatable<{className}>
    {{

        public static bool operator ==([AllowNull]{className} left, [AllowNull]{className} right) => (ReferenceEquals(left, null) && ReferenceEquals(right, null)) || !ReferenceEquals(left, null) && left.Equals(right);
        public static bool operator !=([AllowNull]{className} left, [AllowNull]{className} right) => !(left == right);

        public override bool Equals(object? obj)
        {{
            return Equals(obj as {className});
        }}

        public bool Equals({className}? other)
        {{
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(other, null))
                return false;
");
                var b = pair.Key.BaseList?.Types.FirstOrDefault();
                if (b != null)
                {
                    string type = b.Type.ToString();
                    if (type[0] != 'I')
                    {
                        getHashCode.AppendLine($"            hashCode.Add(base.GetHashCode());");
                        source.AppendLine($@"
            if (this is IEquatable<{type}> && other is IEquatable<{type}> && !Equals(other as {type}))
                return false;");
                    }
                }

                foreach (var property in pair.Value)
                {
                    if (property.AttributeLists.Any(a => a.Attributes.Any(x => x.Name.ToString() == "IgnoreEquality" || x.Name.ToString() == "IgnoreEqualityAttribute")))
                        continue;

                    getHashCode.AppendLine($"            hashCode.Add({property.Identifier.Text});");

                    if (baseTypes.Contains(property.Type.ToString()))
                    {
                        source.AppendLine($@"
            if ({property.Identifier.Text} != other.{property.Identifier.Text})
                return false;");
                    }
                    else
                    {
                        source.AppendLine($@"
            if (ReferenceEquals({property.Identifier.Text}, null)  && !ReferenceEquals(other.{property.Identifier.Text}, null))
                return false;
            if (!ReferenceEquals({property.Identifier.Text}, null) && ReferenceEquals(other.{property.Identifier.Text}, null))
                return false;
            if (!ReferenceEquals({property.Identifier.Text}, null) && !{property.Identifier.Text}.Equals(other.{property.Identifier.Text}))
                return false;
");
                    }
                }

                getHashCode.Append(@"
            return hashCode.ToHashCode();
        }");
                source.AppendLine(@"
            return true;
        }");
                source.Append(getHashCode);
                source.Append(@"
    }
}");

                context.AddSource($"{pair.Key.Identifier.ValueText}.Equatable.cs", source.ToString());

            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((c) =>
            {
                c.AddSource("EquatableAttribute.cs", EquatableAttributeText);
                c.AddSource("IgnoreEqualityAttribute.cs", IgnoreEqualityAttributeText);
            });
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<KeyValuePair<ClassDeclarationSyntax, List<PropertyDeclarationSyntax>>> Pairs = new List<KeyValuePair<ClassDeclarationSyntax, List<PropertyDeclarationSyntax>>>();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is ClassDeclarationSyntax cds && cds.AttributeLists.Any(a => a.Attributes.Any(x => x.Name.ToString() == "Equatable" || x.Name.ToString() == "EquatableAttribute")))
                {
                    Pairs.Add(new KeyValuePair<ClassDeclarationSyntax, List<PropertyDeclarationSyntax>>(cds,
                        new List<PropertyDeclarationSyntax>(cds.Members.OfType<PropertyDeclarationSyntax>().Where(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))))));
                }
            }

        }

    }
}
