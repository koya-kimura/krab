using System;
using System.Collections.Generic;
using System.Linq;
using RosettaUI;
using UnityEngine;
using UnityEngine.UIElements;
using T = Krab.ShapeSynth.PresetEditor.PresetEditorTheme;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private static readonly int[] UiScales = { 100, 125, 140, 160, 180, 200 };
        private bool _showOpen;
        private int _savedRevision;
        private IReadOnlyList<Krab.ShapeSynth.Core.Presets.PresetDocument> _savedPresets = Array.Empty<Krab.ShapeSynth.Core.Presets.PresetDocument>();
        private string _savedErrors = "";
        private string _openId;
        public bool ShowsValues => _showValues;
        public Element CreateElement() => UI.Column(
            UI.Label("Hierarchy, Preview and Inspector in one workspace."),
            UI.Button("Open Preset Workspace", ShowWorkspace));

        public void ShowWorkspace()
        {
            EnsureDocument();
            if (_workspace == null)
            {
                _previewView = new PresetPreviewView(() => _controller && _controller.OutputCamera ? _controller.OutputCamera.targetTexture : null);
                _workspace = UI.Window("ShapeSynth · Preset Workspace",
                    PresetWorkspaceLayout.Create(BuildHeader(), BuildNavigation(), BuildPreviewPane(), BuildInspectorPane(),
                        BuildValuesView(), BuildStatusBar(), () => _showValues));
                PresetWorkspaceLayout.ConfigureWindow(_workspace);
            }
            if (!_workspace.HasBuilt)
            {
                var root = FindFirstObjectByType<RosettaUIRoot>();
                if (!root) { _status = "RosettaUIRoot is missing."; return; }
                root.Build(_workspace);
            }
            _workspace.Open();
            if (!_hasPreview) Preview();
        }

        public void ShowTab(bool values) => _showValues = values;

        private Element BuildHeader() => UI.Column(
            UI.Row(
                UI.Row(T.Text("ShapeSynth", 13, T.Foreground, true), T.Text(" / Preset Editor", 13, T.Hex(0x99a3b3))).SetFlexShrink(0)
                    .Styled(style => style.marginRight = 18),
                Tab("Visual", false), Tab("Values", true),
                UI.Field((LabelElement)null, () => Document.Name, value => { Document.Name = value; Changed(false); }).SetWidth(220).SetMinWidth(120)
                    .Styled(style => style.marginLeft = 18),
                StateText(() => _dirty ? "● Unsaved" : "✓ Saved", () => _dirty ? T.Warning : T.Output, 11).Styled(style => style.marginLeft = 8),
                UI.Space().SetFlexGrow(1),
                Choice("UI", () => Array.IndexOf(UiScales, _workspaceScale?.Percent ?? 140),
                    value => _workspaceScale?.SetPercent(UiScales[value]), UiScales.Select(scale => scale + "%")).SetWidth(110),
                T.Button("New", () => ConfirmIfDirty("create a new preset", () => { NewDocument(); Preview(); })),
                T.Button("Open", () => { RefreshSavedPresets(); _showOpen = !_showOpen; }, null, () => _showOpen),
                T.Button("Save", SaveDocument, () => _dirty, null)
            ).Styled(style =>
            {
                style.height = 44;
                style.alignItems = Align.Center;
                style.backgroundColor = T.Header;
                T.Padding(style, 0, 12);
            }),
            UI.DynamicElementIf(() => _pendingAction != null, () => Banner(
                T.Text(() => "Unsaved changes. Discard edits and " + _pendingDescription + "?", 12, T.Warning),
                UI.Space().SetFlexGrow(1),
                T.Button("Discard & Continue", () => { var action = _pendingAction; _pendingAction = null; action?.Invoke(); }),
                T.Button("Cancel", () => _pendingAction = null))),
            UI.DynamicElementIf(() => _showOpen, BuildOpenBrowser));

        private Element Tab(string label, bool values)
        {
            var button = UI.Button(label, () => ShowTab(values)).SetFlexShrink(0);
            button.RegisterUpdateCallback(element =>
            {
                var active = _showValues == values;
                element.Style.BackgroundColor = active ? T.TabActive : Color.clear;
                element.Style.Color = active ? new Color(0.93f, 0.95f, 0.97f) : T.Hex(0xb4bcc8);
                var view = RosettaUI.UIToolkit.Builder.UIToolkitBuilder.Instance.GetUIObj(element);
                if (view != null) view.style.borderBottomColor = active ? T.Accent : Color.clear;
            });
            return PresetWorkspaceLayout.BindView(button, view =>
            {
                view.name = "preset-tab-" + label.ToLowerInvariant();
                T.Border(view.style, Color.clear, 0, 0, 2, 0);
                T.Radius(view.style, 0);
                T.Margin(view.style, 0, 1);
                T.Padding(view.style, 0, 18);
                view.style.unityFontStyleAndWeight = FontStyle.Bold;
                view.style.alignSelf = Align.Stretch;
                return null;
            });
        }

        private static Element Banner(params Element[] children) => UI.Row(children).Styled(style =>
        {
            style.alignItems = Align.Center;
            style.backgroundColor = T.Raised;
            T.Border(style, T.Stroke, 1, 0, 0, 0);
            T.Padding(style, 4, 12);
        });

        private Element BuildStatusBar() => UI.Row(
            StateText(() => _status, () => ReferenceEquals(_status, _errorStatus) ? T.Warning : T.Hex(0x93a1b5), 10).SetFlexGrow(1).SetFlexShrink(1),
            StateText(() => (IsPreviewRunning ? $"▶ {_bpm:G} BPM  ·  " : "■ Stopped  ·  ") + PreviewModeLabel(),
                () => IsPreviewRunning ? T.Output : T.Hex(0x93a1b5), 10).Styled(style => style.marginRight = 12),
            StateText(() => _dirty ? "● Unsaved" : "✓ Saved", () => _dirty ? T.Warning : T.Hex(0x93a1b5), 10)
        ).SetFlexGrow(1).Styled(style =>
        {
            style.height = 27;
            style.alignItems = Align.Center;
            T.Padding(style, 0, 11);
        });

        private string PreviewModeLabel() => _previewMode == PresetPreviewMode.Static ? "Static · signals = 1"
            : _previewMode == PresetPreviewMode.Beat ? $"Beat {_beat:F2}" : "Animated";

        private void RefreshSavedPresets()
        {
            Guard(() =>
            {
                _savedPresets = Repository.List(out var errors);
                _savedErrors = string.Join("; ", errors);
                if (!_savedPresets.Any(model => model.Id == _openId)) _openId = _savedPresets.FirstOrDefault()?.Id;
                _savedRevision++;
            });
        }
        private Element BuildOpenBrowser() => UI.Column(
            UI.DynamicElementOnStatusChanged(() => _savedRevision, _ => Banner(
                Choice("Saved Preset", () => Array.FindIndex(_savedPresets.ToArray(), model => model.Id == _openId),
                    value => { if (value >= 0 && value < _savedPresets.Count) _openId = _savedPresets[value].Id; },
                    _savedPresets.Select(model => model.Name + " / " + model.Id.Substring(0, Math.Min(8, model.Id.Length))).ToArray()).SetFlexGrow(1),
                EnabledButton("Open Selected", () => ConfirmIfDirty("open the selected preset", () => OpenDocument(_openId)), () => _openId != null),
                T.Button("Refresh", RefreshSavedPresets), T.Button("Close", () => _showOpen = false))),
            T.Text(() => _savedErrors, 10, T.Warning),
            T.Text(() => _savedPresets.Count == 0 ? "No saved presets in " + Repository.DirectoryPath : "", 10, T.Muted));

        // RosettaUI's custom menu uses a removed Unity internal interface in this installed version.
        // Keep Rosetta's data binding and use Unity's default runtime dropdown menu instead.
        private DropdownElement Choice(string label, Func<int> read, Action<int> write, IEnumerable<string> options)
        {
            var element = UI.Dropdown(label == null ? null : Caption(label), read, write, options);
            return PresetWorkspaceLayout.BindView(element, view =>
            {
                for (var type = view.GetType(); type != null; type = type.BaseType)
                {
                    var field = type.GetField("createMenuCallback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
                    if (field == null) continue;
                    field.SetValue(view, null);
                    break;
                }
                return null;
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

        private static LabelElement Caption(string label) => UI.Label(label)
            .SetWidth(Mathf.Clamp(label.Length * 7f + 16, 16, 130))
            .SetMinWidth(Mathf.Clamp(label.Length * 7f + 16, 16, 130))
            .SetMaxWidth(Mathf.Clamp(label.Length * 7f + 16, 16, 130));

        /// <summary>Unlabeled numeric input; captions are placed above it by the caller.</summary>
        private Element NumberInput(Func<double> read, Action<double> write) =>
            UI.Field((LabelElement)null, () => (float)read(), value => { write(value); Changed(); }).SetFlexGrow(1).SetMinWidth(0);
        private Element Number(string label, Func<double> read, Action<double> write) =>
            T.Labeled(label, NumberInput(read, write)).SetFlexGrow(1).SetFlexBasis(0);
        /// <summary>Label whose text color follows state; applied only when the color changes.</summary>
        private static LabelElement StateText(Func<string> text, Func<Color> color, float size)
        {
            Color? applied = null;
            VisualElement bound = null;
            var label = T.Text(text, size);
            label.RegisterUpdateCallback(element =>
            {
                var view = RosettaUI.UIToolkit.Builder.UIToolkitBuilder.Instance.GetUIObj(element);
                var next = color();
                if (view == null || (view == bound && applied == next)) return;
                bound = view; applied = next;
                view.style.color = next;
            });
            return label;
        }

        private Element EnabledButton(string label, Action action, Func<bool> enabled) =>
            T.Button(label, action).RegisterUpdateCallback(element => element.Interactable = enabled());
    }
}
