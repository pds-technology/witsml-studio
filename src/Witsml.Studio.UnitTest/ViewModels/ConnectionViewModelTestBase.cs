//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
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
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio.Core.Connections;
using PDS.Witsml.Studio.Core.Properties;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Core.ViewModels;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// Base class for testing the ConnectionViewModel
    /// </summary>
    [TestClass]
    public class ConnectionViewModelTestBase
    {
        private static string _validWitsmlUri = "http://localhost/Witsml.Web/WitsmlStore.svc";
        private static string _validEtpUri = "ws://localhost/witsml.web/api/etp";

        protected static readonly string PersistedDataFolderName = Settings.Default.PersistedDataFolderName;
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

        protected static void DeletePersistenceFolder()
        {
            var path = string.Format("{0}/{1}", Environment.CurrentDirectory, PersistedDataFolderName);

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
