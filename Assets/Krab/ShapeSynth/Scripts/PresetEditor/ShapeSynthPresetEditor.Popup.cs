using System;
using RosettaUI;
using RosettaUI.UIToolkit;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>
    /// Windows opened from a button (Bind picker, Gallery details) and button text that is left-aligned and cut with
    /// an ellipsis instead of overflowing.
    /// </summary>
    public sealed partial class ShapeSynthPresetEditor
    {
        /// <summary>
        /// Builds the window once on the RosettaUI root, which keeps it updated without a launcher in the Settings
        /// window; it opens only from a button, not with the root.
        /// </summary>
        private static WindowElement PopupWindow(ref WindowElement window, string title, Func<Element> content)
        {
            if (window != null) return window;

            var root = FindAnyObjectByType<RosettaUIRoot>();
            if (root == null) return null;

            window = UI.Window(title, UI.Page(content()));
            window.IsOpen = false;
            root.Build(window, setEnableWhenRootEnabled: false);
            return window;
        }

        /// <summary>Opens the window beside the button that was pressed.</summary>
        private static void ShowPopup(WindowElement window, Element anchor)
        {
            var anchorView = UIToolkitBuilder.Instance.GetUIObj(anchor);
            if (window == null || anchorView?.panel == null) return;
            if (UIToolkitBuilder.Instance.GetUIObj(window) is not Window view) return;

            view.Show(new Vector2(anchorView.worldBound.xMax + 8, anchorView.worldBound.yMin), anchorView);
            view.BringToFront();
        }

        /// <summary>Left-aligns a button's text and cuts what does not fit with an ellipsis.</summary>
        private static T LeftAligned<T>(T element) where T : Element
        {
            element.RegisterUpdateCallback(e =>
            {
                if (UIToolkitBuilder.Instance.GetUIObj(e) is not VisualElement view) return;

                Clip(view);
                foreach (var child in view.Children()) Clip(child);
            });
            return element;
        }

        private static void Clip(VisualElement view)
        {
            view.style.unityTextAlign = TextAnchor.MiddleLeft;
            view.style.overflow = Overflow.Hidden;
            view.style.textOverflow = TextOverflow.Ellipsis;
            view.style.whiteSpace = WhiteSpace.NoWrap;
        }
    }
}
