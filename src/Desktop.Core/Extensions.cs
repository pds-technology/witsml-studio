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
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using Energistics.DataAccess;
using log4net.Appender;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Logs;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

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
        public static T FindParent<T>(this System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
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
        /// <param name="toOffset">The time span to convert the timestamp to if indexes are time.</param>
        /// <returns>LogCurveItem</returns>
        public static LogCurveItem ToLogCurveItem(this Witsml131.ComponentSchemas.LogCurveInfo logCurveInfo, TimeSpan toOffset)
        {
            var startIndex = logCurveInfo.GetStartIndex();
            var endIndex = logCurveInfo.GetEndIndex();

            return new LogCurveItem(
                logCurveInfo.Mnemonic, 
                logCurveInfo.CurveDescription,
                startIndex is Timestamp
                    ? ((Timestamp?)startIndex).ToDisplayDateTime(toOffset)
                    : startIndex?.ToString() ?? string.Empty,
                endIndex is Timestamp
                    ? ((Timestamp?)endIndex).ToDisplayDateTime(toOffset)
                    : endIndex?.ToString() ?? string.Empty,
                logCurveInfo.Unit,
                logCurveInfo.TypeLogData?.ToString("F"),
                logCurveInfo.NullValue);
        }

        /// <summary>
        /// Converts a list of 131 LogCurveInfos to a list of LogCurveItems.
        /// </summary>
        /// <param name="logCurveInfos">The log curve infos.</param>
        /// <param name="toOffset">The time span to convert the timestamp to if indexes are time.</param>
        /// <returns>A list of LogCurveItems</returns>
        public static List<LogCurveItem> ToLogCurveItemList(this List<Witsml131.ComponentSchemas.LogCurveInfo> logCurveInfos, TimeSpan toOffset)
        {
            return logCurveInfos?.Select(l => l.ToLogCurveItem(toOffset)).ToList() ?? new List<LogCurveItem>();
        }

        /// <summary>
        /// Converts a 141 LogCurveInfo to a LogCurveItem
        /// </summary>
        /// <param name="logCurveInfo">The log curve information.</param>
        /// <param name="toOffset">The time span to convert the timestamp to if indexes are time.</param>
        /// <returns>LogCurveItem</returns>
        public static LogCurveItem ToLogCurveItem(this Witsml141.ComponentSchemas.LogCurveInfo logCurveInfo, TimeSpan toOffset)
        {
            var startIndex = logCurveInfo.GetStartIndex();
            var endIndex = logCurveInfo.GetEndIndex();

            return new LogCurveItem(
                logCurveInfo.Mnemonic?.Value,
                logCurveInfo.CurveDescription,
                startIndex is Timestamp
                    ? ((Timestamp?)startIndex).ToDisplayDateTime(toOffset)
                    : startIndex?.ToString() ?? string.Empty,
                endIndex is Timestamp
                    ? ((Timestamp?)endIndex).ToDisplayDateTime(toOffset)
                    : endIndex?.ToString() ?? string.Empty,
                logCurveInfo.Unit,
                logCurveInfo.TypeLogData?.ToString("F"),
                logCurveInfo.NullValue);
        }

        /// <summary>
        /// Converts a list of 141 LogCurveInfos to a list of LogCurveItems.
        /// </summary>
        /// <param name="logCurveInfos">The log curve infos.</param>
        /// <param name="toOffset">The time span to convert the timestamp to if indexes are time.</param>
        /// <returns>A list of LogCurveItems</returns>
        public static List<LogCurveItem> ToLogCurveItemList(this List<Witsml141.ComponentSchemas.LogCurveInfo> logCurveInfos, TimeSpan toOffset)
        {
            return logCurveInfos?.Select(l => l.ToLogCurveItem(toOffset)).ToList() ?? new List<LogCurveItem>();
        }

        /// <summary>
        /// Gets the data object represented by the resource view model.
        /// </summary>
        /// <param name="resourceViewModel">The resource view model.</param>
        /// <returns>The data object instance.</returns>
        public static IDataObject GetDataObject(this ResourceViewModel resourceViewModel)
        {
            var dataContext = resourceViewModel.DataContext as DataObjectWrapper;
            return dataContext?.Instance as IDataObject;
        }

        /// <summary>
        /// Gets the well object represented by the resource view model.
        /// </summary>
        /// <param name="resourceViewModel">The resource view model.</param>
        /// <returns>The well object instance.</returns>
        public static IWellObject GetWellObject(this ResourceViewModel resourceViewModel)
        {
            return resourceViewModel.GetDataObject() as IWellObject;
        }

        /// <summary>
        /// Gets the wellbore object represented by the resource view model.
        /// </summary>
        /// <param name="resourceViewModel">The resource view model.</param>
        /// <returns>The wellbore object instance.</returns>
        public static IWellboreObject GetWellboreObject(this ResourceViewModel resourceViewModel)
        {
            return resourceViewModel.GetDataObject() as IWellboreObject;
        }

        /// <summary>
        /// Opens the log file.
        /// </summary>
        /// <param name="log">The log.</param>
        public static void OpenLogFile(this log4net.ILog log)
        {
            if (log.Logger.Repository.GetAppenders().Length == 0)
                return;

            var appender = log.Logger.Repository.GetAppenders()[0] as FileAppender;
            if (appender == null) return;

            Process.Start(appender.File);
        }
    }
}
