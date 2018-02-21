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

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Properties;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.ViewModels
{
    /// <summary>
    /// Base class for testing the ConnectionViewModel
    /// </summary>
    [TestClass]
    public class ConnectionViewModelTestBase
    {
        private static string _validWitsmlUri = "http://localhost/Witsml.Web/WitsmlStore.svc";
        private static string _validEtpUri = "ws://localhost/witsml.web/api/etp";

        protected static readonly string ConnectionBaseFileName = Settings.Default.ConnectionBaseFileName;

        protected BootstrapperHarness _bootstrapper;
        protected TestRuntimeService _runtime;

        protected ConnectionViewModel _witsmlConnectionVm;
        protected ConnectionViewModel _etpConnectionVm;
        protected Connection _witsmlConnection;
        protected Connection _etpConnection;

        public TestContext TestContext { get; set; }

        /// <summary>
        /// Sets up the environment for each test.  
        /// ConnectionViewModels and Connections are created 
        /// for ConnectionTypes Witsml and Etp.
        /// In addition the persisence folder is cleard and deleted.
        /// </summary>
        [TestInitialize]
        public void TestSetUp()
        {
            if (TestContext.Properties.Contains("WitsmlStoreUrl"))
                _validWitsmlUri = TestContext.Properties["WitsmlStoreUrl"].ToString();

            if (TestContext.Properties.Contains("EtpServerUrl"))
                _validEtpUri = TestContext.Properties["EtpServerUrl"].ToString();

            _bootstrapper = new BootstrapperHarness();
            _runtime = new TestRuntimeService(_bootstrapper.Container);

            _witsmlConnection = new Connection()
            {
                Name = "Witsml",
                Uri = _validWitsmlUri,
                Username = "WitsmlUser"
            };

            _etpConnection = new Connection()
            {
                Name = "Etp",
                Uri = _validEtpUri,
                Username = "EtpUser"
            };

            _witsmlConnectionVm = new ConnectionViewModel(_runtime, ConnectionTypes.Witsml);
            _etpConnectionVm = new ConnectionViewModel(_runtime, ConnectionTypes.Etp);

            DeletePersistenceFolder();
        }

        protected void DeletePersistenceFolder()
        {
            var path = _runtime.DataFolderPath;

            // Delete the Persistence Folder
            if (Directory.Exists(path))
            {
                // Delete all files in the Persistence Folder
                DirectoryInfo di = new DirectoryInfo(path);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }

                Directory.Delete(path);
            }
        }
    }
}
