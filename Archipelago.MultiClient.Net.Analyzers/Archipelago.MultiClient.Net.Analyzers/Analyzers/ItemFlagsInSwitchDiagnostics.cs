using Archipelago.MultiClient.Net.Analyzers.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Archipelago.MultiClient.Net.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ItemFlagsInSwitchDiagnostics : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor DoNotUseItemFlagsInSwitchConstants = new(
            id: Constants.DiagnosticPrefix + "003",
            title: "Avoid using switch statements with ItemFlags",
            messageFormat: "Avoid using switch statements with ItemFlags",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/BadMagic100/Archipelago.MultiClient.Net.Analyzers#multiclient003---avoid-using-switch-statements-with-itemflags"
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
            DoNotUseItemFlagsInSwitchConstants
        ];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeSwitch, SyntaxKind.SwitchStatement);
        }

        private void AnalyzeSwitch(SyntaxNodeAnalysisContext context)
        {
            SwitchStatementSyntax syntax = (SwitchStatementSyntax)context.Node;
            TypeInfo switchType = context.SemanticModel.GetTypeInfo(syntax.Expression, context.CancellationToken);

            if (!ArchipelagoTypeUtils.IsTypeItemFlags(switchType.Type, context.Compilation))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(DoNotUseItemFlagsInSwitchConstants, syntax.Expression.GetLocation()));
        }
    }
}