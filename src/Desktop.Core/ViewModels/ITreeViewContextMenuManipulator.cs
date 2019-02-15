//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2018.1
//
// Copyright 2019 PDS Americas LLC
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
using System.Windows.Controls;
using PDS.WITSMLstudio.Linq;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// Interface describing a part that is capable of modifying/intercepting/hooking into a treeview context menu OnSelectedItemChanged
    /// </summary>
    public interface ITreeViewContextMenuManipulator
    {
        /// <summary>
        /// Allows the implementor to process the data context of the request and the menu itself
        /// </summary>
        /// <param name="contextMenu">the menu to manipulate</param>
        /// <param name="context"></param>
        /// <param name="selectedResource">the resource/data context for the menu</param>
        void Process(ContextMenu contextMenu, IWitsmlContext context, ResourceViewModel selectedResource);
    }
}
