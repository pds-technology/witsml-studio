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

using Energistics.DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.ViewModels;
using PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.ViewModels.Request;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser
{
    [TestClass]
    public class WitsmlBrowserPluginTests
    {
        private BootstrapperHarness _bootstrapper;
        private TestRuntimeService _runtime;
        private MainViewModel _mainViewModel;

        [TestInitialize]
        public void TestSetup()
        {
            _bootstrapper = new BootstrapperHarness();
            _runtime = new TestRuntimeService(_bootstrapper.Container);
            _mainViewModel = new MainViewModel(_runtime);
        }

        [TestMethod]
        public void MainViewModel_LoadScreens_Can_Load_MainViewModel_Screens()
        {
            _mainViewModel.LoadScreens();
            
            Assert.AreEqual(2, _mainViewModel.Items.Count);
        }

        [TestMethod]
        public void MainViewModel_GetWitsmlVersionEnum_Can_Get_Witsml_Version_Enum()
        {
            // Test version 131
            Assert.AreEqual(WMLSVersion.WITSML131, _mainViewModel.GetWitsmlVersionEnum(OptionsIn.DataVersion.Version131.Value));

            // Test version 141
            Assert.AreEqual(WMLSVersion.WITSML141, _mainViewModel.GetWitsmlVersionEnum(OptionsIn.DataVersion.Version141.Value));

            // Test null version
            Assert.AreEqual(WMLSVersion.WITSML141, _mainViewModel.GetWitsmlVersionEnum(null));
        }

        [TestMethod]
        public void MainViewModel_CreateProxy_Can_Create_Proxy()
        {
            Assert.IsNotNull(_mainViewModel.CreateProxy());
        }

        [TestMethod]
        public void RequestViewModel_LoadScreens_Can_Load_RequestViewModel_Screens()
        {
            var mainViewModel = new MainViewModel(_runtime);
            var xmlQuery = new TextEditorViewModel(_runtime, "XML")
            {
                IsPrettyPrintAllowed = true
            };
            var requestViewModel = new RequestViewModel(_runtime, xmlQuery);

            mainViewModel.Items.Add(requestViewModel);
            requestViewModel.LoadScreens();

            Assert.AreEqual(3, requestViewModel.Items.Count);
        }
    }
}
