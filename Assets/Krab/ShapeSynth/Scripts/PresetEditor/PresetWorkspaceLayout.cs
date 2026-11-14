using System;
using RosettaUI;
using RosettaUI.UIToolkit;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>Header, Visual/Values views and status bar. Visual keeps the responsive three-panel shell.</summary>
    public static class PresetWorkspaceLayout
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

        private static Action BindDivider(
            VisualElement divider,
            Element panel,
            Element oppositePanel,
            Element panels,
            float minimumWidth,
            float direction)
        {
            divider.name = direction > 0 ? "preset-navigation-divider" : "preset-inspector-divider";
            divider.style.backgroundColor = PresetEditorTheme.Stroke;
            divider.style.cursor = new StyleCursor(CursorHolder.Get(CursorType.ResizeHorizontal));

            var startX = 0f;
            var startWidth = 0f;
            var dragging = false;

            void OnDown(PointerDownEvent evt)
            {
                if (evt.button != 0) return;

                dragging = true;
                startX = evt.position.x;
                startWidth = UIToolkitBuilder.Instance.GetUIObj(panel).layout.width;
                divider.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            }

            void OnMove(PointerMoveEvent evt)
            {
                if (!dragging) return;

                var width = startWidth + direction * (evt.position.x - startX);
                var rowWidth = UIToolkitBuilder.Instance.GetUIObj(panels).layout.width;
                var oppositeWidth = UIToolkitBuilder.Instance.GetUIObj(oppositePanel).layout.width;
                var maximumWidth = Mathf.Max(minimumWidth, rowWidth - oppositeWidth - DividerWidth * 2 - 240);
                panel.Style.Width = Mathf.Clamp(width, minimumWidth, maximumWidth);
                evt.StopPropagation();
            }

            void OnUp(PointerUpEvent evt)
            {
                if (!dragging) return;

                dragging = false;
                if (divider.HasPointerCapture(evt.pointerId)) divider.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            }

            divider.RegisterCallback<PointerDownEvent>(OnDown);
            divider.RegisterCallback<PointerMoveEvent>(OnMove);
            divider.RegisterCallback<PointerUpEvent>(OnUp);
            return () =>
            {
                divider.UnregisterCallback<PointerDownEvent>(OnDown);
                divider.UnregisterCallback<PointerMoveEvent>(OnMove);
                divider.UnregisterCallback<PointerUpEvent>(OnUp);
            };
        }

        private static void ConfigureResponsiveToggles(
            Element toolbar,
            Element navigation,
            Element navigationDivider,
            Element inspector,
            Element inspectorDivider,
            Element panels)
        {
            BindView(toolbar, view =>
            {
                var navButton = new Button { name = "preset-toggle-navigation", text = "Navigation" };
                var inspectorButton = new Button { name = "preset-toggle-inspector", text = "Inspector" };
                var toggleRow = new VisualElement { name = "preset-panel-toggles" };
                toggleRow.style.flexDirection = FlexDirection.Row;
                toggleRow.Add(navButton);
                toggleRow.Add(inspectorButton);
                view.Add(toggleRow);
                var navOpen = true;
                var inspectorOpen = true;
                bool? wasNarrow = null;

                void SetVisibility(Element panel, Element divider, bool open)
                {
                    var panelView = UIToolkitBuilder.Instance.GetUIObj(panel);
                    var dividerView = UIToolkitBuilder.Instance.GetUIObj(divider);
                    if (panelView != null) panelView.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
                    if (dividerView != null) dividerView.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
                }

                void OnNavClicked()
                {
                    if (wasNarrow == true)
                    {
                        navOpen = !navOpen;
                        inspectorOpen = false;
                        SetVisibility(inspector, inspectorDivider, false);
                    }
                    else navOpen = !navOpen;

                    SetVisibility(navigation, navigationDivider, navOpen);
                }

                void OnInspectorClicked()
                {
                    if (wasNarrow == true)
                    {
                        inspectorOpen = !inspectorOpen;
                        navOpen = false;
                        SetVisibility(navigation, navigationDivider, false);
                    }
                    else inspectorOpen = !inspectorOpen;

                    SetVisibility(inspector, inspectorDivider, inspectorOpen);
                }

                navButton.clicked += OnNavClicked;
                inspectorButton.clicked += OnInspectorClicked;

                var panelsView = UIToolkitBuilder.Instance.GetUIObj(panels);

                void ApplyResponsive(float width)
                {
                    if (width <= 0) return; // Hidden while the Values tab is open.

                    var narrow = width < 850;
                    if (wasNarrow == narrow) return;

                    wasNarrow = narrow;
                    toggleRow.style.display = narrow ? DisplayStyle.Flex : DisplayStyle.None;
                    navButton.style.display = narrow ? DisplayStyle.Flex : DisplayStyle.None;
                    inspectorButton.style.display = narrow ? DisplayStyle.Flex : DisplayStyle.None;
                    if (narrow)
                    {
                        navOpen = true;
                        inspectorOpen = false;
                        SetVisibility(navigation, navigationDivider, true);
                        SetVisibility(inspector, inspectorDivider, false);
                    }
                    else
                    {
                        navOpen = true;
                        inspectorOpen = true;
                        SetVisibility(navigation, navigationDivider, true);
                        SetVisibility(inspector, inspectorDivider, true);
                    }
                }

                void OnGeometryChanged(GeometryChangedEvent evt)
                {
                    if (!ReferenceEquals(evt.target, panelsView)) return;

                    ApplyResponsive(evt.newRect.width);
                }

                if (panelsView != null)
                {
                    panelsView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                    if (panelsView.layout.width > 0) ApplyResponsive(panelsView.layout.width);
                }

                return () =>
                {
                    navButton.clicked -= OnNavClicked;
                    inspectorButton.clicked -= OnInspectorClicked;
                    if (panelsView != null) panelsView.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                    toggleRow.RemoveFromHierarchy();
                };
            });
        }
    }
}
