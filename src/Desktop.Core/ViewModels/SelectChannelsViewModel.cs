//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Caliburn.Micro;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// Manages the selection of data channel names from a list of available channels
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class SelectChannelsViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SelectChannelsViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectChannelsViewModel" /> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <param name="availableChannels">The available channels.</param>
        /// <param name="indexChannel">The index channel.</param>
        /// <param name="selectedChannels">The defaulted selected channels.</param>
        public SelectChannelsViewModel(IRuntimeService runtime, List<string> availableChannels, string indexChannel, List<string> selectedChannels = null)
        {
            Runtime = runtime;
            IndexChannel = indexChannel;

            if (selectedChannels == null)
            {
                selectedChannels = new List<string>();
            }

            if (!selectedChannels.Contains(IndexChannel))
            {
                selectedChannels.Add(IndexChannel);
            }
            availableChannels.ForEach(c => AvailableChannels.Add(c));
            selectedChannels?.ForEach(s => MoveChannel(s, AvailableChannels, SelectedChannels));
            MoveToTop(SelectedChannels, IndexChannel);
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the available channels.
        /// </summary>
        /// <value>
        /// The available channels.
        /// </value>
        public ObservableCollection<string> AvailableChannels { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets the selected channels.
        /// </summary>
        /// <value>
        /// The selected channels.
        /// </value>
        public ObservableCollection<string> SelectedChannels { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets the index channel.
        /// </summary>
        /// <value>
        /// The index channel.
        /// </value>
        public string IndexChannel { get; }

        /// <summary>
        /// Gets or sets the available channel selected.
        /// </summary>
        /// <value>
        /// The available channel selected.
        /// </value>
        public string AvailableChannelSelected { get; set; }

        /// <summary>
        /// Gets or sets the index of the available channel selected.
        /// </summary>
        /// <value>
        /// The index of the available channel selected.
        /// </value>
        public int AvailableChannelSelectedIndex { get; set; }

        /// <summary>
        /// Gets or sets the selected channel selected.
        /// </summary>
        /// <value>
        /// The selected channel selected.
        /// </value>
        public string SelectedChannelSelected { get; set; }

        /// <summary>
        /// Gets or sets the index of the selected channel selected.
        /// </summary>
        /// <value>
        /// The index of the selected channel selected.
        /// </value>
        public int SelectedChannelSelectedIndex { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has selected channels.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has selected channels; otherwise, <c>false</c>.
        /// </value>
        public bool HasSelected => SelectedChannels.Count > 0;

        /// <summary>
        /// Gets a value indicating whether this instance has available channels.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has available channels; otherwise, <c>false</c>.
        /// </value>
        public bool HasAvailable => AvailableChannels.Count > 0;

        /// <summary>
        /// Selects the channel.
        /// </summary>
        public void SelectChannel()
        {
            var sourceIndex = MoveChannel(AvailableChannelSelected, AvailableChannels, SelectedChannels);
            if (sourceIndex < 0) return;
            AvailableChannelSelectedIndex = sourceIndex;
            NotifyOfPropertyChange(() => AvailableChannelSelectedIndex);
        }

        /// <summary>
        /// Selects all channels.
        /// </summary>
        public void SelectAllChannels()
        {
            var available = AvailableChannels.ToArray();
            available.ForEach(a => MoveChannel(a, AvailableChannels, SelectedChannels));
        }

        /// <summary>
        /// Unselects the channel.
        /// </summary>
        public void UnselectChannel()
        {
            if (SelectedChannelSelected.Equals(IndexChannel)) return;
            var sourceIndex = MoveChannel(SelectedChannelSelected, SelectedChannels, AvailableChannels);
            if (sourceIndex < 0) return;
            SelectedChannelSelectedIndex = sourceIndex;
            NotifyOfPropertyChange(() => SelectedChannelSelectedIndex);
        }

        /// <summary>
        /// Unselects all channels.
        /// </summary>
        public void UnselectAllChannels()
        {
            var selected = SelectedChannels.Where(s => !s.Equals(IndexChannel)).ToArray();
            selected.ForEach(s => MoveChannel(s, SelectedChannels, AvailableChannels));
        }

        private int MoveChannel(string channelSelected, ObservableCollection<string> sourceChannels, ObservableCollection<string> destinationChannels)
        {
            if (string.IsNullOrEmpty(channelSelected)) return -1;

            var index = sourceChannels.IndexOf(channelSelected);
            if (index < 0) return -1;

            sourceChannels.RemoveAt(index);

            destinationChannels.Add(channelSelected);
            NotifyOfPropertyChange(() => HasSelected);
            NotifyOfPropertyChange(() => HasAvailable);

            return index <= (sourceChannels.Count - 1) 
                ? index 
                : index - 1;
        }

        /// <summary>
        /// Notifies when SelectedChannelSelected has changed.
        /// </summary>
        public void SelectedChannelSelectionChanged()
        {
            NotifyOfPropertyChange(() => SelectedChannelSelected);
        }

        /// <summary>
        /// Notifies when AvailableChannelSelected has changed.
        /// </summary>
        public void AvailableChannelSelectionChanged()
        {
            NotifyOfPropertyChange(() => AvailableChannelSelected);
        }

        private void MoveToTop(ObservableCollection<string> channels, string indexChannel)
        {
            var index = channels.IndexOf(indexChannel);
            if (index < 0) return;

            channels.RemoveAt(index);
            channels.Insert(0, indexChannel);
        }

        /// <summary>
        /// Moves a slected channel up.
        /// </summary>
        public void MoveUp()
        {
            var selectedIndex = SelectedChannelSelectedIndex;

            if (selectedIndex > 1)
            {
                var itemToMoveUp = SelectedChannels[selectedIndex];
                SelectedChannels.RemoveAt(selectedIndex);
                SelectedChannels.Insert(selectedIndex - 1, itemToMoveUp);
                SelectedChannelSelectedIndex = selectedIndex - 1;
                NotifyOfPropertyChange(() => SelectedChannelSelectedIndex);
            }
        }

        /// <summary>
        /// Moves a selected channel down.
        /// </summary>
        public void MoveDown()
        {
            var selectedIndex = SelectedChannelSelectedIndex;

            if (selectedIndex + 1 < SelectedChannels.Count)
            {
                var itemToMoveDown = SelectedChannels[selectedIndex];
                if (itemToMoveDown.Equals(IndexChannel)) return;
                SelectedChannels.RemoveAt(selectedIndex);
                SelectedChannels.Insert(selectedIndex + 1, itemToMoveDown);
                SelectedChannelSelectedIndex = selectedIndex + 1;
                NotifyOfPropertyChange(() => SelectedChannelSelectedIndex);
            }
        }

        /// <summary>
        /// Cancels the selection of channels.
        /// </summary>
        public void Cancel()
        {
            _log.Debug("Channel selection canceled");
            TryClose(false);
        }

        /// <summary>
        /// Accepts the selection of channels.
        /// </summary>
        public void Accept()
        {
            _log.Debug("Channel selection accepted");
            TryClose(true);
        }
    }
}
