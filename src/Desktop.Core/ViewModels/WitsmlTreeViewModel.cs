//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2017.1
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Caliburn.Micro;
using Energistics.DataAccess;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Linq;
using PDS.WITSMLstudio.Query;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using IDataObject = Energistics.DataAccess.IDataObject;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// Manages the display and interaction of the WITSML hierarchy view.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class WitsmlTreeViewModel : Screen, IDisposable
    {
        private FrameworkElement _hierarchy;
        private long _messageId;

        private readonly object _lock = new object();
        private readonly object _loadLock = new object();
        private bool _cleared;
        private CancellationTokenSource _tokenSource;

        private readonly HashSet<EtpUri> _growingObjects = new HashSet<EtpUri>();
        private readonly HashSet<EtpUri> _activeWellbores = new HashSet<EtpUri>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlTreeViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public WitsmlTreeViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            Items = new BindableCollection<ResourceViewModel>();
            DataObjects = new BindableCollection<string>();
            RigNames = new BindableCollection<string>();

            BindingOperations.EnableCollectionSynchronization(Items, _lock);
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the TreeView items.
        /// </summary>
        /// <value>The TreeView items.</value>
        public BindableCollection<ResourceViewModel> Items { get; }

        /// <summary>
        /// Gets the collection of supported data objects.
        /// </summary>
        /// <value>The data objects.</value>
        public BindableCollection<string> DataObjects { get; }

        /// <summary>
        /// Gets the collection of rig names.
        /// </summary>
        public BindableCollection<string> RigNames { get; }

        /// <summary>
        /// Gets or sets an action to execute when the context menu is refreshed.
        /// </summary>
        public System.Action OnRefreshContextMenu { get; set; }

        private string _wellName = string.Empty;

        /// <summary>
        /// Gets or sets the name of the well.
        /// </summary>
        /// <value>The name of the well.</value>
        public string WellName
        {
            get { return _wellName; }
            set
            {
                if (value == null) value = string.Empty;

                if (string.Equals(_wellName, value))
                    return;

                _wellName = value;
                NotifyOfPropertyChange(() => WellName);
                NotifyOfPropertyChange(() => CanClearWellName);
                UpdateWellVisibility();
            }
        }


        private string _selectedRigName = string.Empty;

        /// <summary>
        /// Gets or sets the name of the rig.
        /// </summary>
        /// <value>The name of the rig.</value>
        public string SelectedRigName
        {
            get { return _selectedRigName; }
            set
            {
                if (value == null) value = string.Empty;

                if (string.Equals(_selectedRigName, value))
                    return;

                _selectedRigName = value;
                NotifyOfPropertyChange(() => SelectedRigName);
                NotifyOfPropertyChange(() => CanClearSelectedRigName);
                UpdateWellVisibility();
            }
        }

        private bool _showOnlyActiveWells;

        /// <summary>
        /// Gets or sets whether to show only active wells in 
        /// </summary>
        public bool ShowOnlyActiveWells
        {
            get { return _showOnlyActiveWells; }
            set
            {
                if (_showOnlyActiveWells == value) return;

                _showOnlyActiveWells = value;
                NotifyOfPropertyChange(() => ShowOnlyActiveWells);
                UpdateWellVisibility();
            }
        }

        private IWitsmlContext _context;

        /// <summary>
        /// Gets or sets the WITSML context.
        /// </summary>
        /// <value>The WITSML context.</value>
        public IWitsmlContext Context
        {
            get { return _context; }
            private set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(() => Context);

                    UpdateFromContext();
                }
            }
        }

        private bool _loading;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="WitsmlTreeViewModel"/> is loading.
        /// </summary>
        /// <value>
        ///   <c>true</c> if loading; otherwise, <c>false</c>.
        /// </value>
        public bool Loading
        {
            get { return _loading; }
            set
            {
                if (_loading == value) return;
                _loading = value;
                NotifyOfPropertyChange(() => Loading);
            }
        }

        private Thickness _checkBoxPadding = new Thickness(5, 0, 0, 0);

        /// <summary>
        /// Gets or sets the CheckBox padding.
        /// </summary>
        /// <value>The CheckBox padding.</value>
        public Thickness CheckBoxPadding
        {
            get { return _checkBoxPadding; }
            set
            {
                if (_checkBoxPadding == value) return;
                _checkBoxPadding = value;
                NotifyOfPropertyChange(() => CheckBoxPadding);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can clear well name.
        /// </summary>
        /// <value><c>true</c> if this instance can clear well name; otherwise, <c>false</c>.</value>
        public bool CanClearWellName => !string.IsNullOrEmpty(WellName);

        /// <summary>
        /// Clears the name of the well.
        /// </summary>
        public void ClearWellName()
        {
            WellName = string.Empty;
            NotifyOfPropertyChange(() => CanClearWellName);
        }

        /// <summary>
        /// Gets a value indicating whether this instance can clear rig name.
        /// </summary>
        /// <value><c>true</c> if this instance can clear rig name; otherwise, <c>false</c>.</value>
        public bool CanClearSelectedRigName => !string.IsNullOrEmpty(SelectedRigName);

        /// <summary>
        /// Clears the name of the rig.
        /// </summary>
        public void ClearSelectedRigName()
        {
            SelectedRigName = string.Empty;
            NotifyOfPropertyChange(() => CanClearSelectedRigName);
        }

        private RigsMonitor _rigsMonitor;

        /// <summary>
        /// The rigs monitor.
        /// </summary>
        private RigsMonitor RigsMonitor
        {
            get { return _rigsMonitor; }
            set
            {
                lock (_lock)
                {
                    if (_rigsMonitor == value) return;

                    if (_rigsMonitor != null)
                        _rigsMonitor.RigsChanged -= OnRigsMonitorRigsChanged;

                    _rigsMonitor = value;

                    if (_rigsMonitor != null)
                        _rigsMonitor.RigsChanged += OnRigsMonitorRigsChanged;

                    UpdateFromRigsMonitor();
                }
            }
        }

        #region Context Menu

        private bool _isContextMenuEnabled;

        /// <summary>
        /// Gets or sets a value indicating whether the context menu is enabled.
        /// </summary>
        /// <value><c>true</c> if the context menu is enabled; otherwise, <c>false</c>.</value>
        public bool IsContextMenuEnabled
        {
            get { return _isContextMenuEnabled; }
            set
            {
                if (_isContextMenuEnabled == value) return;
                _isContextMenuEnabled = value;
                NotifyOfPropertyChange(() => IsContextMenuEnabled);
            }
        }

        private int? _maxDataRows;

        /// <summary>
        /// Gets or sets the maximum data rows.
        /// </summary>
        /// <value>The maximum data rows.</value>
        public int? MaxDataRows
        {
            get { return _maxDataRows; }
            set
            {
                if (_maxDataRows != value)
                {
                    _maxDataRows = value;
                    NotifyOfPropertyChange(() => MaxDataRows);
                    UpdateGetObjectDetailsProperties();
                }
            }
        }

        private int? _requestLatestValues;

        /// <summary>
        /// Gets or sets the number of latest values for the request(growing object only).
        /// </summary>
        /// The number of latest values for the request.
        public int? RequestLatestValues
        {
            get { return _requestLatestValues; }
            set
            {
                if (_requestLatestValues != value)
                {
                    _requestLatestValues = value;
                    NotifyOfPropertyChange(() => RequestLatestValues);
                    UpdateGetObjectDetailsProperties();
                }
            }
        }

        private string _extraOptionsIn;

        /// <summary>
        /// Gets or sets the extra options in.
        /// </summary>
        /// <value>
        /// The extra options in.
        /// </value>
        public string ExtraOptionsIn
        {
            get { return _extraOptionsIn; }
            set
            {
                if (_extraOptionsIn != value)
                {
                    _extraOptionsIn = value;
                    NotifyOfPropertyChange(() => ExtraOptionsIn);
                    NotifyOfPropertyChange(() => CanGetObjectDetailsWithExtraOptionsIn);
                }
            }
        }

        /// <summary>
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetObjectIds
        {
            get
            {
                var resource = Items.FindSelected(_lock);
                if (resource == null) return false;

                var uri = new EtpUri(resource.Resource.Uri);
                var parentUri = uri.Parent;

                return !string.IsNullOrWhiteSpace(uri.ObjectId)
                       && !ObjectTypes.IsGrowingDataObject(parentUri.ObjectType);
            }
        }

        /// <summary>
        /// Gets the selected item's IDs using a GetFromStore request.
        /// </summary>
        public void GetObjectIds()
        {
            var resource = Items.FindSelected(_lock);
            var uri = new EtpUri(resource.Resource.Uri);

            Runtime.ShowBusy();
            Task.Run(() =>
            {
                Context.GetObjectIdOnly(uri.ObjectType, uri);
                Runtime.ShowBusy(false);
            });
        }

        /// <summary>
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetObjectHeader
        {
            get
            {
                var resource = Items.FindSelected(_lock);
                if (resource == null) return false;

                var uri = new EtpUri(resource.Resource.Uri);

                return !string.IsNullOrWhiteSpace(uri.ObjectId)
                       && ObjectTypes.IsGrowingDataObject(uri.ObjectType);
            }
        }

        /// <summary>
        /// Gets the selected item's details using a GetFromStore request.
        /// </summary>
        public void GetObjectHeader()
        {
            var resource = Items.FindSelected(_lock);
            var uri = new EtpUri(resource.Resource.Uri);

            Runtime.ShowBusy();
            Task.Run(() =>
            {
                Context.GetGrowingObjectHeaderOnly(uri.ObjectType, uri);
                Runtime.ShowBusy(false);
            });
        }

        /// <summary>
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetObjectDetails
        {
            get
            {
                if (!CanGetObjectHeader)
                    return false;

                var resource = Items.FindSelected(_lock);
                var uri = new EtpUri(resource.Resource.Uri);

                return uri.Version.Equals(OptionsIn.DataVersion.Version141.Value);
            }
        }

        /// <summary>
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetObjectDetailsWithReturnElementsAll => CanGetObjectIds;

        /// <summary>
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetObjectDetailsWithMaxDataRows => CanGetObjectHeader && MaxDataRows.HasValue;

        /// <summary>
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetObjectDetailsWithRequestLatest => CanGetObjectHeader && RequestLatestValues.HasValue;

        /// <summary>
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetObjectDetailsWithAllOptions
            => CanGetObjectHeader && (MaxDataRows.HasValue || RequestLatestValues.HasValue);

        /// <summary>
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetObjectDetailsWithExtraOptionsIn
            => CanGetObjectHeader && !string.IsNullOrWhiteSpace(ExtraOptionsIn);

        /// <summary>
        /// Gets the selected item's tooltip.
        /// </summary>
        /// <value>The tooltip</value>
        public string ObjectDetailsWithMaxDataRowsTooltip => MaxDataRows.HasValue
            ? $"returnElements=all;maxReturnNodes={MaxDataRows.Value}"
            : "returnElements=all";

        /// <summary>
        /// Gets the selected item's tooltip.
        /// </summary>
        /// <value>The tooltip</value>
        public string ObjectDetailsWithRequestLatestTooltip => RequestLatestValues.HasValue
            ? $"returnElements=all;requestLatestValues={RequestLatestValues.Value}"
            : "returnElements=all";

        /// <summary>
        /// Gets the selected item's tooltip.
        /// </summary>
        /// <value>The tooltip</value>
        public string ObjectDetailsWithAllOptionsTooltip
        {
            get
            {
                var tooltip = new StringBuilder("returnElements=all;");
                if (MaxDataRows.HasValue)
                    tooltip.Append($"maxReturnNodes={MaxDataRows.Value};");
                if (RequestLatestValues.HasValue)
                    tooltip.Append($"requestLatestValues={RequestLatestValues.Value};");

                return tooltip.ToString();
            }
        }

        /// <summary>
        /// Updates the properties.
        /// </summary>
        public void UpdateGetObjectDetailsProperties()
        {
            NotifyOfPropertyChange(() => CanGetObjectDetailsWithMaxDataRows);
            NotifyOfPropertyChange(() => CanGetObjectDetailsWithRequestLatest);
            NotifyOfPropertyChange(() => CanGetObjectDetailsWithAllOptions);
            NotifyOfPropertyChange(() => ObjectDetailsWithMaxDataRowsTooltip);
            NotifyOfPropertyChange(() => ObjectDetailsWithRequestLatestTooltip);
            NotifyOfPropertyChange(() => ObjectDetailsWithAllOptionsTooltip);
        }

        /// <summary>
        /// Gets the selected item's details using a GetFromStore request.
        /// </summary>
        /// <param name="optionIn"></param>
        public void GetObjectDetails(params OptionsIn[] optionIn)
        {
            var resource = Items.FindSelected(_lock);
            var uri = new EtpUri(resource.Resource.Uri);

            // For 131 always perform requested for details
            optionIn = uri.Version.Equals(OptionsIn.DataVersion.Version131.Value)
                ? new OptionsIn[] { OptionsIn.ReturnElements.Requested }
                : optionIn;

            Runtime.ShowBusy();

            Task.Run(() =>
            {
                try
                {
                    Context.GetObjectDetails(uri.ObjectType, uri, optionIn);
                }
                finally
                {
                    Runtime.ShowBusy(false);
                }
            });
        }

        /// <summary>
        /// Gets the selected item's details using a GetFromStore request.
        /// </summary>
        public void GetObjectDetailsWithReturnElementsAll()
        {
            GetObjectDetails(OptionsIn.ReturnElements.All);
        }

        /// <summary>
        /// Gets the selected item's details using a GetFromStore request.
        /// </summary>
        public void GetObjectDetailsWithMaxDataRows()
        {
            if (CanGetObjectHeader && MaxDataRows.HasValue)
                GetObjectDetails(OptionsIn.ReturnElements.All, OptionsIn.MaxReturnNodes.Eq(MaxDataRows.Value));
        }

        /// <summary>
        /// Gets the selected item's details using a GetFromStore request.
        /// </summary>
        public void GetObjectDetailsWithRequestLatest()
        {
            if (CanGetObjectHeader && RequestLatestValues.HasValue)
                GetObjectDetails(OptionsIn.ReturnElements.All, OptionsIn.RequestLatestValues.Eq(RequestLatestValues.Value));
        }


        /// <summary>
        /// Gets the selected item's details using a GetFromStore request.
        /// </summary>
        public void GetObjectDetailsWithAllOptions()
        {
            if (CanGetObjectHeader)
            {
                var optionsIn = new List<OptionsIn> {OptionsIn.ReturnElements.All};
                if (MaxDataRows.HasValue)
                    optionsIn.Add(OptionsIn.MaxReturnNodes.Eq(MaxDataRows.Value));
                if (RequestLatestValues.HasValue)
                    optionsIn.Add(OptionsIn.RequestLatestValues.Eq(RequestLatestValues.Value));

                GetObjectDetails(optionsIn.ToArray());
            }
        }

        /// <summary>
        /// Gets the selected item's details using a GetFromStore request.
        /// </summary>
        public void GetObjectDetailsWithExtraOptionsIn()
        {
            if (CanGetObjectHeader && !string.IsNullOrWhiteSpace(ExtraOptionsIn))
            {
                var optionsIn = new List<OptionsIn> {OptionsIn.ReturnElements.All};
                var extraOptions = OptionsIn.Parse(ExtraOptionsIn);
                foreach (var extraOptionsKey in extraOptions.Keys)
                {
                    try
                    {
                        optionsIn.Add(new OptionsIn(extraOptionsKey, extraOptions[extraOptionsKey]));
                    }
                    catch
                    {
                        //ignore if invalid optionsIn pair
                    }
                }
                GetObjectDetails(optionsIn.ToArray());
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether this selected node can be refreshed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can be refreshed; otherwise, <c>false</c>.
        /// </value>
        public bool CanRefreshSelected
        {
            get { return CanGetObjectIds; }
        }

        /// <summary>
        /// Refreshes the selected item.
        /// </summary>
        public void RefreshSelected()
        {
            var resource = Items.FindSelected(_lock);
            // Return if there is nothing currently selected
            if (resource == null) return;

            resource.ClearAndLoadChildren();
            // Expand the node if it wasn't previously
            resource.IsExpanded = true;
        }

        /// <summary>
        /// Refreshes the context menu.
        /// </summary>
        public void RefreshContextMenu()
        {
            NotifyOfPropertyChange(() => CanGetObjectIds);
            NotifyOfPropertyChange(() => CanGetObjectHeader);
            NotifyOfPropertyChange(() => CanGetObjectDetails);
            NotifyOfPropertyChange(() => CanGetObjectDetailsWithReturnElementsAll);
            NotifyOfPropertyChange(() => CanGetObjectDetailsWithMaxDataRows);
            NotifyOfPropertyChange(() => CanGetObjectDetailsWithRequestLatest);
            NotifyOfPropertyChange(() => CanGetObjectDetailsWithExtraOptionsIn);
            NotifyOfPropertyChange(() => CanGetObjectDetailsWithAllOptions);
            NotifyOfPropertyChange(() => CanRefreshSelected);
            //NotifyOfPropertyChange(() => CanDeleteObject);
            OnRefreshContextMenu?.Invoke();
        }
        
        /// <summary>
        /// Sets the context menu using the supplied user control.
        /// </summary>
        /// <param name="control">The control.</param>
        public void SetContextMenu(FrameworkElement control)
        {
            if (_hierarchy == null || control?.ContextMenu == null) return;
            _hierarchy.ContextMenu = control.ContextMenu;
            _hierarchy.ContextMenu.DataContext = control.DataContext;
            control.ContextMenu = null;
        }

        #endregion

        /// <summary>
        /// Creates a WITSML proxy for the specified version.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="version">The WITSML version.</param>
        public void CreateContext(Connection connection, WMLSVersion version)
        {
            Context = new WitsmlQueryContext(connection.CreateProxy(version), version);
        }

        /// <summary>
        /// Clears data from the view.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                if (_cleared) return;

                _cleared = true;

                Items.Clear();

                _activeWellbores.Clear();
                _growingObjects.Clear();

                UpdateRigsMonitor();

                if (Loading)
                {
                    Loading = false;
                    _tokenSource.Cancel();
                    _tokenSource.Dispose();
                    _tokenSource = null;
                }
            }
        }

        /// <summary>
        /// Called when the parent view is ready.
        /// </summary>
        public void OnViewReady()
        {
            lock (_lock)
            {
                if (!Items.Any() && Context != null)
                    LoadWells();
            }
        }

        /// <summary>
        /// Clears and sets the data objects.
        /// </summary>
        public void SetDataObjects(IEnumerable<string> dataObjects)
        {
            DataObjects.Clear();
            DataObjects.AddRange(dataObjects);
        }

        /// <summary>
        /// Refreshes the hierarchy.
        /// </summary>
        public void RefreshHierarchy()
        {
            Clear();
            LoadWells();
        }

        /// <summary>
        /// Called when the editor gets focus.
        /// </summary>
        /// <param name="control">The control.</param>
        public void OnEditorFocus(TextBox control)
        {
            control?.SelectAll();
        }

        /// <summary>
        /// Called when we want to ignore mouse button clicks.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        public void OnIgnoreMouseButton(TextBox control, MouseButtonEventArgs e)
        {
            if (control != null && !control.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                control.Focus();
            }
        }

        /// <summary>
        /// Called when an attached view's Loaded event fires.
        /// </summary>
        /// <param name="view">The view.</param>
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            var control = view as UserControl;
            _hierarchy = control?.FindName("Hierarchy") as FrameworkElement;
        }

        /// <summary>
        /// Called when deactivating.
        /// </summary>
        /// <param name="close">Inidicates whether this instance will be closed.</param>
        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                Dispose(true);
            }

            base.OnDeactivate(close);
        }

        /// <summary>
        /// Updates the tree view when the context changes.
        /// </summary>
        private void UpdateFromContext()
        {
            Clear();
            UpdateRigsMonitor();
        }

        /// <summary>
        /// Updates the rigs monitor when needed.
        /// </summary>
        private void UpdateRigsMonitor()
        {
            lock (_lock)
            {
                if (Context == null || _cleared)
                    RigsMonitor = null;
                else if (Loading)
                    RigsMonitor = new RigsMonitor(Runtime, Context);
            }
        }

        private void OnRigsMonitorRigsChanged(object sender, EventArgs e)
        {
            UpdateFromRigsMonitor();
        }

        /// <summary>
        /// Updates the tree view when the rigs monitor object has changed.
        /// </summary>
        private void UpdateFromRigsMonitor()
        {
            lock (_lock)
            {
                RigNames.Clear();

                if (RigsMonitor != null)
                    RigNames.AddRange(RigsMonitor.RigNames);

                NotifyOfPropertyChange(() => RigNames);
            }
        }

        /// <summary>
        /// Updates the well visibility.
        /// </summary>
        private void UpdateWellVisibility()
        {
            lock (_lock)
            {
                var pattern = WellName ?? string.Empty;

                // Treat well name patterns like /pattern/ as regular expressions but other patterns as literal strings
                if (pattern.StartsWith("/") && pattern.EndsWith("/") && pattern.Length >= 2)
                    pattern = pattern.Trim('/');
                else
                    pattern = Regex.Escape(pattern);

                HashSet<string> wellUids = null;
                if (RigsMonitor != null)
                    wellUids = RigsMonitor.GetWellUids(SelectedRigName);

                Items.ForEach(x =>
                {
                    var active = !ShowOnlyActiveWells || x.IsActiveOrGrowing;
                    var matchesWell = Regex.IsMatch(x.Resource.Name, pattern, RegexOptions.IgnoreCase);

                    var matchesRig = true;
                    var dataObject = x.GetDataObject();
                    if (wellUids != null && dataObject != null)
                        matchesRig = wellUids.Contains(dataObject.Uid);

                    x.IsVisible = active && matchesWell && matchesRig;
                });
            }
        }

        private void LoadWells()
        {
            lock (_lock)
            {
                if (Loading)
                {
                    _tokenSource.Cancel();
                    _tokenSource.Dispose();
                }

                Loading = true;
                _cleared = false;
                _tokenSource = new CancellationTokenSource();
                var token = _tokenSource.Token;

                Runtime.ShowBusy();

                Task.Run(async () =>
                {
                    await LoadWellCore(token);
                    Runtime.ShowBusy(false);
                });
            }
        }

        private async Task LoadWellCore(CancellationToken token)
        {
            IEnumerable<IWellObject> wellbores = null;
            IEnumerable<IWellboreObject> mudLogs = null;
            IEnumerable<IWellboreObject> trajectories = null;
            IEnumerable<IWellboreObject> logs = null;
            IEnumerable<IDataObject> wells = null;

            var wellsTask = Task.Run(() => wells = Context.GetAllWells().ToList());
            var wellboresTask = Task.Run(() => wellbores = Context.GetActiveWellbores(EtpUri.RootUri).ToList());
            var mudLogsTask = Task.Run(() => mudLogs =
                Context.GetGrowingObjects(ObjectTypes.MudLog, EtpUri.RootUri).ToList());
            var trajectoryTask = Task.Run(() => trajectories =
                Context.GetGrowingObjects(ObjectTypes.Trajectory, EtpUri.RootUri).ToList());
            var logsTask = Task.Run(() => logs = Context.GetGrowingObjects(ObjectTypes.Log, EtpUri.RootUri)
                .ToList());

            UpdateRigsMonitor();

            await Task.WhenAll(wellsTask, wellboresTask, mudLogsTask, trajectoryTask, logsTask);

            lock (_lock)
            {
                if (token.IsCancellationRequested)
                    return;

                _activeWellbores.UnionWith(wellbores.Select(x => x.GetUri()));
                _growingObjects.UnionWith(mudLogs.Select(x => x.GetUri()));
                _growingObjects.UnionWith(trajectories.Select(x => x.GetUri()));
                _growingObjects.UnionWith(logs.Select(x => x.GetUri()));
            }

            lock (_loadLock)
            {
                lock (_lock)
                {
                    if (token.IsCancellationRequested)
                        return;
                }

                LoadDataItems(null, wells, Items, LoadWellbores, x => x.GetUri());

                // Apply well name filter
                UpdateWellVisibility();

                lock (_lock)
                {
                    Loading = false;
                    _tokenSource?.Dispose();
                    _tokenSource = null;
                }
            }
        }

        private void UpdateResourceViewModelIndicators(IEnumerable<ResourceViewModel> resourceViewModels)
        {
            foreach (var resourceViewModel in resourceViewModels)
            {
                UpdateResourceViewModelIndicators(resourceViewModel.Children);

                UpdateResourceViewModelIndicator(resourceViewModel);
            }
        }

        private void UpdateResourceViewModelIndicator(ResourceViewModel resourceViewModel)
        {
            if (string.IsNullOrEmpty(resourceViewModel.Resource.Uri))
                return;

            var uri = new EtpUri(resourceViewModel.Resource.Uri);

            if (ObjectTypes.Well.EqualsIgnoreCase(uri.ObjectType))
            {
                UpdateWellIndicator(resourceViewModel);
            }
            else if (ObjectTypes.Wellbore.EqualsIgnoreCase(uri.ObjectType))
            {
                UpdateWellboreIndicator(resourceViewModel);
            }
            else if (ObjectTypes.IsGrowingDataObject(uri.ObjectType))
            {
                UpdateGrowingObjectIndicator(resourceViewModel);
            }
        }

        private void LoadWellbores(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            Task.Run(() =>
            {
                var wellbores = Context.GetWellbores(new EtpUri(uri));
                LoadDataItems(parent, wellbores, parent.Children, LoadWellboreFolders, x => x.GetUri());
                Runtime.ShowBusy(false);
            });
        }

        private void LoadWellboreFolders(ResourceViewModel parent, string uri)
        {
            var etpUri = new EtpUri(uri);

            DataObjects
                .Select(x => ToResourceViewModel(parent, etpUri.Append(x), x, LoadWellboreObjects))
                .ForEach(parent.Children.Add);
        }

        private void LoadWellboreObjects(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            Task.Run(() =>
            {
                var etpUri = new EtpUri(uri);

                if (ObjectTypes.Log.EqualsIgnoreCase(etpUri.ObjectType))
                {
                    var logFolders = new Dictionary<string, string>
                    {
                        { ObjectFolders.Time, Witsml141.ReferenceData.LogIndexType.datetime.ToString() },
                        { ObjectFolders.Depth, Witsml141.ReferenceData.LogIndexType.measureddepth.ToString() },
                        { ObjectFolders.All, ObjectFolders.All }
                    };

                    logFolders
                        .Select(x => ToResourceViewModel(parent, etpUri.Append(x.Value), x.Key, LoadLogObjects))
                        .ForEach(parent.Children.Add);
                }
                else
                {
                    var dataObjects = ObjectTypes.IsGrowingDataObject(etpUri.ObjectType)
                        ? Context.GetGrowingObjectsWithStatus(etpUri.ObjectType, etpUri)
                        : Context.GetWellboreObjects(etpUri.ObjectType, etpUri);

                    LoadDataItems(parent, dataObjects, parent.Children, LoadGrowingObjectChildren, x => x.GetUri(), 0);
                }

                Runtime.ShowBusy(false);
            });
        }

        private void LoadLogObjects(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            Task.Run(() =>
            {
                var etpUri = new EtpUri(uri);
                var indexType = ObjectFolders.All.EqualsIgnoreCase(etpUri.ObjectType) ? null : etpUri.ObjectType;
                var dataObjects = Context.GetGrowingObjectsWithStatus(ObjectTypes.Log, etpUri.Parent, indexType);

                LoadDataItems(parent, dataObjects, parent.Children, LoadGrowingObjectChildren, x => x.GetUri());

                Runtime.ShowBusy(false);
            });
        }

        private void LoadGrowingObjectChildren(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            Task.Run(async () =>
            {
                var etpUri = new EtpUri(uri);
                var dataObject = Context.GetGrowingObjectHeaderOnly(etpUri.ObjectType, etpUri);

                if (ObjectTypes.Log.EqualsIgnoreCase(etpUri.ObjectType))
                    LoadLogCurveInfo(parent, parent.Children, dataObject);

                await Task.Yield();
                Runtime.ShowBusy(false);
            });
        }

        private void LoadLogCurveInfo(ResourceViewModel parent, IList<ResourceViewModel> items, IWellboreObject dataObject)
        {
            var log131 = dataObject as Witsml131.Log;
            var log141 = dataObject as Witsml141.Log;

            log131?.LogCurveInfo
                .Select(x => ToResourceViewModel(parent, x.GetUri(log131), x.Mnemonic, null, 0, new DataObjectWrapper(x)))
                .ForEach(items.Add);

            log141?.LogCurveInfo
                .Select(x => ToResourceViewModel(parent, x.GetUri(log141), x.Mnemonic.Value, null, 0, new DataObjectWrapper(x)))
                .ForEach(items.Add);
        }

        private void LoadDataItems<T>(
            ResourceViewModel parent,
            IEnumerable<T> dataObjects,
            IList<ResourceViewModel> items,
            Action<ResourceViewModel, string> action,
            Func<T, EtpUri> getUri,
            int children = -1)
            where T : IDataObject
        {
            Runtime.Invoke(() =>
            {
                dataObjects
                    .Select(x => ToResourceViewModel(parent, x, action, getUri, children))
                    .ForEach(items.Add);
            });
        }

        private ResourceViewModel ToResourceViewModel<T>(ResourceViewModel parent, T dataObject, Action<ResourceViewModel, string> action, Func<T, EtpUri> getUri, int children = -1) where T : IDataObject
        {
            var uri = getUri(dataObject);

            var resourceViewModel = ToResourceViewModel(parent, uri, dataObject.Name, action, children, new DataObjectWrapper(dataObject));

            UpdateResourceViewModelIndicator(resourceViewModel);

            return resourceViewModel;
        }

        private void UpdateWellboreIndicator(ResourceViewModel wellboreVM)
        {
            var wellbore = wellboreVM.GetWellObject();

            bool growing;
            var active = wellbore.GetWellboreStatus();
            lock (_lock)
            {
                growing = _growingObjects.Any(o => o.Parent == wellboreVM.Resource.Uri);
            }

            wellboreVM.IsGrowing = growing;
            wellboreVM.IsActive = active;

            if (active != null)
                UpdateWellboreParents(wellboreVM, wellbore, active.Value);
        }

        private void UpdateWellboreParents(ResourceViewModel wellboreVM, IWellObject wellbore, bool active)
        {
            var wellboreUri = wellbore.GetUri();
            var updateWell = false;

            lock (_lock)
            {
                if (active && !_activeWellbores.Contains(wellboreUri))
                {
                    _activeWellbores.Add(wellboreUri);
                    updateWell = true;
                }
                else if (!active && _activeWellbores.Contains(wellboreUri))
                {
                    _activeWellbores.Remove(wellboreUri);
                    updateWell = true;
                }
            }

            if (updateWell)
            {
                if (wellboreVM.Parent != null)
                    UpdateWellIndicator(wellboreVM.Parent);
            }

        }
        private void UpdateGrowingObjectIndicator(ResourceViewModel growingObjectVM)
        {
            var growingObject = growingObjectVM.GetWellboreObject();

            var growing = growingObject.GetObjectGrowingStatus();
            var empty = growingObject.IsGrowingObjectEmpty();
            if (growing == null && empty == null)
                return;

            growingObjectVM.IsGrowing = growing.GetValueOrDefault();
            growingObjectVM.IsEmpty = empty.GetValueOrDefault();

            if (growing != null)
                UpdateGrowingObjectParents(growingObjectVM, growingObject, growing.Value);

        }

        private void UpdateGrowingObjectParents(ResourceViewModel growingObjectVM, IWellboreObject growingObject, bool growing)
        {
            var growingObjectUri = growingObject.GetUri();
            var updateParents = false;

            lock (_lock)
            {
                if (growing && !_growingObjects.Contains(growingObjectUri))
                {
                    _growingObjects.Add(growingObjectUri);
                    updateParents = true;
                }
                else if (!growing && _growingObjects.Contains(growingObjectUri))
                {
                    _growingObjects.Remove(growingObjectUri);
                    updateParents = true;
                }
            }

            if (updateParents)
            {
                var wellboreVM = growingObjectVM.Parent;
                while (wellboreVM != null && wellboreVM.GetWellObject() == null && wellboreVM.Parent != null)
                    wellboreVM = wellboreVM.Parent;

                if (wellboreVM != null)
                {
                    UpdateWellboreIndicator(wellboreVM);

                    if (wellboreVM.Parent != null)
                        UpdateWellIndicator(wellboreVM.Parent);
                }
            }
        }

        private void UpdateWellIndicator(ResourceViewModel wellVM)
        {
            lock (_lock)
            {
                var growing = _growingObjects.Any(o => o.Parent.Parent == wellVM.Resource.Uri);
                var active = _activeWellbores.Any(o => o.Parent == wellVM.Resource.Uri);

                wellVM.IsGrowing = growing;
                wellVM.IsActive = active;
            }
        }

        private ResourceViewModel ToResourceViewModel(ResourceViewModel parent, EtpUri uri, string name, Action<ResourceViewModel, string> action, int children = -1, object dataContext = null)
        {
            var resource = new Resource
            {
                Uri = uri,
                Name = name,
                ContentType = uri.ContentType,
                HasChildren = children
            };

            var viewModel = new ResourceViewModel(Runtime, resource, dataContext) {Parent = parent};

            if (children != 0 && action != null)
            {
                viewModel.LoadChildren = x =>
                {
                    action(viewModel, x);
                    return _messageId++;
                };
            }

            return viewModel;
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    lock (_lock)
                    {
                        if (Loading)
                            _tokenSource.Cancel();

                        if (_tokenSource != null)
                            _tokenSource.Dispose();

                        _tokenSource = null;
                    }
                }

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
