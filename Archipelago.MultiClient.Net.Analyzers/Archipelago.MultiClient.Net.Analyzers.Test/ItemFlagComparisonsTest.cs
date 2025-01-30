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
                {|#0:case ItemFlags.Advancement:|}
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
                {|#0:case ItemFlags.Advancement:|}
                    return true;
                {|#1:case ItemFlags.None:|}
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
        public async Task VerifyFallthroughItemFlagsInSwitchStatementYieldsDiagnostics()
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
                {|#0:case ItemFlags.Advancement:|}
                {|#1:case ItemFlags.None:|}
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
                case var f when f.HasFlag(ItemFlags.Advancement):
                    return true;
                {|#0:case ItemFlags.None:|}
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
                case var f when f.HasFlag(ItemFlags.Advancement):
                    return true;
                default:
                    return false;
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VerifyFixSingleItemFlagsInSwitchStatement()
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
                {|#0:case ItemFlags.Advancement:|}
                    return true;
                default:
                    return false;
            }
        }
    }
}";
            string fixTest = @"
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
                case var f when f.HasFlag(ItemFlags.Advancement):
                    return true;
                default:
                    return false;
            }
        }
    }
}";
            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }

        [TestMethod]
        public async Task VerifyFixMultipleItemFlagsInSwitchStatement()
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
                {|#0:case ItemFlags.Advancement:|}
                    return true;
                {|#1:case ItemFlags.None:|}
                    return false;
                default:
                    return false;
            }
        }
    }
}";
            string fixTest = @"
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
                case var f when f.HasFlag(ItemFlags.Advancement):
                    return true;
                case var f when f.HasFlag(ItemFlags.None):
                    return false;
                default:
                    return false;
            }
        }
    }
}";
            DiagnosticResult expected1 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
            DiagnosticResult expected2 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(1);
            await VerifyCS.VerifyCodeFixAsync(test, [expected1, expected2], fixTest);
        }

        [TestMethod]
        public async Task VerifyFixFallthroughItemFlagsInSwitchStatement()
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
                {|#0:case ItemFlags.Advancement:|}
                {|#1:case ItemFlags.None:|}
                    return true;
                default:
                    return false;
            }
        }
    }
}";
            string fixTest = @"
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
                case var f when f.HasFlag(ItemFlags.Advancement):
                case var f1 when f1.HasFlag(ItemFlags.None):
                    return true;
                default:
                    return false;
            }
        }
    }
}";
            DiagnosticResult expected1 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
            DiagnosticResult expected2 = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(1);
            await VerifyCS.VerifyCodeFixAsync(test, [expected1, expected2], fixTest);
        }

        [TestMethod]
        public async Task VerifyFixMixedUseItemFlagsInSwitchStatement()
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
                case var f when f.HasFlag(ItemFlags.Advancement):
                    return true;
                {|#0:case ItemFlags.None:|}
                    return true;
                default:
                    return false;
            }
        }
    }
}";
            string fixTest = @"
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
                case var f when f.HasFlag(ItemFlags.Advancement):
                    return true;
                case var f when f.HasFlag(ItemFlags.None):
                    return true;
                default:
                    return false;
            }
        }
    }
}";
            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }
    }
}
