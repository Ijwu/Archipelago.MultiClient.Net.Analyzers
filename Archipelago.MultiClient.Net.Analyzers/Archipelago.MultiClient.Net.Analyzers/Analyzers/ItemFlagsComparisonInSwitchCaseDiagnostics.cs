using Archipelago.MultiClient.Net.Analyzers.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Archipelago.MultiClient.Net.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ItemFlagsComparisonInSwitchCaseDiagnostics : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor NoItemFlagsComparisonsInSwitchCaseConstants = new(
            id: Constants.DiagnosticPrefix + "003",
            title: "Avoid value comparisons for ItemFlags objects in switch cases",
            messageFormat: "Avoid value comparisons for ItemFlags objects in switch cases",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            // TODO
            helpLinkUri: "https://github.com/BadMagic100/Archipelago.MultiClient.Net.Analyzers#multiclient003---avoid-using-switch-statements-with-itemflags"
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
            NoItemFlagsComparisonsInSwitchCaseConstants
        ];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeSwitch, SyntaxKind.SwitchStatement);
        }

        private void AnalyzeSwitch(SyntaxNodeAnalysisContext context)
        {
            // analyze switch statement for any cases which perform comparisons to ItemFlags object types
            SwitchStatementSyntax syntax = (SwitchStatementSyntax)context.Node;
            foreach (SwitchSectionSyntax switchSection in syntax.Sections)
            {
                foreach (SwitchLabelSyntax label in switchSection.Labels)
                {
                    if (label is CaseSwitchLabelSyntax caseLabel)
                    {
                        if (caseLabel.Value is IdentifierNameSyntax identifierName)
                        {
                            TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(identifierName, context.CancellationToken);
                            if (ArchipelagoTypeUtils.IsTypeItemFlags(typeInfo.Type, context.Compilation))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(NoItemFlagsComparisonsInSwitchCaseConstants, identifierName.GetLocation()));
                            }
                        }
                    }
                }
            }
        }
    }
}