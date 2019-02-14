using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
