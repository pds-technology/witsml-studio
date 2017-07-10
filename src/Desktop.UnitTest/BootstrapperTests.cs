//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2017.2
//
// Copyright 2017 PDS Americas LLC
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

using System.Linq;
using Caliburn.Micro;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop
{
    /// <summary>
    /// Unit tests for Witsml Studio Bootstrapper
    /// </summary>
    [TestClass]
    public class BootstrapperTests
    {
        private BootstrapperHarness _bootstrapper;

        /// <summary>
        /// Initialization before each test
        /// </summary>
        [TestInitialize]
        public void TestSetUp()
        {
            _bootstrapper = new BootstrapperHarness();
        }

        /// <summary>
        /// Test that all Bootstrapper assemblieses can be loaded.
        /// </summary>
        [TestMethod]
        public void Bootstrapper_CallSelectAssemblies_Can_Load_Assemblies()
        {
            var thisAssembly = _bootstrapper.CallSelectAssemblies()
                .FirstOrDefault(a => a == GetType().Assembly);
            
            Assert.IsNotNull(thisAssembly);
        }

        /// <summary>
        /// Test that an IWindowManager instance was registered.
        /// </summary>
        [TestMethod]
        public void Bootstrapper_CallGetInstance_Registered_Window_Manager()
        {
            // Get instance of IWindowManager from bootstrapper's GetInstance
            var windownManager = _bootstrapper.CallGetInstance(typeof(IWindowManager));

            Assert.IsNotNull(windownManager);
        }

        /// <summary>
        /// Test that an IEventAggregator instance was registered.
        /// </summary>
        [TestMethod]
        public void Bootstrapper_CallGetInstance_Registered_Event_Aggregator()
        {
            // Get instance of IEventAggregator from bootstrapper's GetInstance
            var eventAggregator = _bootstrapper.CallGetInstance(typeof(IEventAggregator));

            Assert.IsNotNull(eventAggregator);
        }

        /// <summary>
        /// Test that an IShellViewModel instance was registered.
        /// </summary>
        [TestMethod]
        public void Bootstrapper_CallGetInstance_Can_Resolve_Shell_View_Model()
        {
            // Get instance of IShellViewModel from bootstrapper's GetInstance
            var eventAggregator = _bootstrapper.CallGetInstance(typeof(IShellViewModel));

            Assert.IsNotNull(_bootstrapper);
        }
    }
}
