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

using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using Energistics.DataAccess;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.ViewModels.Request
{
    /// <summary>
    /// Manages the behavior for the request UI elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Conductor{IScreen}.Collection.OneActive" />
    public class RequestViewModel : Conductor<IScreen>.Collection.OneActive, IConnectionAware
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(RequestViewModel));
        private static readonly OptionsIn.ReturnElements _blank = new OptionsIn.ReturnElements(string.Empty);

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        /// <param name="xmlQuery"></param>
        public RequestViewModel(IRuntimeService runtime, TextEditorViewModel xmlQuery)
        {
            _log.Debug("Creating view model instance");
            Runtime = runtime;
            XmlQuery = xmlQuery;
        }

        /// <summary>
        /// Gets the Parent <see cref="T:Caliburn.Micro.IConductor" /> for this view model.
        /// </summary>
        public new MainViewModel Parent => (MainViewModel)base.Parent;

        /// <summary>
        /// Gets the data model for this view model.
        /// </summary>
        /// <value>
        /// The WitsmlSettings data model.
        /// </value>
        public Models.WitsmlSettings Model => Parent.Model;

        private TextEditorViewModel _xmlQuery;

        /// <summary>
        /// Gets or sets the query editor.
        /// </summary>
        /// <value>The query editor.</value>
        public TextEditorViewModel XmlQuery
        {
            get { return _xmlQuery; }
            set
            {
                if (!ReferenceEquals(_xmlQuery, value))
                {
                    _xmlQuery = value;
                    NotifyOfPropertyChange(() => XmlQuery);
                }
            }
        }

        /// <summary>
        /// Gets the proxy to the WITSML web service.
        /// </summary>
        /// <value>
        /// The WITSML web service proxy.
        /// </value>
        public WITSMLWebServiceConnection Proxy => Parent.Proxy;

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the store functions that can be executed.
        /// </summary>
        /// <value>
        /// The executable store functions.
        /// </value>
        public IEnumerable<Functions> StoreFunctions => new [] { Functions.AddToStore, Functions.GetFromStore, Functions.UpdateInStore, Functions.DeleteFromStore };

        /// <summary>
        /// Gets the options in return elements.
        /// </summary>
        /// <value>
        /// The options in return elements.
        /// </value>
        public IEnumerable<OptionsIn.ReturnElements> ReturnElements => OptionsIn.ReturnElements.GetValues().Concat(new [] { _blank });

        /// <summary>
        /// Called when the selected WITSML version has changed.
        /// </summary>
        /// <param name="version">The WITSML version.</param>
        public void OnWitsmlVersionChanged(string version)
        {
            Items
                .OfType<IConnectionAware>()
                .ForEach(x => x.OnWitsmlVersionChanged(version));
        }

        /// <summary>
        /// Called when the data objects have changed.
        /// </summary>
        public void OnDataObjectsChanged(IEnumerable<string> dataObjects)
        {
            Items
                .OfType<IConnectionAware>()
                .ForEach(x => x.OnDataObjectsChanged(dataObjects));
        }

        /// <summary>
        /// Called when maximum data rows has changed.
        /// </summary>
        /// <param name="maxDataRows">The maximum data rows.</param>
        public void OnMaxDataRowsChanged(int? maxDataRows)
        {
            Items
                .OfType<TreeViewViewModel>()
                .ForEach(x => x.OnMaxDataRowsChanged(maxDataRows));
        }

        /// <summary>
        /// Called when request latest values has changed.
        /// </summary>
        /// <param name="requestLatestValues">The request latest values.</param>
        public void OnRequestLatestValuesChanged(int? requestLatestValues)
        {
            Items
             .OfType<TreeViewViewModel>()
             .ForEach(x => x.OnRequestLatestValuesChanged(requestLatestValues));
        }

        /// <summary>
        /// Called when extra options in is changed.
        /// </summary>
        /// <param name="extraOptionsIn">The extra options in.</param>
        public void OnExtraOptionsInChanged(string extraOptionsIn)
        {
            Items
             .OfType<TreeViewViewModel>()
             .ForEach(x => x.OnExtraOptionsInChanged(extraOptionsIn));
        }

        /// <summary>
        /// Loads the screens for the request view model.
        /// </summary>
        internal void LoadScreens()
        {
            _log.Debug("Loading RequestViewModel screens");
            Items.Add(new SettingsViewModel(Runtime));
            Items.Add(new TreeViewViewModel(Runtime));
            //Items.Add(new TemplatesViewModel());
            Items.Add(new QueryViewModel(Runtime, XmlQuery));

            ActivateItem(Items.FirstOrDefault());
        }

        /// <summary>
        /// Called when initializing the request view model.
        /// </summary>
        protected override void OnInitialize()
        {
            _log.Debug("Initializing screen");
            base.OnInitialize();
            LoadScreens();
        }
    }
}
