using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows;

namespace EasyMotion.Implementation.Adornment
{
    internal sealed class EasyMotionAdornmentController : IEasyMotionNavigator
    {
        private static string[] _navigationKeysLong;
        private static string[] _navigationKeysShort;

        private readonly IEasyMotionUtil _easyMotionUtil;
        private readonly IWpfTextView _wpfTextView;
        private readonly IEditorFormatMap _editorFormatMap;
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly Dictionary<string, SnapshotPoint> _navigateMap = new Dictionary<string, SnapshotPoint>();
        private readonly object _tag = new object();
        private IAdornmentLayer _adornmentLayer;

        internal EasyMotionAdornmentController(IEasyMotionUtil easyMotionUtil, IWpfTextView wpfTextview, IEditorFormatMap editorFormatMap, IClassificationFormatMap classificationFormatMap)
        {
            _easyMotionUtil = easyMotionUtil;
            _wpfTextView = wpfTextview;
            _editorFormatMap = editorFormatMap;
            _classificationFormatMap = classificationFormatMap;
            if (_navigationKeysLong == null)
            {
                _navigationKeysLong = CreateNavigationKeysLong();
            }
            if (_navigationKeysShort == null)
            {
                _navigationKeysShort = CreateNavigationKeysShort();
            }
        }

        private static string[] CreateNavigationKeysShort()
        {
            return "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".Select(c => c.ToString()).ToArray();
        }

        private static string[] CreateNavigationKeysLong()
        {
            var r1 = "qwertyuiop";
            var r2 = "asdfghjkl";
            var r3 = "zxcvbnm";
            var combis = new List<string>();
            combis.AddRange("abcdefghijklmnopqrstuvwxyz".Select(c => c.ToString() + c.ToString()));
            Action<string> hkeys = (a) =>
            {
                for (int i = 0; i < a.Length - 1; i++)
                {
                    combis.Add(a[i].ToString() + a[i + 1]);
                    combis.Add(a[i + 1].ToString() + a[i]);
                }
            };
            Action<string, string> vkeys = (a, b) =>
            {
                for (int i = 0; i < a.Length; i++)
                {
                    if ((i >= b.Length) || (i >= a.Length)) continue;
                    combis.Add(a[i].ToString() + b[i]);
                    combis.Add(b[i].ToString() + a[i]);
                }
            };
            hkeys(r1);
            hkeys(r2);
            hkeys(r3);
            vkeys(r1, r2);
            vkeys(r2, r3);
            var keypairs =
                "hjkl yuiop bnm gfdsa trewq vcxz".Where(c => !char.IsWhiteSpace(c))
                    .SelectMany(c => combis.Where(s => s.StartsWith(c.ToString()))).ToArray();

            return keypairs.ToArray();
        }

        internal void SetAdornmentLayer(IAdornmentLayer adornmentLayer)
        {
            Debug.Assert(_adornmentLayer == null);
            _adornmentLayer = adornmentLayer;
            Subscribe();
        }

        private void Subscribe()
        {
            _easyMotionUtil.StateChanged += OnStateChanged;
            _wpfTextView.LayoutChanged += OnLayoutChanged;
        }

        private void Unsubscribe()
        {
            _easyMotionUtil.StateChanged -= OnStateChanged;
            _wpfTextView.LayoutChanged -= OnLayoutChanged;
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            if (_easyMotionUtil.State == EasyMotionState.LookingForDecision)
            {
                AddAdornments();
            }
            else
            {
                _adornmentLayer.RemoveAdornmentsByTag(_tag);
            }
        }

        private void OnLayoutChanged(object sender, EventArgs e)
        {
            switch (_easyMotionUtil.State)
            {
                case EasyMotionState.LookingCharNotFound:
                    _easyMotionUtil.ChangeToLookingForDecision(_easyMotionUtil.TargetChar);
                    break;

                case EasyMotionState.LookingForDecision:
                    ResetAdornments();
                    break;
            }
        }

        private void ResetAdornments()
        {
            _adornmentLayer.RemoveAdornmentsByTag(_tag);
            AddAdornments();
        }

        private void AddAdornments()
        {
            Debug.Assert(_easyMotionUtil.State == EasyMotionState.LookingForDecision);

            if (_wpfTextView.InLayout)
            {
                return;
            }

            _navigateMap.Clear();
            var textViewLines = _wpfTextView.TextViewLines;
            var startPoint = textViewLines.FirstVisibleLine.Start;
            var endPoint = textViewLines.LastVisibleLine.End;
            var snapshot = startPoint.Snapshot;

            var count = 0;
            for (int i = startPoint.Position; i < endPoint.Position; i++)
            {
                var point = new SnapshotPoint(snapshot, i);
                if (char.ToLower(point.GetChar()) == char.ToLower(_easyMotionUtil.TargetChar))
                {
                    count++;
                }
            }
            var navigationKeys = _navigationKeysShort;
            if (count > _navigationKeysShort.Length)
            {
                navigationKeys = _navigationKeysLong;
            }
            int navigateIndex = 0;
            for (int i = startPoint.Position; i < endPoint.Position; i++)
            {
                var point = new SnapshotPoint(snapshot, i);

                if (char.ToLower(point.GetChar()) == char.ToLower(_easyMotionUtil.TargetChar) && navigateIndex < navigationKeys.Length)
                {
                    var key = navigationKeys[navigateIndex];
                    navigateIndex++;
                    AddNavigateToPoint(textViewLines, point, key);
                }
            }

            if (navigateIndex == 0)
            {
                _easyMotionUtil.ChangeToLookingCharNotFound();
            }
        }

        private void AddNavigateToPoint(IWpfTextViewLineCollection textViewLines, SnapshotPoint point, string key)
        {
            _navigateMap[key] = point;

            var resourceDictionary = _editorFormatMap.GetProperties(EasyMotionNavigateFormatDefinition.Name);

            var span = new SnapshotSpan(point, key.Length);
            var bounds = textViewLines.GetCharacterBounds(point);

            var label = new Label();
            label.Content = key;
            label.FontFamily = _classificationFormatMap.DefaultTextProperties.Typeface.FontFamily;
            label.FontSize = 10.0;
            label.Foreground = resourceDictionary.GetForegroundBrush(EasyMotionNavigateFormatDefinition.DefaultForegroundBrush);
            label.Background = resourceDictionary.GetBackgroundBrush(EasyMotionNavigateFormatDefinition.DefaultBackgroundBrush);
            //            textBox.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));


            Canvas.SetTop(label, bounds.TextTop);
            Canvas.SetLeft(label, bounds.Left);
            Canvas.SetZIndex(label, 10);

            _adornmentLayer.AddAdornment(span, _tag, label);
        }

        public bool NavigateTo(string key)
        {
            SnapshotPoint point;
            if (!_navigateMap.TryGetValue(key, out point))
            {
                return false;
            }

            if (point.Snapshot != _wpfTextView.TextSnapshot)
            {
                return false;
            }

            _wpfTextView.VisualElement.Focus();
            _wpfTextView.Caret.MoveTo(point);
            return true;
        }
    }
}
