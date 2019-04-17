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
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the tree view user interface elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class Hierarchy11ViewModel : HierarchyViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Hierarchy11ViewModel"/> class.
        /// </summary>
        public Hierarchy11ViewModel(IRuntimeService runtime) : base(runtime)
        {
            SupportedVersions = new[] { EtpSettings.Etp11SubProtocol };
        }

        /// <summary>
        /// Copies the URI to streaming.
        /// </summary>
        public override void CopyUriToStreaming()
        {
            CopyUriToStreaming<Streaming11ViewModel>(x => x.AddUri());
        }

        /// <summary>
        /// Called when the OpenSession message is recieved.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        public override void OnSessionOpened(IList<ISupportedProtocol> supportedProtocols)
        {
            if (supportedProtocols.All(x => x.Protocol != Parent.EtpExtender.Protocols.Discovery && x.Protocol != Parent.EtpExtender.Protocols.DiscoveryQuery))
                return;
            
            CanExecute = true;
            RefreshContextMenu();
        }

        /// <summary>
        /// Refreshes the function list.
        /// </summary>
        protected override void RefreshFunctionList()
        {
            Parent.DiscoveryFunctions = new[] { Functions.GetResources };
            Model.DiscoveryFunction = Functions.GetResources;
        }
    }
}
