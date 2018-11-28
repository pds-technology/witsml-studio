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
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Base implementation for all streaming view model classes.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels.ISessionAware" />
    public abstract class StreamingViewModelBase : Screen, ISessionAware
    {
        protected StreamingViewModelBase(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = "Streaming";
            Channels = new BindableCollection<ChannelMetadataViewModel>();
            ToggleChannelCommand = new DelegateCommand(x => ToggleSelectedChannel());
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets a collection of supported ETP versions.
        /// </summary>
        public string[] SupportedVersions { get; protected set; }

        /// <summary>
        /// Gets the collection of channel metadata.
        /// </summary>
        /// <value>The channel metadata.</value>
        public BindableCollection<ChannelMetadataViewModel> Channels { get; }

        /// <summary>
        /// Gets or Sets the Parent <see cref="IConductor" />
        /// </summary>
        public new MainViewModel Parent => (MainViewModel)base.Parent;

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        public Models.EtpSettings Model => Parent.Model;

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
        public virtual void ToggleSelectedChannel()
        {
            if (SelectedChannel == null) return;
            SelectedChannel.IsChecked = !SelectedChannel.IsChecked;
        }

        /// <summary>
        /// Called when checkbox in ID column of channels datagrid is checked or unchecked.
        /// </summary>
        /// <param name="isSelected">if set to <c>true</c> if all channels should be selected, <c>false</c> if channels should be unselected.</param>
        public virtual void OnChannelSelection(bool isSelected)
        {
            foreach (var channelMetadataViewModel in Channels)
            {
                channelMetadataViewModel.IsChecked = isSelected;
            }
        }

        /// <summary>
        /// Sets the type of channel streaming.
        /// </summary>
        /// <param name="type">The type.</param>
        public virtual void SetStreamingType(string type)
        {
            Model.Streaming.StreamingType = type;
        }

        /// <summary>
        /// Adds the URI to the collection of URIs.
        /// </summary>
        public virtual void AddUri()
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
        public virtual void OnKeyUp(ListBox control, KeyEventArgs e)
        {
            var index = control.SelectedIndex;

            if (e.Key == Key.Delete && index > -1)
            {
                Model.Streaming.Uris.RemoveAt(index);
            }
        }

        /// <summary>
        /// Called when the selected connection has changed.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public virtual void OnConnectionChanged(Connection connection)
        {
        }

        /// <summary>
        /// Called when the OpenSession message is recieved.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        public virtual void OnSessionOpened(IList<ISupportedProtocol> supportedProtocols)
        {
            if (supportedProtocols.All(x => x.Protocol != Parent.EtpExtender.Protocols.ChannelStreaming))
                return;

            Parent.EtpExtender.Register(
                onChannelMetadata: OnChannelMetadata,
                onChannelData: OnChannelData);

            Channels.Clear();
        }

        /// <summary>
        /// Called when the <see cref="Energistics.Etp.Common.IEtpClient" /> web socket is closed.
        /// </summary>
        public virtual void OnSocketClosed()
        {
        }

        protected virtual void OnChannelMetadata(IMessageHeader header, IList<IChannelMetadataRecord> channels)
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

            if (header.MessageFlags != (int)MessageFlags.MultiPart)
            {
                LogChannelMetadata(Channels.Select(c => c.Record).ToArray());
            }
        }

        protected virtual void OnChannelData(IMessageHeader header, IList<IDataItem> channelData)
        {
            if (channelData.Any())
                LogChannelData(channelData);
        }

        protected virtual void LogSessionClientError()
        {
            Parent.Details.SetText(string.Format(
                "// ERROR: No ETP connection for Channel Streaming session.{0}{0}",
                Environment.NewLine));
        }

        protected virtual void LogChannelMetadata(IList<IChannelMetadataRecord> channels)
        {
            var headers = string.Join("\", \"", channels.Select(x => x.ChannelName));
            var units = string.Join("\", \"", channels.Select(x => x.Uom));

            Parent.Details.Append(string.Format(
                "// Mnemonics:{2}[ \"{0}\" ]{2}{2}// Units:{2}[ \"{1}\" ]{2}{2}",
                headers,
                units,
                Environment.NewLine));

            // Check if user wants to see decoded byte arrays
            if (!Model.DecodeByteArrays) return;

            var crlf = Environment.NewLine + Environment.NewLine;

            var dataObjects = string.Join(crlf, channels
                .Select(x => x.DomainObject?.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            Parent.DataObject.SetText(dataObjects);
        }

        protected virtual void LogChannelData(IList<IDataItem> dataItems)
        {
            var dataObjects = new List<Tuple<ChannelMetadataViewModel, byte[]>>();

            // Check if producer is sending index/value pairs
            if (!dataItems.Take(1).SelectMany(x => x.Indexes.Cast<object>()).Any())
            {
                for (int i = 0; i < dataItems.Count; i += 2)
                {
                    var indexItem = dataItems[i];
                    var valueItem = dataItems[i + 1];
                    var valueChannel = Channels.FirstOrDefault(c => c.Record.ChannelId == valueItem.ChannelId);

                    if (valueChannel != null && valueItem.Value.Item is byte[])
                    {
                        dataObjects.Add(Tuple.Create(valueChannel, (byte[])valueItem.Value.Item));
                    }

                    Parent.Details.Append(string.Format(
                        "[ \"{0}\", {1}, {2} ],{3}",
                        valueChannel?.Record.ChannelName,
                        indexItem.Value.Item,
                        valueItem.Value.Item,
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
                    var indexValue = x.Indexes.Cast<object>().FirstOrDefault();

                    var indexFormat = isTimeIndex
                        ? DateTimeExtensions.FromUnixTimeMicroseconds(Convert.ToInt64(indexValue)).ToString("o")
                        : $"{(indexValue as long?)?.IndexFromScale(channelIndex?.Scale ?? 3) ?? indexValue}";

                    if (channel != null && x.Value.Item is byte[])
                    {
                        dataObjects.Add(Tuple.Create(channel, (byte[])x.Value.Item));
                    }

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

            // Check if user wants to see decoded byte arrays
            if (!Model.DecodeByteArrays) return;

            foreach (var tuple in dataObjects)
            {
                var dataObject = new Energistics.Etp.v11.Datatypes.Object.DataObject
                {
                    Data = tuple.Item2
                };

                var xml = dataObject.GetString();
                if (string.IsNullOrWhiteSpace(xml)) continue;

                Parent.DataObject.Append(Environment.NewLine + Environment.NewLine);
                Parent.DataObject.Append(xml);
            }
        }
    }
}