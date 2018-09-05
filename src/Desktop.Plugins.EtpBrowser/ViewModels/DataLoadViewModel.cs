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

using Caliburn.Micro;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the Data Load user interface elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class DataLoadViewModel : Screen
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoadViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public DataLoadViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = "Data Load";
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets or Sets the Parent <see cref="T:Caliburn.Micro.IConductor" />
        /// </summary>
        public new MainViewModel Parent
        {
            get { return (MainViewModel)base.Parent; }
        }

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        public Models.EtpSettings Model => Parent.Model;

        /// <summary>
        /// Sets the type of the index.
        /// </summary>
        /// <param name="isTimeIndex">if set to <c>true</c> the index is time, otherwise index is depth.</param>
        public void SetIndexType(bool isTimeIndex)
        {
            Model.DataLoad.IsTimeIndex = isTimeIndex;
        }
    }
}
