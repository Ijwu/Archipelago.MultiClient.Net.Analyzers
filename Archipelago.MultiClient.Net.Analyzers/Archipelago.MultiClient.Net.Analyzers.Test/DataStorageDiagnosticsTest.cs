﻿using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Archipelago.MultiClient.Net.Analyzers.Test.CSharpAnalyzerVerifier<Archipelago.MultiClient.Net.Analyzers.DataStorageDiagnostics>;

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
            DataStorageElement myElement = session.DataStorage[Scope.Slot, ""MyData""];
        }
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT001").WithSpan(16, 13, 16, 86);
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
            myElement += Operation.Update(new Dictionary<string, bool>()
            {
                [""key1""] = true
            });
        }
    }
}";

            DiagnosticResult expected = VerifyCS.Diagnostic("MULTICLIENT001").WithSpan(17, 13, 20, 15);
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
    }
}