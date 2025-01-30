using Archipelago.MultiClient.Net.Analyzers.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

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
            // analyze the switch statement for any cases which compare directly to the ItemFlags enum
            SwitchStatementSyntax syntax = (SwitchStatementSyntax)context.Node;
            foreach (SwitchSectionSyntax section in syntax.Sections)
            {
                foreach (CaseSwitchLabelSyntax label in section.Labels.OfType<CaseSwitchLabelSyntax>())
                {
                    if (label.Value is MemberAccessExpressionSyntax identifier)
                    {
                        TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(identifier);
                        if (ArchipelagoTypeUtils.IsTypeItemFlags(typeInfo.Type, context.Compilation))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(NoItemFlagsComparisonsInSwitchCaseConstants, label.GetLocation()));
                        }
                    }
                }
            }

        }
    }
}