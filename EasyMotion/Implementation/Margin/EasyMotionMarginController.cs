using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text.Editor;

namespace EasyMotion.Implementation.Margin
{
    internal sealed class EasyMotionMarginController : IWpfTextViewMargin
    {
        private readonly IEasyMotionUtil _easyMotionUtil;
        private readonly IEasyMotionNavigator _easyMotionNavigator;
        private readonly EasyMotionMargin _control;

        internal EasyMotionMarginController(IEasyMotionUtil easyMotionUtil, IEasyMotionNavigator easyMotionNavigator)
        {
            _easyMotionUtil = easyMotionUtil;
            _easyMotionNavigator = easyMotionNavigator;
            _easyMotionUtil.StateChanged += OnStateChanged;
            _control = new EasyMotionMargin();
            _control.CmdChanged += OnCmdChanged;
            _control.EscapeKey += OnEscapeKey;
            UpdateControl();
        }


        private void OnCmdChanged(object sender, TextChangedEventArgs e)
        {
            var cmd = ((TextBox)e.Source).Text;
            if (cmd.Length == 1)
            {
                _easyMotionUtil.ChangeToLookingForDecision(cmd[0]);
            }
            else if (cmd.Length > 1)
            {
                if (_easyMotionNavigator.NavigateTo(cmd.Substring(1)))
                {
                    _easyMotionUtil.ChangeToDisabled();
                    _control.ClearCmd();
                }
                else
                {
                    SystemSounds.Beep.Play();
                }
            }
        }
        private void OnEscapeKey(object sender, EventArgs e)
        {
            _easyMotionUtil.ChangeToDisabled();
            _control.ClearCmd();
        }

        private void Unsubscribe()
        {
            _easyMotionUtil.StateChanged -= OnStateChanged;
            _control.CmdChanged -= OnCmdChanged;
            _control.EscapeKey -= OnEscapeKey;
        }

        private void UpdateControl()
        {
            switch (_easyMotionUtil.State)
            {
                case EasyMotionState.Disabled:
                    _control.Visibility = Visibility.Collapsed;
                    break;
                case EasyMotionState.LookingForChar:
                    _control.Visibility = Visibility.Visible;
                    _control.StatusLine = "Type the character you want to search for";
                    _control.EditCmd();
                    break;
                case EasyMotionState.LookingForDecision:
                    _control.Visibility = Visibility.Visible;
                    _control.StatusLine = "Type the character at the location you want to jump to";
                    break;
                case EasyMotionState.LookingCharNotFound:
                    _control.Visibility = Visibility.Visible;
                    _control.StatusLine = string.Format("Character '{0}' not found. Type the character you want to search for", _easyMotionUtil.TargetChar);
                    _control.ClearCmd();
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            UpdateControl();
        }

        public bool Enabled
        {
            get { return _easyMotionUtil.State != EasyMotionState.Disabled; }
        }

        public double MarginSize
        {
            get { return 25; }
        }

        public FrameworkElement VisualElement
        {
            get { return _control; }
        }

        public void Dispose()
        {
            Unsubscribe();
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return EasyMotionMarginProvider.Name == marginName ? this : null;
        }
    }
}
