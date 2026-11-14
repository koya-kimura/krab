using System;
using RosettaUI;
using RosettaUI.UIToolkit;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>Header, Visual/Values views and status bar. Visual keeps the responsive three-panel shell.</summary>
    public static partial class PresetWorkspaceLayout
    {
        private const float DefaultNavigationWidth = 240;
        private const float DefaultInspectorWidth = 360;
        private const float CompactNavigationWidth = 200;
        private const float CompactInspectorWidth = 310;
        private const float DividerWidth = 4;

        public static Element Create(Element header, Element navigation, Element preview, Element inspector,
            Element values, Element status, Func<bool> showValues)
        {
            navigation.SetFlexGrow(1).SetFlexShrink(1).SetMinHeight(0);
            preview.SetFlexGrow(1).SetFlexShrink(1).SetMinHeight(0);
            inspector.SetFlexGrow(1).SetFlexShrink(1).SetMinHeight(0);
            var navigationPanel = UI.Column(navigation)
                .SetWidth(DefaultNavigationWidth).SetMinWidth(160).SetFlexShrink(0).Fill(PresetEditorTheme.Panel);
            var previewPanel = UI.Column(preview)
                .SetFlexGrow(1).SetFlexShrink(1).SetMinWidth(240).Fill(PresetEditorTheme.Center);
            var inspectorPanel = UI.Column(inspector)
                .SetWidth(DefaultInspectorWidth).SetMinWidth(240).SetFlexShrink(0).Fill(PresetEditorTheme.Panel);
            var navigationDivider = UI.Space().SetWidth(DividerWidth).SetMinWidth(DividerWidth).SetFlexShrink(0);
            var inspectorDivider = UI.Space().SetWidth(DividerWidth).SetMinWidth(DividerWidth).SetFlexShrink(0);
            var panels = UI.Row(navigationPanel, navigationDivider, previewPanel, inspectorDivider, inspectorPanel)
                .SetFlexGrow(1).SetFlexShrink(1).SetMinHeight(0);
            values.SetFlexGrow(1).SetFlexShrink(1).SetMinHeight(0);
            var views = UI.Column(panels, values).SetFlexGrow(1).SetFlexShrink(1).SetMinHeight(0)
                .RegisterUpdateCallback(_ =>
                {
                    var valuesOpen = showValues();
                    if (panels.Enable == valuesOpen) panels.Enable = !valuesOpen;
                    if (values.Enable != valuesOpen) values.Enable = valuesOpen;
                })
                .Styled(style => PresetEditorTheme.Border(style, PresetEditorTheme.Stroke, 1, 0, 1, 0));
            values.Enable = showValues();
            panels.Enable = !values.Enable;
            header.SetFlexGrow(0).SetFlexShrink(0);
            var headerRow = UI.Column(header).SetFlexGrow(0).SetFlexShrink(0);
            var statusRow = UI.Row(status).SetFlexShrink(0).Fill(PresetEditorTheme.Header);
            var workspace = UI.Column(headerRow, views, statusRow)
                .SetFlexGrow(1).SetFlexShrink(1).SetMinWidth(640).SetMinHeight(0).Fill(PresetEditorTheme.Background);

            BindView(navigationPanel, view => Name(view, "preset-navigation"));
            BindView(previewPanel, view => Name(view, "preset-preview"));
            BindView(inspectorPanel, view => Name(view, "preset-inspector"));
            BindView(values, view => Name(view, "preset-values"));
            BindView(headerRow, view => Name(view, "preset-toolbar"));
            BindView(statusRow, view => Name(view, "preset-status"));
            BindView(workspace, view => Name(view, "preset-workspace"));
            BindView(panels, view =>
            {
                Name(view, "preset-panels");
                var compact = false;

                void OnGeometryChanged(GeometryChangedEvent evt)
                {
                    if (!ReferenceEquals(evt.target, view) || evt.newRect.width <= 0) return;

                    var nextCompact = evt.newRect.width < 1100;
                    if (nextCompact == compact) return;

                    compact = nextCompact;
                    navigationPanel.Style.Width = compact ? CompactNavigationWidth : DefaultNavigationWidth;
                    inspectorPanel.Style.Width = compact ? CompactInspectorWidth : DefaultInspectorWidth;
                }

                view.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                return () => view.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            });
            BindView(navigationDivider, view => BindDivider(view, navigationPanel, inspectorPanel, panels, 160, 1));
            BindView(inspectorDivider, view => BindDivider(view, inspectorPanel, navigationPanel, panels, 240, -1));
            ConfigureResponsiveToggles(headerRow, navigationPanel, navigationDivider, inspectorPanel, inspectorDivider, panels);
            return workspace;
        }

        /// <summary>Sizes and anchors the workspace to the current logical panel, then follows panel resizes.</summary>
        public static void ConfigureWindow(WindowElement window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            BindView(window, view =>
            {
                view.name = "preset-workspace-window";
                view.style.position = Position.Absolute;
                view.style.backgroundColor = PresetEditorTheme.Window;

                // This is a bounded workspace, not a content-sized popup. Dynamic fields must not
                // ask Rosetta's Window to expand to an Image's intrinsic texture dimensions.
                void OnContentResize(RequestResizeWindowEvent evt)
                {
                    evt.StopImmediatePropagation();
                }

                view.RegisterCallback<RequestResizeWindowEvent>(OnContentResize, TrickleDown.TrickleDown);
                VisualElement root = null;
                IVisualElementScheduledItem schedule = null;

                void ResizeToPanel(GeometryChangedEvent _)
                {
                    if (_ != null && !ReferenceEquals(_.target, root)) return;

                    var panel = view.panel;
                    if (panel == null) return;

                    var bounds = panel.visualTree.layout;
                    if (bounds.width <= 0 || bounds.height <= 0) return;

                    var availableWidth = Mathf.Max(1, bounds.width - 24);
                    var availableHeight = Mathf.Max(1, bounds.height - 24);
                    view.style.left = 12;
                    view.style.top = 12;
                    view.style.width = availableWidth;
                    view.style.height = availableHeight;
                    view.style.minWidth = Mathf.Min(640, availableWidth);
                    view.style.maxWidth = availableWidth;
                    view.style.maxHeight = availableHeight;
                }

                void AttachToPanelRoot()
                {
                    root = view.panel?.visualTree;
                    if (root == null) return;

                    root.RegisterCallback<GeometryChangedEvent>(ResizeToPanel);
                    ResizeToPanel(null);
                    schedule?.Pause();
                }

                AttachToPanelRoot();
                if (root == null)
                    schedule = view.schedule.Execute(AttachToPanelRoot).Every(16).Until(() => root != null);

                return () =>
                {
                    root?.UnregisterCallback<GeometryChangedEvent>(ResizeToPanel);
                    view.UnregisterCallback<RequestResizeWindowEvent>(OnContentResize, TrickleDown.TrickleDown);
                    schedule?.Pause();
                };
            });
        }

        /// <summary>Attaches one UI Toolkit view at a time and releases its handlers when RosettaUI unbinds.</summary>
        public static T BindView<T>(T element, Func<VisualElement, Action> attach) where T : Element
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (attach == null) throw new ArgumentNullException(nameof(attach));

            var bridge = element.GetViewBridge();
            VisualElement boundView = null;
            Action detachView = null;

            void ClearView()
            {
                var detach = detachView;
                detachView = null;
                boundView = null;
                detach?.Invoke();
            }

            void OnUpdate(Element _)
            {
                var view = UIToolkitBuilder.Instance.GetUIObj(element);
                if (ReferenceEquals(view, boundView)) return;

                ClearView();
                if (view == null) return;

                boundView = view;
                detachView = attach(view);
                bridge.onUnsubscribe += OnUnsubscribe;
            }

            void OnUnsubscribe()
            {
                ClearView();
                bridge.onUnsubscribe -= OnUnsubscribe;
            }

            element.RegisterUpdateCallback(OnUpdate);
            OnUpdate(element);
            return element;
        }

        private static Action Name(VisualElement view, string name)
        {
            view.name = name;
            return () => { };
        }
    }
}
