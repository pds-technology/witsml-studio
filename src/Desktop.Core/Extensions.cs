//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2017.1
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

using System.Windows;
using System.Windows.Media;

namespace PDS.Witsml.Studio.Core
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
    }
}
