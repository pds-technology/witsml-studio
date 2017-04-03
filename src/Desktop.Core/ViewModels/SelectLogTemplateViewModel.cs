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

using Caliburn.Micro;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    ///  Manages the display and interaction of the Select Log Template view.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class SelectLogTemplateViewModel : Screen
    {            
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectLogTemplateViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public SelectLogTemplateViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            IsAllChannelsSelection = true;
            NumberOfChannels = 2;
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime ervice.</value>
        public IRuntimeService Runtime { get; }
        
        /// <summary>
        /// Gets or sets a value indicating whether all channels are selected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if all channels are selected; otherwise, <c>false</c>.
        /// </value>
        public bool IsAllChannelsSelection { get; set; }

        private bool _isChannelSelectionManual;

        /// <summary>
        /// Gets or sets a value indicating whether channels are manually entered.
        /// </summary>
        /// <value>
        /// <c>true</c> if channels are manually entered ; otherwise, <c>false</c>.
        /// </value>
        public bool IsChannelSelectionManual
        {
            get { return _isChannelSelectionManual; }
            set
            {
                if (value == _isChannelSelectionManual) return;
                _isChannelSelectionManual = value;
                NotifyOfPropertyChange(() => IsChannelSelectionManual);
            }
        }

        /// <summary>
        /// Gets or sets the number of channels for manual entry.
        /// </summary>
        /// <value>
        /// The number of channels.
        /// </value>
        public int NumberOfChannels { get; set; }

        /// <summary>
        /// Accepts the template option.
        /// </summary>
        public void Accept()
        {
            TryClose(true);
        }

        /// <summary>
        /// Cancels the selection.
        /// </summary>
        public void Cancel()
        {
            TryClose(false);
        }
    }
}
