//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2017.2
//
// Copyright 2017 PDS Americas LLC
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
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.Models
{
    /// <summary>
    /// Defines properties to display log curve metadata
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    public class LogCurveItem : PropertyChangedBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogCurveItem" /> class.
        /// </summary>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <param name="description">The description.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="uom">The uom.</param>
        /// <param name="typeLogData">The type log data.</param>
        /// <param name="nullValue">The null value.</param>
        /// <param name="notFound">if set to <c>true</c> if the LogCurveItem represents a LogCurveInfo that was not found.</param>
        public LogCurveItem(string mnemonic, string description, string startIndex, string endIndex, string uom, string typeLogData, string nullValue, bool notFound = false)
        {
            Mnemonic = mnemonic;
            NotFound = notFound;
            Description = NotFound ? "Not Found" : description;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Uom = uom;
            TypeLogData = typeLogData;
            NullValue = nullValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogCurveItem"/> class.
        /// </summary>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <param name="notFound">if set to <c>true</c> if the LogCurveItem represents a LogCurveInfo that was not found.</param>
        public LogCurveItem(string mnemonic, bool notFound)
            : this(mnemonic, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, notFound)
        {

        }

        /// <summary>
        /// Gets or sets the mnemonic.
        /// </summary>
        public string Mnemonic { get; }

        /// <summary>
        /// Gets the type log data.
        /// </summary>
        public string TypeLogData { get; }

        /// <summary>
        /// Gets the null value.
        /// </summary>
        public string NullValue { get; }

        /// <summary>
        /// Gets or sets the mnemonic.
        /// </summary>
        public string Uom { get; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets or sets the start index.
        /// </summary>
        public string StartIndex { get; }

        /// <summary>
        /// Gets or sets the end index.
        /// </summary>
        public string EndIndex { get; }

        /// <summary>
        /// Gets a value indicating whether a corresponding LogCurveInfo was not found.
        /// </summary>
        public bool NotFound { get; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this LogCurveItem.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this LogCurveItem.
        /// </returns>
        public override string ToString()
        {
            return Mnemonic;
        }

        /// <summary>
        /// Determines whether the specified mnemonic equals mnemonic.
        /// </summary>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <returns>
        ///   <c>true</c> if the specified mnemonic equals mnemonic; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(string mnemonic)
        {
            return Mnemonic.EqualsIgnoreCase(mnemonic);
        }
    }
}
