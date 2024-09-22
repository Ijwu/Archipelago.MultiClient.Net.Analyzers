using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Archipelago.MultiClient.Net.Analyzers.Util
{
    internal static class ArchipelagoSyntaxFactory
    {
        public static FieldDeclarationSyntax CreateDataStorageProperty(
            SyntaxGenerator generator,
            string fieldName, 
            string sessionName,
            IEnumerable<ArgumentSyntax> dataStorageAccessArgs)
        {
            IEnumerable<AttributeArgumentSyntax> convertedAttributeArgs = dataStorageAccessArgs
                .Select(a => AttributeArgument(null, a.NameColon, a.Expression));
            IEnumerable<SyntaxNode> attributeArgs = [
                generator.AttributeArgument(generator.NameOfExpression(generator.IdentifierName(sessionName))),
                .. convertedAttributeArgs
            ];

            SyntaxNode decl = generator.FieldDeclaration(
                fieldName, 
                generator.IdentifierName("DataStorageElement"), 
                Accessibility.Private, 
                DeclarationModifiers.ReadOnly
            );
            return (FieldDeclarationSyntax) generator.AddAttributes(
                decl,
                generator.Attribute("DataStorageProperty", attributeArgs)
            );
        }
    }
}
