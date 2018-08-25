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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Desktop.Core;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the settings view.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class SettingsViewModel : Screen, ISessionAware
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SettingsViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel" /> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        public SettingsViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName =  "Core";

            ConnectionPicker = new ConnectionPickerViewModel(runtime, ConnectionTypes.Etp)
            {
                AutoConnectEnabled = true,
                OnConnectionChanged = OnConnectionChanged
            };

            EtpProtocols = new BindableCollection<EtpProtocolItem>();
        }

        /// <summary>
        /// Gets or Sets the Parent <see cref="T:Caliburn.Micro.IConductor" />
        /// </summary>
        public new MainViewModel Parent => (MainViewModel) base.Parent;

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        public Models.EtpSettings Model => Parent.Model;

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
        /// Gets the collection of all ETP protocols.
        /// </summary>
        /// <value>The collection of ETP protocols.</value>
        public BindableCollection<EtpProtocolItem> EtpProtocols { get; }

        private bool _canRequestSession;

        /// <summary>
        /// Gets or sets a value indicating whether the Request Session button is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if Request Session is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool CanRequestSession
        {
            get { return _canRequestSession; }
            set
            {
                if (_canRequestSession == value)
                    return;

                _canRequestSession = value;
                NotifyOfPropertyChange(() => CanRequestSession);
            }
        }

        private bool _canCloseSession;

        /// <summary>
        /// Gets or sets a value indicating whether the Close Session button is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if Close Session is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool CanCloseSession
        {
            get { return _canCloseSession; }
            set
            {
                if (_canCloseSession == value)
                    return;

                _canCloseSession = value;
                NotifyOfPropertyChange(() => CanCloseSession);
            }
        }

        private bool _canStartServer;

        /// <summary>
        /// Gets or sets a value indicating whether the Start Server button is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if Start Server is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool CanStartServer
        {
            get { return _canStartServer; }
            set
            {
                if (_canStartServer == value)
                    return;

                _canStartServer = value;
                NotifyOfPropertyChange(() => CanStartServer);
            }
        }

        private bool _canStopServer;

        /// <summary>
        /// Gets or sets a value indicating whether the Stop Server button is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if Stop Server is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool CanStopServer
        {
            get { return _canStopServer; }
            set
            {
                if (_canStopServer == value)
                    return;

                _canStopServer = value;
                NotifyOfPropertyChange(() => CanStopServer);
            }
        }

        /// <summary>
        /// Requests a new ETP session.
        /// </summary>
        public void StartServer()
        {
            Model.RequestedProtocols.Clear();
            Model.RequestedProtocols.AddRange(EtpProtocols.Where(x => x.IsSelected));
            Parent.InitEtpServer();
            CanRequestSession = false;
            CanStartServer = !Parent.SocketServer?.IsRunning ?? true;
            CanStopServer = !CanStartServer;
        }

        /// <summary>
        /// Closes the current ETP session.
        /// </summary>
        public void StopServer()
        {
            Parent.SocketServer?.Stop();
            CanStartServer = true;
            CanStopServer = false;
            CanCloseSession = false;
            CanRequestSession = true;
        }

        /// <summary>
        /// Requests a new ETP session.
        /// </summary>
        public void RequestSession()
        {
            Model.RequestedProtocols.Clear();
            Model.RequestedProtocols.AddRange(EtpProtocols.Where(x => x.IsSelected));
            Parent.OnConnectionChanged();
            CanRequestSession = false;
        }

        /// <summary>
        /// Closes the current ETP session.
        /// </summary>
        public void CloseSession()
        {
            Parent.EtpExtender?.CloseSession();
        }

        /// <summary>
        /// Retrieves the ETP Server's capabilities.
        /// </summary>
        public void ServerCapabilities()
        {
            Task.Run(GetServerCapabilities);
        }

        /// <summary>
        /// Called when the selected connection has changed.
        /// </summary>
        /// <param name="connection">The connection.</param>
        void ISessionAware.OnConnectionChanged(Connection connection)
        {
            // Nothing to do here as the connection change was initiated on this tab.
        }

        public void OnSessionOpened(IList<ISupportedProtocol> supportedProtocols)
        {
            CanRequestSession = false;
            CanCloseSession = true;
            CanStartServer = false;
            CanStopServer = Parent.SocketServer?.IsRunning ?? false;
        }

        public void OnSocketClosed()
        {
            CanCloseSession = false;
            CanStartServer = !Parent.SocketServer?.IsRunning ?? true;
            CanStopServer = !CanStartServer;
            CanRequestSession = CanStartServer;
        }

        private void OnConnectionChanged(Connection connection)
        {
            Model.Connection = connection;
            Model.Connection.SetServerCertificateValidation();
            Parent.OnConnectionChanged(false);
            CanStartServer = !Parent.SocketServer?.IsRunning ?? true;
            CanStopServer = !CanStartServer;
            CanRequestSession = !CanStopServer;
            CanCloseSession = false;

            var protocols = Parent.EtpExtender?.Protocols ??
                            connection.CreateEtpProtocols();

            Runtime.InvokeAsync(() =>
            {
                EtpProtocols.Clear();
                EtpProtocols.AddRange(protocols.GetProtocolItems());
            });
        }

        private Task<bool> GetServerCapabilities()
        {
            if (!Model.Connection.Uri.ToLowerInvariant().StartsWith("ws"))
                return Task.FromResult(false);

            try
            {
                Runtime.ShowBusy();
                
                var capabilities = Model.Connection.GetEtpServerCapabilities();

                Parent.LogDetailMessage(
                    "Server Capabilites:",
                    Parent.Session.Serialize(capabilities, true));

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _log.Warn("Error getting server capabilities", ex);
                Parent.LogClientError("Error getting server capabilities:", ex);
                return Task.FromResult(false);
            }
            finally
            {
                Runtime.ShowBusy(false);
            }
        }
    }
}
