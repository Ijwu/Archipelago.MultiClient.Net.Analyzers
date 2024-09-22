using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Archipelago.MultiClient.Net.Analyzers.Generators
{
    [Generator(LanguageNames.CSharp)]
    public class DataStoragePropertyGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new DataStorageAttributeReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not DataStorageAttributeReceiver rec || rec.Containers.Count == 0)
            {
                return;
            }

            foreach (DataStorageContainer container in rec.Containers)
            {
                StringBuilder source = new($@"
#nullable enable annotations

using Archipelago.MultiClient.Net.Models;

namespace {container.ContainingType.ContainingNamespace.ToDisplayString()}
{{
    {SyntaxFacts.GetText(container.ContainingType.DeclaredAccessibility)} partial class {container.ContainingType.Name}
    {{".TrimStart('\r', '\n'));
                
                foreach (DataStorageField field in container.Fields)
                {
                    string propName = GeneratePropName(field.Field.Name);
                    ImmutableArray<TypedConstant> args = field.Data.ConstructorArguments;
                    string referenceText;
                    if (args.Length == 2)
                    {
                        // session, key
                        referenceText = $"{args[0].Value}.DataStorage[{args[1].ToCSharpString()}]";
                    }
                    else
                    {
                        // session, scope, key
                        referenceText = $"{args[0].Value}.DataStorage[{args[1].ToCSharpString()}, {args[2].ToCSharpString()}]";
                    }

                    source.AppendLine($@"
        [System.CodeDom.Compiler.GeneratedCode(tool: ""{nameof(DataStoragePropertyGenerator)}"", version: null)]
        private DataStorageElement {propName}
        {{
            get => {referenceText};
            set => {referenceText} = value;
        }}");
                }

                source.AppendLine(@"
    }
}".TrimStart('\r', '\n'));
                context.AddSource(container.ContainingType.Name + ".g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
            }
        }

        public static string GeneratePropName(string fieldName)
        {
            string propName = fieldName.Trim('_');
            propName = char.ToUpper(propName[0]) + propName[1..];
            return propName;
        }
    }
}
