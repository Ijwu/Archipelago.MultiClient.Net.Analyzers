using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Archipelago.MultiClient.Net.Analyzers.Fixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class ItemFlagsInSwitchFixes : CodeFixProvider
    {
        public const string FixKeyConvertItemFlagsSwitch = "ConvertItemFlagsSwitch";

        public override ImmutableArray<string> FixableDiagnosticIds => [
            Constants.DiagnosticPrefix + "003"
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

            Diagnostic diagnostic = context.Diagnostics.First();
            TextSpan span = diagnostic.Location.SourceSpan;
            SwitchStatementSyntax? switchStatement = root.FindToken(span.Start).Parent?
                .FirstAncestorOrSelf<SwitchStatementSyntax>();
            if (switchStatement == null)
            {
                return;
            }

            List<SyntaxNode> caseTargets = new();
            foreach (SwitchSectionSyntax switchSection in switchStatement.Sections)
            {
                IEnumerable<SyntaxNode> nodesToMove = switchSection.Labels
                    .OfType<CaseSwitchLabelSyntax>()
                    .SelectMany(label => label.DescendantNodes());

                caseTargets.AddRange(nodesToMove);
            }

            context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Convert ItemFlags switch to if/else",
                        createChangedDocument: c => ConvertHasFlagsSwitchToIfElse(
                            document: context.Document,
                            oldRoot: root,
                            switchStatement: switchStatement,
                            cancellationToken: c
                        ),
                        equivalenceKey: FixKeyConvertItemFlagsSwitch
                    ),
                    diagnostic
                );
        }

        private async Task<Document> ConvertHasFlagsSwitchToIfElse(
            Document document,
            SyntaxNode oldRoot,
            SwitchStatementSyntax switchStatement,
            CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            var switchSections = editor.Generator.GetSwitchSections(switchStatement);
            var switchExpression = switchStatement.Expression;

            // This should be an empty SyntaxList based on the default.
            SyntaxList<StatementSyntax> @else = default;
            SyntaxNode current = null;

            // Iterating in reverse order to generate the if-statement chain from the bottom up.
            foreach (var node in switchSections.Reverse())
            {
                SwitchSectionSyntax switchSection = (SwitchSectionSyntax)node;
                List<ExpressionSyntax> cases = switchSection.Labels.OfType<CaseSwitchLabelSyntax>().Select(x => x.Value).ToList();
                
                if (cases.Count == 0 || switchSection.Labels.OfType<DefaultSwitchLabelSyntax>().Any())
                {
                    // If no `CaseSwitchLabelSyntax` elements, then this should be a `DefaultSwitchLabelSyntax` marking a `case default:`.
                    // In that case, I need to grab the statement to form the `else` of the if-statement
                    @else = switchSection.Statements;
                    continue;
                }
                
                SyntaxNode condition = GenerateHasFlagsInvocation(editor, cases, switchExpression);
                if (current is null)
                {
                    current = editor.Generator.IfStatement(condition, switchSection.Statements, @else);
                }
                else
                {
                    current = editor.Generator.IfStatement(condition, switchSection.Statements, current);
                }
            }

            editor.ReplaceNode(switchStatement, current);

            Document newDoc = editor.GetChangedDocument();
            return newDoc;
        }

        private SyntaxNode GenerateHasFlagsInvocation(DocumentEditor editor, List<ExpressionSyntax> cases, ExpressionSyntax switchExpression)
        {
            SyntaxNode GenerateInvocation(ExpressionSyntax expression) => 
                editor.Generator.InvocationExpression(editor.Generator.MemberAccessExpression(switchExpression, "HasFlag"), expression);

            if (cases.Count == 1)
            {
                return GenerateInvocation(cases[0]);
            }

            SyntaxNode current = editor.Generator.LogicalOrExpression(GenerateInvocation(cases[0]), GenerateInvocation(cases[1]));
            foreach (var target in cases.Skip(2))
            {
                current = editor.Generator.LogicalOrExpression(current, GenerateInvocation(target));    
            }

            return current;
        }
    }
}
