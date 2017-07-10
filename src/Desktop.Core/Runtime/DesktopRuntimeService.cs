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

using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.Runtime
{
    /// <summary>
    /// Provides an implementation of <see cref="IRuntimeService"/> that can be used from within desktop applications.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Core.Runtime.IRuntimeService" />
    public class DesktopRuntimeService : RuntimeServiceBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DesktopRuntimeService"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public DesktopRuntimeService(IContainer container)
            : base(container)
        {
        }
    }
}
