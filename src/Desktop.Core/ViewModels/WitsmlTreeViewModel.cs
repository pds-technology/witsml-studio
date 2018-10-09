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
using Action = System.Action;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
using IDataObject = Energistics.DataAccess.IDataObject;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Energistics.Etp.Common.Datatypes;
using Microsoft.Win32;
using PDS.WITSMLstudio.Adapters;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Linq;
using PDS.WITSMLstudio.Query;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// Manages the display and interaction of the WITSML hierarchy view.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class WitsmlTreeViewModel : Screen, IDisposable
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(WitsmlTreeViewModel));
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

            BindingOperations.EnableCollectionSynchronization(Items, null, ReaderWriterLockManager.SynchronizeReadWriteAccess);
            BindingOperations.EnableCollectionSynchronization(DataObjects, null, ReaderWriterLockManager.SynchronizeReadWriteAccess);
            BindingOperations.EnableCollectionSynchronization(RigNames, null, ReaderWriterLockManager.SynchronizeReadWriteAccess);
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
        /// Gets or sets a value indicating whether to the show property grid.
        /// </summary>
        public bool ShowPropertyGrid { get; set; } = true;

        /// <summary>
        /// Gets or sets the height of the property grid.
        /// </summary>
        public int PropertyGridHeight { get; set; } = 200;

        /// <summary>
        /// Gets or sets an action to execute when the context menu is refreshed.
        /// </summary>
        public System.Action OnRefreshContextMenu { get; set; }

        /// <summary>
        /// Gets or sets the delegate to invoke when the selected item is changed.
        /// </summary>
        public Action<ResourceViewModel> OnSelectedItemChanged { get; set; }

        /// <summary>
        /// Gets or sets the on load children completed action.
        /// </summary>
        public Func<ResourceViewModel, IList<ResourceViewModel>, Task> OnLoadChildrenCompleted { get; set; }

        /// <summary>
        /// Gets an <see cref="EtpUri"/> for the selected URI.
        /// </summary>
        public EtpUri SelectedEtpUri { get; private set; }

        private string _selectedUri;

        /// <summary>
        /// Gets or sets the selected URI.
        /// </summary>
        /// <value>The selected URI.</value>
        public string SelectedUri
        {
            get { return _selectedUri; }
            set
            {
                if (value == _selectedUri) return;
                _selectedUri = value;
                SelectedEtpUri = new EtpUri(SelectedUri);
                NotifyOfPropertyChange(() => SelectedUri);
            }
        }

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

        private bool _canFilter = true;

        /// <summary>
        /// Gets a value indicating whether this instance can filter by well name.
        /// </summary>
        public bool CanFilter
        {
            get { return _canFilter && !Loading; }
            set
            {
                if (_canFilter == value) return;

                _canFilter = value;
                NotifyOfPropertyChange(() => CanFilter);
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
                NotifyOfPropertyChange(() => CanFilter);
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

        private bool _canUseActiveWellsFilter;
        /// <summary>
        /// Gets a value indicating whether this instance can use active wells filter.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can use active wells filter; otherwise, <c>false</c>.
        /// </value>
        public bool CanUseActiveWellsFilter
        {
            get { return _canUseActiveWellsFilter; }
            set
            {
                if (_canUseActiveWellsFilter == value) return;
                _canUseActiveWellsFilter = value;
                NotifyOfPropertyChange(() => CanUseActiveWellsFilter);
            }
        }

        private bool _canUseRigFilter;
        /// <summary>
        /// Gets or sets a value indicating whether this instance can use rig filter.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can use rig filter; otherwise, <c>false</c>.
        /// </value>
        public bool CanUseRigFilter
        {
            get { return _canUseRigFilter; }
            set
            {
                if (_canUseRigFilter == value) return;
                _canUseRigFilter = value;
                NotifyOfPropertyChange(() => CanUseRigFilter);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can clear rig name.
        /// </summary>
        /// <value><c>true</c> if this instance can clear rig name; otherwise, <c>false</c>.</value>
        public bool CanClearSelectedRigName => !string.IsNullOrEmpty(SelectedRigName);

        /// <summary>
        /// Gets or sets a value indicating whether indicator queries should be disabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if indicator queries are disabled; otherwise, <c>false</c>.
        /// </value>
        public bool DisableIndicatorQueries { get; set; }

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

        private bool _showOnlyRefreshMenuOptions;

        /// <summary>
        /// Gets or sets a value indicating whether context menu displays refresh menu options only.
        /// </summary>
        public bool ShowOnlyRefreshMenuOptions
        {
            get { return _showOnlyRefreshMenuOptions; }
            set
            {
                if (value == _showOnlyRefreshMenuOptions) return;
                _showOnlyRefreshMenuOptions = value;
                NotifyOfPropertyChange(() => ShowOnlyRefreshMenuOptions);
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
                var resource = Items.FindSelectedSynchronized();
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
            var resource = Items.FindSelectedSynchronized();
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
                var resource = Items.FindSelectedSynchronized();
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
            var resource = Items.FindSelectedSynchronized();
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

                var resource = Items.FindSelectedSynchronized();
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
        public Task<IDataObject> GetObjectDetails(params OptionsIn[] optionIn)
        {
            var resource = Items.FindSelectedSynchronized();
            var uri = new EtpUri(resource.Resource.Uri);

            // For 131 always perform requested for details
            optionIn = uri.Version.Equals(OptionsIn.DataVersion.Version131.Value)
                ? new OptionsIn[] { OptionsIn.ReturnElements.Requested }
                : optionIn;

            Runtime.ShowBusy();

            return Task.Run(() =>
            {
                try
                {
                    return Context.GetObjectDetails(uri.ObjectType, uri, optionIn);
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
        public Task<IDataObject> GetObjectDetailsWithReturnElementsAll()
        {
            return GetObjectDetails(OptionsIn.ReturnElements.All);
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
                var optionsIn = new List<OptionsIn> { OptionsIn.ReturnElements.All };
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
                var optionsIn = new List<OptionsIn> { OptionsIn.ReturnElements.All };
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
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetAttachments
        {
            get
            {
                if (!CanGetObjectIds)
                    return false;

                var resource = Items.FindSelectedSynchronized();
                var uri = new EtpUri(resource.Resource.Uri);

                return uri.Version.Equals(OptionsIn.DataVersion.Version141.Value);
            }
        }

        /// <summary>
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetAttachment
        {
            get
            {
                if (!CanGetAttachments)
                    return false;

                var resource = Items.FindSelectedSynchronized();
                var uri = new EtpUri(resource.Resource.Uri);

                return uri.ObjectType.EqualsIgnoreCase(ObjectTypes.Attachment)
                    && !string.IsNullOrWhiteSpace(uri.ObjectId);
            }
        }

        /// <summary>
        /// Uploads the attachment.
        /// </summary>
        public void UploadAttachment()
        {
            var resource = Items.FindSelectedSynchronized();
            var uri = new EtpUri(resource.Resource.Uri);
            var ids = uri.GetObjectIdMap();

            var attachment = new Witsml141.Attachment
            {
                Uid = uri.ObjectId,
                UidWell = ids[ObjectTypes.Well],
                UidWellbore = ids[ObjectTypes.Wellbore]
            };

            // TODO: Show Upload Dialog
        }

        /// <summary>
        /// Downloads the attachment.
        /// </summary>
        public void DownloadAttachment()
        {
            GetObjectDetailsWithReturnElementsAll()
                .ContinueWith(x =>
                {
                    var attachment = x.Result as Witsml141.Attachment;
                    if (attachment == null) return;

                    var fileName = attachment.FileName ?? attachment.Name.Replace(" ", "-");
                    var fileType = attachment.FileType ?? ObjectTypes.Unknown;

                    var extension = !fileType.StartsWith(".")
                        ? MimeTypes.MimeTypeMap.GetExtension(fileType, false)
                        : fileType;

                    Runtime.InvokeAsync(() =>
                    {
                        var dialog = new SaveFileDialog
                        {
                            Title = "Save Attachment...",
                            Filter = $"{fileType}|*{extension}|All Files|*.*",
                            DefaultExt = extension,
                            AddExtension = true,
                            FileName = fileName
                        };

                        if (dialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
                        {
                            File.WriteAllBytes(dialog.FileName, attachment.Content);
                        }
                    });
                });
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
            var resource = Items.FindSelectedSynchronized();
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

            NotifyOfPropertyChange(() => CanGetAttachments);
            NotifyOfPropertyChange(() => CanGetAttachment);
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
            OnRefreshContextMenu?.Invoke(); // TODO: OnRefreshContextMenu is only being used by the Excel Plugin.  
                                            // TODO:... It could be changed to use OnSelectedItemChange so this can be removed.

            if (OnSelectedItemChanged == null) return;

            var selectedResource = Items.FindSelectedSynchronized();

            OnSelectedItemChanged(selectedResource);
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

                _activeWellbores.ExecuteWithWriteLock(_activeWellbores.Clear);
                _growingObjects.ExecuteWithWriteLock(_growingObjects.Clear);

                ShowOnlyActiveWells = false;
                CanUseActiveWellsFilter = false;
                CanUseRigFilter = false;
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
                bool hasItems = Items.ExecuteWithReadLock(Items.Any);

                if (!hasItems && Context != null)
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
                else
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
            RigNames.Clear();

            if (RigsMonitor != null)
            {
                CanUseRigFilter = RigsMonitor.RigNames.Count > 0;
                RigNames.AddRange(RigsMonitor.RigNames);
            }

            NotifyOfPropertyChange(() => RigNames);
        }

        /// <summary>
        /// Updates the well visibility.
        /// </summary>
        private void UpdateWellVisibility()
        {
            HashSet<string> wellUids = null;

            var pattern = WellName ?? string.Empty;

            // Treat well name patterns like /pattern/ as regular expressions but other patterns as literal strings
            if (pattern.StartsWith("/") && pattern.EndsWith("/") && pattern.Length >= 2)
                pattern = pattern.Trim('/');
            else
                pattern = Regex.Escape(pattern);

            lock (_lock)
            {
                if (RigsMonitor != null)
                    wellUids = RigsMonitor.GetWellUids(SelectedRigName);
            }

            var access = new Action(() =>
            {
                Items.ForEach(x =>
                {
                    var active = !ShowOnlyActiveWells || x.IsActiveOrGrowing;
                    var matchesWell = Regex.IsMatch(x.Resource.Name, pattern, RegexOptions.IgnoreCase);

                    var matchesRig = true;
                    var dataObject = x.GetDataObject();
                    if (wellUids != null && dataObject != null)
                        matchesRig = wellUids.Contains(dataObject.Uid);

                    x.IsVisible = active && matchesWell && matchesRig;

                    if (x.IsVisible) return;

                    x.IsSelected = false;
                });
            });
            Items.ExecuteWithReadLock(access);

            UnselectWellbores();
            UnselectDataObjects();
        }

        private void UnselectWellbores()
        {
            var access = new Action(() =>
            {
                var selectedWellbores =
                    Items
                        .Where(well => !well.IsVisible)
                        .SelectMany(well => well.Children)
                        .Where(wellbore => wellbore.IsSelected);

                selectedWellbores.ForEach(wellbore => wellbore.IsSelected = false);
            });
            Items.ExecuteWithReadLock(access);
        }

        private void UnselectDataObjects()
        {
            var access = new Action(() =>
            {
                var dataTypeFolders =
                    Items
                        .Where(well => !well.IsVisible) // Non-Visible Wells
                        .SelectMany(well => well.Children) // Wellbores
                        .SelectMany(wellbores => wellbores.Children)
                        .ToArray(); // datatype folders

                var selectedNonLogDataTypes =
                    dataTypeFolders
                        .Where(folder => !ObjectTypes.Log.Equals(folder.DisplayName)) // Non-Log datatype folders
                        .SelectMany(folder => folder.Children) // data objects
                        .Where(dataObjects => dataObjects.IsSelected) // Selected data objects
                        .ForEach(dataObject => dataObject.IsSelected = false);

                // Unselect selected non-log data objects
                selectedNonLogDataTypes.ForEach(dataObject => dataObject.IsSelected = false);

                var logs =
                    dataTypeFolders
                        .Where(folder => ObjectTypes.Log.Equals(folder.DisplayName)) // Non-Log datatype folders
                        .SelectMany(logFolder => logFolder.Children) // log sub-folders
                        .SelectMany(subFolder => subFolder.Children)
                        .ToArray(); // Logs

                // Unselected, selected logs
                logs
                    .Where(log => log.IsSelected) // Selected logs
                    .ForEach(dataObject => dataObject.IsSelected = false);

                // Unselect selected mnemonics
                logs
                    .SelectMany(log => log.Children)
                    .Where(mnemonic => mnemonic.IsSelected)
                    .ForEach(channel => channel.IsSelected = false);
            });
            Items.ExecuteWithReadLock(access);
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
                }, token)
                .ContinueWith(prevTask =>
                {
                    // Find active and growing objects
                    Task.Run(() => GetActiveAndGrowingObjects(), token);
                }, token);
            }
        }

        private async Task LoadWellCore(CancellationToken token)
        {
            IEnumerable<IDataObject> wells = null;
            await Task.Run(() => wells = Context.GetAllWells().ToList(), token);

            lock (_loadLock)
            {
                lock (_lock)
                {
                    if (token.IsCancellationRequested)
                        return;
                }

                LoadDataItems(null, wells, Items, LoadWellbores, x => x.GetUri());

                lock (_lock)
                {
                    Loading = false;
                }

                // Apply well name filter
                UpdateWellVisibility();
            }
        }

        private async void GetActiveAndGrowingObjects()
        {
            // if DisableIndicatorQueries flag is true exit without searching for active and growing objects.
            if (DisableIndicatorQueries) return;

            // Get the server capabilities
            var suppportedObjects = Context.GetSupportedGetFromStoreObjects();

            // If the server doesn't support wellbores do not search for any data object
            if (!suppportedObjects.Any() || !suppportedObjects.ContainsIgnoreCase(ObjectTypes.Wellbore))
                return;

            // Run async query for rigs
            if (suppportedObjects.ContainsIgnoreCase(ObjectTypes.Rig))
            {
                UpdateRigsMonitor();
            }

            List<IWellObject> wellbores = null;
            await Task.Run(() =>
            {
                try
                {
                    wellbores = Context.GetActiveWellbores(EtpUri.RootUri, false).ToList();
                }
                catch (Exception)
                {
                    // ignored
                }
            });

            // If query was unable to find wellbores do not atempt data objects
            if (wellbores == null)
                return;

            // Do not trust server to filter out non growing objects
            wellbores.RemoveAll(x => !x.GetObjectGrowingStatus().GetValueOrDefault());

            var access = new Action(() =>
            {
                _activeWellbores.UnionWith(wellbores.Select(x => x.GetUri()));
            });
            _activeWellbores.ExecuteWithWriteLock(access);

            // Update the indicator for active wellbores
            if (_activeWellbores.Count > 0)
                UpdateResourceViewModelIndicators();

            // Only get objects that are supported by the server
            if (suppportedObjects.ContainsIgnoreCase(ObjectTypes.Log))
            {
                if (!await GetGrowingLogs())
                    return;
            }
            if (suppportedObjects.ContainsIgnoreCase(ObjectTypes.MudLog))
            {
                if (!await GetGrowingMudLogs())
                    return;
            }
            if (suppportedObjects.ContainsIgnoreCase(ObjectTypes.Trajectory))
            {
                if (!await GetGrowingTrajectories())
                    return;
            }

            // Update the indicator for wells and wellbores that have growing data objects
            if (_growingObjects.Count > 0)
                UpdateResourceViewModelIndicators();
        }

        private void UpdateResourceViewModelIndicators()
        {
            CanUseActiveWellsFilter = true;
            Runtime.Invoke(() =>
            {
                var access = new Action(() =>
                {
                    Items.ForEach(UpdateResourceViewModelIndicator);
                });
                Items.ExecuteWithReadLock(access);
            });
        }

        private async Task<bool> GetGrowingLogs()
        {
            List<IWellboreObject> logs = null;
            await Task.Run(() =>
            {
                try
                {
                    logs = Context.GetGrowingObjects(ObjectTypes.Log, EtpUri.RootUri, false).ToList();
                }
                catch (Exception)
                {
                    // ignored
                }
            });

            if (logs == null)
                return false;

            RemoveNonGrowingObjects(logs);

            var access = new Action(() =>
            {
                _growingObjects.UnionWith(logs.Select(x => x.GetUri()));
            });
            _growingObjects.ExecuteWithWriteLock(access);

            return true;
        }

        private async Task<bool> GetGrowingMudLogs()
        {
            List<IWellboreObject> mudLogs = null;
            await Task.Run(() =>
            {
                try
                {
                    mudLogs = Context.GetGrowingObjects(ObjectTypes.MudLog, EtpUri.RootUri, false).ToList();
                }
                catch (Exception)
                {
                    // ignored
                }
            });

            if (mudLogs == null)
                return false;

            RemoveNonGrowingObjects(mudLogs);

            var access = new Action(() =>
            {
                _growingObjects.UnionWith(mudLogs.Select(x => x.GetUri()));
            });
            _growingObjects.ExecuteWithWriteLock(access);

            return true;
        }

        private async Task<bool> GetGrowingTrajectories()
        {
            List<IWellboreObject> trajectories = null;
            await Task.Run(() =>
            {
                try
                {
                    trajectories = Context.GetGrowingObjects(ObjectTypes.Trajectory, EtpUri.RootUri, false).ToList();
                }
                catch (Exception)
                {
                    // ignored
                }
            });

            if (trajectories == null)
                return false;

            RemoveNonGrowingObjects(trajectories);

            var access = new Action(() =>
            {
                _growingObjects.UnionWith(trajectories.Select(x => x.GetUri()));
            });
            _growingObjects.ExecuteWithWriteLock(access);

            return true;
        }

        private static void RemoveNonGrowingObjects(List<IWellboreObject> logs)
        {
            // Do not trust server to filter out non growing objects
            logs.RemoveAll(x => !x.GetObjectGrowingStatus().GetValueOrDefault());
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

        private Task LoadWellbores(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            return Task.Run(() =>
            {
                var wellbores = Context.GetWellbores(new EtpUri(uri));
                LoadDataItems(parent, wellbores, parent.Children, LoadWellboreFolders, x => x.GetUri());
                Runtime.ShowBusy(false);
            });
        }

        private Task LoadWellboreFolders(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            return Task.Run(() =>
            {
                var etpUri = new EtpUri(uri);

                var access = new Action(() =>
                {
                    DataObjects
                        .Select(x => ToResourceViewModel(parent, etpUri.Append(x), x, LoadWellboreObjects))
                        .ForEach(parent.Children.Add);
                });
                DataObjects.ExecuteWithReadLock(access);

                Runtime.ShowBusy(false);
            });
        }

        private Task LoadWellboreObjects(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            return Task.Run(() =>
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
                    try
                    {
                        // Handle exception when the expanded object type is not supported 
                        // So that the application does not crash
                        var dataObjects = ObjectTypes.IsGrowingDataObject(etpUri.ObjectType)
                            ? Context.GetGrowingObjectsWithStatus(etpUri.ObjectType, etpUri)
                            : Context.GetWellboreObjects(etpUri.ObjectType, etpUri);
                        LoadDataItems(parent, dataObjects, parent.Children, LoadGrowingObjectChildren, x => x.GetUri(), 0);
                    }
                    catch (InvalidCastException ice)
                    {
                        // Do nothing
                        _log.Error(ice);
                    }
                    catch (ArgumentNullException ane)
                    {
                        // Do nothing
                        _log.Error(ane);
                    }
                }

                Runtime.ShowBusy(false);
            });
        }

        private Task LoadLogObjects(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            return Task.Run(() =>
            {
                var etpUri = new EtpUri(uri);
                var indexType = ObjectFolders.All.EqualsIgnoreCase(etpUri.ObjectType) ? null : etpUri.ObjectType;
                var dataObjects = Context.GetGrowingObjectsWithStatus(ObjectTypes.Log, etpUri.Parent, indexType).ToList();

                if (dataObjects.Any() && !string.IsNullOrEmpty(indexType))
                    dataObjects.RemoveAll(l => !l.HasSpecifiedIndexType(indexType));

                LoadDataItems(parent, dataObjects, parent.Children, LoadGrowingObjectChildren, x => x.GetUri());

                Runtime.ShowBusy(false);
            });
        }

        private Task LoadGrowingObjectChildren(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            return Task.Run(async () =>
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
            var log = new Log(dataObject);
            var logUri = dataObject.GetUri();

            var logCurves = log.GetLogCurves();
            var indexCurve = logCurves
                .Where(x => x.Mnemonic.EqualsIgnoreCase(log.IndexCurve))
                .ToArray();

            indexCurve
                .Union(logCurves
                .Except(indexCurve)
                .OrderBy(x => x.Mnemonic))
                .Select(x => ToResourceViewModel(parent, GetLogCurveInfoUri(logUri, x), x.Mnemonic, null, 0, new DataObjectWrapper(x.WrappedLogCurveInfo)))
                .ForEach(items.Add);
        }

        private EtpUri GetLogCurveInfoUri(EtpUri logUri, LogCurveInfo logCurveInfo)
        {
            var encodedMnemonic = WebUtility.UrlEncode(logCurveInfo.Mnemonic);
            return logUri.Append(ObjectTypes.LogCurveInfo, encodedMnemonic);
        }

        private void LoadDataItems<T>(
            ResourceViewModel parent,
            IEnumerable<T> dataObjects,
            IList<ResourceViewModel> items,
            Func<ResourceViewModel, string, Task> action,
            Func<T, EtpUri> getUri,
            int children = -1)
            where T : IDataObject
        {
            Runtime.Invoke(() =>
            {
                dataObjects
                    .Select(x => ToResourceViewModel(parent, x, action, getUri, children))
                    .ForEach(items.Add);

                if (parent == null)
                {
                    OnLoadChildrenCompleted?.Invoke(null, items);
                }
            });
        }

        private ResourceViewModel ToResourceViewModel<T>(ResourceViewModel parent, T dataObject, Func<ResourceViewModel, string, Task> action, Func<T, EtpUri> getUri, int children = -1) where T : IDataObject
        {
            var uri = getUri(dataObject);

            var resourceViewModel = ToResourceViewModel(parent, uri, dataObject.Name, action, children, new DataObjectWrapper(dataObject));

            UpdateResourceViewModelIndicator(resourceViewModel);

            return resourceViewModel;
        }

        private void UpdateWellboreIndicator(ResourceViewModel wellboreVM)
        {
            var wellbore = wellboreVM.GetWellObject();

            var active = wellbore.GetWellboreStatus();
            var access = new Func<bool>(() =>
            {
                return _growingObjects.Any(o => o.Parent == wellboreVM.Resource.Uri);
            });
            bool growing = _growingObjects.ExecuteWithReadLock(access);

            wellboreVM.IsGrowing = growing;
            wellboreVM.IsActive = active;

            if (active != null)
                UpdateWellboreParents(wellboreVM, wellbore, active.Value);
        }

        private void UpdateWellboreParents(ResourceViewModel wellboreVM, IWellObject wellbore, bool active)
        {
            var wellboreUri = wellbore.GetUri();
            var updateWell = false;

            var access = new Action(() =>
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
            });
            _activeWellbores.ExecuteWithWriteLock(access);

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
            growingObjectVM.IsEmpty = empty ?? true;

            if (growing != null)
                UpdateGrowingObjectParents(growingObjectVM, growingObject, growing.Value);
        }

        private void UpdateGrowingObjectParents(ResourceViewModel growingObjectVM, IWellboreObject growingObject, bool growing)
        {
            var growingObjectUri = growingObject.GetUri();
            var updateParents = false;

            var access = new Action(() =>
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
            });
            _growingObjects.ExecuteWithWriteLock(access);

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
            var access = new Func<bool>(() =>
            {
                return _growingObjects.Any(o => o.Parent.Parent == wellVM.Resource.Uri);
            });
            wellVM.IsGrowing = _growingObjects.ExecuteWithReadLock(access);

            access = new Func<bool>(() =>
            {
                return _activeWellbores.Any(o => o.Parent == wellVM.Resource.Uri);
            });
            wellVM.IsActive = _activeWellbores.ExecuteWithReadLock(access);
        }

        private ResourceViewModel ToResourceViewModel(ResourceViewModel parent, EtpUri uri, string name, Func<ResourceViewModel, string, Task> action, int children = -1, object dataContext = null)
        {
            var resource = new Energistics.Etp.v11.Datatypes.Object.Resource
            {
                Uri = uri,
                Name = name,
                ContentType = uri.ContentType,
                HasChildren = children
            };

            var viewModel = new ResourceViewModel(Runtime, resource, dataContext) { Parent = parent };

            if (children != 0 && action != null)
            {
                viewModel.LoadChildren = async (x, y) =>
                {
                    await action(viewModel, x);

                    if (OnLoadChildrenCompleted != null)
                        await OnLoadChildrenCompleted(viewModel, viewModel.Children);

                    return _messageId++;
                };
            }

            return viewModel;
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

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

                        _tokenSource?.Dispose();
                        _tokenSource = null;

                        BindingOperations.DisableCollectionSynchronization(Items);
                        BindingOperations.DisableCollectionSynchronization(DataObjects);
                        BindingOperations.DisableCollectionSynchronization(RigNames);

                        Items.RemoveLock();
                        DataObjects.RemoveLock();
                        RigNames.RemoveLock();
                        _growingObjects.RemoveLock();
                        _activeWellbores.RemoveLock();
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
