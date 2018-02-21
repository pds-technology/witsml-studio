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

namespace PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.ViewModels
{
    /// <summary>
    /// Defines methods that can be used to receive notifications of connection/version changes.
    /// </summary>
    public interface IConnectionAware
    {
        /// <summary>
        /// Called when the selected WITSML version has changed.
        /// </summary>
        /// <param name="version">The WITSML version.</param>
        void OnWitsmlVersionChanged(string version);

        /// <summary>
        /// Called when data objects changed.
        /// </summary>
        /// <param name="dataObjects">The data objects.</param>
        void OnDataObjectsChanged(IEnumerable<string> dataObjects);
    }
}
