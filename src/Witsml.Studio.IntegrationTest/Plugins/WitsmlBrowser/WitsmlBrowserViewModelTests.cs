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

using System;
using System.Threading.Tasks;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Core.ViewModels;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser
{
    [TestClass]
    public class WitsmlBrowserViewModelTests
    {
        private static string _validWitsmlUri = "http://localhost/Witsml.Web/WitsmlStore.svc";

        private static readonly string _addWellTemplate =
                "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" + Environment.NewLine +
                "<wells version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\" >" + Environment.NewLine +
                "<well uid=\"{0}\">" + Environment.NewLine +
                "<name>Unit Test Well {1}</name>" + Environment.NewLine +
                "<timeZone>-06:00</timeZone>" + Environment.NewLine +
                "</well>" + Environment.NewLine +
                "</wells>";

        private static readonly string _getWellTemplate =
            "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" + Environment.NewLine +
            "<wells version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\" >" + Environment.NewLine +
            "<well uid=\"{0}\" />" + Environment.NewLine +
            "</wells>";

        private BootstrapperHarness _bootstrapper;
        private TestRuntimeService _runtime;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _bootstrapper = new BootstrapperHarness();
            _runtime = new TestRuntimeService(_bootstrapper.Container);
            _runtime.Shell = new ShellViewModel(_runtime);

            if (TestContext.Properties.Contains("WitsmlStoreUrl"))
                _validWitsmlUri = TestContext.Properties["WitsmlStoreUrl"].ToString();
        }

        [TestMethod]
        public async Task MainViewModel_SubmitQuery_Can_AddToStore_For_Well()
        {
            // The expected result
            var expectedUid = Guid.NewGuid().ToString();

            // Create the view model and initialize data to add a well to the store
            var vm = new MainViewModel(_runtime);
            vm.Model.Connection = new Core.Connections.Connection() { Uri = _validWitsmlUri };
            vm.Proxy.Url = vm.Model.Connection.Uri;

            var xmlIn = string.Format(
                _addWellTemplate,
                expectedUid,
                DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            vm.Model.ReturnElementType = OptionsIn.ReturnElements.All;

            // Submit the query
            var result = await vm.SubmitQuery(Functions.AddToStore, xmlIn, vm.GetOptionsIn(Functions.AddToStore));
            var suppMsgOut = result.MessageOut;

            // The same uid should be returned as the results.
            Assert.AreEqual(expectedUid, suppMsgOut);
        }

        [TestMethod]
        public async Task MainViewModel_SubmitQuery_Can_GetFromStore_For_Well()
        {
            // The expected result
            var expectedUid = Guid.NewGuid().ToString();

            // Create the view model and initialize data to add a well to the store
            var vm = new MainViewModel(_runtime);
            vm.Model.Connection = new Core.Connections.Connection() { Uri = _validWitsmlUri };
            vm.Proxy.Url = vm.Model.Connection.Uri;
            vm.Model.ReturnElementType = OptionsIn.ReturnElements.All;

            // Add a well to the store
            var xmlIn = string.Format(
                _addWellTemplate,
                expectedUid,
                DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            await vm.SubmitQuery(Functions.AddToStore, xmlIn, vm.GetOptionsIn(Functions.AddToStore));

            // Retrieve the same well from the store
            xmlIn = string.Format(_getWellTemplate, expectedUid);

            var result = await vm.SubmitQuery(Functions.GetFromStore, xmlIn, vm.GetOptionsIn(Functions.GetFromStore));
            var xmlOut = result.XmlOut;

            // The same uid should be returned as the results.
            Assert.IsNotNull(xmlOut);

            var wellList = EnergisticsConverter.XmlToObject<WellList>(xmlOut);

            Assert.IsNotNull(wellList);
            Assert.AreEqual(1, wellList.Items.Count);

            var well = wellList.Items[0] as Well;

            Assert.IsNotNull(well);
            Assert.AreEqual(expectedUid, well.Uid);
        }

        [TestMethod]
        public async Task MainViewModel_SubmitQuery_Can_GetCap()
        {
            var vm = new MainViewModel(_runtime);
            vm.Model.Connection = new Core.Connections.Connection() { Uri = _validWitsmlUri };
            vm.Proxy.Url = vm.Model.Connection.Uri;
            vm.Model.WitsmlVersion = OptionsIn.DataVersion.Version141.Value;

            var result = await vm.SubmitQuery(Functions.GetCap, string.Empty, vm.GetOptionsIn(Functions.GetCap));

            // Test that the xmlOut is a Capserver List
            var capServerList = EnergisticsConverter.XmlToObject<CapServers>(result.XmlOut);
            Assert.IsNotNull(capServerList);

            // Is this the version we're expecting
            Assert.AreEqual(OptionsIn.DataVersion.Version141.Value, capServerList.CapServer.SchemaVersion);
        }
    }
}
