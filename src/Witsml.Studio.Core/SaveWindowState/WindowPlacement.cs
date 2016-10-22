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

//----------------------------------------------------------------------- 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// https://github.com/Microsoft/WPF-Samples/tree/master/Windows/SaveWindowState
//----------------------------------------------------------------------- 

using System;
using System.Runtime.InteropServices;

namespace PDS.Witsml.Studio.Core.SaveWindowState
{
    /// <summary>
    /// WINDOWPLACEMENT stores the position, size, and state of a window
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPlacement
    {
        /// <summary>The length.</summary>
        public int length;
        /// <summary>The flags.</summary>
        public int flags;
        /// <summary>The showCmd.</summary>
        public int showCmd;
        /// <summary>The minPosition.</summary>
        public Point minPosition;
        /// <summary>The maxPosition.</summary>
        public Point maxPosition;
        /// <summary>The normalPosition.</summary>
        public Rect normalPosition;
    }
}