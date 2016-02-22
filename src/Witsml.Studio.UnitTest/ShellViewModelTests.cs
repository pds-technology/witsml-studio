﻿using System.Linq;
using System.Windows;
using Caliburn.Micro;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio
{
    [TestClass]
    public class ShellViewModelTests
    {
        private static Application _application;

        [AssemblyInitialize]
        public static void AssemblySetUp(TestContext context)
        {
            _application = new App();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanUp()
        {
            _application.Shutdown();
        }

        [TestInitialize]
        public void TestSetUp()
        {
            AssemblySource.Instance.Clear();
            _application.Resources["bootstrapper"] = new BootstrapperHarness();
        }

        [TestMethod]
        public void TestShellViewModelLoadsAllPlugins()
        {
            var viewModel = new ShellViewModel();
            viewModel.LoadPlugins();

            Assert.AreEqual(3, viewModel.Items.Count);
        }

        [TestMethod]
        public void TestLoadedPluginsDisplayInAscendingOrder()
        {
            var viewModel = new ShellViewModel();
            viewModel.LoadPlugins();

            var actual = viewModel.Items.ToArray();

            var expected = viewModel.Items.Cast<IPluginViewModel>()
                .OrderBy(x => x.DisplayOrder)
                .ToArray();

            for (int i=0; i<actual.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
    }
}
