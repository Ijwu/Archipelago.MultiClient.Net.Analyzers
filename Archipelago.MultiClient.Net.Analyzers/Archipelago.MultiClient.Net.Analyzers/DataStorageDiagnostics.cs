using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Archipelago.MultiClient.Net.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DataStorageDiagnostics : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor NoElementsAssignedOutsideDataStorageHelper = new(
            id: Constants.DiagnosticPrefix + "001",
            title: "Data storage element assigned outside of data storage",
            messageFormat: "Data storage element assigned outside of data storage",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Data storage operations are generally only sent to the server when reassigning data storage elements to " +
                         "the data storage helper. To prevent bugs, it is best to avoid assigning data storage elements other than " +
                         "to the data storage helper, e.g. to variables."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
            NoElementsAssignedOutsideDataStorageHelper
        ];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeAssignment,
                SyntaxKind.LocalDeclarationStatement,
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.AddAssignmentExpression,
                SyntaxKind.SubtractAssignmentExpression,
                SyntaxKind.MultiplyAssignmentExpression,
                SyntaxKind.DivideAssignmentExpression,
                SyntaxKind.ModuloAssignmentExpression,
                SyntaxKind.AndAssignmentExpression,
                SyntaxKind.OrAssignmentExpression,
                SyntaxKind.ExclusiveOrAssignmentExpression,
                SyntaxKind.LeftShiftAssignmentExpression,
                SyntaxKind.RightShiftAssignmentExpression,
                SyntaxKind.UnsignedRightShiftAssignmentExpression,
                SyntaxKind.CoalesceAssignmentExpression);
        }

        private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            INamedTypeSymbol? wantedType = context.SemanticModel.Compilation.GetTypeByMetadataName("Archipelago.MultiClient.Net.Models.DataStorageElement");

            if (context.Node is LocalDeclarationStatementSyntax lds)
            {
                ITypeSymbol? type = context.SemanticModel.GetTypeInfo(lds.Declaration.Type).Type;
                if (!IsTypeDataStorageElement(type, context.SemanticModel.Compilation))
                {
                    return;
                }
            }
            else
            {
                AssignmentExpressionSyntax node = (AssignmentExpressionSyntax)context.Node;
                ITypeSymbol? type = context.SemanticModel.GetTypeInfo(node.Left).Type;
                if (!IsTypeDataStorageElement(type, context.SemanticModel.Compilation))
                {
                    return;
                }
                if (node.Left.IsKind(SyntaxKind.ElementAccessExpression))
                {
                    // inline reassignments to DataStorageHelper are fine and expected
                    SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(node.Left);
                    if (symbol.Symbol is IPropertySymbol ips && IsTypeDataStorageHelper(ips.ContainingType, context.SemanticModel.Compilation))
                    {
                        return;
                    }
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(NoElementsAssignedOutsideDataStorageHelper, context.Node.GetLocation()));
        }

        private bool IsTypeDataStorageElement(ITypeSymbol? type, Compilation compilation)
        {
            INamedTypeSymbol? dataStorageElement = compilation.GetTypeByMetadataName("Archipelago.MultiClient.Net.Models.DataStorageElement");
            return type != null && dataStorageElement != null && dataStorageElement.Equals(type, SymbolEqualityComparer.Default);
        }

        private bool IsTypeDataStorageHelper(ITypeSymbol? type, Compilation compilation)
        {
            INamedTypeSymbol? iDataStorageHelper = compilation.GetTypeByMetadataName("Archipelago.MultiClient.Net.Helpers.IDataStorageHelper");
            return type != null && iDataStorageHelper != null && (
                iDataStorageHelper.Equals(type, SymbolEqualityComparer.Default)
                || type.AllInterfaces.Contains(iDataStorageHelper, SymbolEqualityComparer.Default)
            );
        }
    }
}
