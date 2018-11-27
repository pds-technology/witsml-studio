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
using System.Windows.Controls;
using System.Windows.Input;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes.ChannelData;
using PDS.WITSMLstudio.Desktop.Core.Commands;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Models;
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
            ToggleChannelCommand = new DelegateCommand(x => ToggleSelectedChannel());
        }

        /// <summary>
        /// Gets the toggle channel command.
        /// </summary>
        public ICommand ToggleChannelCommand { get; }

        private ChannelMetadataViewModel _selectedChannel;

        /// <summary>
        /// Gets or sets the selected channel.
        /// </summary>
        public ChannelMetadataViewModel SelectedChannel
        {
            get { return _selectedChannel; }
            set
            {
                if (ReferenceEquals(_selectedChannel, value)) return;
                _selectedChannel = value;
                NotifyOfPropertyChange(() => SelectedChannel);
            }
        }

        /// <summary>
        /// Toggles the selected channel.
        /// </summary>
        public void ToggleSelectedChannel()
        {
            if (SelectedChannel == null) return;
            SelectedChannel.IsChecked = !SelectedChannel.IsChecked;
        }

        /// <summary>
        /// Sets the type of channel streaming.
        /// </summary>
        /// <param name="type">The type.</param>
        public void SetStreamingType(string type)
        {
            Model.Streaming.StreamingType = type;
        }

        /// <summary>
        /// Adds the URI to the collection of URIs.
        /// </summary>
        public void AddUri()
        {
            var uri = Model.Streaming.Uri;

            if (string.IsNullOrWhiteSpace(uri) || Model.Streaming.Uris.Contains(uri))
                return;

            Model.Streaming.Uris.Add(uri);
            Model.Streaming.Uri = string.Empty;
        }

        /// <summary>
        /// Handles the KeyUp event for the ListBox control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        public void OnKeyUp(ListBox control, KeyEventArgs e)
        {
            var index = control.SelectedIndex;

            if (e.Key == Key.Delete && index > -1)
            {
                Model.Streaming.Uris.RemoveAt(index);
            }
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

        /// <summary>
        /// Called when checkbox in ID column of channels datagrid is checked or unchecked.
        /// </summary>
        /// <param name="isSelected">if set to <c>true</c> if all channels should be selected, <c>false</c> if channels should be unselected.</param>
        public void OnChannelSelection(bool isSelected)
        {
            foreach (var channelMetadataViewModel in Channels)
            {
                channelMetadataViewModel.IsChecked = isSelected;
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



