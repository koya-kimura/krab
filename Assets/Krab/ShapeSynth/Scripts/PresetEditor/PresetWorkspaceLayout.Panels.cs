using System;
using RosettaUI;
using RosettaUI.UIToolkit;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>Resizable dividers and the narrow-width panel toggles of the Visual workspace.</summary>
    public static partial class PresetWorkspaceLayout
    {
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
