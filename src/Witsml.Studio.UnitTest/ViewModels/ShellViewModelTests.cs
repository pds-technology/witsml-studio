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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Core.ViewModels;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// Unit tests for the Witsml Studio application shell.
    /// </summary>
    [TestClass]
    public class ShellViewModelTests
    {
        private BootstrapperHarness _bootstrapper;
        private TestRuntimeService _runtime;
        private ShellViewModel _viewModel;

        [TestInitialize]
        public void TestSetUp()
        {
            _bootstrapper = new BootstrapperHarness();
            _runtime = new TestRuntimeService(_bootstrapper.Container);
            _viewModel = new ShellViewModel(_runtime);
        }

        /// <summary>
        /// Tests that all of the expected IPluginViewModels were loaded.
        /// </summary>
        [TestMethod]
        public void ShellViewModel_LoadPlugins_Can_Load_ShellViewModel_Plugins()
        {
            _viewModel.LoadPlugins();

            Assert.AreEqual(4, _viewModel.Items.Count);
        }

        /// <summary>
        /// Tests that all of the IPluginViewModels were loaded in the correct display order.
        /// </summary>
        [TestMethod]
        public void ShellViewModel_LoadPlugins_Plugins_Are_Displayed_In_Ascending_Order()
        {
            _viewModel.LoadPlugins();

            var actual = _viewModel.Items.ToArray();

            var expected = _viewModel.Items.Cast<IPluginViewModel>()
                .OrderBy(x => x.DisplayOrder)
                .ToArray();

            for (int i=0; i<actual.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void ShellViewModel_LoadPlugins_Status_Is_Set()
        {
            _viewModel.LoadPlugins();

            Assert.AreEqual("Ready", _viewModel.StatusBarText);
        }

        [TestMethod]
        public void ShellViewModel_LoadPlugins_Breadcrumb_Is_First_Plugin_DisplayName()
        {
            _viewModel.LoadPlugins();
           
            // Test that the Shell breadcrumb is the same as the first plugin
            Assert.AreEqual(_viewModel.Items[0].DisplayName, _viewModel.BreadcrumbText);
        }
    }
}
