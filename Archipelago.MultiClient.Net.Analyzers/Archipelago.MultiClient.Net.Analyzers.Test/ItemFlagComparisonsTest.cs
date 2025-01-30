using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Archipelago.MultiClient.Net.Analyzers.Test.CSharpCodeFixVerifier<
    Archipelago.MultiClient.Net.Analyzers.Analyzers.ItemFlagsComparisonInSwitchCaseDiagnostics,
    Archipelago.MultiClient.Net.Analyzers.Fixes.ItemFlagsInSwitchFixes>;

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
            switch (i)
            {
                case {|#0:ItemFlags.Advancement|}:
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

        [TestMethod]
        public async Task VerifyMultipleItemFlagsInSwitchStatementYieldsDiagnostics()
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
            switch (i)
            {
                case {|#0:ItemFlags.Advancement|}:
                    return true;
                case {|#1:ItemFlags.None|}:
                    return false;
                default:
                    return false;
            }
        }
    }
}";
            DiagnosticResult[] expected = [
                VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0), 
                VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(1)
            ];
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task VerifyMixedUseItemFlagsInSwitchStatementYieldsDiagnostic()
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
            switch (i)
            {
                case var f when i.HasFlag(ItemFlags.Advancement):
                    return true;
                case {|#0:ItemFlags.Advancement|}:
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
    

        [TestMethod]
        public async Task VerifyItemFlagsInSwitchExpressionYieldsNoDiagnostic()
        {
            string test = @"
using System;
using Archipelago.MultiClient.Net.Enums;

namespace MyClient
{
    class MyClass
    {
        public bool Test(ItemFlags flags)
        {
            return flags switch
            {
                _ when flags.HasFlag(ItemFlags.Advancement) => true,
                ItemFlags.None => false,
                _ => false
            };
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VerifyItemFlagsInSwitchStatementWithPatternMatchingYieldsNoDiagnostic()
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
            switch (i)
            {
                case var f when i.HasFlag(ItemFlags.Advancement):
                    return true;
                default:
                    return false;
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
