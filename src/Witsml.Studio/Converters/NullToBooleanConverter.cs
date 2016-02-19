﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PDS.Witsml.Studio.Converters
{
    /// <summary>
    /// Converts an object to a Boolean using a null test.
    /// </summary>
    /// <seealso cref="System.Windows.Data.IValueConverter" />
    public class NullToBooleanConverter : IValueConverter
    {
        /// <summary>Initializes a new instance of the <see cref="T:PDS.Witsml.Studio.Converters.NullToBooleanConverter" /> class.</summary>
        public NullToBooleanConverter()
        {
        }

        /// <summary>Converts a Null value to a <see cref="T:System.Boolean" />.</summary>
        /// <returns>
        /// true is returned if <paramref name="value" /> is not null or if the value is a non-empty string; otherwise, false.</returns>
        /// <param name="value">The object that is Null tested to convert.</param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }
            return (string.IsNullOrEmpty(value.ToString()) ? false : true);
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
