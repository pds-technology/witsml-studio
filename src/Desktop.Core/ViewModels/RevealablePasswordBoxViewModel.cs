using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Caliburn.Micro;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// A revealable PasswordBox class 
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class RevealablePasswordBoxViewModel : Screen
    {
        private PasswordBox _passwordBox;

        private string _password;
        /// <summary>
        /// The password
        /// </summary>
        public string Password
        {
            get { return _password; }
            set
            {
                if (_password == value) return;
                _password = value;
                if (_passwordBox != null && _passwordBox.Password != _password)
                {
                    _passwordBox.Password = _password;
                }
                NotifyOfPropertyChange(() => Password);
            }
        }

        private bool _paswordVisible=false;
        /// <summary>
        /// The pasword visible
        /// </summary>
        public bool PasswordVisible
        {
            get { return _paswordVisible; }
            set
            {
                if (_paswordVisible == value) return;
                _paswordVisible = value;
                if (_passwordBox != null)
                {
                    _passwordBox.IsTabStop = !_paswordVisible;
                }
                NotifyOfPropertyChange(() => PasswordVisible);
            }
        }

        private bool _revealable = true;
        /// <summary>
        /// The Revealable button visibility
        /// </summary>
        public bool Revealable
        {
            get { return _revealable; }
            set
            {
                if (_revealable == value) return;
                _revealable = value;
                NotifyOfPropertyChange(() => Revealable);
            }
        }

        private bool _autoPasswordEnabled = false;
        /// <summary>
        /// The new button visibility
        /// </summary>
        public bool AutoPasswordEnabled
        {
            get { return _autoPasswordEnabled; }
            set
            {
                if (_autoPasswordEnabled == value) return;
                _autoPasswordEnabled = value;
                NotifyOfPropertyChange(() => AutoPasswordEnabled);
            }
        }


        /// <summary>
        /// Called when [password changed].
        /// </summary>
        /// <param name="control">The control.</param>
        public void OnPasswordChanged(PasswordBox control)
        {
            Password = control.Password;
        }

        /// <summary>
        /// Generates the password.
        /// </summary>
        public void GeneratePassword()
        {
            Password = System.Web.Security.Membership.GeneratePassword(8, 2);
            PasswordVisible = true;
        }

        /// <summary>
        /// Called when a view is attached.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="context">The context in which the view appears.</param>
        protected override void OnViewAttached(object view, object context)
        {
            _passwordBox = (PasswordBox)((FrameworkElement)view).FindName("passwordBox");
        }

    }
}
