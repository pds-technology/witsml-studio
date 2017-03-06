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

//----------------------------------------------------------------------- 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// https://github.com/Microsoft/WPF-Samples/tree/master/Windows/SaveWindowState
//----------------------------------------------------------------------- 

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Energistics.DataAccess;

namespace PDS.Witsml.Studio.Core.SaveWindowState
{
    /// <summary>
    /// Provides static helper methods that can be used to save and restore <see cref="Window"/> state.
    /// </summary>
    public static class WindowExtensions
    {
        private const int SwShowNormal = 1;
        private const int SwShowMinimized = 2;

        /// <summary>
        /// Provides access to native methods.
        /// </summary>
        private static class SafeNativeMethods
        {
            [DllImport("user32.dll")]
            public static extern bool GetWindowPlacement(IntPtr hWnd, out WindowPlacement lpwndpl);

            [DllImport("user32.dll")]
            public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WindowPlacement lpwndpl);
        }

        /// <summary>
        /// Gets the placement.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns>The placement XML.</returns>
        public static string GetPlacement(this Window window)
        {
            try
            {
                // Persist window placement details to application settings
                WindowPlacement wp;
                var hwnd = new WindowInteropHelper(window).Handle;
                SafeNativeMethods.GetWindowPlacement(hwnd, out wp);
                return EnergisticsConverter.ObjectToXml(wp);
            }
            catch
            {
                // Ignore
                return null;
            }
        }

        /// <summary>
        /// Sets the placement.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <param name="placementXml">The placement XML.</param>
        public static void SetPlacement(this Window window, string placementXml)
        {
            // Ignore missing placement xml
            if (string.IsNullOrWhiteSpace(placementXml)) return;

            try
            {
                // Load window placement details for previous application session from application settings
                // Note - if window was closed on a monitor that is now disconnected from the computer,
                //        SetWindowPlacement will place the window onto a visible monitor.
                var wp = EnergisticsConverter.XmlToObject<WindowPlacement>(placementXml);
                wp.length = Marshal.SizeOf(typeof(WindowPlacement));
                wp.flags = 0;
                wp.showCmd = wp.showCmd == SwShowMinimized ? SwShowNormal : wp.showCmd;

                var hwnd = new WindowInteropHelper(window).Handle;
                SafeNativeMethods.SetWindowPlacement(hwnd, ref wp);
            }
            catch
            {
                // Ignore
            }
        }
    }
}
