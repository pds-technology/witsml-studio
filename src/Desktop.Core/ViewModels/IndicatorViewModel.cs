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

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// Represents properties of an Indicator
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    public class IndicatorViewModel : PropertyChangedBase
    {
        private const string Transparent = "Transparent";

        private bool _isVisible;
        /// <summary>
        /// Gets or sets a value whether indicator is visible.
        /// </summary>
        /// <value><c>true</c> if indicator is visible; otherwise, <c>false</c>.</value>
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    NotifyOfPropertyChange(() => IsVisible);
                }
            }
        }

        private string _tooltip;

        /// <summary>
        /// Gets or sets the indicator tootip.
        /// </summary>
        /// <value>The indicator tooltip.</value>
        public string Tooltip
        {
            get { return _tooltip; }
            set
            {
                if (_tooltip != value)
                {
                    _tooltip = value;
                    NotifyOfPropertyChange(() => Tooltip);
                }
            }
        }

        private string _color;

        /// <summary>
        /// Gets or sets the indicator color.
        /// </summary>
        /// <value>The indicator color.</value>
        public string Color
        {
            get { return _color ?? Transparent; }
            set
            {
                if (_color != value)
                {
                    _color = value;
                    NotifyOfPropertyChange(() => Color);
                }
            }
        }
    }
}
