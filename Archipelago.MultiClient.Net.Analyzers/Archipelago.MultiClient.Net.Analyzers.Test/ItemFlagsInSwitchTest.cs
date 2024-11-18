using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VerifyCS = Archipelago.MultiClient.Net.Analyzers.Test.CSharpCodeFixVerifier<
    Archipelago.MultiClient.Net.Analyzers.Analyzers.ItemFlagsInSwitchDiagnostics,
    Archipelago.MultiClient.Net.Analyzers.Fixes.ItemFlagsFixes>;

namespace Archipelago.MultiClient.Net.Analyzers.Test
{
    [TestClass]
    public class ItemFlagsInSwitchTest
    {
        [TestMethod]
        public async Task VerifyItemFlagsInSwitchStatementYieldsDiagnostic()
        {
            string test = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        public bool Test()
        {
            ItemFlags i = ItemFlags.Advancement;
            switch ({|#0:i|})
            {
                case ItemFlags.Advancement:
                    return true;
                default:
                    return false;
            }
        }
    }
}";
            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
