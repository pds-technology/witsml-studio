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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Caliburn.Micro;
using Newtonsoft.Json;
using PDS.Witsml.Studio.Core.Properties;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Core.SaveWindowState;

namespace PDS.Witsml.Studio.Core.ViewModels
{
    /// <summary>
    /// Manages the main application user interface
    /// </summary>
    public sealed class ShellViewModel : Conductor<IScreen>.Collection.OneActive, IShellViewModel
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ShellViewModel));
        private static readonly string _applicationTitle = Settings.Default.ApplicationTitle;
        private static readonly string _onlineHelpUrl = Settings.Default.OnlineHelpUrl;
        private static readonly string _windowSettingsFileName = Settings.Default.WindowSettingsFileName;
        private WindowSettings _windowSettings;

        /// <summary>
        /// Initializes an instance of the ShellViewModel
        /// </summary>
        [ImportingConstructor]
        public ShellViewModel(IRuntimeService runtime)
        {
            _log.Debug("Loading Shell");

            Runtime = runtime;
            DisplayName = _applicationTitle;
            StatusBarText = "Ready.";
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service instance.</value>
        public IRuntimeService Runtime { get; private set; }

        private string _breadcrumbText;

        /// <summary>
        /// Gets or sets the breadcrumb path for the application shell
        /// </summary>
        public string BreadcrumbText
        {
            get { return _breadcrumbText; }
            set
            {
                if (!string.Equals(_breadcrumbText, value))
                {
                    _breadcrumbText = value;
                    NotifyOfPropertyChange(() => BreadcrumbText);
                }
            }
        }

        private string _statusBarText;

        /// <summary>
        /// Gets or sets the status bar text for the application shell
        /// </summary>
        public string StatusBarText
        {
            get { return _statusBarText; }
            set
            {
                if (!string.Equals(_statusBarText, value))
                {
                    _statusBarText = value;
                    NotifyOfPropertyChange(() => StatusBarText);
                }
            }
        }

        /// <summary>
        /// Opens the current working directory.
        /// </summary>
        public void OpenWorkingDirectory()
        {
            Process.Start("explorer.exe", Environment.CurrentDirectory);
        }

        /// <summary>
        /// Opens the current working directory.
        /// </summary>
        public void OpenDataDirectory()
        {
            Process.Start("explorer.exe", Runtime.DataFolderPath);
        }

        /// <summary>
        /// Opens the current output directory.
        /// </summary>
        public void OpenOutputDirectory()
        {
            Process.Start("explorer.exe", Runtime.OutputFolderPath);
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        public void Exit()
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Shows the About dialog for the application.
        /// </summary>
        public void About()
        {
            Runtime.ShowDialog(new AboutViewModel(Runtime));
        }

        /// <summary>
        /// Opens the online help content.
        /// </summary>
        public void OpenHelp()
        {
            Process.Start(_onlineHelpUrl);
        }

        /// <summary>
        /// Restores the main application window placement.
        /// </summary>
        /// <param name="window">The main window.</param>
        public void RestoreWindowPlacement(Window window)
        {
            LoadWindowSettings();
            if (_windowSettings == null) return;
            window.SetPlacement(_windowSettings.WindowPlacement);
        }

        /// <summary>
        /// Saves the main application window placement.
        /// </summary>
        /// <param name="window">The main window.</param>
        public void SaveWindowPlacement(Window window)
        {
            var settingsPath = $"{Runtime.DataFolderPath}\\{_windowSettingsFileName}";

            var settings = new WindowSettings
            {
                WindowPlacement = window.GetPlacement(),
                CurrentPluginIndex = Items.IndexOf(ActiveItem)
            };

            File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings));
        }

        /// <summary>
        /// Sets the application title.
        /// </summary>
        /// <param name="screen">The screen.</param>
        public void SetApplicationTitle(IScreen screen)
        {
            var plugin = screen as IPluginViewModel;

            if (string.IsNullOrWhiteSpace(plugin?.SubTitle))
            {
                SetApplicationTitle(screen.DisplayName);
                SetBreadcrumb(screen.DisplayName);
            }
            else
            {
                SetApplicationTitle(screen.DisplayName);
                //SetApplicationTitle($"{plugin.DisplayName} : {plugin.SubTitle}");
                SetBreadcrumb(plugin.DisplayName, plugin.SubTitle);
            }
        }

        /// <summary>
        /// Sets the application title.
        /// </summary>
        /// <param name="title">The title.</param>
        public void SetApplicationTitle(string title)
        {
            DisplayName = string.IsNullOrWhiteSpace(title)
                ? _applicationTitle
                : $"{_applicationTitle} : {title}";
        }

        /// <summary>
        /// Sets the breadcrumb text.
        /// </summary>
        /// <param name="values">The values.</param>
        public void SetBreadcrumb(params object[] values)
        {
            BreadcrumbText = string.Join(" / ", values);
        }

        /// <summary>
        /// Loads the plug-ins.
        /// </summary>
        internal void LoadPlugins()
        {
            LoadWindowSettings();

            Items.AddRange(Runtime.Container
                .ResolveAll<IPluginViewModel>()
                .OrderBy(x => x.DisplayOrder));

            if (_log.IsDebugEnabled)
            {
                _log.DebugFormat("{0}{1}{2}", "Plugins Loaded:", Environment.NewLine, string.Join(Environment.NewLine, Items.Select(x => x.DisplayName)));
            }

            var index = Math.Min(_windowSettings?.CurrentPluginIndex ?? 0, Math.Max(Items.Count - 1, 0));
            ActivateItem(Items.Skip(index).FirstOrDefault());
        }

        /// <summary>
        /// Called the first time the page's LayoutUpdated event fires after it is navigated to.
        /// </summary>
        /// <param name="view">The view attached to the current view model.</param>
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            LoadPlugins();
        }

        /// <summary>
        /// Called by a subclass when an activation needs processing.
        /// </summary>
        /// <param name="item">The item on which activation was attempted.</param>
        /// <param name="success">if set to <c>true</c> activation was successful.</param>
        protected override void OnActivationProcessed(IScreen item, bool success)
        {
            base.OnActivationProcessed(item, success);

            if (item != null && success)
            {
                SetApplicationTitle(item);
            }
        }

        private void LoadWindowSettings()
        {
            if (_windowSettings != null) return;

            if (!Directory.Exists(Runtime.DataFolderPath))
                Directory.CreateDirectory(Runtime.DataFolderPath);

            var settingsPath = $"{Runtime.DataFolderPath}\\{_windowSettingsFileName}";
            if (!File.Exists(settingsPath)) return;

            var json = File.ReadAllText(settingsPath);
            _windowSettings = JsonConvert.DeserializeObject<WindowSettings>(json);
        }
    }
}
