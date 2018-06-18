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

using Caliburn.Micro;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// Represents properties of an Indicator
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    public class IndicatorViewModel : PropertyChangedBase
    {
        /// <summary>Transparent</summary>
        public const string Transparent = "Transparent";
        /// <summary>Green</summary>
        public const string Green = "#FF32CD32";
        /// <summary>Red</summary>
        public const string Red = "#FFFF0000";
        /// <summary>Gray</summary>
        public const string Gray = "#FFC0C0C0";
        /// <summary>White</summary>
        public const string White = "#FFFFFFFF";
        /// <summary>Black</summary>
        public const string Black = "#FF000000";

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
        
        private string _color;
        /// <summary>
        /// Gets or sets the indicator fill color.
        /// </summary>
        /// <value>The indicator fill color.</value>
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

        private string _outline;
        /// <summary>
        /// Gets or sets the indicator outline color.
        /// </summary>
        /// <value>The indicator outline color.</value>
        public string Outline
        {
            get { return _outline ?? Transparent; }
            set
            {
                if (_outline != value)
                {
                    _outline = value;
                    NotifyOfPropertyChange(() => Outline);
                }
            }
        }
    }
}
