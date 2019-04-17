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

using System.ComponentModel;

namespace PDS.WITSMLstudio.Desktop.Core.Adapters
{
    /// <summary>
    /// An enumeration of graph scopes.
    /// </summary>
    public enum GraphScopes
    {
        /// <summary>Self</summary>
        [Description("Self")]
        Self,
        /// <summary>Sources</summary>
        [Description("Sources")]
        Sources,
        /// <summary>Targets</summary>
        [Description("Targets")]
        Targets,
        /// <summary>SourcesOrSelf</summary>
        [Description("Sources or Self")]
        SourcesOrSelf,
        /// <summary>TargetsOrSelf</summary>
        [Description("Targets or Self")]
        TargetsOrSelf,
    }
}