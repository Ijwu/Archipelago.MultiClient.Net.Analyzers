using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Archipelago.MultiClient.Net.Analyzers.Util
{
    internal static class ArchipelagoSyntaxFactory
    {
        public static FieldDeclarationSyntax CreateDataStorageProperty(string fieldName, string sessionName,
            IEnumerable<ArgumentSyntax> dataStorageAccessArgs)
        {
            IEnumerable<AttributeArgumentSyntax> convertedAttributeArgs = dataStorageAccessArgs
                .Select(a => AttributeArgument(null, a.NameColon, a.Expression));
            IEnumerable<AttributeArgumentSyntax> attributeArgNodes = [
                AttributeArgument(
                    InvocationExpression(
                        IdentifierName(
                            Identifier(
                                TriviaList(),
                                SyntaxKind.NameOfKeyword,
                                "nameof",
                                "nameof",
                                TriviaList()
                            )
                        )
                    )
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    IdentifierName(sessionName)
                                )
                            )
                        )
                    )
                ),
                .. convertedAttributeArgs
            ];

            return FieldDeclaration(
                VariableDeclaration(
                    IdentifierName("DataStorageElement")
                )
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(fieldName)
                        )
                    )
                )
            )
            .WithAttributeLists(
                SingletonList(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(
                                IdentifierName("DataStorageProperty")
                            )
                            .WithArgumentList(
                                AttributeArgumentList(
                                    SeparatedList(
                                        attributeArgNodes
                                    )
                                )
                            )
                        )
                    )
                )
            )
            .WithModifiers(
                TokenList(
                    [
                        Token(SyntaxKind.PrivateKeyword),
                        Token(SyntaxKind.ReadOnlyKeyword),
                    ]
                )
            );
        }
    }
}
