//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2018.1
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Connections;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Connections
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
