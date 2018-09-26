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
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PDS.WITSMLstudio.Desktop.Core.Converters
{
    /// <summary>
    /// Convert a string value to emumeration value.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        /// <summary>Converts a string value to a <see cref="T:System.Windows.Visibility" /> enumeration value for Visibility.</summary>
        /// <param name="value">The boolean value tested to convert.</param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">Reverse the logic of when to display.</param>
        /// <param name="culture">This parameter is not used.</param>
        /// <returns> Display if value is not null, unless the reverse flag is set to true</returns>        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inverse = System.Convert.ToBoolean(parameter ?? false);
            var message = System.Convert.ToString(value);

            if (inverse)
            {
                return string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
            }

            return string.IsNullOrWhiteSpace(message) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// This method is not implemented
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
