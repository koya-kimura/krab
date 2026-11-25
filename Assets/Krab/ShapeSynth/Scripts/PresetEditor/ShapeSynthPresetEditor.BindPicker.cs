using System.Linq;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using RosettaUI.UIToolkit;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>
    /// Bind picker: the Values as graphs, opened from a Parameter's Bind button. It only chooses; Values are edited
    /// in the Values window. Copy &amp; Bind makes a local copy of the bound Value, so editing it changes this preset
    /// only.
    /// </summary>
    public sealed partial class ShapeSynthPresetEditor
    {
        // Smaller than Value Gallery: the picker is for recognizing a shape and choosing, not for reading it.
        private const float BindPickerCellWidth = 112;
        private const float BindPickerGraphHeight = 48;
        private const float BindPickerGap = 8;

        private PresetParameter _bindParameter;
        private InitialDepth _bindInitialDepth;
        private string _bindLabel;
        private int _bindTargetRevision;
        private string _bindPickerTab;
        private WindowElement _bindWindow;

        /// <summary>
        /// The picker window, built once on the RosettaUI root that shows the Bind button. Building it on the root
        /// keeps it updated without a launcher in the Settings window.
        /// </summary>
        private WindowElement BindPickerWindow(RosettaUIRoot root)
        {
            if (_bindWindow != null) return _bindWindow;

            _bindWindow = UI.Window("Preset Editor - Bind", UI.Page(Scrollable(
                BuildValueMessages(),
                UI.DynamicElementOnStatusChanged(
                    () => (_bindTargetRevision, _documentRevision, _valuesRevision, _valueNamesRevision),
                    _ => BuildBindPicker()))));
            _bindWindow.IsOpen = false;
            // Not opened with the root: it opens only from a Bind button.
            root.Build(_bindWindow, setEnableWhenRootEnabled: false);
            return _bindWindow;
        }

        private Element BuildBindPicker()
        {
            var parameter = _bindParameter;
            if (parameter == null) return UI.Label("Press a Bind button in the Visual window.");

            return UI.Column(
                UI.Label(() => $"{_bindLabel}:  {BoundDescription(parameter)}").SetColor(PresetEditorPalette.Title),
                UI.Row(
                    UI.Button("None", () => BindValue(parameter, _bindInitialDepth, null)),
                    UI.Button("Copy & Bind", () => CopyAndBind(parameter))
                        .RegisterUpdateCallback(element => element.Interactable = parameter.GetBinding() != null)),
                ValueTabs(
                    values => BuildValueGalleryGrid(values, value => BindPickerCell(parameter, value), BindPickerGap),
                    _bindPickerTab,
                    tab => _bindPickerTab = tab));
        }

        private string BoundDescription(PresetParameter parameter)
        {
            var value = FindValue(parameter.GetBinding()?.ValueId);
            if (value == null) return NoValueName;

            return Document.IsLocal(value.Id) ? $"{value.Name} (this preset)" : $"{value.Name} (shared)";
        }

        private Element BindPickerCell(PresetParameter parameter, PresetValue value)
        {
            var graph = new PresetSignalGraph(() => value, () => _valueGalleryBeat, BindPickerGraphHeight);
            var choose = UI.Button(UI.Label(() => DisplayName(value)),
                () => BindValue(parameter, _bindInitialDepth, value));
            choose.RegisterUpdateCallback(element => element.Style.BackgroundColor =
                parameter.GetBinding()?.ValueId == value.Id ? PresetEditorPalette.Selected : null);
            return UI.Column(graph.CreateElement(), choose).SetWidth(BindPickerCellWidth);
        }

        /// <summary>
        /// Rebinds the Parameter to a local copy of its Value (Depth kept) and selects the copy in the Values window.
        /// </summary>
        private void CopyAndBind(PresetParameter parameter)
        {
            GuardValues(() =>
            {
                var source = FindValue(parameter.GetBinding()?.ValueId);
                if (source == null) return;

                var name = UniqueName(DisplayName(source), Document.LocalValues.Select(value => value.Name));
                var copy = Document.AddLocalCopy(source, name);
                parameter.Bind(copy.Id, parameter.GetBinding().Depth);
                _valuesRevision++;
                _bindPickerTab = ThisPresetTab;
                SelectValue(copy.Id);
                Changed();
            });
        }

        private void OpenBindPicker(Element anchor, PresetParameter parameter, InitialDepth initialDepth, string label)
        {
            _bindParameter = parameter;
            _bindInitialDepth = initialDepth;
            _bindLabel = label;
            _bindTargetRevision++;
            if (FindValue(parameter.GetBinding()?.ValueId) is { } bound) _bindPickerTab = TabOf(bound);

            var anchorView = UIToolkitBuilder.Instance.GetUIObj(anchor);
            var root = FindAnyObjectByType<RosettaUIRoot>();
            if (anchorView?.panel == null || root == null) return;

            var windowElement = BindPickerWindow(root);
            if (UIToolkitBuilder.Instance.GetUIObj(windowElement) is not Window window) return;

            window.Show(new Vector2(anchorView.worldBound.xMax + 8, anchorView.worldBound.yMin), anchorView);
            window.BringToFront();
        }
    }
}
