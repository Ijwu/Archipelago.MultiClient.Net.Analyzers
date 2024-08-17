using Archipelago.MultiClient.Net.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Archipelago.MultiClient.Net.Analyzers.Fixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class DataStorageFixes : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => [
            Constants.DiagnosticPrefix + "001"
        ];

        public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            // The fix we are trying to do is to inline any variables on the LHS of the assignment.
            // That means the fix can only apply if:
            // 1. The diagnostic is applied to an assignment
            // 2. The definition of the LHS is an indexed element out of datastorage
            Diagnostic diagnostic = context.Diagnostics.First();
            TextSpan span = diagnostic.Location.SourceSpan;
            AssignmentExpressionSyntax? assignment = root.FindToken(span.Start).Parent?
                .FirstAncestorOrSelf<AssignmentExpressionSyntax>();
            if (assignment == null)
            {
                return;
            }

            SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if (semanticModel == null)
            {
                return;
            }
            SymbolInfo assignmentSymbolInfo = semanticModel.GetSymbolInfo(assignment.Left);
            if (assignmentSymbolInfo.Symbol is not ILocalSymbol ils)
            {
                return;
            }
            TextSpan declarationSpan = assignmentSymbolInfo.Symbol.Locations.First().SourceSpan;
            LocalDeclarationStatementSyntax? declaration = root.FindToken(declarationSpan.Start).Parent?
                .FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
            if (declaration == null)
            {
                return;
            }
            VariableDeclaratorSyntax? variableDefinition = declaration.Declaration.Variables
                .FirstOrDefault(v => v.Identifier.Text == assignmentSymbolInfo.Symbol.Name);
            if (variableDefinition == null || variableDefinition.Initializer == null)
            {
                return;
            }
            // inlining is only possible if the declaration is an index access from datastorage
            if (!variableDefinition.Initializer.Value.IsKind(SyntaxKind.ElementAccessExpression))
            {
                return;
            }
            SymbolInfo initializerSymbolInfo = semanticModel.GetSymbolInfo(variableDefinition.Initializer.Value);
            if (initializerSymbolInfo.Symbol is not IPropertySymbol ips 
                || !DataStorageDiagnostics.IsTypeDataStorageHelper(ips.ContainingType, semanticModel.Compilation))
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Make DataStorage access inline",
                    createChangedDocument: c => Task.Run(() => MakeDataStorageAccessInline(
                        document: context.Document,
                        oldRoot: root,
                        assignment: assignment,
                        initialValue: variableDefinition.Initializer.Value
                    ), c)
                ),
                diagnostic
            );
        }

        private Document MakeDataStorageAccessInline(
            Document document,
            SyntaxNode oldRoot,
            AssignmentExpressionSyntax assignment, 
            ExpressionSyntax initialValue)
        {
            AssignmentExpressionSyntax newAssignment = assignment.WithLeft(initialValue.WithTriviaFrom(assignment.Left)).WithTrailingTrivia(new WhiteSpaceTrivia;
            SyntaxNode newRoot = oldRoot.ReplaceNode(assignment, newAssignment);
            string s = newRoot.ToFullString();
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
