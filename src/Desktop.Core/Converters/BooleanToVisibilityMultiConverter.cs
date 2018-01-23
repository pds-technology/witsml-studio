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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.Converters
{
    /// <summary>
    /// Converts multiple boolean values to a <see cref="Visibility"/> emumeration value.
    /// </summary>
    /// <seealso cref="IMultiValueConverter" />
    public class BooleanToVisibilityMultiConverter : IMultiValueConverter
    {
        /// <summary>Converts multiple bool values to a <see cref="T:System.Windows.Visibility" /> enumeration value for Visibility.</summary>
        /// <param name="values">The array of values that the source bindings in the <see cref="T:System.Windows.Data.MultiBinding" /> produces. The value <see cref="F:System.Windows.DependencyProperty.UnsetValue" /> indicates that the source binding has no value to provide for conversion.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use. If true 'AND' else 'OR.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value.If the method returns null, the valid null value is used.A return value of <see cref="T:System.Windows.DependencyProperty" />.<see cref="F:System.Windows.DependencyProperty.UnsetValue" /> indicates that the converter did not produce a value, and that the binding will use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue" /> if it is available, or else will use the default value.A return value of <see cref="T:System.Windows.Data.Binding" />.<see cref="F:System.Windows.Data.Binding.DoNothing" /> indicates that the binding does not transfer the value or use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue" /> or the default value.
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var booleanValues = new List<bool>();
            values.ForEach(v =>
            {
                bool value;
                if (bool.TryParse(v.ToString(), out value))
                    booleanValues.Add(value);
            });

            var isAnd = System.Convert.ToBoolean(parameter ?? true);

            var collapse = isAnd ? booleanValues.Any(v => !v) : !booleanValues.Any(v => v);

            return collapse ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// This method is not implemented
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetTypes">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
