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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Caliburn.Micro;

namespace PDS.WITSMLstudio.Desktop
{
    /// <summary>
    /// Exposes protected bootstrapper methods for unit testing.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Bootstrapper" />
    public class BootstrapperHarness : Bootstrapper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BootstrapperHarness"/> class.
        /// </summary>
        public BootstrapperHarness() : base(false)
        {
            AssemblySource.Instance.Clear();
        }

        /// <summary>
        /// Exposes the SelectAssemblies method.
        /// </summary>
        /// <returns>An IEnumerable of Assemblies</returns>
        public IEnumerable<Assembly> CallSelectAssemblies()
        {
            return SelectAssemblies();
        }

        /// <summary>
        /// Exposes the GetInstance method.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>The instance for the given objectType</returns>
        public object CallGetInstance(Type objectType)
        {
            return GetInstance(objectType, null);
        }

        /// <summary>
        /// Exposes the OnStartup method.
        /// </summary>
        public void CallOnStartup()
        {
            OnStartup(null, null);
        }

        /// <summary>
        /// Overrides the SelectAssemblies() to include the assembly for the unit tests.
        /// </summary>
        /// <returns>
        /// An IEnumerable of the Assemblies found in the Plugins folder and the unit test assembly.
        /// </returns>
        protected override IEnumerable<Assembly> SelectAssemblies()
        {
            return base.SelectAssemblies()
                .Union(new[] { GetType().Assembly });
        }
    }
}
