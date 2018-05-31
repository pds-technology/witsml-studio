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
using PDS.WITSMLstudio.Desktop.Core.Models;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// Provides static helper methods for core view model types.
    /// </summary>
    public static class ViewModelExtensions
    {
        /// <summary>
        /// Finds a resource by URI.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>A <see cref="ResourceViewModel" /> instance.</returns>
        public static ResourceViewModel FindByUri(this IList<ResourceViewModel> resources, string uri)
        {
            return resources.Find(x => x.Resource.Uri == uri);
        }

        /// <summary>
        /// Finds a resource by URI.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <returns>A <see cref="ResourceViewModel" /> instance.</returns>
        public static ResourceViewModel FindByMessageId(this IList<ResourceViewModel> resources, long messageId)
        {
            return resources.Find(x => x.MessageId == messageId);
        }

        /// <summary>
        /// Finds the selected resource.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <returns>A <see cref="ResourceViewModel" /> instance.</returns>
        public static ResourceViewModel FindSelected(this IList<ResourceViewModel> resources)
        {
            return resources.Find(x => x.IsSelected);
        }

        /// <summary>
        /// Finds the selected resource.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <param name="lockObject">The lock object.</param>
        /// <returns>A <see cref="ResourceViewModel" /> instance.</returns>
        public static ResourceViewModel FindSelected(this IList<ResourceViewModel> resources, object lockObject)
        {
            lock (lockObject)
                return resources.FindSelected();
        }

        /// <summary>
        /// Finds the selected resource, using global lock for  synchronization.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <returns>A <see cref="ResourceViewModel" /> instance.</returns>
        public static ResourceViewModel FindSelectedSynchronized(this IList<ResourceViewModel> resources)
        {
            return resources.ExecuteWithReadLock(resources.FindSelected);
        }

        /// <summary>
        /// Finds a resource by evaluating the specified predicate on each item in the collection.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>A <see cref="ResourceViewModel" /> instance.</returns>
        public static ResourceViewModel Find(this IList<ResourceViewModel> resources, Func<ResourceViewModel, bool> predicate)
        {
            foreach (var resource in resources)
            {
                if (predicate(resource))
                    return resource;

                var found = Find(resource.Children, predicate);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds all resources by evaluating the specified predicate on each item in the collection.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>A collection of <see cref="ResourceViewModel" /> instances.</returns>
        public static IEnumerable<ResourceViewModel> FindAll(this IList<ResourceViewModel> resources, Func<ResourceViewModel, bool> predicate)
        {
            foreach (var resource in resources)
            {
                if (predicate(resource))
                    yield return resource;

                foreach (var child in resource.Children.FindAll(predicate))
                    yield return child;
            }
        }

        /// <summary>
        /// Finds all of the checked resources.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <returns>A collection of <see cref="ResourceViewModel" /> instances.</returns>
        public static IEnumerable<ResourceViewModel> FindChecked(this IList<ResourceViewModel> resources)
        {
            return resources.FindAll(x => x.IsChecked);
        }
    }
}
