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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Plugins.ObjectInspector.Models
{
    /// <summary>
    /// Encapsulates the collection of Energistics data objects from a particular standard family data schema.
    /// </summary>
    public sealed class FamilyVersionObjectCollection : IReadOnlyCollection<DataObject>
    {
        private readonly List<DataObject> _dataObjects;

        /// <summary>
        /// Initializes a new instance of the <see cref="FamilyVersionObjectCollection"/> class.
        /// </summary>
        /// <param name="familyVersion">The family version.</param>
        /// <exception cref="ArgumentNullException"><paramref name="familyVersion"/> is null.</exception>
        public FamilyVersionObjectCollection(FamilyVersion familyVersion)
        {
            familyVersion.NotNull(nameof(familyVersion));

            FamilyVersion = familyVersion;
            var dataObjectTypes = EnergisticsHelper.GetAllDataObjectTypes(familyVersion.StandardFamily, familyVersion.DataSchemaVersion);
            _dataObjects = dataObjectTypes.Select(dt => new DataObject(dt)).OrderBy(edo => edo.Name).ToList();
        }

        /// <summary>
        /// The standard family and data schema version for this collection.
        /// </summary>
        public FamilyVersion FamilyVersion { get; }

        #region IReadOnlyCollection<EnergisticsDataObject>
        /// <summary>
        /// Get the number of data objects in the collection.
        /// </summary>
        public int Count => _dataObjects.Count;

        /// <summary>
        /// Get the data objects in the collection.
        /// </summary>
        public IEnumerator<DataObject> GetEnumerator()
        {
            return _dataObjects.GetEnumerator();
        }

        /// <summary>
        /// Get the data objects in the collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dataObjects.GetEnumerator();
        }
        #endregion
    }
}
