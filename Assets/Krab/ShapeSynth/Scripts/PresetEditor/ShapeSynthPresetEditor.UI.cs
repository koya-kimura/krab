using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        // Right of the shared settings window, which UiBuilder places at the top-left.
        private static readonly Vector2 WorkspacePosition = new(390, 10);

        private IReadOnlyList<PresetDocument> _savedPresets = Array.Empty<PresetDocument>();
        private int _savedRevision;
        private int _openIndex;

        public Element CreateElement()
        {
            return UI.Button("Open Preset Editor", ShowWorkspace);
        }

        public void ShowWorkspace()
        {
            EnsureDocument();
            _workspace ??= UI.Window(
                UI.Label(() => "Preset Editor" + (_dirty ? " *" : "")),
                BuildFileBar(),
                UI.DynamicElementIf(() => _error != null, () => UI.Label(() => _error).SetColor(PresetEditorPalette.Error)),
                UI.Tabs(
                    ("Visual", UI.Row(
                        BuildHierarchyPane().SetWidth(260),
                        BuildPreviewPane().SetWidth(480),
                        BuildInspectorPane().SetWidth(620))),
                    ("Values", BuildValuesView()))).SetPosition(WorkspacePosition);

            if (!_workspace.HasBuilt)
            {
                var root = FindFirstObjectByType<RosettaUIRoot>();
                if (!root)
                {
                    _error = $"{nameof(RosettaUIRoot)} is missing.";
                    return;
                }

                root.Build(_workspace);
            }

            _workspace.Open();
            Preview();
            // After Preview: a successful compile clears the error line, which would hide list read errors.
            RefreshSavedPresets();
        }

        private Element BuildFileBar()
        {
            return UI.Column(
                UI.Field("Name", () => Document.Name, value =>
                {
                    Document.Name = value;
                    _dirty = true;
                }),
                UI.Row(
                    UI.Button("New", NewDocument),
                    UI.Button("Save", SaveDocument),
                    UI.DynamicElementOnStatusChanged(() => _savedRevision, _ => BuildOpenChoice())));
        }

        private Element BuildOpenChoice()
        {
            var presets = _savedPresets;
            return UI.Row(
                Choice(
                    null,
                    () => Math.Min(_openIndex, Math.Max(0, presets.Count - 1)),
                    index => _openIndex = index,
                    presets.Select(preset => preset.Name)).SetMinWidth(160),
                UI.Button("Open", () =>
                {
                    if (_openIndex < presets.Count) OpenDocument(presets[_openIndex].Id);
                }).SetInteractable(presets.Count > 0));
        }

        private static Element PaneTitle(string title)
        {
            return UI.Label(title).SetColor(PresetEditorPalette.Title);
        }

        /// <summary>Reloads the Open list; unreadable files stay out of it and their reasons go to the error line.</summary>
        private void RefreshSavedPresets()
        {
            IReadOnlyList<string> errors = Array.Empty<string>();
            Guard(() =>
            {
                _savedPresets = Repository.List(out errors);
                _savedRevision++;
            });
            if (errors.Count > 0) _error = "Some presets could not be read: " + string.Join("; ", errors);
        }

        // RosettaUI's custom dropdown menu uses a Unity internal interface removed in this Unity version.
        // Keep Rosetta's value binding and fall back to Unity's default runtime menu.
        private static DropdownElement Choice(
            LabelElement label,
            Func<int> read,
            Action<int> write,
            IEnumerable<string> options)
        {
            var element = UI.Dropdown(label, read, write, options);
            element.RegisterUpdateCallback(_ => DisableCustomMenu(element));
            return element;
        }

        private static void DisableCustomMenu(Element element)
        {
            var view = UIToolkitBuilder.Instance.GetUIObj(element);
            if (view == null || view.userData is DropdownElement) return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var type = view.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField("createMenuCallback", flags);
                if (field == null) continue;

                field.SetValue(view, null);
                break;
            }

            // Marks the view as patched; RosettaUI does not use userData on dropdown views.
            view.userData = element;
        }
    }
}
