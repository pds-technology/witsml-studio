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

using Energistics.Etp.Common;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the Streaming user interface elements.
    /// </summary>
    public sealed class Streaming12ViewModel : StreamingViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Streaming12ViewModel" /> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        public Streaming12ViewModel(IRuntimeService runtime) : base(runtime)
        {
            SupportedVersions = new[] { EtpSettings.Etp12SubProtocol };
        }

        /// <summary>
        /// Starts the streaming of channel data.
        /// </summary>
        public void StartStreaming()
        {
            if (Parent.Session == null)
            {
                LogSessionClientError();
                return;
            }

            Parent.EtpExtender.StartStreaming();
        }

        /// <summary>
        /// Stops the streaming of channel data.
        /// </summary>
        public void StopStreaming()
        {
            if (Parent.Session == null)
            {
                LogSessionClientError();
                return;
            }

            Parent.EtpExtender.StopStreaming();
        }
    }
}