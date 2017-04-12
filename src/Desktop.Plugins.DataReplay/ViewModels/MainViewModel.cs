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
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using Caliburn.Micro;
using Microsoft.Win32;
using Newtonsoft.Json;
using PDS.WITSMLstudio.Desktop.Plugins.DataReplay.Properties;
using PDS.WITSMLstudio.Desktop.Plugins.DataReplay.ViewModels.Simulation;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.Plugins.DataReplay.ViewModels
{
    public sealed class MainViewModel : Conductor<IScreen>.Collection.OneActive, IPluginViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        [ImportingConstructor]
        public MainViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = Settings.Default.PluginDisplayName;
        }

        /// <summary>
        /// Gets the display order of the plug-in when loaded by the main application shell
        /// </summary>
        public int DisplayOrder => Settings.Default.PluginDisplayOrder;

        /// <summary>
        /// Gets the sub title to display in the main application title bar.
        /// </summary>
        public string SubTitle => string.Empty;

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; private set; }

        public void NewDataReplay()
        {
            var viewModel = new DataReplayViewModel()
            {
                DisplayName = string.Format("Data Replay {0:yyMMdd-HHmmss}", DateTime.Now)
            };

            ActivateItem(viewModel);
        }

        public void NewSimulation()
        {
            var viewModel = new SimulationViewModel(Runtime)
            {
                DisplayName = string.Format("Simulation {0:yyMMdd-HHmmss}", DateTime.Now)
            };

            ActivateItem(viewModel);
        }

        public void OpenDataReplay()
        {
        }

        public void OpenSimulation()
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Open Simulation Configuration Settings file...",
                Filter = "JSON Files|*.json;*.js|All Files|*.*"
            };

            if (dialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var model = JsonConvert.DeserializeObject<Models.Simulation>(json);

                    var viewModel = new SimulationViewModel(Runtime)
                    {
                        Model = model,
                        DisplayName = model.Name
                    };

                    ActivateItem(viewModel);
                }
                catch (Exception ex)
                {
                    Runtime.ShowError("Error opening file.", ex);
                }
            }
        }

        public void RemoveItem()
        {
            if (ActiveItem != null)
            {
                this.CloseItem(ActiveItem);
            }
        }

        /// <summary>
        /// Update status when activated
        /// </summary>
        protected override void OnActivate()
        {
            base.OnActivate();
            Runtime.Invoke(() =>
            {
                if (Runtime.Shell != null)
                    Runtime.Shell.StatusBarText = "Ready";
            });
        }
    }
}
