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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    public class WitsmlTreeViewModel : Screen
    {
        private FrameworkElement _hierarchy;
        private long _messageId;

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlTreeViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public WitsmlTreeViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            Items = new BindableCollection<ResourceViewModel>();
            DataObjects = new BindableCollection<string>();
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
        /// Gets or sets an action to execute when the context menu is refreshed.
        /// </summary>
        public System.Action OnRefreshContextMenu { get; set; }

        private string _wellName;

        /// <summary>
        /// Gets or sets the name of the well.
        /// </summary>
        /// <value>The name of the well.</value>
        public string WellName
        {
            get { return _wellName; }
            set
            {
                if (string.Equals(_wellName, value))
                    return;

                _wellName = value;
                NotifyOfPropertyChange(() => WellName);
                NotifyOfPropertyChange(() => CanClearWellName);
                OnWellNameChanged();
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

        private IWitsmlContext _context;

        /// <summary>
        /// Gets or sets the WITSML context.
        /// </summary>
        /// <value>The WITSML context.</value>
        public IWitsmlContext Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(() => Context);
                }
            }
        }

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
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetObjectIds
        {
            get
            {
                var resource = Items.FindSelected();
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
            var resource = Items.FindSelected();
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
                var resource = Items.FindSelected();
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
            var resource = Items.FindSelected();
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

                var resource = Items.FindSelected();
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
            var resource = Items.FindSelected();
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
        /// Creates a WITSML proxy for the specified version.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="version">The WITSML version.</param>
        public void CreateContext(Connection connection, WMLSVersion version)
        {
            if (Context != null)
            {
                Context.LogQuery = null;
                Context.LogResponse = null;
            }

            Context = new WitsmlQueryContext(connection.CreateProxy(version), version);

            Items.Clear();
        }

        /// <summary>
        /// Called when the parent view is ready.
        /// </summary>
        public void OnViewReady()
        {
            if (!Items.Any() && Context != null)
                LoadWells();
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
            Items.Clear();
            LoadWells();
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
            var resource = Items.FindSelected();
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

        private void OnWellNameChanged()
        {
            var pattern = WellName;

            if (string.IsNullOrEmpty(pattern))
            {
                Items.ForEach(x =>
                {
                    x.IsVisible = true;
                });
            }
            else if (pattern.StartsWith("/") && pattern.EndsWith("/"))
            {
                pattern = pattern.Trim('/');

                Items.ForEach(x =>
                {
                    x.IsVisible = Regex.IsMatch(x.Resource.Name, pattern, RegexOptions.IgnoreCase);
                });
            }
            else
            {
                Items.ForEach(x =>
                {
                    x.IsVisible = x.Resource.Name.IndexOf(pattern, StringComparison.InvariantCultureIgnoreCase) > -1;
                });
            }
        }

        private void LoadWells()
        {
            Runtime.ShowBusy();

            Task.Run(async () =>
            {
                var wells = Context.GetAllWells();
                await LoadDataItems(wells, Items, LoadWellbores, x => x.GetUri());

                // Apply well name filter
                if (!string.IsNullOrWhiteSpace(WellName))
                    OnWellNameChanged();

                Runtime.ShowBusy(false);
            });
        }

        private void LoadWellbores(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            Task.Run(async () =>
            {
                var wellbores = Context.GetWellbores(new EtpUri(uri));
                await LoadDataItems(wellbores, parent.Children, LoadWellboreFolders, x => x.GetUri());
                Runtime.ShowBusy(false);
            });
        }

        private void LoadWellboreFolders(ResourceViewModel parent, string uri)
        {
            var etpUri = new EtpUri(uri);

            DataObjects
                .Select(x => ToResourceViewModel(etpUri.Append(x), x, LoadWellboreObjects))
                .ForEach(parent.Children.Add);
        }

        private void LoadWellboreObjects(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            Task.Run(async () =>
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
                        .Select(x => ToResourceViewModel(etpUri.Append(x.Value), x.Key, LoadLogObjects))
                        .ForEach(parent.Children.Add);
                }
                else
                {
                    var dataObjects = ObjectTypes.IsGrowingDataObject(etpUri.ObjectType)
                        ? Context.GetGrowingObjectsWithStatus(etpUri.ObjectType, etpUri)
                        : Context.GetWellboreObjects(etpUri.ObjectType, etpUri);

                    await LoadDataItems(dataObjects, parent.Children, LoadGrowingObjectChildren, x => x.GetUri(), 0);
                }

                Runtime.ShowBusy(false);
            });
        }

        private void LoadLogObjects(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            Task.Run(async () =>
            {
                var etpUri = new EtpUri(uri);
                var indexType = ObjectFolders.All.EqualsIgnoreCase(etpUri.ObjectType) ? null : etpUri.ObjectType;
                var dataObjects = Context.GetGrowingObjectsWithStatus(ObjectTypes.Log, etpUri.Parent, indexType);

                await LoadDataItems(dataObjects, parent.Children, LoadGrowingObjectChildren, x => x.GetUri());

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
                    LoadLogCurveInfo(parent.Children, dataObject);

                await Task.Yield();
                Runtime.ShowBusy(false);
            });
        }

        private void LoadLogCurveInfo(IList<ResourceViewModel> items, IWellboreObject dataObject)
        {
            var log131 = dataObject as Witsml131.Log;
            var log141 = dataObject as Witsml141.Log;

            log131?.LogCurveInfo
                .Select(x => ToResourceViewModel(x.GetUri(log131), x.Mnemonic, null, 0, dataContext: new DataObjectWrapper(x)))
                .ForEach(items.Add);

            log141?.LogCurveInfo
                .Select(x => ToResourceViewModel(x.GetUri(log141), x.Mnemonic.Value, null, 0, dataContext: new DataObjectWrapper(x)))
                .ForEach(items.Add);
        }

        private async Task LoadDataItems<T>(
            IEnumerable<T> dataObjects,
            IList<ResourceViewModel> items,
            Action<ResourceViewModel, string> action,
            Func<T, EtpUri> getUri,
            int children = -1)
            where T : IDataObject
        {
            await Runtime.InvokeAsync(() =>
            {
                dataObjects
                    .Select(x => ToResourceViewModel(x, action, getUri, children))
                    .ForEach(items.Add);
            });
        }

        private ResourceViewModel ToResourceViewModel<T>(T dataObject, Action<ResourceViewModel, string> action, Func<T, EtpUri> getUri, int children = -1) where T : IDataObject
        {
            var uri = getUri(dataObject);

            var indicator = new IndicatorViewModel { Color = IndicatorViewModel.Green, Outline = IndicatorViewModel.Black };

            if (ObjectTypes.Wellbore.EqualsIgnoreCase(uri.ObjectType))
            {
                indicator.IsVisible = OptionsIn.DataVersion.Version141.Equals(uri.Version);

                if (dataObject.GetWellboreStatus().GetValueOrDefault())
                {
                    indicator.Tooltip = "Active";
                }
                else
                {
                    indicator.Color = IndicatorViewModel.White;
                    indicator.Outline = IndicatorViewModel.Gray;
                    indicator.Tooltip = null;
                }
            }
            else if (ObjectTypes.IsGrowingDataObject(uri.ObjectType))
            {
                var iWellboreObject = dataObject as IWellboreObject;
                var startIndex = iWellboreObject?.GetStartIndex();
                var endIndex = iWellboreObject?.GetEndIndex();

                // TODO: Improve empty check using DateTime.MinValue and epoch timestamp
                var isEmpty = string.IsNullOrWhiteSpace(startIndex) && string.IsNullOrWhiteSpace(endIndex);

                indicator.IsVisible = true;

                if (dataObject.GetObjectGrowingStatus().GetValueOrDefault())
                {
                    indicator.Tooltip = "Growing";
                }
                else if (isEmpty)
                {
                    indicator.Color = IndicatorViewModel.White;
                    indicator.Outline = IndicatorViewModel.Red;
                    indicator.Tooltip = "Empty";
                }
                else
                {
                    indicator.Color = IndicatorViewModel.White;
                    indicator.Outline = IndicatorViewModel.Gray;
                    indicator.Tooltip = null;
                }
            }

            return ToResourceViewModel(uri, dataObject.Name, action, children, indicator, new DataObjectWrapper(dataObject));
        }

        private ResourceViewModel ToResourceViewModel(EtpUri uri, string name, Action<ResourceViewModel, string> action, int children = -1, IndicatorViewModel indicator = null, object dataContext = null)
        {
            var resource = new Resource
            {
                Uri = uri,
                Name = name,
                ContentType = uri.ContentType,
                HasChildren = children
            };

            var viewModel = new ResourceViewModel(Runtime, resource, dataContext);

            if (indicator != null)
                viewModel.Indicator = indicator;

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
    }
}
