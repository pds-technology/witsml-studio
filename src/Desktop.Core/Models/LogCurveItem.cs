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
        public LogCurveItem(string mnemonic, string description, string startIndex, string endIndex, string uom)
        {
            Mnemonic = mnemonic;
            Description = description;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Uom = uom;
        }

        /// <summary>
        /// Gets or sets the mnemonic.
        /// </summary>
        public string Mnemonic { get; }

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
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var objLogCurveItem = obj as LogCurveItem;

            return objLogCurveItem != null && objLogCurveItem.Mnemonic.Equals(Mnemonic);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Mnemonic.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified mnemonic contains mnemonic.
        /// </summary>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <returns>
        ///   <c>true</c> if the specified mnemonic contains mnemonic; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsMnemonic(string mnemonic)
        {
            return Mnemonic.Equals(mnemonic);
        }
    }
}
