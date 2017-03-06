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

namespace PDS.Witsml.Studio.Core.SaveWindowState
{
    /// <summary>
    /// POINT structure required by WINDOWPLACEMENT structure.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        /// <summary>The x.</summary>
        public int X;
        /// <summary>The y.</summary>
        public int Y;

        /// <summary>
        /// Initializes a new instance of the <see cref="Point"/> struct.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
