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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace PDS.WITSMLstudio.Desktop.Core.Runtime
{
    /// <summary>
    /// Helper class to assemble dialog settings.
    /// </summary>
    public static class DialogSettings
    {
        /// <summary>
        /// Creates an empty settings dictionary.
        /// </summary>
        /// <returns></returns>
        public static IDictionary<string, object> Create() { return new Dictionary<string, object>(); }

        /// <summary>
        /// Adds a setting centering the dialog.
        /// </summary>
        /// <param name="settings">The settings dictionary to extend.</param>
        /// <returns>The extended settings dictionary.</returns>
        public static IDictionary<string, object> Centered(this IDictionary<string, object> settings)
        {
            return settings.Merge(new Dictionary<string, object>()
            {
                { "WindowStartupLocation", WindowStartupLocation.CenterOwner }
            });
        }

        /// <summary>
        /// Adds settings to explicitly position the dialog.
        /// </summary>
        /// <param name="settings">The settings dictionary to extend.</param>
        /// <param name="x">The left position.</param>
        /// <param name="y">The top position.</param>
        /// <returns>The extended settings dictionary.</returns>
        public static IDictionary<string, object> AtLocation(this IDictionary<string, object> settings, double x, double y)
        {
            return settings.Merge(new Dictionary<string, object>()
            {
                { "WindowStartupLocation", WindowStartupLocation.Manual },
                { "Left", x },
                { "Top", y }
            });
        }

        /// <summary>
        /// Adds settings to explicitly position the dialog.
        /// </summary>
        /// <param name="settings">The settings dictionary to extend.</param>
        /// <param name="icon">The dialog icon.</param>
        /// <returns>The extended settings dictionary.</returns>
        public static IDictionary<string, object> WithIcon(this IDictionary<string, object> settings, ImageSource icon)
        {
            if (icon == null)
                return settings;

            return settings.Merge(new Dictionary<string, object>()
            {
                { "Icon", icon},
            });
        }

        /// <summary>
        /// Merges the right dictionary into the left one.  The left dictionary is created as needed.  Settings in the left dictionary
        /// are overwritten if they already exist.
        /// </summary>
        /// <param name="left">The left dictionary</param>
        /// <param name="right">The right dictionary</param>
        /// <returns>The merged left dictionary</returns>
        public static IDictionary<string, object> Merge(this IDictionary<string, object> left, IDictionary<string, object> right)
        {
            if (left == null)
                left = Create();

            if (right == null)
                return left;

            foreach (var kvp in right)
            {
                if (left.ContainsKey(kvp.Key))
                {
                    left[kvp.Key] = kvp.Value;
                    continue;
                }

                left.Add(kvp);
            }

            return left;
        }
    }
}
