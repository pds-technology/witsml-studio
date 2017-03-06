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

using System;
using System.ComponentModel;
using System.Windows;
using PDS.Witsml.Studio.Core.ViewModels;

namespace PDS.Witsml.Studio.Core.Views
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShellView"/> class.
        /// </summary>
        public ShellView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Raises the <see cref="Window.SourceInitialized" /> event.
        /// </summary>
        /// <param name="e">An <see cref="EventArgs" /> that contains the event data.</param>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var shellViewModel = DataContext as IShellViewModel;
            shellViewModel?.RestoreWindowPlacement(this);
        }

        /// <summary>
        /// Handles the OnClosing event of the ShellView window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
        private void ShellView_OnClosing(object sender, CancelEventArgs e)
        {
            var shellViewModel = DataContext as IShellViewModel;
            shellViewModel?.SaveWindowPlacement(this);
        }
    }
}
