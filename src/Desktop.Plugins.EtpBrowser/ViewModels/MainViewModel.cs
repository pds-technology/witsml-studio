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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avro.Specific;
using Caliburn.Micro;
using Energistics.Etp;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using Energistics.Etp.Common.Datatypes.Object;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PDS.WITSMLstudio.Connections;
using PDS.WITSMLstudio.Desktop.Core;
using PDS.WITSMLstudio.Desktop.Core.Adapters;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;
using PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.Properties;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the main user interface for the ETP Browser plug-in.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Conductor{IScreen}.Collection.OneActive" />
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Core.ViewModels.IPluginViewModel" />
    public sealed class MainViewModel : Conductor<IScreen>.Collection.OneActive, IPluginViewModel, IDisposable
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MainViewModel));
        private static readonly string _pluginDisplayName = Settings.Default.PluginDisplayName;
        private static readonly string _pluginVersion = typeof(MainViewModel).GetAssemblyVersion();
        private static readonly char[] _whiteSpace = Enumerable.Range(0, 20).Select(Convert.ToChar).ToArray();

        private readonly ConcurrentDictionary<int, JToken> _channels;
        private readonly List<IScreen> _protocolTabs;
        private DateTimeOffset _dateReceived;
        private IEtpClient _client;
        private IEtpServer _server;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel" /> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        [ImportingConstructor]
        public MainViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = EtpSettings.EtpSubProtocolName;
            DisplayName = _pluginDisplayName;
            Resources = new BindableCollection<ResourceViewModel>();
            _channels = new ConcurrentDictionary<int, JToken>();
            _protocolTabs = new List<IScreen>();

            Model = new Models.EtpSettings()
            {
                ApplicationName = Assembly.GetEntryAssembly().GetAssemblyName(),
                ApplicationVersion = _pluginVersion
            };

            Details = new TextEditorViewModel(runtime, "JavaScript", true)
            {
                ShowWriteSettings = true,
                IsScrollingEnabled = true
            };
            Messages = new TextEditorViewModel(runtime, "JavaScript", true)
            {
                ShowWriteSettings = true,
                IsScrollingEnabled = true
            };
            DataObject = new TextEditorViewModel(runtime, "XML", true)
            {
                ShowWriteSettings = true,
                IsPrettyPrintAllowed = true
            };
        }

        /// <summary>
        /// Gets the available ETP discovery functions.
        /// </summary>
        public IEnumerable<Functions> DiscoveryFunctions => new[] { Functions.GetResources, Functions.FindResources };

        /// <summary>
        /// Gets the available ETP store functions.
        /// </summary>
        public IEnumerable<Functions> StoreFunctions => new[] { Functions.GetObject, Functions.PutObject, Functions.DeleteObject, Functions.FindObjects };

        /// <summary>
        /// Gets the available ETP store functions.
        /// </summary>
        public IEnumerable<Functions> GrowingObjectFunctions => new[] { Functions.GetPart, Functions.GetPartsByRange, Functions.PutPart, Functions.DeletePart, Functions.DeletePartsByRange, Functions.FindParts };

        /// <summary>
        /// Gets the display order of the plug-in when loaded by the main application shell
        /// </summary>
        public int DisplayOrder => Settings.Default.PluginDisplayOrder;

        /// <summary>
        /// Gets the sub title to display in the main application title bar.
        /// </summary>
        public string SubTitle => Model?.Connection?.Name;

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        public Models.EtpSettings Model { get; }

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

        /// <summary>
        /// Gets the ETP socket server instance.
        /// </summary>
        public IEtpSelfHostedWebServer SelfHostedWebServer { get; private set; }

        /// <summary>
        /// Gets the ETP extender instance.
        /// </summary>
        public IEtpExtender EtpExtender { get; private set; }

        /// <summary>
        /// Gets or sets the currently active <see cref="IEtpClient"/> instance.
        /// </summary>
        /// <value>The ETP client instance.</value>
        public IEtpClient Client
        {
            get { return _client; }
            set
            {
                if (ReferenceEquals(_client, value))
                    return;

                _client = value;
                NotifyOfPropertyChange(() => Client);
            }
        }

        /// <summary>
        /// Gets the currently active <see cref="IEtpSession"/> instance.
        /// </summary>
        /// <value>The ETP client instance.</value>
        public IEtpSession Session => (IEtpSession)_client ?? _server;

        private TextEditorViewModel _details;

        /// <summary>
        /// Gets or sets the details editor.
        /// </summary>
        /// <value>The details editor.</value>
        public TextEditorViewModel Details
        {
            get { return _details; }
            set
            {
                if (!string.Equals(_details, value))
                {
                    _details = value;
                    NotifyOfPropertyChange(() => Details);
                }
            }
        }

        private TextEditorViewModel _messages;

        /// <summary>
        /// Gets or sets the messages editor.
        /// </summary>
        /// <value>The messages editor.</value>
        public TextEditorViewModel Messages
        {
            get { return _messages; }
            set
            {
                if (!string.Equals(_messages, value))
                {
                    _messages = value;
                    NotifyOfPropertyChange(() => Messages);
                }
            }
        }

        private TextEditorViewModel _dataObject;

        /// <summary>
        /// Gets or sets the data object editor.
        /// </summary>
        /// <value>The data object editor.</value>
        public TextEditorViewModel DataObject
        {
            get { return _dataObject; }
            set
            {
                if (!string.Equals(_dataObject, value))
                {
                    _dataObject = value;
                    NotifyOfPropertyChange(() => DataObject);
                }
            }
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
        /// Finds the resources using the DiscoveryQuery protocol.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>The message identifier.</returns>
        public Task<long> FindResources(string uri, ResourceViewModel parent = null)
        {
            var result = EtpExtender.FindResources(uri);
            return Task.FromResult(result);
        }

        /// <summary>
        /// Gets the selected resource's details using the Store protocol.
        /// </summary>
        public void GetObject()
        {
            var resource = SelectedResource;
            if (resource == null) return;

            SendGetObject(resource.Resource.Uri);
        }

        /// <summary>
        /// Sends the GetObject message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void SendGetObject(string uri)
        {
            EtpExtender.GetObject(uri);
        }

        /// <summary>
        /// Sends the FindObjects message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void SendFindObjects(string uri)
        {
            EtpExtender.FindObjects(uri);
        }

        /// <summary>
        /// Deletes the selected resource using the Store protocol.
        /// </summary>
        public void DeleteObject()
        {
            var resource = SelectedResource;
            if (resource == null) return;

            SendDeleteObject(resource.Resource.Uri);

            if (resource.Parent == null)
            {
                Resources.Remove(resource);
            }
            else
            {
                resource.Parent.Children.Remove(resource);
            } 
        }

        /// <summary>
        /// Sends the DeleteObject message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void SendDeleteObject(string uri)
        {
            EtpExtender.DeleteObject(uri);
        }

        /// <summary>
        /// Called when initializing.
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            _protocolTabs.Add(new SettingsViewModel(Runtime));
            _protocolTabs.Add(new Streaming11ViewModel(Runtime));
            _protocolTabs.Add(new Streaming12ViewModel(Runtime));
            _protocolTabs.Add(new HierarchyViewModel(Runtime));
            _protocolTabs.Add(new StoreViewModel(Runtime));
            _protocolTabs.Add(new StoreNotificationViewModel(Runtime));
            _protocolTabs.Add(new GrowingObject11ViewModel(Runtime));
            _protocolTabs.Add(new SubscribeViewModel(Runtime));
            _protocolTabs.Add(new DataLoadViewModel(Runtime));
            _protocolTabs.Add(new JsonMessageViewModel(Runtime));
            _protocolTabs.ForEach(Items.Add);
            ActivateItem(Items.First());
        }

        /// <summary>
        /// Update status when activated
        /// </summary>
        protected override void OnActivate()
        {
            base.OnActivate();
            Runtime.Invoke(() =>
            {
                if (Runtime.Shell != null)
                    Runtime.Shell.StatusBarText = "Ready";
            });
        }

        /// <summary>
        /// Called when deactivating.
        /// </summary>
        /// <param name="close">Inidicates whether this instance will be closed.</param>
        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                CloseEtpClient();
                CloseEtpServer();
            }

            base.OnDeactivate(close);
        }

        /// <summary>
        /// Called by a subclass when an activation needs processing.
        /// </summary>
        /// <param name="item">The item on which activation was attempted.</param>
        /// <param name="success">if set to <c>true</c> activation was successful.</param>
        protected override void OnActivationProcessed(IScreen item, bool success)
        {
            base.OnActivationProcessed(item, success);

            Runtime.Invoke(() =>
            {
                if (string.IsNullOrWhiteSpace(SubTitle))
                {
                    Runtime.Shell?.SetBreadcrumb(DisplayName, item.DisplayName);
                }
                else
                {
                    Runtime.Shell?.SetBreadcrumb(DisplayName, SubTitle, item.DisplayName);
                }
            });
        }

        /// <summary>
        /// Called when the connection has changed.
        /// </summary>
        /// <param name="reconnect">if set to <c>true</c> automatically reconnect.</param>
        /// <param name="updateTitle">if set to <c>true</c> set the application title.</param>
        public async Task OnConnectionChanged(bool reconnect = true, bool updateTitle = true)
        {
            CloseEtpClient();
            Resources.Clear();
            Messages.Clear();
            Details.Clear();
            EtpExtender = null;

            // notify child view models
            foreach (var screen in _protocolTabs)
            {
                var tab = screen as ISessionAware;
                tab?.OnConnectionChanged(Model.Connection);

                Items.Remove(screen);

                if (tab?.SupportedVersions == null ||
                    tab.SupportedVersions.ContainsIgnoreCase(Model.Connection.SubProtocol))
                {
                    Items.Add(tab);
                }
            }

            if (!string.IsNullOrWhiteSpace(Model.Connection.Uri) && reconnect)
            {
                await InitEtpClient();
            }

            if (updateTitle)
            {
                Runtime.Shell.SetApplicationTitle(this);
            }
        }

        /// <summary>
        /// Initializes the ETP client.
        /// </summary>
        public async Task InitEtpClient()
        {
            try
            {
                Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Connecting...");

                _log.Debug($"Establishing ETP connection for {Model.Connection}");

                Client = Model.Connection.CreateEtpClient(Model.ApplicationName, Model.ApplicationVersion);
                EtpExtender = Client.CreateEtpExtender(Model.RequestedProtocols);

                EtpExtender.Register(LogObjectDetails,
                    onOpenSession: OnOpenSession,
                    onGetResourcesResponse: OnGetResourcesResponse,
                    onObject: OnObject,
                    onObjectPart: OnObjectPart,
                    onOpenChannel: OnOpenChannel);

                Client.SocketClosed += OnClientSocketClosed;
                Client.Output = LogClientOutput;
                await Client.OpenAsync();
            }
            catch (Exception ex)
            {
                Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Error");
                Runtime.ShowError("Error connecting to server.", ex);
            }
        }

        /// <summary>
        /// Initializes the ETP client.
        /// </summary>
        public void InitEtpServer()
        {
            try
            {
                Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Listening...");

                var message = $"Listening for ETP connections on port {Model.PortNumber}...";
                LogClientOutput(message, true);
                _log.Debug(message);

                SelfHostedWebServer = EtpFactory.CreateSelfHostedWebServer(Model.PortNumber, Model.ApplicationName, Model.ApplicationVersion);
                SelfHostedWebServer.SessionConnected += OnServerSessionConnected;
                SelfHostedWebServer.SessionClosed += OnServerSessionClosed;
                SelfHostedWebServer.Start();
            }
            catch (Exception ex)
            {
                Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Error");
                Runtime.ShowError("Error initializing ETP server.", ex);
            }
        }

        private void OnServerSessionConnected(object sender, IEtpSession session)
        {
            var server = session as IEtpServer;
            if (server == null) return;

            server.Output = LogClientOutput;
            _server = server;

            var message = $"[{server.SessionId}] ETP client connected.";
            LogClientOutput(message, true);
            _log.Debug(message);

            EtpExtender = server.CreateEtpExtender(Model.RequestedProtocols);

            EtpExtender.Register(LogObjectDetails,
                onOpenSession: OnOpenSession,
                onCloseSession: OnCloseSession,
                onGetResourcesResponse: OnGetResourcesResponse,
                onObject: OnObject,
                onObjectPart: OnObjectPart,
                onOpenChannel: OnOpenChannel);
        }

        private async void OnServerSessionClosed(object sender, IEtpSession session)
        {
            var server = session as IEtpServer;
            if (server == null) return;

            var message = $"[{server.SessionId}] ETP client disconnected.";
            LogClientOutput(message, true);
            _log.Debug(message);

            await OnCloseSession();

            if (server == _server)
            {
                _server = null;
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
            Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Connected");
            LogObjectDetails(new ProtocolEventArgs<ISpecificRecord>(header, message));

            // notify child view models
            Items.OfType<ISessionAware>()
                .ForEach(x => x.OnSessionOpened(supportedProtocols));
        }

        /// <summary>
        /// Called when the ETP session is closed.
        /// </summary>
        private async Task OnCloseSession()
        {
            await Runtime.InvokeAsync(() =>
            {
                if (Runtime.Shell != null)
                    Runtime.Shell.StatusBarText = "Connection Closed";
            });

            // notify child view models
            Items.OfType<ISessionAware>()
                .ForEach(x => x.OnSocketClosed());
        }

        /// <summary>
        /// Closes the ETP server.
        /// </summary>
        private void CloseEtpServer()
        {
            if (SelfHostedWebServer == null) return;

            OnServerSessionClosed(this, _server);

            SelfHostedWebServer.SessionConnected -= OnServerSessionConnected;
            SelfHostedWebServer.SessionClosed -= OnServerSessionClosed;
            SelfHostedWebServer.Dispose();
            SelfHostedWebServer = null;
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
        /// Called when the <see cref="IEtpClient"/> socket is closed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnClientSocketClosed(object sender, EventArgs e)
        {
            Runtime.Invoke(() =>
            {
                if (Runtime.Shell != null)
                    Runtime.Shell.StatusBarText = "Connection Closed";
            });

            // notify child view models
            Items.OfType<ISessionAware>()
                .ForEach(x => x.OnSocketClosed());
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

            LogObjectDetails(new ProtocolEventArgs<ISpecificRecord>(header, message));

            //  Handle when message is received from JSON Message tab
            if (string.IsNullOrWhiteSpace(uri))
                return;
            
            //  If the message URI equals "/" or the current base URI then treat
            //  it as a root object.
            if (EtpUri.IsRoot(uri) || uri.EqualsIgnoreCase(Model.BaseUri))
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
        }

        /// <summary>
        /// Called when the OpenChannel message is recieved.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        /// <param name="channels">The channels.</param>
        private void OnOpenChannel(IMessageHeader header, ISpecificRecord message, IList<IChannelMetadataRecord> channels)
        {
            var dataLoadSettings = Model.DataLoad;

            var lastIndex = dataLoadSettings.IsTimeIndex
                ? new DateTimeOffset(dataLoadSettings.LastTimeIndex).ToUnixTimeMicroseconds()
                : (object) dataLoadSettings.LastDepthIndex;

            foreach (var channel in channels)
            {
                EtpExtender.OpenChannelResponse(header, channel.ChannelUri, channel.ChannelId, lastIndex, dataLoadSettings.IsInfill, dataLoadSettings.IsDataChange);
            }
        }

        /// <summary>
        /// Called when the GetObject response is received.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        /// <param name="dataObject">The data object.</param>
        private void OnObject(IMessageHeader header, ISpecificRecord message, IDataObject dataObject)
        {
            LogDataObject(new ProtocolEventArgs<ISpecificRecord>(header, message), dataObject);
        }

        /// <summary>
        /// Called when an ObjectFragment or ObjectPart message is recieved.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        /// <param name="dataObject">The data object.</param>
        private void OnObjectPart(IMessageHeader header, ISpecificRecord message, IDataObject dataObject)
        {
            LogDataObject(new ProtocolEventArgs<ISpecificRecord>(header, message), dataObject, true);
        }

        /// <summary>
        /// Logs the data object.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="e">The <see cref="ProtocolEventArgs{T}"/> instance containing the event data.</param>
        /// <param name="dataObject">The data object.</param>
        /// <param name="append">if set to <c>true</c> append the data object; otherwise, replace.</param>
        private void LogDataObject<T>(ProtocolEventArgs<T> e, IDataObject dataObject, bool append = false) where T : ISpecificRecord
        {
            dataObject.Resource?.FormatLastChanged();

            LogObjectDetails(e);

            // Check if user wants to see decoded byte arrays
            if (!Model.DecodeByteArrays) return;

            var data = dataObject
                .GetString()
                .Trim(_whiteSpace);

            var uri = new EtpUri(dataObject.Resource.Uri);
            var isJson = EtpContentType.Json.EqualsIgnoreCase(uri.Format);

            if (isJson)
            {
                var objectType = OptionsIn.DataVersion.Version200.Equals(uri.Version)
                    ? ObjectTypes.GetObjectType(uri.ObjectType, uri.Version)
                    : ObjectTypes.GetObjectGroupType(uri.ObjectType, uri.Version);

                var instance = EtpExtensions.Deserialize(objectType, data);
                data = EtpExtensions.Serialize(instance, true);
            }

            if (append)
            {
                DataObject.Append(data);
                DataObject.Append(Environment.NewLine + Environment.NewLine);
            }
            else
            {
                DataObject.SetText(data);
            }

            Runtime.Invoke(() => DataObject.Language = isJson ? "JavaScript" : "XML");
        }

        /// <summary>
        /// Logs the object details.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="e">The <see cref="ProtocolEventArgs{T}"/> instance containing the event data.</param>
        private void LogObjectDetails<T>(ProtocolEventArgs<T> e) where T : ISpecificRecord
        {
            Details.SetText(string.Format(
                "// Header:{2}{0}{2}{2}// Body:{2}{1}{2}",
                EtpExtensions.Serialize(e.Header, true),
                EtpExtensions.Serialize(e.Message, true),
                Environment.NewLine));
        }

        /// <summary>
        /// Logs the object details.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <typeparam name="TContext">The context type.</typeparam>
        /// <param name="e">The <see cref="ProtocolEventArgs{T, TContext}"/> instance containing the event data.</param>
        private void LogObjectDetails<T, TContext>(ProtocolEventArgs<T, TContext> e) where T : ISpecificRecord
        {
            Details.SetText(string.Format(
                "// Header:{3}{0}{3}{3}// Body:{3}{1}{3}{3}// Context:{3}{2}{3}",
                EtpExtensions.Serialize(e.Header, true),
                EtpExtensions.Serialize(e.Message, true),
                EtpExtensions.Serialize(e.Context, true),
                Environment.NewLine));
        }

        /// <summary>
        /// Logs the detail message.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        internal void LogDetailMessage(string header, string message = null)
        {
            Details.SetText(string.Concat(
                header.IsJsonString() ? string.Empty : "// ",
                header,
                Environment.NewLine));

            if (string.IsNullOrWhiteSpace(message))
                return;

            Details.Append(string.Concat(
                message.IsJsonString() ? string.Empty : "// ",
                message,
                Environment.NewLine));
        }

        /// <summary>
        /// Logs the client output.
        /// </summary>
        /// <param name="message">The message.</param>
        internal void LogClientOutput(string message)
        {
            LogClientOutput(message, false);
        }

        /// <summary>
        /// Logs the client output.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="logDetails">if set to <c>true</c> logs the detail message.</param>
        internal void LogClientOutput(string message, bool logDetails)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            if (logDetails)
                LogDetailMessage(message);

            try
            {
                if (message.IsJsonString())
                {
                    message = FormatMessage(message);
                }
                else
                {
                    const string receivedText = "Message received at ";
                    var index = message.IndexOf(receivedText, StringComparison.InvariantCultureIgnoreCase);
                    if (index != -1)
                    {
                        if (DateTimeOffset.TryParse(message.Substring(index + receivedText.Length).Trim(), out _dateReceived))
                        {
                            _dateReceived = _dateReceived.ToUniversalTime();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Error formatting ETP message:{Environment.NewLine}{message}", ex);
            }

            Messages.Append(string.Concat(
                message.IsJsonString() ? string.Empty : "// ",
                message,
                Environment.NewLine));
        }

        /// <summary>
        /// Logs the client error.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        internal void LogClientError(string message, Exception error)
        {
            LogClientOutput($"{message}\n/*\n{error}\n*/", true);
        }

        private string FormatMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return string.Empty;

            var jObject = JObject.Parse(message);

            FormatResource(jObject["resource"] as JObject);
            FormatDataObject(jObject["dataObject"] as JObject);
            FormatChannelMetadataRecords(jObject["channels"] as JArray);
            FormatChannelRangeRequests(jObject["channelRanges"] as JArray);
            FormatChannelData(jObject["data"] as JArray);

            return jObject["protocol"] != null
                ? jObject.ToString(Formatting.None)
                : jObject.ToString();
        }

        private void FormatChannelData(JArray data)
        {
            if (data == null || data.Count < 1) return;

            foreach (var dataItem in data)
            {
                var channelId = dataItem.Value<int>("channelId");

                JToken channel;
                if (!_channels.TryGetValue(channelId, out channel)) continue;

                // Append custom data to each channel data item (for visual inspection only)
                var customData = dataItem["_customData"] = new JObject();
                customData["_mnemonic"] = channel.Value<string>("channelName");

                // Process index values
                var indexes = channel["indexes"] as JArray;
                var values = dataItem["indexes"] as JArray;

                if (indexes == null || values == null) continue;
                if (indexes.Count != values.Count) continue;

                for (var i = 0; i < indexes.Count; i++)
                {
                    var index = indexes[i];
                    var indexValue = values.Value<long>(i);
                    FormatIndex(index, customData, indexValue);
                }

                // Check if user wants to display byte arrays
                var dataItemValue = dataItem["value"] as JObject;

                if (!Model.DisplayByteArrays && dataItemValue != null && $"{dataItemValue["item"]}".StartsWith(@"\"))
                {
                    dataItemValue["item"] = @"\u001F...";
                }
            }
        }

        private void FormatChannelRangeRequests(JArray channelRanges)
        {
            if (channelRanges == null || channelRanges.Count < 1) return;

            foreach (var channelRange in channelRanges)
            {
                var channelId = channelRange["channelId"].Value<int>(0);

                JToken channel;
                if (!_channels.TryGetValue(channelId, out channel)) continue;

                var indexes = channel?["indexes"] as JArray;
                if (indexes == null || indexes.Count < 1) return;

                // Append custom data to each channel data item (for visual inspection only)
                var customData = channelRange["_customData"] = new JObject();
                customData["_mnemonic"] = channel.Value<string>("channelName");

                var primaryStartIndex = indexes[0].DeepClone();
                primaryStartIndex["mnemonic"]["string"] = "startIndex";

                var primaryEndIndex = indexes[0].DeepClone();
                primaryEndIndex["mnemonic"]["string"] = "endIndex";

                var startIndex = channelRange.Value<long>("startIndex");
                var endIndex = channelRange.Value<long>("endIndex");

                FormatIndex(primaryStartIndex, customData, startIndex);
                FormatIndex(primaryEndIndex, customData, endIndex);
            }
        }

        private void FormatChannelMetadataRecords(JArray channels)
        {
            if (channels == null || channels.Count < 1) return;

            // Check to make sure we only process ChannelMetadataRecord
            var firstChannel = channels[0] as JObject;
            var indexes = firstChannel?["indexes"] as JArray;

            if (indexes == null || indexes.Count < 1)
            {
                var startIndex = firstChannel?["startIndex"] as JObject;

                if (startIndex != null)
                    FormatStreamingStartInfo(channels);

                return;
            }

            foreach (var channel in channels)
            {
                var channelId = channel.Value<int>("channelId");
                _channels[channelId] = channel;

                FormatIndexRange(channel as JObject);
                FormatDataObject(channel["domainObject"] as JObject);
            }
        }

        private void FormatStreamingStartInfo(JArray channelInfos)
        {
            if (channelInfos == null || channelInfos.Count < 1) return;

            foreach (var channelInfo in channelInfos)
            {
                var channelId = channelInfo.Value<int>("channelId");

                JToken channel;
                if (!_channels.TryGetValue(channelId, out channel)) continue;

                FormatIndexRange(channel as JObject, channelInfo);
            }
        }

        private void FormatIndexRange(JObject channel, JToken channelData = null)
        {
            var indexes = channel?["indexes"] as JArray;
            if (indexes == null || indexes.Count < 1) return;

            var primaryIndex = indexes[0];
            channelData = channelData ?? channel;

            FormatIndex(primaryIndex, channelData["startIndex"]);
            FormatIndex(primaryIndex, channelData["endIndex"]);
        }

        private void FormatIndex(JToken indexMetadata, JToken indexData)
        {
            if (indexData == null || !indexData.HasValues) return;

            // Check if there is an embedded item attribute
            indexData = indexData["item"] ?? indexData;

            if (!indexData.HasValues) return;

            if (indexData["long"] != null)
                FormatIndex(indexMetadata, indexData, indexData.Value<long>("long"));

            if (indexData["double"] != null)
                FormatIndex(indexMetadata, indexData, indexData.Value<double>("double"));
        }

        private void FormatIndex(JToken indexMetadata, JToken indexData, object indexValue)
        {
            if (indexMetadata == null || indexData == null) return;

            var mnemonicObj = indexMetadata["mnemonic"] as JObject;
            var mnemonic = mnemonicObj?.Value<string>("string") ??
                           indexMetadata.Value<string>("mnemonic");
            
            var indexType = indexMetadata.Value<string>("indexType") ?? indexMetadata.Value<string>("indexKind");
            var scale = indexMetadata.Value<int?>("scale");

            if ("Time".EqualsIgnoreCase(indexType))
            {
                var timeIndex = DateTimeExtensions.FromUnixTimeMicroseconds((long)indexValue);
                var elapsedTime = _dateReceived.Subtract(timeIndex);
                indexData[$"_{mnemonic}"] = JToken.FromObject(timeIndex.ToString("o"));
                indexData["_ElapsedTime"] = elapsedTime;
            }
            else
            {
                var value = scale.HasValue ? ((long)indexValue).IndexFromScale(scale.Value) : indexValue;
                indexData[$"_{mnemonic}"] = JToken.FromObject(value);
            }
        }

        private void FormatDataObject(JObject dataObject)
        {
            if (dataObject == null) return;
            FormatResource(dataObject["resource"] as JObject);

            // Check if user wants to display byte arrays
            if (!Model.DisplayByteArrays && dataObject["data"] != null)
                dataObject["data"] = @"\u001F...";
        }

        private void FormatResource(JObject resource)
        {
            if (resource == null) return;

            var lastChangedObj = resource["lastChanged"] as JObject;
            var lastChanged = lastChangedObj?.Value<long?>("long") ??
                              resource.Value<long?>("lastChanged");

            if (!lastChanged.HasValue || lastChanged < 1) return;

            var customData = resource["customData"] as JObject;
            if (customData == null) return;

            customData["_lastChanged"] = DateTimeExtensions
                .FromUnixTimeMicroseconds(lastChanged.Value)
                .ToString("o");
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // NOTE: dispose managed state (managed objects).
                    Client?.Dispose();
                    SelfHostedWebServer?.Dispose();
                }

                // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // NOTE: set large fields to null.

                _disposedValue = true;
            }
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MainViewModel() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // NOTE: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
