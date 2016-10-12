//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
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

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Xml;
using System.Xml.Serialization;

namespace PDS.Witsml.Studio.Core.Runtime
{
    /// <summary>
    /// Helper class that can be used to get or set the a window's current position.
    /// </summary>
    /// <remarks>
    /// https://blogs.msdn.microsoft.com/davidrickard/2010/03/08/saving-window-size-and-location-in-wpf-and-winforms/
    /// </remarks>
    public static class WindowPlacement
    {
        /// <summary>
        /// Provides access to native methods.
        /// </summary>
        private static class SafeNativeMethods
        {
            [DllImport("user32.dll")]
            public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

            [DllImport("user32.dll")]
            public static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);
        }

        private static Encoding encoding = new UTF8Encoding();
        private static XmlSerializer serializer = new XmlSerializer(typeof(WINDOWPLACEMENT));

        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;

        /// <summary>
        /// Sets the placement.
        /// </summary>
        /// <param name="windowHandle">The window handle.</param>
        /// <param name="placementXml">The placement XML.</param>
        public static void SetPlacement(IntPtr windowHandle, string placementXml)
        {
            if (string.IsNullOrEmpty(placementXml))
            {
                return;
            }

            byte[] xmlBytes = encoding.GetBytes(placementXml);

            try
            {
                WINDOWPLACEMENT placement;
                using (MemoryStream memoryStream = new MemoryStream(xmlBytes))
                {
                    placement = (WINDOWPLACEMENT) serializer.Deserialize(memoryStream);
                }

                placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                placement.flags = 0;
                placement.showCmd = (placement.showCmd == SW_SHOWMINIMIZED ? SW_SHOWNORMAL : placement.showCmd);
                SafeNativeMethods.SetWindowPlacement(windowHandle, ref placement);
            }
            catch (InvalidOperationException)
            {
                // Parsing placement XML failed. Fail silently.
            }
        }

        /// <summary>
        /// Gets the placement.
        /// </summary>
        /// <param name="windowHandle">The window handle.</param>
        /// <returns>The placement XML.</returns>
        public static string GetPlacement(IntPtr windowHandle)
        {
            WINDOWPLACEMENT placement;
            SafeNativeMethods.GetWindowPlacement(windowHandle, out placement);
            MemoryStream memoryStream = new MemoryStream();

            using (XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8))
            {
                serializer.Serialize(xmlTextWriter, placement);
                byte[] xmlBytes = memoryStream.ToArray();
                return encoding.GetString(xmlBytes);
            }
        }

        /// <summary>
        /// Sets the placement.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <param name="placementXml">The placement XML.</param>
        public static void SetPlacement(this Window window, string placementXml)
        {
            SetPlacement(new WindowInteropHelper(window).Handle, placementXml);
        }

        /// <summary>
        /// Gets the placement.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns>The placement XML.</returns>
        public static string GetPlacement(this Window window)
        {
            return GetPlacement(new WindowInteropHelper(window).Handle);
        }
    }

    /// <summary>
    /// RECT structure required by WINDOWPLACEMENT structure
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        /// <summary>The left</summary>
        public int Left;
        /// <summary>The top</summary>
        public int Top;
        /// <summary>The right</summary>
        public int Right;
        /// <summary>The bottom</summary>
        public int Bottom;

        /// <summary>
        /// Initializes a new instance of the <see cref="RECT"/> struct.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="top">The top.</param>
        /// <param name="right">The right.</param>
        /// <param name="bottom">The bottom.</param>
        public RECT(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    /// <summary>
    /// POINT structure required by WINDOWPLACEMENT structure
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        /// <summary>The x.</summary>
        public int X;
        /// <summary>The y.</summary>
        public int Y;

        /// <summary>
        /// Initializes a new instance of the <see cref="POINT"/> struct.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// WINDOWPLACEMENT stores the position, size, and state of a window
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        /// <summary>The length</summary>
        public int length;
        /// <summary>The flags</summary>
        public int flags;
        /// <summary>The show command</summary>
        public int showCmd;
        /// <summary>The minimum position</summary>
        public POINT minPosition;
        /// <summary>The maximum position</summary>
        public POINT maxPosition;
        /// <summary>The normal position</summary>
        public RECT normalPosition;
    }
}