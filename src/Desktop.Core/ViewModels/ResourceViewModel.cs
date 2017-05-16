//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2017.1
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

using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Caliburn.Micro;
using Energistics.Datatypes.Object;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// Represents the user interface elements that will be displayed in the tree view.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    public class ResourceViewModel : PropertyChangedBase
    {
        /// <summary>The placeholder resource.</summary>
        public static readonly ResourceViewModel Placeholder = new ResourceViewModel(null, new Resource { Name = "loading..." })
        {
            _isPlaceholder = true
        };

        /// <summary>The no data resource.</summary>
        public static readonly ResourceViewModel NoData = new ResourceViewModel(null, new Resource { Name = "(no data)" })
        {
            _isPlaceholder = true
        };

        private bool _isPlaceholder;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceViewModel" /> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="dataContext">The data context.</param>
        public ResourceViewModel(IRuntimeService runtime, Resource resource, object dataContext = null)
        {
            Runtime = runtime;
            Resource = resource;
            Children = new BindableCollection<ResourceViewModel>();
            Indicator = new IndicatorViewModel();
            DataContext = dataContext;
            IsVisible = true;

            if (resource.HasChildren != 0)
            {
                Children.Add(Placeholder);
            }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the resource.
        /// </summary>
        /// <value>The resource.</value>
        public Resource Resource { get; }

        /// <summary>
        /// Gets the data context.
        /// </summary>
        /// <value>The data context.</value>
        public object DataContext { get; }

        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        /// <value>The message identifier.</value>
        public long? MessageId { get; private set; }

        /// <summary>
        /// Gets the collection of child resources.
        /// </summary>
        /// <value>The children.</value>
        public BindableCollection<ResourceViewModel> Children { get; }

        /// <summary>
        /// Gets the Indicator
        /// </summary>
        public IndicatorViewModel Indicator { get; set; }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName => Resource.HasChildren > 0 ? $"{Resource.Name} ({Resource.HasChildren})" : Resource.Name;

        /// <summary>
        /// Gets a value indicating whether this instance has a placeholder element.
        /// </summary>
        /// <value><c>true</c> if this instance has placeholder; otherwise, <c>false</c>.</value>
        public bool HasPlaceholder => Children.Count == 1 && Children[0]._isPlaceholder;

        /// <summary>
        /// Gets or sets the action method used to load child resources.
        /// </summary>
        /// <value>The load children.</value>
        public Func<string, long> LoadChildren { get; set; }

        private ResourceViewModel _parent;
        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        public ResourceViewModel Parent
        {
            get { return _parent; }
            set
            {
                if (!ReferenceEquals(_parent, value))
                {
                    _parent = value;
                    NotifyOfPropertyChange(() => Parent);
                }
            }
        }

        private bool _isExpanded;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is expanded.
        /// </summary>
        /// <value><c>true</c> if this instance is expanded; otherwise, <c>false</c>.</value>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyOfPropertyChange(() => IsExpanded);
                }

                if (HasPlaceholder && value)
                {
                    ClearAndLoadChildren();
                }
            }
        }

        private bool _isSelected;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is selected.
        /// </summary>
        /// <value><c>true</c> if this instance is selected; otherwise, <c>false</c>.</value>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    NotifyOfPropertyChange(() => IsSelected);
                }
            }
        }

        private bool _isVisible;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is visible.
        /// </summary>
        /// <value><c>true</c> if this instance is visible; otherwise, <c>false</c>.</value>
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible == value)
                    return;

                _isVisible = value;
                NotifyOfPropertyChange(() => IsVisible);
            }
        }

        /// <summary>
        /// Removes the children and loads children.
        /// </summary>
        public void ClearAndLoadChildren()
        {
            Runtime?.Invoke(() => Children.Clear(), DispatcherPriority.Send);
            Task.Run(() => MessageId = LoadChildren(Resource.Uri));
        }
    }
}
