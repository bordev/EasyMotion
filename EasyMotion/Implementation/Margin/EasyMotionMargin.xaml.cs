using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualStudio.PlatformUI;

namespace EasyMotion.Implementation.Margin
{
    public partial class EasyMotionMargin : UserControl
    {
        public event EventHandler<TextChangedEventArgs> CmdChanged;
        public static readonly DependencyProperty StatusLineProperty = DependencyProperty.Register(
            "StatusLine",
            typeof(string),
            typeof(EasyMotionMargin));

        public string StatusLine
        {
            get { return (string)GetValue(StatusLineProperty); }
            set { SetValue(StatusLineProperty, value); }
        }

        public EasyMotionMargin()
        {
            InitializeComponent();

            statusLine.SetResourceReference(Control.ForegroundProperty, EnvironmentColors.DropDownTextBrushKey);
            statusLine.SetResourceReference(Control.BackgroundProperty, EnvironmentColors.DropDownBackgroundBrushKey);
            statusLine.SetResourceReference(Control.BorderBrushProperty, EnvironmentColors.DropDownBorderBrushKey);
        }

        public void EditCmd()
        {
            cmdLine.Focus();
            ClearCmd();
        }
        public void ClearCmd()
        {
            cmdLine.Text = "";
        }

        private void cmdLine_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CmdChanged != null)
            {
                CmdChanged(this, e);
            }
        }

        private void CmdLine_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                cmdLine.Text = "";
            }
        }
    }
}
