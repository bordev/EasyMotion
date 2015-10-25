using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace EasyMotion.Implementation.Margin
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [MarginContainer(PredefinedMarginNames.Bottom)]
    [ContentType("any")]
    [Name(Name)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class EasyMotionMarginProvider : IWpfTextViewMarginProvider
    {
        internal const string Name = "Easy Motion Margin";

        private readonly IEasyMotionUtilProvider _easyMotionUtilProvider;
        private readonly IEasyMotionNavigatorProvider _easyMotionNavigatorProvider;

        [ImportingConstructor]
        internal EasyMotionMarginProvider(IEasyMotionUtilProvider easyMotionUtilProvider, IEasyMotionNavigatorProvider easyMotionNavigatorProvider)
        {
            _easyMotionUtilProvider = easyMotionUtilProvider;
            _easyMotionNavigatorProvider = easyMotionNavigatorProvider;
        }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            var easyMotionUtil = _easyMotionUtilProvider.GetEasyMotionUtil(wpfTextViewHost.TextView);
            var easyMotionNavigator = _easyMotionNavigatorProvider.GetEasyMotionNavigator(wpfTextViewHost.TextView);
            return new EasyMotionMarginController(easyMotionUtil, easyMotionNavigator);
        }
    }
}
