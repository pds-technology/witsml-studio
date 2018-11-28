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
using System.Linq;
using Energistics.Etp.Common;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the Streaming user interface elements.
    /// </summary>
    public sealed class SubscribeViewModel : StreamingViewModelBase
    {
        private const string ErrorSettingIndexMessage = "Error setting indexes for Get Range";
        private const string NoChannelsSelectedMessage = "No channels selected for {0}";

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscribeViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        public SubscribeViewModel(IRuntimeService runtime) : base(runtime)
        {
            DisplayName = "Subscribe";
            SupportedVersions = new[] {EtpSettings.Etp12SubProtocol};
        }

        /// <summary>
        /// Gets channel metadata for the collection of URIs.
        /// </summary>
        public void GetChannelMetadata()
        {
            if (Parent.Session == null)
            {
                LogSessionClientError();
                return;
            }

            Parent.EtpExtender.GetChannelMetadata(Model.Streaming.Uris);
        }

        /// <summary>
        /// Starts the streaming of channel data.
        /// </summary>
        public void SubscribeChannels()
        {
            if (Parent.Session == null)
            {
                LogSessionClientError();
                return;
            }

            // If no channels were selected display a warning message
            if (!Channels.Any(c => c.IsChecked))
            {
                Runtime.ShowWarning(string.Format(NoChannelsSelectedMessage, "Subscribe Channels"));
                return;
            }

            // TODO: Add input controls to view, similar to Data Load tab
            object lastIndex = null;
            var infill = false;
            var dataChanges = false;

            Parent.EtpExtender.SubscribeChannels(Channels, lastIndex, infill, dataChanges);
        }

        /// <summary>
        /// Stops the streaming of channel data.
        /// </summary>
        public void UnsubscribeChannels()
        {
            if (Parent.Session == null)
            {
                LogSessionClientError();
                return;
            }

            // Create an array of channel ids for selected, described channels.
            var channelIds = Channels
                .Where(c => c.IsChecked)
                .Select(x => x.Record.ChannelId)
                .ToArray();

            // If no channels were selected display a warning message
            if (!channelIds.Any())
            {
                Runtime.ShowWarning(string.Format(NoChannelsSelectedMessage, "Unsubscribe Channels"));
                return;
            }

            Parent.EtpExtender.UnsubscribeChannels(channelIds);
        }

        /// <summary>
        /// Gets a range of channel data.
        /// </summary>
        public void GetRange()
        {
            if (Parent.Session == null)
            {
                LogSessionClientError();
                return;
            }

            var channelIds = Channels
                .Where(c => c.IsChecked)
                .Select(x => x.Record.ChannelId).ToArray();

            if (!channelIds.Any())
            {
                Runtime.ShowWarning(string.Format(NoChannelsSelectedMessage, "Get Range"));
                return;
            }

            // TODO: Add input controls to view
            var requestUuid = Guid.Empty;
            object startIndex = null;
            object endIndex = null;

            Parent.EtpExtender.GetRange(requestUuid, channelIds, startIndex, endIndex);
        }

        public void CancelGetRange()
        {
            if (Parent.Session == null)
            {
                LogSessionClientError();
                return;
            }

            // TODO: Add input controls to view
            var requestUuid = Guid.Empty;

            Parent.EtpExtender.CancelGetRange(requestUuid);
        }
    }
}