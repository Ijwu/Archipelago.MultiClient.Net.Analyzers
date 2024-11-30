using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        public async Task VerifyItemFlagsInSwitchStatementWithPatternMatchingYieldsDiagnostic()
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
                case var f when i.HasFlag(ItemFlags.Advancement):
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
        public async Task VerifyItemFlagsInSwitchStatementWithPatternMatchingFix()
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
                case var f when i.HasFlag(ItemFlags.Advancement):
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

        [TestMethod]
        public async Task VerifyFixConvertItemFlagsSwitchWithMultipleCases()
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
                    return true;
                case ItemFlags.Advancement:
                    return true;
                case ItemFlags.None:
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
            if (i.HasFlag(ItemFlags.Trap))
            {
                return true;
            }
            else if (i.HasFlag(ItemFlags.Advancement))
            {
                return true;
            }
            else if (i.HasFlag(ItemFlags.None))
            {
                return false;
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
