using Archipelago.MultiClient.Net.Analyzers.Generators;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Archipelago.MultiClient.Net.Analyzers.Test.CSharpCodeFixVerifier<
    Archipelago.MultiClient.Net.Analyzers.Analyzers.DataStorageDiagnostics,
    Archipelago.MultiClient.Net.Analyzers.Fixes.DataStorageFixes>;

namespace Archipelago.MultiClient.Net.Analyzers.Test
{
    [TestClass]
    public class ArchipelagoMultiClientNetAnalyzersUnitTest
    {
        [TestMethod]
        public async Task VerifyEmptySourceYieldsNoDiagnostic()
        {
            string test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VerifyInitializationYieldsDiagnostic()
        {
            string test = @"
using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace MyClient
{
    class MyClass
    {   
        private ArchipelagoSession session;

        public void Initialize()
        {
            DataStorageElement {|#0:myElement = session.DataStorage[Scope.Slot, ""MyData""]|};
        }
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT001").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task VerifyInitializationOfOtherVariableYieldsNoDiagnostic()
        {
            string test = @"
using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace MyClient
{
    class MyClass
    {   
        private ArchipelagoSession session;

        public void Initialize()
        {
            int i = 0;
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VerifyReassignmentYieldsDiagnostic()
        {
            string test = @"
using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace MyClient
{
    class MyClass
    {   
        private ArchipelagoSession session;
        private DataStorageElement myElement;

        public void Initialize()
        {
            {|#0:myElement += Operation.Update(new Dictionary<string, bool>()
            {
                [""key1""] = true
            })|};
        }
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT001").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task VerifyReassignmentOfOtherVariableYieldsDiagnostic()
        {
            string test = @"
using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace MyClient
{
    class MyClass
    {   
        private ArchipelagoSession session;
        private int i;

        public void Initialize()
        {
            i += 2;
        }
    }
}";
            
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VerifyInlineUsageYieldsNoDiagnostic()
        {
            string test = @"
using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace MyClient
{
    class MyClass
    {   
        private ArchipelagoSession session;

        public void Initialize()
        {
            session.DataStorage[Scope.Slot, ""MyData""] += Operation.Update(new Dictionary<string, bool>()
            {
                [""key1""] = true
            });
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VerifyInlineUsageOfAnnotatedPropertyYieldsNoDiagnostic()
        {
            string test = @"
using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace MyClient
{
    partial class MyClass
    {
        private ArchipelagoSession session;
        
        [DataStorageProperty(nameof(session), Scope.Slot, ""MyData"")]
        private DataStorageElement _myData;

        [DataStorageProperty(nameof(session), ""MyGlobalData"")]
        private DataStorageElement _myGlobalData;

        public void Initialize()
        {
            MyData += Operation.Update(new Dictionary<string, bool>()
            {
                [""key1""] = true
            });
        }
    }
}";

            string generatedSource = @"#nullable enable annotations

using Archipelago.MultiClient.Net.Models;

namespace MyClient
{
    internal partial class MyClass
    {
        [System.CodeDom.Compiler.GeneratedCode(tool: ""DataStoragePropertyGenerator"", version: null)]
        private DataStorageElement MyData
        {
            get => session.DataStorage[Archipelago.MultiClient.Net.Enums.Scope.Slot, ""MyData""];
            set => session.DataStorage[Archipelago.MultiClient.Net.Enums.Scope.Slot, ""MyData""] = value;
        }

        [System.CodeDom.Compiler.GeneratedCode(tool: ""DataStoragePropertyGenerator"", version: null)]
        private DataStorageElement MyGlobalData
        {
            get => session.DataStorage[""MyGlobalData""];
            set => session.DataStorage[""MyGlobalData""] = value;
        }
    }
}
";

            await new VerifyCS.Test
            {
                TestBehaviors = TestBehaviors.None,
                TestCode = test,
                TestState =
                {
                    GeneratedSources =
                    {
                        (typeof(DataStorageAttributeGenerator), "DataStoragePropertyAttribute.g.cs", DataStorageAttributeGenerator.AttributeSource),
                        (typeof(DataStoragePropertyGenerator), "MyClass.g.cs", generatedSource)
                    }
                }
            }.RunAsync();
        }

        [TestMethod]
        public async Task VerifyHarmlessDeclarationYieldsNoDiagnostic()
        {
            string test = @"
using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace MyClient
{
    class MyClass
    {   
        private ArchipelagoSession session;

        public void Initialize()
        {
            DataStorageElement myElement;
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VerifyFixWithSingleDeclaration()
        {
            string test = @"
using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace MyClient
{
    class MyClass
    {   
        private ArchipelagoSession session;

        public void Initialize()
        {
            DataStorageElement {|#0:myElement = session.DataStorage[Scope.Slot, ""MyData""]|};
            myElement.Initialize(0);
            {|#1:myElement += Operation.Update(new Dictionary<string, bool>()
            {
                [""key1""] = true
            })|};
        }
    }
}";
            string fixTest = @"
using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace MyClient
{
    class MyClass
    {   
        private ArchipelagoSession session;

        public void Initialize()
        {
            session.DataStorage[Scope.Slot, ""MyData""].Initialize(0);
            session.DataStorage[Scope.Slot, ""MyData""] += Operation.Update(new Dictionary<string, bool>()
            {
                [""key1""] = true
            });
        }
    }
}";
            DiagnosticResult expected1 = VerifyCS.Diagnostic("MULTICLIENT001").WithLocation(0);
            DiagnosticResult expected2 = VerifyCS.Diagnostic("MULTICLIENT001").WithLocation(1);
            await VerifyCS.VerifyCodeFixAsync(test, [expected1, expected2], fixTest);
        }

        [TestMethod]
        public async Task VerifyFixWithMultiDeclaration()
        {
            string test = @"
using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace MyClient
{
    class MyClass
    {   
        private ArchipelagoSession session;

        public void Initialize()
        {
            DataStorageElement {|#0:myElement = session.DataStorage[Scope.Slot, ""MyData""]|}, myOtherElement;
            myElement.Initialize(0);
            {|#1:myElement += Operation.Update(new Dictionary<string, bool>()
            {
                [""key1""] = true
            })|};
        }
    }
}";
            string fixTest = @"
using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace MyClient
{
    class MyClass
    {   
        private ArchipelagoSession session;

        public void Initialize()
        {
            DataStorageElement myOtherElement;
            session.DataStorage[Scope.Slot, ""MyData""].Initialize(0);
            session.DataStorage[Scope.Slot, ""MyData""] += Operation.Update(new Dictionary<string, bool>()
            {
                [""key1""] = true
            });
        }
    }
}";
            DiagnosticResult expected1 = VerifyCS.Diagnostic("MULTICLIENT001").WithLocation(0);
            DiagnosticResult expected2 = VerifyCS.Diagnostic("MULTICLIENT001").WithLocation(1);
            await VerifyCS.VerifyCodeFixAsync(test, [expected1, expected2], fixTest);
        }
    }
}
