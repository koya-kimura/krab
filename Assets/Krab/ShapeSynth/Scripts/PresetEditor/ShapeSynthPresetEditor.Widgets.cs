using System;
using System.Collections.Generic;
using System.Reflection;
using RosettaUI;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;
using T = Krab.ShapeSynth.PresetEditor.PresetEditorTheme;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        // RosettaUI's custom menu uses a removed Unity internal interface in this installed version.
        // Keep Rosetta's data binding and use Unity's default runtime dropdown menu instead.
        private DropdownElement Choice(string label, Func<int> read, Action<int> write, IEnumerable<string> options)
        {
            var element = UI.Dropdown(label == null ? null : Caption(label), read, write, options);
            return PresetWorkspaceLayout.BindView(element, view =>
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                for (var type = view.GetType(); type != null; type = type.BaseType)
                {
                    var field = type.GetField("createMenuCallback", flags);
                    if (field == null) continue;

                    field.SetValue(view, null);
                    break;
                }

                return null;
            });
        }

        /// <summary>Unlabeled numeric input; captions are placed above it by the caller.</summary>
        private Element NumberInput(Func<double> read, Action<double> write)
        {
            return UI.Field((LabelElement)null, () => (float)read(), value =>
            {
                write(value);
                Changed();
            }).SetFlexGrow(1).SetMinWidth(0);
        }

        private Element Number(string label, Func<double> read, Action<double> write)
        {
            return T.Labeled(label, NumberInput(read, write)).SetFlexGrow(1).SetFlexBasis(0);
        }

        private Element EnabledButton(string label, Action action, Func<bool> enabled)
        {
            return T.Button(label, action).RegisterUpdateCallback(element => element.Interactable = enabled());
        }

        private static Element Banner(params Element[] children)
        {
            return UI.Row(children).Styled(style =>
            {
                style.alignItems = Align.Center;
                style.backgroundColor = T.Raised;
                T.Border(style, T.Stroke, 1, 0, 0, 0);
                T.Padding(style, 4, 12);
            });
        }

        private static ScrollViewElement VerticalScroll(Element content)
        {
            var scroll = UI.ScrollViewVertical(null, content).SetMinHeight(0).SetFlexShrink(1);
            return PresetWorkspaceLayout.BindView(scroll, view =>
            {
                ((ScrollView)view).horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                return null;
            });
        }

        private static LabelElement Caption(string label)
        {
            var width = Mathf.Clamp(label.Length * 7f + 16, 16, 130);
            return UI.Label(label).SetWidth(width).SetMinWidth(width).SetMaxWidth(width);
        }

        /// <summary>Label whose text color follows state; applied only when the color changes.</summary>
        private static LabelElement StateText(Func<string> text, Func<Color> color, float size)
        {
            Color? applied = null;
            VisualElement bound = null;
            var label = T.Text(text, size);
            label.RegisterUpdateCallback(element =>
            {
                var view = UIToolkitBuilder.Instance.GetUIObj(element);
                var next = color();
                if (view == null || (view == bound && applied == next)) return;

                bound = view;
                applied = next;
                view.style.color = next;
            });
            return label;
        }
    }
}
