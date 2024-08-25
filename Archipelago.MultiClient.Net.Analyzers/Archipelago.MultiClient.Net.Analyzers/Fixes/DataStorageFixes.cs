using Archipelago.MultiClient.Net.Analyzers.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
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
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            // The fix we are trying to do is to take local declarations and inline them at all usages
            // That means:
            // 1. The diagnostic must be targeting a local declaration
            // 2. The local declaration may ONLY be an index access from datastorage
            Diagnostic diagnostic = context.Diagnostics.First();
            TextSpan span = diagnostic.Location.SourceSpan;
            VariableDeclaratorSyntax? declarator = root.FindToken(span.Start).Parent?
                .FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            if (declarator == null || declarator.Initializer == null || !declarator.Initializer.Value.IsKind(SyntaxKind.ElementAccessExpression))
            {
                return;
            }

            SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if (semanticModel == null)
            {
                return;
            }

            SymbolInfo initializerInfo = semanticModel.GetSymbolInfo(declarator.Initializer.Value);
            if (initializerInfo.Symbol is not IPropertySymbol ips 
                || !ArchipelagoTypeUtils.IsTypeDataStorageHelper(ips.ContainingType, semanticModel.Compilation)
                || semanticModel.GetDeclaredSymbol(declarator) is not ISymbol symbol)
            {
                return;
            }

            IEnumerable<ReferencedSymbol> references = await SymbolFinder.FindReferencesAsync(symbol,
                context.TextDocument.Project.Solution,
                [context.Document],
                context.CancellationToken).ConfigureAwait(false);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Make DataStorage access inline",
                    createChangedDocument: c => MakeDataStorageAccessInlineAsync(
                        document: context.Document,
                        oldRoot: root,
                        declarator: declarator,
                        references: references,
                        cancellationToken: c
                    )
                ),
                diagnostic
            );
        }

        private async Task<Document> MakeDataStorageAccessInlineAsync(
            Document document,
            SyntaxNode oldRoot,
            IEnumerable<ReferencedSymbol> references,
            VariableDeclaratorSyntax declarator,
            CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            foreach (ReferenceLocation reference in references.SelectMany(r => r.Locations))
            {
                SyntaxNode node = oldRoot.FindNode(reference.Location.SourceSpan);
                if (node is ExpressionSyntax)
                {
                    editor.ReplaceNode(node, declarator.Initializer!.Value.WithTriviaFrom(node));
                }
            }
            editor.RemoveNode(declarator, SyntaxRemoveOptions.KeepUnbalancedDirectives);
            Document newDoc = editor.GetChangedDocument();
            string s = (await newDoc.GetTextAsync(cancellationToken)).ToString();
            return newDoc;
        }
    }
}
