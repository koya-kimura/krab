using System;
using RosettaUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>Visual tokens and small RosettaUI helpers for the dark Preset Editor workspace.</summary>
    public static class PresetEditorTheme
    {
        public static readonly Color Window = Hex(0x111216), Background = Hex(0x15171c), Panel = Hex(0x1d2027), Raised = Hex(0x252a33);
        public static readonly Color Header = Hex(0x1b1e25), Center = Hex(0x111318), Screen = Hex(0x030406), Stroke = Hex(0x3a404b);
        public static readonly Color SoftStroke = Hex(0x30353f), Foreground = Hex(0xe2e5ed), Muted = Hex(0x929cae), Caption = Hex(0x9ca8b8);
        public static readonly Color Selected = Hex(0x314e6a), TabActive = Hex(0x272d36), Accent = Hex(0x86add2), Output = Hex(0xa2d9ca);
        public static readonly Color ButtonFill = Hex(0x30343d), ButtonBorder = Hex(0x464c59), PrimaryFill = Hex(0x355779), PrimaryBorder = Hex(0x5680a4);
        public static readonly Color ConnectedFill = Hex(0x29463e), ConnectedText = Hex(0xb6e7d7), ConnectedBorder = Hex(0x457667), BindLine = Hex(0x567f78);
        public static readonly Color SectionText = Hex(0xbac4d2), SectionLine = Hex(0x363e4a), Icon = Hex(0x9cbad7), TreeText = Hex(0xbec6d1);
        public static readonly Color Graph = Hex(0x10141c), GraphGrid = Hex(0x263140), GraphZero = Hex(0x505d6b), Sum = Hex(0xe6bd89);
        public static readonly Color Adsr = Hex(0x86d5c2), Lfo = Hex(0xb7a5e7), Warning = Hex(0xe6bd89);

        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1);

        /// <summary>Applies inline UI Toolkit style once per bound view.</summary>
        public static T Styled<T>(this T element, Action<IStyle> apply) where T : Element =>
            PresetWorkspaceLayout.BindView(element, view => { apply(view.style); return null; });

        public static void Border(IStyle style, Color color, float top, float right, float bottom, float left)
        {
            style.borderTopWidth = top; style.borderRightWidth = right; style.borderBottomWidth = bottom; style.borderLeftWidth = left;
            style.borderTopColor = color; style.borderRightColor = color; style.borderBottomColor = color; style.borderLeftColor = color;
        }
        public static void Border(IStyle style, Color color, float width = 1) => Border(style, color, width, width, width, width);
        public static void Padding(IStyle style, float vertical, float horizontal)
        {
            style.paddingTop = vertical; style.paddingBottom = vertical; style.paddingLeft = horizontal; style.paddingRight = horizontal;
        }
        public static void Margin(IStyle style, float vertical, float horizontal)
        {
            style.marginTop = vertical; style.marginBottom = vertical; style.marginLeft = horizontal; style.marginRight = horizontal;
        }
        public static void Radius(IStyle style, float radius)
        {
            style.borderTopLeftRadius = radius; style.borderTopRightRadius = radius;
            style.borderBottomLeftRadius = radius; style.borderBottomRightRadius = radius;
        }

        public static LabelElement Text(string text, float size = 12, Color? color = null, bool bold = false) =>
            StyleText(UI.Label(text), size, color, bold);
        public static LabelElement Text(Func<string> text, float size = 12, Color? color = null, bool bold = false) =>
            StyleText(UI.Label(text), size, color, bold);
        private static LabelElement StyleText(LabelElement label, float size, Color? color, bool bold) => label.Styled(style =>
        {
            style.fontSize = size;
            style.color = color ?? Foreground;
            style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            style.minWidth = 0;
            Padding(style, 0, 0);
            Margin(style, 0, 0);
        });

        /// <summary>Small caption above an input, like the reference HTML field labels.</summary>
        public static Element Labeled(string caption, Element field) => UI.Column(
            Text(caption, 10, Muted).Styled(style => style.marginBottom = 2),
            field).SetMinWidth(0);

        public static ButtonElement Button(string label, Action action, bool primary = false) =>
            PresetButtonStyle.Create(label, action, primary ? () => true : (Func<bool>)null);

        /// <summary>Button whose tone follows editor state: primary = call to action, active = currently on.</summary>
        public static ButtonElement Button(string label, Action action, Func<bool> primary, Func<bool> active) =>
            PresetButtonStyle.Create(label, action, primary, active);

        public static ButtonElement Button(Func<string> label, Action action, Func<bool> primary, Func<bool> active) =>
            PresetButtonStyle.Create(UI.Label(label), action, primary, active);

        public static Element PaneTitle(string title, Element trailing = null)
        {
            var row = trailing == null
                ? UI.Row(Text(title.ToUpperInvariant(), 11, SectionText, true))
                : UI.Row(Text(title.ToUpperInvariant(), 11, SectionText, true), UI.Space().SetFlexGrow(1), trailing);
            return row.SetFlexShrink(0).Styled(style =>
            {
                style.height = 37;
                style.alignItems = Align.Center;
                Padding(style, 0, 10);
                Border(style, SoftStroke, 0, 0, 1, 0);
                style.flexShrink = 0;
            });
        }

        public static Element SectionTitle(string title) => Text(title.ToUpperInvariant(), 10, SectionText, true).Styled(style =>
        {
            style.marginTop = 17;
            style.marginBottom = 7;
            style.paddingBottom = 5;
            Border(style, SectionLine, 0, 0, 1, 0);
        });

        public static T Fill<T>(this T element, Color color) where T : Element => element.Styled(style => style.backgroundColor = color);

        /// <summary>Bottom action strip shared by the side panes.</summary>
        public static Element ActionStrip(params Element[] buttons)
        {
            foreach (var button in buttons) button.SetMinWidth(0).Styled(style => style.flexGrow = 1);
            return UI.Row(buttons).SetFlexShrink(0).Styled(style =>
            {
                Border(style, Stroke, 1, 0, 0, 0);
                Padding(style, 8, 6);
                style.flexShrink = 0;
            });
        }
    }
}
