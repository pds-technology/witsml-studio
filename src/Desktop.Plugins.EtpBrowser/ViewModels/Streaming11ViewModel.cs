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
using Energistics.Etp.Common.Datatypes.ChannelData;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the Streaming user interface elements.
    /// </summary>
    public sealed class Streaming11ViewModel : StreamingViewModelBase
    {
        private const string UnscaledIndexMessage = "Unscaled index values are required";
        private const string ErrorSettingIndexMessage = "Error setting indexes for range request";
        private const string NoChannelsSelectedMessage = "No channels selected for {0}";

        /// <summary>
        /// Initializes a new instance of the <see cref="Streaming11ViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        public Streaming11ViewModel(IRuntimeService runtime) : base(runtime)
        {
            SupportedVersions = new[] {EtpSettings.Etp11SubProtocol};
        }

        /// <summary>
        /// Starts a Channel Streaming session.
        /// </summary>
        public void Start()
        {
            if (Parent.Session == null)
            {
                LogSessionClientError();
                return;
            }

            Parent.EtpExtender.Start(Model.Streaming.MaxDataItems, Model.Streaming.MaxMessageRate);

            //Channels.Clear();
            //ChannelStreamingInfos.Clear();
            LogStartSession(Model.Streaming.MaxDataItems, Model.Streaming.MaxMessageRate);
        }

        /// <summary>
        /// Requests channel metadata for the collection of URIs.
        /// </summary>
        public void Describe()
        {
            if (Parent.Session == null)
            {
                LogSessionClientError();
                return;
            }

            //Channels.Clear();
            //ChannelStreamingInfos.Clear();

            // Verify streaming start value is not scaled
            try
            {
                Model.Streaming.StartIndex.IndexToScale(GetScale());
            }
            catch (OverflowException ex)
            {
                Runtime.ShowError(UnscaledIndexMessage, ex);
                return;
            }
            catch (Exception ex)
            {
                Runtime.ShowError(ErrorSettingIndexMessage, ex);
                return;
            }

            Parent.EtpExtender.ChannelDescribe(Model.Streaming.Uris);
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

            // If no channels were selected display a warning message
            if (!Channels.Any(c => c.IsChecked))
            {
                Runtime.ShowWarning(string.Format(NoChannelsSelectedMessage, "Streaming Start"));
                return;
            }

            try
            {
                Parent.EtpExtender.ChannelStreamingStart(Channels, GetStreamingStartValue());
            }
            catch (OverflowException ex)
            {
                Runtime.ShowError(UnscaledIndexMessage, ex);
            }
            catch (Exception ex)
            {
                Runtime.ShowError(ErrorSettingIndexMessage, ex);
            }
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

            // Create an array of channel ids for selected, described channels.
            var channelIds = Channels
                .Where(c => c.IsChecked)
                .Select(x => x.Record.ChannelId)
                .ToArray();

            // If no channels were selected display a warning message
            if (!channelIds.Any())
            {
                Runtime.ShowWarning(string.Format(NoChannelsSelectedMessage, "Streaming Stop"));
                return;
            }

            Parent.EtpExtender.ChannelStreamingStop(channelIds);
        }

        /// <summary>
        /// Requests a range of channel data.
        /// </summary>
        public void RequestRange()
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
                Runtime.ShowWarning(string.Format(NoChannelsSelectedMessage, "Range Request"));
                return;
            }

            try
            {
                var startIndex = (long)GetStreamingStartValue(true);
                var endIndex = (long)GetStreamingEndValue();

                Parent.EtpExtender.ChannelRangeRequest(channelIds, startIndex, endIndex);
            }
            catch (OverflowException ex)
            {
                Runtime.ShowError(UnscaledIndexMessage, ex);
            }
            catch (Exception ex)
            {
                Runtime.ShowError(ErrorSettingIndexMessage, ex);
            }
        }

        private object GetStreamingStartValue(bool isRangeRequest = false)
        {
            if (isRangeRequest && !"TimeIndex".EqualsIgnoreCase(Model.Streaming.StreamingType) && !"DepthIndex".EqualsIgnoreCase(Model.Streaming.StreamingType))
                return default(long);
            if ("LatestValue".EqualsIgnoreCase(Model.Streaming.StreamingType))
                return null;
            else if ("IndexCount".EqualsIgnoreCase(Model.Streaming.StreamingType))
                return Model.Streaming.IndexCount;

            var isTimeIndex = "TimeIndex".EqualsIgnoreCase(Model.Streaming.StreamingType);

            var startIndex = isTimeIndex
                ? new DateTimeOffset(Model.Streaming.StartTime).ToUnixTimeMicroseconds()
                : Model.Streaming.StartIndex.IndexToScale(GetScale());

            return startIndex;
        }

        private object GetStreamingEndValue()
        {
            var isTimeIndex = "TimeIndex".EqualsIgnoreCase(Model.Streaming.StreamingType);

            if ("LatestValue".EqualsIgnoreCase(Model.Streaming.StreamingType) ||
                "IndexCount".EqualsIgnoreCase(Model.Streaming.StreamingType) ||
                (isTimeIndex && !Model.Streaming.EndTime.HasValue) ||
                (!isTimeIndex && !Model.Streaming.EndIndex.HasValue))
                return default(long);

            var endIndex = isTimeIndex
                ? new DateTimeOffset(Model.Streaming.EndTime.Value).ToUnixTimeMicroseconds()
                : ((double)Model.Streaming.EndIndex).IndexToScale(GetScale());

            return endIndex;
        }

        private int GetScale()
        {
            return Channels
                .Select(c => c.Record)
                .FirstOrDefault()?
                .Indexes
                .Cast<IIndexMetadataRecord>()
                .FirstOrDefault()?
                .Scale ?? 0; // Default to no scale of no index is found.;
        }

        private void LogStartSession(int maxDataItems, int maxMessageRate)
        {
            Parent.Details.SetText(string.Format(
                "// Channel Streaming session started. [maxMessageRate={1}, maxDataItems={2}]{0}{0}",
                Environment.NewLine, maxMessageRate, maxDataItems));
        }
    }
}



