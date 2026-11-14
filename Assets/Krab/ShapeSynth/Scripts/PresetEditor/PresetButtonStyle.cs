using System;
using RosettaUI;
using UnityEngine;
using UnityEngine.UIElements;
using T = Krab.ShapeSynth.PresetEditor.PresetEditorTheme;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>
    /// Visible button states for the workspace. Inline colors override UI Toolkit's :hover/:active USS,
    /// so hover, press, "on" and disabled looks are applied here and only when the state changes.
    /// </summary>
    public static class PresetButtonStyle
    {
        public static readonly Color HoverFill = T.Hex(0x3c4653);
        public static readonly Color PressedFill = T.Hex(0x1f2630);
        public static readonly Color PressedBorder = T.Hex(0x86add2);
        public static readonly Color PrimaryHoverFill = T.Hex(0x41678d);
        public static readonly Color ActiveFill = T.Hex(0x2e6a55);
        public static readonly Color ActiveHoverFill = T.Hex(0x367a62);
        public static readonly Color ActiveBorder = T.Hex(0x6cc9a6);

        /// <param name="primary">Call-to-action tone, e.g. Save while there are unsaved edits.</param>
        /// <param name="active">"On" state, e.g. Preview while a preview is running or Open while its browser is shown.</param>
        public static ButtonElement Create(
            LabelElement label,
            Action action,
            Func<bool> primary = null,
            Func<bool> active = null)
        {
            var button = UI.Button(label, action);
            var hover = false;
            var pressed = false;
            VisualElement bound = null;
            Look? applied = null;

            void Apply()
            {
                if (bound == null) return;
                var enabled = button.Interactable && bound.enabledInHierarchy;
                var look = !enabled ? Look.Disabled
                    : pressed ? Look.Pressed
                    : active != null && active() ? (hover ? Look.ActiveHover : Look.Active)
                    : primary != null && primary() ? (hover ? Look.PrimaryHover : Look.Primary)
                    : hover ? Look.Hover : Look.Normal;
                if (applied == look) return;
                applied = look;
                Paint(bound, look);
            }

            button.RegisterUpdateCallback(_ => Apply());
            return PresetWorkspaceLayout.BindView(button, view =>
            {
                bound = view;
                applied = null;
                hover = false;
                pressed = false;
                T.Radius(view.style, 3);
                T.Padding(view.style, 4, 9);
                T.Margin(view.style, 0, 2);
                view.style.fontSize = 12;
                view.style.minHeight = 24;
                view.style.height = 26;
                view.style.alignSelf = Align.Center;
                view.style.flexGrow = 0;

                void Enter(PointerEnterEvent _)
                {
                    hover = true;
                    Apply();
                }

                void Leave(PointerLeaveEvent _)
                {
                    hover = false;
                    pressed = false;
                    Apply();
                }

                void Down(PointerDownEvent evt)
                {
                    if (evt.button != 0) return;

                    pressed = true;
                    Apply();
                }

                void Up(PointerUpEvent _)
                {
                    pressed = false;
                    Apply();
                }

                void CaptureOut(PointerCaptureOutEvent _)
                {
                    pressed = false;
                    Apply();
                }

                view.RegisterCallback<PointerEnterEvent>(Enter);
                view.RegisterCallback<PointerLeaveEvent>(Leave);
                view.RegisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
                view.RegisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
                view.RegisterCallback<PointerCaptureOutEvent>(CaptureOut);
                Apply();
                return () =>
                {
                    view.UnregisterCallback<PointerEnterEvent>(Enter);
                    view.UnregisterCallback<PointerLeaveEvent>(Leave);
                    view.UnregisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
                    view.UnregisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
                    view.UnregisterCallback<PointerCaptureOutEvent>(CaptureOut);
                    if (bound == view) bound = null;
                };
            });
        }

        public static void Paint(VisualElement view, Look look)
        {
            Color fill;
            Color border;
            var text = T.Foreground;
            switch (look)
            {
                case Look.Hover:
                    fill = HoverFill;
                    border = T.Hex(0x5a6273);
                    break;
                case Look.Pressed:
                    fill = PressedFill;
                    border = PressedBorder;
                    break;
                case Look.Primary:
                    fill = T.PrimaryFill;
                    border = T.PrimaryBorder;
                    text = Color.white;
                    break;
                case Look.PrimaryHover:
                    fill = PrimaryHoverFill;
                    border = T.Hex(0x77a2c9);
                    text = Color.white;
                    break;
                case Look.Active:
                    fill = ActiveFill;
                    border = ActiveBorder;
                    text = Color.white;
                    break;
                case Look.ActiveHover:
                    fill = ActiveHoverFill;
                    border = ActiveBorder;
                    text = Color.white;
                    break;
                case Look.Disabled:
                    fill = T.Hex(0x23262d);
                    border = T.Hex(0x30343c);
                    text = T.Hex(0x5f6878);
                    break;
                default:
                    fill = T.ButtonFill;
                    border = T.ButtonBorder;
                    break;
            }

            view.style.backgroundColor = fill;
            T.Border(view.style, border);
            view.style.color = text;
            // Rosetta may render the caption as a child Label; keep it in step with the button.
            view.Query<Label>().ForEach(label => label.style.color = text);
            // UI Toolkit's own disabled opacity stacks with the dimmed colors; keep the text legible.
            view.style.opacity = look == Look.Disabled ? 0.85f : 1f;
        }

        /// <summary>Row highlight for list items: selected wins, otherwise a subtle hover tint.</summary>
        public static TElement Selectable<TElement>(TElement row, Func<bool> selected) where TElement : Element
        {
            var hover = false;
            VisualElement bound = null;
            int? applied = null;
            void Apply()
            {
                if (bound == null) return;
                var state = selected() ? 2 : hover ? 1 : 0;
                if (applied == state) return;
                applied = state;
                if (state == 2) bound.style.backgroundColor = T.Selected;
                else if (state == 1) bound.style.backgroundColor = T.Hex(0x2a303b);
                else bound.style.backgroundColor = Color.clear;
            }
            row.RegisterUpdateCallback(_ => Apply());
            return PresetWorkspaceLayout.BindView(row, view =>
            {
                bound = view;
                applied = null;

                void Enter(PointerEnterEvent _)
                {
                    hover = true;
                    Apply();
                }

                void Leave(PointerLeaveEvent _)
                {
                    hover = false;
                    Apply();
                }

                view.RegisterCallback<PointerEnterEvent>(Enter);
                view.RegisterCallback<PointerLeaveEvent>(Leave);
                Apply();
                return () =>
                {
                    view.UnregisterCallback<PointerEnterEvent>(Enter);
                    view.UnregisterCallback<PointerLeaveEvent>(Leave);
                    if (bound == view) bound = null;
                };
            });
        }

        public enum Look
        {
            Normal,
            Hover,
            Pressed,
            Primary,
            PrimaryHover,
            Active,
            ActiveHover,
            Disabled,
        }
    }
}
