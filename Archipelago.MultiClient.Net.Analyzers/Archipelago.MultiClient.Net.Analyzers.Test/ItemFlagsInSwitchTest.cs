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

        [TestMethod]
        public async Task VerifyFixConvertItemFlagsSwitch()
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
            if (i.HasFlag(ItemFlags.Advancement))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}";
            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT003").WithLocation(0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }

        [TestMethod]
        public async Task VerifyFixConvertItemFlagsSwitchWithFallthroughCase()
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
                case ItemFlags.Trap:
                case ItemFlags.Advancement:
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
            if (i.HasFlag(ItemFlags.Trap) || i.HasFlag(ItemFlags.Advancement))
            {
                return true;
            }
            else
            {
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
