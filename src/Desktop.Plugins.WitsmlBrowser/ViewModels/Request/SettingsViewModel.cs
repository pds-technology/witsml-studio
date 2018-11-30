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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Energistics.DataAccess;
using PDS.WITSMLstudio.Connections;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.ViewModels.Request
{
    /// <summary>
    /// Manages the behavior for the Settings view UI elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class SettingsViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SettingsViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public SettingsViewModel(IRuntimeService runtime)
        {
            _log.Debug("Creating view model instance");
            Runtime = runtime;            
            DisplayName = "Settings";
            WitsmlVersions = new BindableCollection<string>();
            ConnectionPicker = new ConnectionPickerViewModel(runtime, ConnectionTypes.Witsml)
            {
                AutoConnectEnabled = true,
                OnConnectionChanged = OnConnectionChanged
            };
        }

        /// <summary>
        /// Gets the Parent <see cref="T:Caliburn.Micro.IConductor" /> for this view model
        /// </summary>
        public new RequestViewModel Parent
        {
            get { return (RequestViewModel)base.Parent; }
        }

        /// <summary>
        /// Gets the proxy for the WITSML web service.
        /// </summary>
        /// <value>
        /// The WITSML seb service proxy.
        /// </value>
        public WITSMLWebServiceConnection Proxy
        {
            get { return Parent.Proxy; }
        }

        /// <summary>
        /// Gets or sets the data model.
        /// </summary>
        /// <value>
        /// The WitsmlSettings data model.
        /// </value>
        public Models.WitsmlSettings Model
        {
            get { return Parent.Model; }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the connection picker view model.
        /// </summary>
        /// <value>The connection picker view model.</value>
        public ConnectionPickerViewModel ConnectionPicker { get; }

        /// <summary>
        /// Gets the witsml versions retrieved from the server.
        /// </summary>
        /// <value>
        /// The server's supported witsml versions.
        /// </value>
        public BindableCollection<string> WitsmlVersions { get; }

        /// <summary>
        /// Gets the supported versions from the server.
        /// </summary>
        public void GetVersions()
        {
            GetVersions(Proxy, Model.Connection, false);
        }

        /// <summary>
        /// Gets the capabilities from the server.
        /// </summary>
        public void GetCapabilities()
        {
            Parent.Parent.GetCapabilities();
        }

        /// <summary>
        /// Gets the base message from the server.
        /// </summary>
        public void GetBaseMessage()
        {
            Parent.Parent.GetBaseMessage();
        }

        /// <summary>
        /// Selects the output path.
        /// </summary>
        public void SelectOutputPath()
        {
            var info = new DirectoryInfo(Model.OutputPath);
            var owner = new Win32WindowHandle(Application.Current.MainWindow);
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Output Path",
                SelectedPath = info.FullName,
                ShowNewFolderButton = true,
            };

            if (dialog.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK)
            {
                Model.OutputPath = dialog.SelectedPath;
                Runtime.OutputFolderPath = Model.OutputPath;
            }
        }

        /// <summary>
        /// Gets the supported versions from the server.
        /// </summary>
        /// <param name="proxy">The proxy.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="throwOnError">if set to <c>true</c> throw on error.</param>
        /// <returns>The supported versions.</returns>
        internal string GetVersions(WITSMLWebServiceConnection proxy, Connection connection, bool throwOnError = true)
        {
            // Update proxy connection settings
            connection.UpdateProxy(proxy);

            var parent = Parent?.Parent;
            var supportedVersions = string.Empty;

            // Output Request for GetVersion
            parent?.OutputRequestMessages(Functions.GetVersion, null, parent?.GetOptionsIn(Functions.GetVersion));

            try
            {
                supportedVersions = proxy.GetVersion();
                _log.DebugFormat("Supported versions '{0}' found on WITSML server with uri '{1}'", supportedVersions, connection.Uri);
            }
            catch (Exception ex)
            {
                _log.WarnFormat("Exception getting versions on WITSML server with uri '{0}' : '{1}'", connection.Uri, ex.Message);

                if (throwOnError)
                    throw;

                parent?.OutputError("Error connecting to server.", ex);
                return supportedVersions;
            }

            if (parent == null) return supportedVersions;

            parent.ClearQueryResults();
            parent.OutputResults(null, supportedVersions, 0);
            parent.OutputMessages(null, supportedVersions, 0);

            return supportedVersions;
        }

        /// <summary>
        /// Called when initializing the SettingsViewModel.
        /// </summary>
        protected override void OnInitialize()
        {
            _log.Debug("Initializing screen");
            base.OnInitialize();
            Model.ReturnElementType = OptionsIn.ReturnElements.All;
            Runtime.OutputFolderPath = $"{Environment.CurrentDirectory}\\Data\\Results";            
        }

        /// <summary>
        /// Called when the selected connection has changed.
        /// </summary>
        /// <param name="connection">The connection.</param>
        private Task OnConnectionChanged(Connection connection)
        {
            Model.Connection = connection;

            _log.DebugFormat("Selected connection changed: Name: {0}; Uri: {1}; Username: {2}",
                Model.Connection.Name, Model.Connection.Uri, Model.Connection.Username);

            // Make connection and get version
            Runtime.ShowBusy();
            Task.Run(() =>
            {
                GetWitsmlVersions();
                Runtime.ShowBusy(false);
            });

            return Task.FromResult(true);
        }

        /// <summary>
        /// Gets the supported versions from the server and initializes the UI element for version selection.
        /// </summary>
        private void GetWitsmlVersions()
        {
            _log.Debug("Selecting supported versions from WITSML server.");

            var parent = Parent?.Parent;

            try
            {
                WitsmlVersions.Clear();

                var versions = GetVersions(Proxy, Model.Connection);

                if (!string.IsNullOrEmpty(versions))
                {
                    WitsmlVersions.AddRange(versions.Split(','));
                    Model.WitsmlVersion = WitsmlVersions.Last();
                }
                else
                {
                    var message = "The Witsml server does not support any versions.";

                    // Log the warning.
                    _log.Warn(message);
                    
                    // Show the user the warning
                    parent?.OutputError(message);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "Error connecting to server.";
                
                // Log the error
                _log.Error(errorMessage, ex);

                // Show the user the error.
                parent?.OutputError(errorMessage, ex);
            }
        }
    }
}
