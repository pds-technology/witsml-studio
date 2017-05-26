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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Caliburn.Micro;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Desktop.Core.Properties;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// Manages the selection of data channel names from a list of available channels
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class SelectChannelsViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SelectChannelsViewModel));
        private static readonly string _dialogTitlePrefix = Settings.Default.DialogTitlePrefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectChannelsViewModel" /> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <param name="availableChannels">The available channels.</param>
        /// <param name="indexChannel">The index channel.</param>
        /// <param name="selectedChannels">The defaulted selected channels.</param>
        public SelectChannelsViewModel(IRuntimeService runtime, List<LogCurveItem> availableChannels, string indexChannel, List<LogCurveItem> selectedChannels = null)
        {
            Runtime = runtime;
            DisplayName = $"{_dialogTitlePrefix} - Select Channels";

            // Create a selectedChannels list if one was not sent in
            if (selectedChannels == null)
            {
                selectedChannels = new List<LogCurveItem>();
            }

            // If the selectedChannels list does not contain the index channel 
            //... then find it in the availableChannels and add it to the selectedChannels
            if (!selectedChannels.Any(l => l.Equals(indexChannel)))
            {
                IndexChannel = availableChannels.FirstOrDefault(a => a.Equals(indexChannel));
                selectedChannels.Add(IndexChannel);
            }

            // Get the IndexChannel LogCurveItem from the selectedChannels
            IndexChannel = selectedChannels.FirstOrDefault(a => a.Equals(indexChannel));

            availableChannels.ForEach(c => AvailableChannels.Add(c));
            SelectedChannels = new ObservableCollection<LogCurveItem>(selectedChannels);

            RemoveSelectedFromAvailableChannels();
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
        public ObservableCollection<LogCurveItem> AvailableChannels { get; } = new ObservableCollection<LogCurveItem>();

        /// <summary>
        /// Gets the selected channels.
        /// </summary>
        /// <value>
        /// The selected channels.
        /// </value>
        public ObservableCollection<LogCurveItem> SelectedChannels { get; } = new ObservableCollection<LogCurveItem>();

        /// <summary>
        /// Gets the index channel.
        /// </summary>
        /// <value>
        /// The index channel.
        /// </value>
        public LogCurveItem IndexChannel { get; }

        /// <summary>
        /// Gets or sets the available channel selected.
        /// </summary>
        /// <value>
        /// The available channel selected.
        /// </value>
        public LogCurveItem AvailableChannelSelected { get; set; }

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
        public LogCurveItem SelectedChannelSelected { get; set; }

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
        /// Defaults list view selections.
        /// </summary>
        /// <returns></returns>
        public void DefaultListViews()
        {
            if (AvailableChannels.Count > 0)
            {
                AvailableChannelSelectedIndex = 0;
                NotifyOfPropertyChange(() => AvailableChannelSelectedIndex);
            }

            if (SelectedChannels.Count > 0)
            {
                SelectedChannelSelectedIndex = 0;
                NotifyOfPropertyChange(() => SelectedChannelSelectedIndex);
            }
        }

        /// <summary>
        /// Selects the channel moving it from Available to Selected
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs" /> instance containing the event data.</param>
        public void SelectChannel(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e?.OriginalSource;
            if (sender is ListBox && !(originalSource is UIElement && ((UIElement)originalSource).IsMouseOver)) return;

            var sourceIndex = MoveChannel(AvailableChannelSelected, AvailableChannels, SelectedChannels, AvailableChannelSelectedIndex);
            if (sourceIndex < 0) return;

            AvailableChannelSelectedIndex = sourceIndex;
            NotifyOfPropertyChange(() => AvailableChannelSelectedIndex);
        }

        /// <summary>
        /// Selects all channels from Available and moves them to Selected
        /// </summary>
        public void SelectAllChannels()
        {
            var available = AvailableChannels.ToList().OrderBy(c => c.Mnemonic);
            available.ForEach(a => MoveChannel(a, AvailableChannels, SelectedChannels));
        }

        /// <summary>
        /// Unselects the channel from Selected and moves it to Available.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs" /> instance containing the event data.</param>
        public void UnselectChannel(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e?.OriginalSource;
            if (sender is ListBox && !(originalSource is UIElement && ((UIElement)originalSource).IsMouseOver)) return;

            if (SelectedChannelSelectedIndex == 0 && SelectedChannelSelected.Equals(IndexChannel.Mnemonic)) return;

            var sourceIndex = MoveChannel(SelectedChannelSelected, SelectedChannels, AvailableChannels, SelectedChannelSelectedIndex);
            if (sourceIndex < 0) return;

            SelectedChannelSelectedIndex = sourceIndex;
            NotifyOfPropertyChange(() => SelectedChannelSelectedIndex);
        }

        /// <summary>
        /// Unselects all channels from Selected and moves them to Available.
        /// </summary>
        public void UnselectAllChannels()
        {
            var indexChannel = SelectedChannels.FirstOrDefault(s => s.Equals(IndexChannel.Mnemonic));
            var selected = new List<LogCurveItem>(SelectedChannels);

            if (indexChannel != null)
                selected.Remove(indexChannel);

            selected.ForEach(s => MoveChannel(s, SelectedChannels, AvailableChannels));
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
                if (selectedIndex == 0 && itemToMoveDown.Equals(IndexChannel.Mnemonic)) return;
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

        private void RemoveSelectedFromAvailableChannels()
        {
            if (SelectedChannels.Count == 0) return;

            var alreadySelectedCurves = new List<LogCurveItem>();
            AvailableChannels.ForEach(a =>
            {
                if (SelectedChannels.FirstOrDefault(sc => sc.Mnemonic.EqualsIgnoreCase(a.Mnemonic)) != null)
                    alreadySelectedCurves.Add(a);
            });
            alreadySelectedCurves.ForEach(r => AvailableChannels.Remove(r));
        }

        private int MoveChannel(LogCurveItem logCurveItem, ObservableCollection<LogCurveItem> sourceChannels, ObservableCollection<LogCurveItem> destinationChannels, int selectedIndex = -1)
        {
            // If the selected channel cannot be found in the source list get out
            var channelSelectedIndex = -1;

            // Channel was found in the source list and is removed.
            if (sourceChannels.Contains(logCurveItem))
            {
                channelSelectedIndex = selectedIndex == -1 ? sourceChannels.IndexOf(logCurveItem) : selectedIndex;
                sourceChannels.Remove(logCurveItem);
            }

            // Channel is added to the destination list and notifications are sent about changes to both lists
            destinationChannels.Add(logCurveItem);
            NotifyOfPropertyChange(() => HasSelected);
            NotifyOfPropertyChange(() => HasAvailable);

            // Return the index of the channel removed if the list has that many items, otherwise return the index above the channel
            return channelSelectedIndex <= (sourceChannels.Count - 1)
                ? channelSelectedIndex
                : channelSelectedIndex - 1;
        }

        private void MoveToTop(ObservableCollection<LogCurveItem> channels, LogCurveItem indexChannel)
        {
            // If the channel is not found in the channel list get out
            var index = channels.IndexOf(indexChannel);
            if (index < 0) return;

            // Remove the channel from its current position in the list and insert it at the top
            channels.RemoveAt(index);
            channels.Insert(0, indexChannel);
        }
    }
}
