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
using System.Linq;
using System.Windows;
using System.Windows.Media;
using PDS.WITSMLstudio.Data.Logs;
using PDS.WITSMLstudio.Desktop.Core.Models;

namespace PDS.WITSMLstudio.Desktop.Core
{
    /// <summary>
    /// Provides helper methods for WPF controls.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Finds the parent of the child element having the specified type.
        /// </summary>
        /// <typeparam name="T">The parent type.</typeparam>
        /// <param name="child">The child.</param>
        /// <returns>The first matching parent element of the specified type.</returns>
        public static T FindParent<T>(this DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;

            var parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            return parentObject as T
                ?? FindParent<T>(parentObject);
        }

        /// <summary>
        /// Converts a 131 LogCurveInfo to a LogCurveItem
        /// </summary>
        /// <param name="logCurveInfo">The log curve information.</param>
        /// <returns>LogCurveItem</returns>
        public static LogCurveItem ToLogCurveItem(this Energistics.DataAccess.WITSML131.ComponentSchemas.LogCurveInfo logCurveInfo)
        {
            return new LogCurveItem(
                logCurveInfo.Mnemonic, 
                logCurveInfo.CurveDescription, 
                logCurveInfo.GetStartIndex()?.ToString() ?? string.Empty, 
                logCurveInfo.GetEndIndex()?.ToString() ?? string.Empty, 
                logCurveInfo.Unit);
        }

        /// <summary>
        /// Converts a list of 131 LogCurveInfos to a list of LogCurveItems.
        /// </summary>
        /// <param name="logCurveInfos">The log curve infos.</param>
        /// <returns>A list of LogCurveItems</returns>
        public static List<LogCurveItem> ToLogCurveItemList(this List<Energistics.DataAccess.WITSML131.ComponentSchemas.LogCurveInfo> logCurveInfos)
        {
            return logCurveInfos?.Select(l => l.ToLogCurveItem()).ToList() ?? new List<LogCurveItem>();
        }

        /// <summary>
        /// Converts a 141 LogCurveInfo to a LogCurveItem
        /// </summary>
        /// <param name="logCurveInfo">The log curve information.</param>
        /// <returns>LogCurveItem</returns>
        public static LogCurveItem ToLogCurveItem(this Energistics.DataAccess.WITSML141.ComponentSchemas.LogCurveInfo logCurveInfo)
        {
            return new LogCurveItem(
                logCurveInfo.Mnemonic?.Value,
                logCurveInfo.CurveDescription,
                logCurveInfo.GetStartIndex()?.ToString() ?? string.Empty,
                logCurveInfo.GetEndIndex()?.ToString() ?? string.Empty,
                logCurveInfo.Unit);
        }

        /// <summary>
        /// Converts a list of 141 LogCurveInfos to a list of LogCurveItems.
        /// </summary>
        /// <param name="logCurveInfos">The log curve infos.</param>
        /// <returns>A list of LogCurveItems</returns>
        public static List<LogCurveItem> ToLogCurveItemList(this List<Energistics.DataAccess.WITSML141.ComponentSchemas.LogCurveInfo> logCurveInfos)
        {
            return logCurveInfos?.Select(l => l.ToLogCurveItem()).ToList() ?? new List<LogCurveItem>();
        }
    }
}
