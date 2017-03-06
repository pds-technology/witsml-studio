//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using PDS.Witsml.Studio.Core.Connections;
using PDS.Witsml.Studio.Core.Runtime;

namespace PDS.Witsml.Studio.Connections
{
    [TestClass]
    public class ConnectionTestTests
    {
        private static string _validWitsmlUri = "http://localhost/Witsml.Web/WitsmlStore.svc";
        private static string _validEtpUri = "ws://localhost/witsml.web/api/etp";
        private IRuntimeService _runtime;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetup()
        {
            _runtime = new TestRuntimeService(ContainerFactory.Create());
        }

        [TestMethod]
        public async Task WitsmlConnectionTest_CanConnect_Valid_Endpoint()
        {
            if (TestContext.Properties.Contains("WitsmlStoreUrl"))
                _validWitsmlUri = TestContext.Properties["WitsmlStoreUrl"].ToString();

            if (TestContext.Properties.Contains("EtpServerUrl"))
                _validEtpUri = TestContext.Properties["EtpServerUrl"].ToString();

            var witsmlConnectionTest = new WitsmlConnectionTest();
            var result = await witsmlConnectionTest.CanConnect(new Connection() { Uri = _validWitsmlUri });

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task WitsmlConnectionTest_CanConnect_Invalid_Endpoint()
        {
            var witsmlConnectionTest = new WitsmlConnectionTest();
            var result = await witsmlConnectionTest.CanConnect(new Connection() { Uri = _validWitsmlUri + "x" });

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task EtpConnectionTest_CanConnect_Valid_Endpoint()
        {
            var etpConnectionTest = new EtpConnectionTest(_runtime);
            var result = await etpConnectionTest.CanConnect(new Connection() { Uri = _validEtpUri });

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task EtpConnectionTest_CanConnect_Invalid_Endpoint()
        {
            var etpConnectionTest = new EtpConnectionTest(_runtime);
            var result = await etpConnectionTest.CanConnect(new Connection() { Uri = _validEtpUri + "x" });

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task EtpConnectionTest_CanConnect_Invalid_BadFormat()
        {
            var etpConnectionTest = new EtpConnectionTest(_runtime);
            var result = await etpConnectionTest.CanConnect(new Connection() { Uri = "xxxxxxxx" });

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task WitsmlConnectionTest_CanConnect_Invalid_BadFormat()
        {
            var witsmlConnectionTest = new WitsmlConnectionTest();
            var result = await witsmlConnectionTest.CanConnect(new Connection() { Uri = "xxxxxxxx" });

            Assert.IsFalse(result);
        }
    }
}
