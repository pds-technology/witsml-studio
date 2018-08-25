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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Caliburn.Micro;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using PDS.WITSMLstudio.Desktop.Core.Commands;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the Streaming user interface elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class StreamingViewModel : Screen, ISessionAware
    {
        private const string UnscaledIndexMessage = "Unscaled index values are required";
        private const string ErrorSettingIndexMessage = "Error setting indexes for range request";
        private const string NoChannelsSelectedMessage = "No channels selected for {0}";

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingViewModel"/> class.
        /// </summary>
        public StreamingViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = "Streaming";
            Channels = new BindableCollection<ChannelMetadataViewModel>();
            ToggleChannelCommand = new DelegateCommand(x => ToggleSelectedChannel());
        }

        /// <summary>
        /// Gets or Sets the Parent <see cref="T:Caliburn.Micro.IConductor" />
        /// </summary>
        public new MainViewModel Parent => (MainViewModel) base.Parent;

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        public Models.EtpSettings Model => Parent.Model;

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the collection of channel metadata.
        /// </summary>
        /// <value>The channel metadata.</value>
        public BindableCollection<ChannelMetadataViewModel> Channels { get; }

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
        /// Called when the selected connection has changed.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public void OnConnectionChanged(Connection connection)
        {
        }

        /// <summary>
        /// Called when the OpenSession message is recieved.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        public void OnSessionOpened(IList<ISupportedProtocol> supportedProtocols)
        {
            if (supportedProtocols.All(x => x.Protocol != Parent.EtpExtender.Protocols.ChannelStreaming))
                return;

            Parent.EtpExtender.Register(
                onChannelMetadata: OnChannelMetadata,
                onChannelData: OnChannelData);

            Channels.Clear();
        }

        /// <summary>
        /// Called when the <see cref="Energistics.Etp.EtpClient" /> web socket is closed.
        /// </summary>
        public void OnSocketClosed()
        {
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

        private void OnChannelMetadata(IMessageHeader header, IList<IChannelMetadataRecord> channels)
        {
            if (!channels.Any())
            {
                Parent.Details.Append(Environment.NewLine + "// No channels were described");
                return;
            }

            // add to channel metadata collection
            channels.ForEach(x =>
            {
                if (Channels.Any(c => c.Record.ChannelUri.EqualsIgnoreCase(x.ChannelUri)))
                    return;

                Channels.Add(new ChannelMetadataViewModel(x));
            });

            if (header.MessageFlags != (int) MessageFlags.MultiPart)
            {
                LogChannelMetadata(Channels.Select(c => c.Record).ToArray());
            }
        }


        private void OnChannelData(IMessageHeader header, IList<IDataItem> channelData)
        {
            if (channelData.Any())
                LogChannelData(channelData);
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

        private void LogSessionClientError()
        {
            Parent.Details.SetText(string.Format(
                "// ERROR: No ETP connection for Channel Streaming session.{0}{0}",
                Environment.NewLine));
        }

        private void LogStartSession(int maxDataItems, int maxMessageRate)
        {
            Parent.Details.SetText(string.Format(
                "// Channel Streaming session started. [maxMessageRate={1}, maxDataItems={2}]{0}{0}",
                Environment.NewLine, maxMessageRate, maxDataItems));
        }

        private void LogChannelMetadata(IList<IChannelMetadataRecord> channels)
        {
            var headers = string.Join("\", \"", channels.Select(x => x.ChannelName));
            var units = string.Join("\", \"", channels.Select(x => x.Uom));

            Parent.Details.Append(string.Format(
                "// Mnemonics:{2}[ \"{0}\" ]{2}{2}// Units:{2}[ \"{1}\" ]{2}{2}",
                headers,
                units,
                Environment.NewLine));

            var crlf = Environment.NewLine + Environment.NewLine;

            var dataObjects = string.Join(crlf, channels
                .Select(x => x.DomainObject?.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            Parent.DataObject.SetText(dataObjects);
        }

        private void LogChannelData(IList<IDataItem> dataItems)
        {
            // Check if producer is sending index/value pairs
            if (!dataItems.Take(1).SelectMany(x => x.Indexes).Any())
            {
                for (int i=0; i<dataItems.Count; i+=2)
                {
                    var valueChannel = Channels.FirstOrDefault(c => c.Record.ChannelId == dataItems[i + 1].ChannelId);

                    Parent.Details.Append(string.Format(
                        "[ \"{0}\", {1}, {2} ],{3}",
                        valueChannel?.Record.ChannelName,
                        dataItems[i].Value.Item,
                        dataItems[i + 1].Value.Item,
                        Environment.NewLine));
                }
            }
            else // DataItems with indexes
            {
                var dataValues = string.Join(Environment.NewLine, dataItems.Select(x =>
                {
                    var channel = Channels.FirstOrDefault(c => c.Record.ChannelId == x.ChannelId);
                    var channelIndex = channel?.Record.Indexes.Cast<IIndexMetadataRecord>().FirstOrDefault();
                    var isTimeIndex = Parent.EtpExtender.IsTimeIndex(channelIndex);
                    var indexValue = x.Indexes.FirstOrDefault();

                    var indexFormat = isTimeIndex
                        ? DateTimeExtensions.FromUnixTimeMicroseconds(indexValue).ToString("o")
                        : $"{indexValue.IndexFromScale(channelIndex?.Scale ?? 3)}";

                    return string.Format("[ \"{0}\", {1}, {2} ] // Channel ID: {3} // {4}",
                        channel?.Record.ChannelName,
                        indexValue,
                        x.Value.Item,
                        x.ChannelId,
                        indexFormat);
                }));

                Parent.Details.Append(string.Format(
                    "{0}{1}",
                    dataValues,
                    Environment.NewLine));
            }
        }
    }
}



