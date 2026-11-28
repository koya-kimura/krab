using System.Linq;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using RosettaUI.UIToolkit;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>
    /// Bind picker: the Values as graphs, opened from a Parameter's Bind button. A bound local Value can be edited
    /// here, since that changes this preset only; a shared Value is edited only in the Values window, so choosing
    /// here never changes other presets. Copy &amp; Bind turns the bound Value into a local copy to edit.
    /// </summary>
    public sealed partial class ShapeSynthPresetEditor
    {
        // Smaller than Value Gallery: the picker is for recognizing a shape and choosing, not for reading it.
        private const float BindPickerCellWidth = 112;
        private const float BindPickerGraphHeight = 48;
        private const float BindPickerGap = 8;
        private const float BindPickerEditorWidth = 520;

        private PresetParameter _bindParameter;
        private InitialDepth _bindInitialDepth;
        private string _bindLabel;
        private int _bindTargetRevision;
        private string _bindPickerTab;
        private WindowElement _bindWindow;

        private WindowElement BindPickerWindow() =>
            PopupWindow(ref _bindWindow, "Preset Editor - Bind", () => Scrollable(
                BuildValueMessages(),
                UI.DynamicElementOnStatusChanged(
                    () => (_bindTargetRevision, _documentRevision),
                    _ => BuildBindPicker())));

        private Element BuildBindPicker()
        {
            var parameter = _bindParameter;
            if (parameter == null) return UI.Label("Press a Bind button in the Visual window.");

            // Rebuilt apart: the editor when the binding or the Value's components change, the choices also when
            // names change. A name typed in the editor then does not rebuild the field it is typed into.
            return UI.Column(
                UI.Label(() => $"{_bindLabel}:  {BoundDescription(parameter)}").SetColor(PresetEditorPalette.Title),
                UI.Row(
                    UI.Button("None", () => BindValue(parameter, _bindInitialDepth, null)),
                    UI.Button("Copy & Bind", () => CopyAndBind(parameter))
                        .RegisterUpdateCallback(element => element.Interactable = parameter.GetBinding() != null)),
                UI.DynamicElementOnStatusChanged(
                    () => (parameter.GetBinding()?.ValueId, _valuesRevision),
                    _ => BoundValueEditor(parameter)),
                UI.DynamicElementOnStatusChanged(
                    () => (_valuesRevision, _valueNamesRevision),
                    _ => ValueTabs(
                        values => BuildValueGalleryGrid(
                            values, value => BindPickerCell(parameter, value), BindPickerGap),
                        _bindPickerTab,
                        tab => _bindPickerTab = tab)));
        }

        /// <summary>The bound local Value's graph and fields; for a shared Value, how to edit it here.</summary>
        private Element BoundValueEditor(PresetParameter parameter)
        {
            var value = FindValue(parameter.GetBinding()?.ValueId);
            if (value == null) return UI.Space();

            if (!Document.IsLocal(value.Id))
            {
                const string hint =
                    "Shared Value: edit it in the Values window, or Copy & Bind to edit it for this preset only.";
                return UI.Label(hint).SetColor(PresetEditorPalette.Title);
            }

            var graph = new PresetSignalGraph(() => value, () => _beat, PresetSignalGraph.DefaultHeight * 0.6f);
            return UI.Column(
                graph.CreateElement(),
                UI.Field("Name", () => value.Name, name =>
                {
                    value.Name = name;
                    _valueNamesRevision++;
                    _dirty = true;
                }),
                UI.Column(value.Components.Select(component => ComponentFold(value, component))),
                UI.Row(
                    UI.Button("+ ADSR", () => AddComponent(value, ModulatorKind.Adsr)),
                    UI.Button("+ LFO", () => AddComponent(value, ModulatorKind.Lfo))))
                .SetWidth(BindPickerEditorWidth);
        }

        private string BoundDescription(PresetParameter parameter)
        {
            var value = FindValue(parameter.GetBinding()?.ValueId);
            if (value == null) return NoValueName;

            return Document.IsLocal(value.Id) ? $"{value.Name} (this preset)" : $"{value.Name} (shared)";
        }

        private Element BindPickerCell(PresetParameter parameter, PresetValue value)
        {
            var graph = new PresetSignalGraph(() => value, () => _beat, BindPickerGraphHeight);
            var choose = LeftAligned(UI.Button(UI.Label(() => DisplayName(value)),
                () => BindValue(parameter, _bindInitialDepth, value)));
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

            ShowPopup(BindPickerWindow(), anchor);
        }
    }
}
