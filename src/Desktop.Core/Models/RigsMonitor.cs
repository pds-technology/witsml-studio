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
using System.Linq;
using System.Threading.Tasks;
using Energistics.Datatypes;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Linq;

namespace PDS.WITSMLstudio.Desktop.Core.Models
{
    /// <summary>
    /// Class to monitor a list of rigs and relate them by name to wells.
    /// </summary>
    public sealed class RigsMonitor
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, HashSet<string>> _rigs = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RigsMonitor" /> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <param name="context">The context.</param>
        public RigsMonitor(IRuntimeService runtime, IWitsmlContext context)
        {
            Context = context;
            Runtime = runtime;

            Task.Run(() => QueryRigsAsync());
        }

        /// <summary>
        /// Occurs when the rigs have changed.
        /// </summary>
        public event EventHandler RigsChanged;

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the context.
        /// </summary>
        public IWitsmlContext Context { get; }

        /// <summary>
        /// Gets the rig names.
        /// </summary>
        public List<string> RigNames
        {
            get
            {
                lock (_lock)
                {
                    return _rigs.Keys.OrderBy(x => x).ToList();
                }
            }
        }

        /// <summary>
        /// Gets the well uids associated with the specified rig name.
        /// </summary>
        /// <param name="rigName">Name of the rig.</param>
        /// <returns>The well uids associated with the rig.</returns>
        public HashSet<string> GetWellUids(string rigName)
        {
            if (string.IsNullOrEmpty(rigName))
                return null;

            lock (_lock)
            {
                HashSet<string> wellUids;

                if (!_rigs.TryGetValue(rigName, out wellUids))
                    return new HashSet<string>();

                return new HashSet<string>(wellUids);
            }
        }

        /// <summary>
        /// Queries the rigs asynchronous.
        /// </summary>
        private void QueryRigsAsync()
        {
            var rigs = Context.GetWellboreObjects(ObjectTypes.Rig, EtpUri.RootUri, false).ToList();

            lock (_lock)
            {
                _rigs.Add(string.Empty, null);
                rigs.ForEach(x =>
                {
                    HashSet<string> wellUids;
                    if (!_rigs.TryGetValue(x.Name, out wellUids))
                    {
                        wellUids = new HashSet<string>();
                        _rigs.Add(x.Name, wellUids);
                    }

                    wellUids.Add(x.UidWell);
                });
            }

            Runtime.InvokeAsync(() => RigsChanged?.Invoke(this, EventArgs.Empty));
        }
    }
}
