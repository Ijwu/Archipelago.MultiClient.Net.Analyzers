using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
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
    public class ItemFlagsInSwitchFixes : CodeFixProvider
    {
        public const string FixKeyConvertItemFlagsSwitch = "ConvertItemFlagsSwitch";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            Constants.DiagnosticPrefix + "003"
        );

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
            CaseSwitchLabelSyntax? caseLabel = root.FindToken(span.Start).Parent?
                .FirstAncestorOrSelf<CaseSwitchLabelSyntax>();
            if (caseLabel == null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Convert case to use HasFlag",
                    createChangedDocument: c => ConvertCaseToHasFlag(
                        document: context.Document,
                        caseLabel: caseLabel,
                        cancellationToken: c
                    ),
                    equivalenceKey: FixKeyConvertItemFlagsSwitch
                ),
                diagnostic
            );
        }

        private async Task<Document> ConvertCaseToHasFlag(
            Document document,
            CaseSwitchLabelSyntax caseLabel,
            CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            SwitchSectionSyntax switchSection = (SwitchSectionSyntax)caseLabel.Parent!;
            SwitchStatementSyntax switchStatement = (SwitchStatementSyntax)switchSection.Parent!;
            ExpressionSyntax switchExpression = switchStatement.Expression;
            
            string variableName = GetPatternMatchingVariableName(editor, switchStatement);

            // Generate the pattern matching case label
            CasePatternSwitchLabelSyntax newCaseLabel = GeneratePatternMatchingCaseLabel(caseLabel, switchExpression, variableName);

            // Replace the old case label with the new case label
            editor.ReplaceNode(caseLabel, newCaseLabel);

            Document newDoc = editor.GetChangedDocument();
            return newDoc;
        }

        private static string GetPatternMatchingVariableName(DocumentEditor editor, SwitchStatementSyntax switchStatement)
        {
            DataFlowAnalysis? analysis = editor.SemanticModel.AnalyzeDataFlow(switchStatement);

            if (analysis is null)
            {
                return "f";
            }

            bool hasCollision = analysis.WrittenInside.Any(x => x.Name.StartsWith("f"));

            if (hasCollision)
            {
                IEnumerable<ISymbol> collisions = analysis.WrittenInside.Where(x => x.Name.StartsWith("f") && int.TryParse(x.Name[1..], out var _));
                IEnumerable<int> existingNumberedFVars = collisions.Select(x => int.Parse(x.Name[1..]));

                if (!existingNumberedFVars.Any())
                {
                    return $"f1";
                }

                return $"f{existingNumberedFVars.Max() + 1}";
            }

            return "f";
        }

        private CasePatternSwitchLabelSyntax GeneratePatternMatchingCaseLabel(CaseSwitchLabelSyntax caseLabel, ExpressionSyntax switchExpression, string newVarName)
        {
            // Create the pattern matching statement: "case var f when f.HasFlag(ItemFlags.Advancement):"
            SingleVariableDesignationSyntax variableDesignation = SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(newVarName));
            VarPatternSyntax varPattern = SyntaxFactory.VarPattern(SyntaxFactory.Token(SyntaxKind.VarKeyword), variableDesignation);

            WhenClauseSyntax whenClause = SyntaxFactory.WhenClause(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(newVarName),
                        SyntaxFactory.IdentifierName("HasFlag")
                    ),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(caseLabel.Value)))
                )
            );

            return SyntaxFactory.CasePatternSwitchLabel(varPattern, whenClause, SyntaxFactory.Token(SyntaxKind.ColonToken));
        }
    }
}
