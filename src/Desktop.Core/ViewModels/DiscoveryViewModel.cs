using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avro.Specific;
using Caliburn.Micro;
using Energistics.Etp;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.Object;
using PDS.WITSMLstudio.Connections;
using PDS.WITSMLstudio.Desktop.Core.Adapters;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// A view model for a ETP client implementing discovering protocol
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class DiscoveryViewModel : Screen, IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly AutoResetEvent _messageRespondedEvent;

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
            _messageRespondedEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Gets the runtime.
        /// </summary>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the connection picker view model.
        /// </summary>
        public ConnectionPickerViewModel ConnectionPicker { get; }

        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
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
        /// Gets or sets the currently active <see cref="IEtpClient"/> instance.
        /// </summary>
        public IEtpClient Client { get; set; }

        /// <summary>
        /// Gets the ETP extender instance.
        /// </summary>
        public IEtpExtender EtpExtender { get; private set; }

        /// <summary>
        /// Gets the resources to display in the tree view.
        /// </summary>
        public BindableCollection<ResourceViewModel> Resources { get; }

        /// <summary>
        /// Gets the selected resource.
        /// </summary>
        public ResourceViewModel SelectedResource => Resources.FindSelected();

        /// <summary>
        /// Gets the checked resources.
        /// </summary>
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

        private bool _expandAllInProcess;
        /// <summary>
        /// Gets or sets the expand all in process.
        /// </summary>
        public bool ExpandAllInProcess
        {
            get { return _expandAllInProcess; }
            set
            {
                if (value != _expandAllInProcess)
                {
                    _expandAllInProcess = value;
                    NotifyOfPropertyChange(() => ExpandAllInProcess);
                }
            }
        }
        /// <summary>
        /// Gets or sets the selected uris.
        /// </summary>
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
            _cancellationTokenSource?.Cancel();
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
        /// Expands all resources
        /// </summary>
        public async void ExpandAll()
        {
            Runtime.Invoke(() => StatusBarText = "Expanding All In Process...");


            await Task.Factory.StartNew(() =>
            {
                _messageRespondedEvent.Reset();
                _cancellationTokenSource = new CancellationTokenSource();
                ExpandAllInProcess = true;
                RefreshHierarchy();

                int index = WaitHandle.WaitAny(new[] { _messageRespondedEvent, _cancellationTokenSource.Token.WaitHandle }, 10000);

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Runtime.Invoke(() => StatusBarText = "Expanding All Cancelled.");
                    ExpandAllInProcess = false;
                    return;
                }
                else if (index == WaitHandle.WaitTimeout)
                {
                    Runtime.Invoke(() => StatusBarText = "Expanding All Timed Out.");
                    ExpandAllInProcess = false;
                    return;
                }

                var resourceList = Resources.ToList();
                foreach (var resource in resourceList)
                {
                    if (resource.ChildCount > 0)
                    {
                        if (!Expand(resource))
                        {
                            ExpandAllInProcess = false;
                            return;
                        }
                    }
                }
                ExpandAllInProcess = false;
                Runtime.Invoke(() => StatusBarText = "Expanding All Completed.");
           });
        }

        /// <summary>
        /// Expands the specified resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        public bool Expand(ResourceViewModel resource)
        {
            resource.IsExpanded = true;

            int index = WaitHandle.WaitAny(new[] { _messageRespondedEvent, _cancellationTokenSource.Token.WaitHandle }, 10000);

            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Runtime.Invoke(() => StatusBarText = "Expanding All Cancelled.");
                ExpandAllInProcess = false;
                return false;
            }
            else if (index == WaitHandle.WaitTimeout)
            {
                Runtime.Invoke(() => StatusBarText = "Expanding All Timed Out.");
                ExpandAllInProcess = false;
                return false;
            }

            var children = resource.Children.ToList();
            foreach (var child in children)
            {
                if (!child.IsExpanded && child.HasPlaceholder && child.Resource.TargetCount > 0)
                {
                    bool succeeded = Expand(child);
                    if (!succeeded)
                    {
                        ExpandAllInProcess = false;
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Cancels the expanding all.
        /// </summary>
        public void CancelExpandAll()
        {
            _cancellationTokenSource.Cancel();
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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_cancellationTokenSource != null)
                _cancellationTokenSource.Dispose();

            if (_messageRespondedEvent != null)
                _messageRespondedEvent.Dispose();

            if (Client != null)
                Client.Dispose();
        }

        /// <summary>
        /// Called when the current selected connection is  changed
        /// </summary>
        /// <param name="connection">The connection.</param>
        private async Task OnConnectionChanged(Connection connection)
        {
            // ToDo: Close current collection and save the selected URIs.
            await CloseEtpClient();
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

                EtpExtender = Client.CreateEtpExtender(requestedProtocols);

                EtpExtender.Register(onOpenSession: OnOpenSession,
                    onCloseSession:CloseEtpClient,
                    onGetResourcesResponse: OnGetResourcesResponse);

                Client.SocketClosed += OnClientSocketClosed;
                Client.OpenAsync();
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
            //int id = Thread.CurrentThread.ManagedThreadId;
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

                if (header.MessageFlags == 3)
                {
                    _messageRespondedEvent.Set();
                }

                return;
            }

            var parent = Resources.FindByMessageId(header.CorrelationId);
            if (parent == null) return;

            viewModel.Parent = parent;
            parent.Children.Add(viewModel);

            viewModel.IsChecked = CheckedUris.Contains(viewModel.Resource.Uri);
            viewModel.PropertyChanged += ResourcePropertyChanged;

            if (header.MessageFlags == 3)
            {
                _messageRespondedEvent.Set();
            }
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
                if (viewModel == null) return;

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
        private async Task CloseEtpClient()
        {
            if (Client == null) return;

            Client.SocketClosed -= OnClientSocketClosed;
            await Client.CloseAsync("Shutting down");
            Client.Dispose();
            Client = null;

            OnClientSocketClosed(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the <see cref="IEtpClient"/> socket is closed.
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
