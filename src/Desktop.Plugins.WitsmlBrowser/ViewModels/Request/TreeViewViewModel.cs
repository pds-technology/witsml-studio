//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2017.2
//
// Copyright 2017 PDS Americas LLC
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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;
using PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.Models;

namespace PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.ViewModels.Request
{
    /// <summary>
    /// Manages the behavior for the TreeView view UI elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class TreeViewViewModel : Conductor<IScreen>.Collection.OneActive, IConnectionAware
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(TreeViewViewModel));
        private readonly string[] _exclude = { MainViewModel.QueryTemplateText, ObjectTypes.Well, ObjectTypes.Wellbore, ObjectTypes.ChangeLog, ObjectTypes.CapServer };

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeViewViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public TreeViewViewModel(IRuntimeService runtime)
        {
            _log.Debug("Creating view model instance");
            Runtime = runtime;
            DisplayName = "Hierarchy";
            TreeViewModel = new WitsmlTreeViewModel(runtime)
            {
                IsContextMenuEnabled = true
            };
        }

        /// <summary>
        /// Gets the Parent <see cref="T:Caliburn.Micro.IConductor" /> for this view model.
        /// </summary>
        public new RequestViewModel Parent
        {
            get { return (RequestViewModel)base.Parent; }
        }

        /// <summary>
        /// Gets or sets the data model.
        /// </summary>
        /// <value>
        /// The WitsmlSettings data model.
        /// </value>
        public WitsmlSettings Model
        {
            get { return Parent.Model; }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the TreeView view model.
        /// </summary>
        /// <value>The TreeView view model.</value>
        public WitsmlTreeViewModel TreeViewModel { get; }

        /// <summary>
        /// Called when the selected WITSML version has changed.
        /// </summary>
        /// <param name="version">The WITSML version.</param>
        public void OnWitsmlVersionChanged(string version)
        {
            var witsmlVersion = Parent.Parent.GetWitsmlVersionEnum(version);
            TreeViewModel.CreateContext(Parent.Parent.Model.Connection, witsmlVersion);

            TreeViewModel.Context.LogQuery = LogQuery;
            TreeViewModel.Context.LogResponse = LogResponse;
        }

        /// <summary>
        /// Called when data objects changed.
        /// </summary>
        /// <param name="dataObjects">The data objects.</param>
        public void OnDataObjectsChanged(IEnumerable<string> dataObjects)
        {
            TreeViewModel.SetDataObjects(dataObjects.Where(x => !_exclude.Contains(x)));
        }

        /// <summary>
        /// Called when maximum data rows changed.
        /// </summary>
        /// <param name="maxDataRows">The maximum data rows.</param>
        public void OnMaxDataRowsChanged(int? maxDataRows)
        {
            TreeViewModel.MaxDataRows = maxDataRows;
        }

        /// <summary>
        /// Called when request latest values has changed.
        /// </summary>
        /// <param name="requestLatestValues">The request latest values.</param>
        public void OnRequestLatestValuesChanged(int? requestLatestValues)
        {
            TreeViewModel.RequestLatestValues = requestLatestValues;
        }

        /// <summary>
        /// Called when extra options in is changed.
        /// </summary>
        /// <param name="extraOptionsIn">The extra options in.</param>
        public void OnExtraOptionsInChanged(string extraOptionsIn)
        {
            TreeViewModel.ExtraOptionsIn = extraOptionsIn;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            ActivateItem(TreeViewModel);
        }

        /// <summary>
        /// Called the first time the page's LayoutUpdated event fires after it is navigated to.
        /// </summary>
        /// <param name="view"></param>
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            
            TreeViewModel.SetDataObjects(Parent.Parent.DataObjects.Where(x => !_exclude.Contains(x)));
            TreeViewModel.OnViewReady();
        }

        private void LogQuery(Functions function, string objectType, string xmlIn, string optionsIn)
        {
            Task.Run(() =>
            {
                Parent.Parent.Model.StoreFunction = function;
                Parent.Parent.OutputRequestMessages(function, xmlIn, optionsIn);
                Parent.Parent.XmlQuery.SetText(xmlIn);
            });
        }

        private void LogResponse(Functions function, string objectType, string xmlIn, string optionsIn, string xmlOut, short returnCode, string suppMsgOut)
        {
            var result = new WitsmlResult(
                objectType: objectType,
                xmlIn: xmlIn,
                optionsIn: optionsIn,
                capClientIn: null,
                xmlOut: xmlOut,
                messageOut: suppMsgOut,
                returnCode: returnCode);

            Task.Run(() =>
            {
                Parent.Parent.ClearQueryResults();
                Parent.Parent.ShowSubmitResult(function, result);
            });
        }
    }
}
