using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avro.Specific;
using Caliburn.Micro;

using Energistics.Etp;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.Object;
using Energistics.Etp.v12;
using PDS.WITSMLstudio.Desktop.Core;
using PDS.WITSMLstudio.Desktop.Core.Adapters;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// A view model for a ETP client implementing discovering protocol
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class DiscoveryViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(DiscoveryViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public DiscoveryViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            ConnectionPicker = new ConnectionPickerViewModel(runtime, ConnectionTypes.Etp)
            {
                AutoConnectEnabled = true,
                OnConnectionChanged = OnConnectionChanged
            };
            Resources = new BindableCollection<ResourceViewModel>();
        }

        /// <summary>
        /// Gets the runtime.
        /// </summary>
        /// <value>
        /// The runtime.
        /// </value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the connection picker view model.
        /// </summary>
        /// <value>The connection picker view model.</value>
        public ConnectionPickerViewModel ConnectionPicker { get; }

        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        public Connection Connection { get; set; }

        private bool _isConnected;
        /// <summary>
        /// This indicates if the server is connected
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected;}
            set
            {
                if (value != _isConnected)
                {
                    _isConnected = value;
                    NotifyOfPropertyChange(()=> IsConnected);
                }
            }
        }

         /// <summary>
        /// Gets or sets the currently active <see cref="EtpClient"/> instance.
        /// </summary>
        /// <value>The ETP client instance.</value>
        public EtpClient Client { get; set; }

        /// <summary>
        /// Gets the ETP extender instance.
        /// </summary>
        public IEtpExtender EtpExtender { get; private set; }

        /// <summary>
        /// Gets the resources to display in the tree view.
        /// </summary>
        /// <value>The collection of resources.</value>
        public BindableCollection<ResourceViewModel> Resources { get; }

        /// <summary>
        /// Gets the selected resource.
        /// </summary>
        /// <value>The selected resource.</value>
        public ResourceViewModel SelectedResource => Resources.FindSelected();

        /// <summary>
        /// Gets the checked resources.
        /// </summary>
        /// <value>The checked resources.</value>
        public IEnumerable<ResourceViewModel> CheckedResources => Resources.FindChecked();

        private string _statusBarText;
        /// <summary>
        /// Gets or sets the status bar text
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
        /// Gets or sets the selected uris.
        /// </summary>
        /// <value>
        /// The selected uris.
        /// </value>
        public ObservableCollection<string> CheckedUris { get; set; }

        /// <summary>
        /// Connect to the selected ETP server.
        /// </summary>
        public void Connect()
        {
            InitEtpClient();
        }

        /// <summary>
        /// Disconnects to the ETP server.
        /// </summary>
        public void Disconnect()
        {
            EtpExtender?.CloseSession();
        }

        /// <summary>
        /// Close the view.
        /// </summary>
        public void CloseDialog()
        {
            Disconnect();
            TryClose(true);
        }

        /// <summary>
        /// Gets the resources using the Discovery protocol.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>The message identifier.</returns>
        public Task<long> GetResources(string uri, ResourceViewModel parent = null)
        {
            var result = EtpExtender.GetResources(uri);
            return Task.FromResult(result);
        }

        /// <summary>
        /// Refreshes the selected node.
        /// </summary>
        public void RefreshSelected()
        {
            var resource = Resources.FindSelected();
            // Return if there is nothing currently selected
            if (resource == null) return;

            resource.ClearAndLoadChildren();
            // Expand the node if it wasn't previously
            resource.IsExpanded = true;
        }

        /// <summary>
        /// Refreshes the hierarchy.
        /// </summary>
        public void RefreshHierarchy()
        {
            Resources.Clear();
            GetResources(EtpUri.RootUri);
        }

        /// <summary>
        /// Clears the checked items.
        /// </summary>
        public void ClearCheckedItems()
        {
            foreach (var resource in CheckedResources)
            {
                resource.IsChecked = false;
            }
        }

        /// <summary>
        /// Called when the current selected connection is  changed
        /// </summary>
        /// <param name="connection">The connection.</param>
        private void OnConnectionChanged(Connection connection)
        {
            // ToDo: Close current collection and save the selected URIs.
            CloseEtpClient();
            Resources.Clear();
            EtpExtender = null;

            Connection = connection;
        }

        /// <summary>
        /// Initializes the ETP client.
        /// </summary>
        private void InitEtpClient()
        {
            try
            {
                Runtime.Invoke(() => StatusBarText = "Connecting...");

                var applicationName = GetType().Assembly.FullName;
                var applicationVersion = GetType().GetAssemblyVersion();

                Client = Connection.CreateEtpClient(applicationName, applicationVersion);
                BindableCollection<EtpProtocolItem> requestedProtocols = new BindableCollection<EtpProtocolItem>();
                requestedProtocols.Add(new EtpProtocolItem(Energistics.Etp.v11.Protocols.Discovery, "store", true));

                EtpExtender = Client.CreateEtpExtender(requestedProtocols, true);

                EtpExtender.Register(onOpenSession: OnOpenSession,
                    onGetResourcesResponse: OnGetResourcesResponse);

                Client.SocketClosed += OnClientSocketClosed;
                Client.Open();
            }
            catch (Exception)
            {
                Runtime.Invoke(() => StatusBarText = "Error Connecting");
            }

        }

        /// <summary>
        /// Called when the ETP session is initialized.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        /// <param name="supportedProtocols">The supported protocols.</param>
        private void OnOpenSession(IMessageHeader header, ISpecificRecord message, IList<ISupportedProtocol> supportedProtocols)
        {
            Runtime.Invoke(() => StatusBarText = "Connected");
            IsConnected = true;
            GetResources(EtpUri.RootUri);
        }

        /// <summary>
        /// Called when the GetResources response is received.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="uri">The URI.</param>
        private void OnGetResourcesResponse(IMessageHeader header, ISpecificRecord message, IResource resource, string uri)
        {
            int id = Thread.CurrentThread.ManagedThreadId;
            var viewModel = ResourceViewModel.NoData;

            // Handle case when "No Data" Acknowledge message was received
            if (resource != null)
            {
                viewModel = new ResourceViewModel(Runtime, resource)
                {
                    LoadChildren = GetResources
                };

                resource.FormatLastChanged();
            }

            //  Handle when message is received from JSON Message tab
            if (string.IsNullOrWhiteSpace(uri))
                return;

           //  If the message URI equals "/" or the current base URI then treat
            //  it as a root object.
            if (EtpUri.IsRoot(uri))
            {
                Resources.ForEach(x => x.IsSelected = false);
                viewModel.IsSelected = true;
                Resources.Add(viewModel);
                return;
            }

            var parent = Resources.FindByMessageId(header.CorrelationId);
            if (parent == null) return;

            viewModel.Parent = parent;
            parent.Children.Add(viewModel);

            viewModel.IsChecked = CheckedUris.Contains(viewModel.Resource.Uri);
            viewModel.PropertyChanged += ResourcePropertyChanged;


        }

        /// <summary>
        /// Event handler when resources property is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void ResourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked")
            {
                var viewModel = sender as ResourceViewModel;
                if (viewModel.IsChecked && !CheckedUris.Contains(viewModel.Resource.Uri))
                {
                    CheckedUris.Add(viewModel.Resource.Uri);
                }
                else if (!viewModel.IsChecked)
                {
                    CheckedUris.Remove(viewModel.Resource.Uri);
                }
            }
        }

        /// <summary>
        /// Closes the ETP client.
        /// </summary>
        private void CloseEtpClient()
        {
            if (Client == null) return;

            Client.SocketClosed -= OnClientSocketClosed;
            Client.Dispose();
            Client = null;

            OnClientSocketClosed(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the <see cref="EtpClient"/> socket is closed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnClientSocketClosed(object sender, EventArgs e)
        {
            IsConnected = false;
            Runtime.Invoke(() =>
            {
                StatusBarText = "Connection Closed";
            });
        }

    }
}
