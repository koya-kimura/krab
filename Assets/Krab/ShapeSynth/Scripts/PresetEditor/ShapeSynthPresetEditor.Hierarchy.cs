using System;
using System.Linq;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private static readonly PresetNodeKind[] ShapeKinds =
            { PresetNodeKind.Circle, PresetNodeKind.Rectangle, PresetNodeKind.Polygon };

        private PresetHierarchyDrag _hierarchyDrag;
        private PresetNodeKind _addKind = PresetNodeKind.Circle;

        private Element BuildHierarchyPane()
        {
            return UI.Column(
                PaneTitle("Hierarchy"),
                UI.ScrollViewVertical(
                    320,
                    UI.DynamicElementOnStatusChanged(() => _documentRevision, _ => NodeRow(Document.RootGroup, 0))),
                Choice(
                    "Shape",
                    () => Array.IndexOf(ShapeKinds, _addKind),
                    value => _addKind = ShapeKinds[value],
                    ShapeKinds.Select(kind => kind.ToString())),
                UI.Row(
                    UI.Button("+ Shape", () => AddNode(_addKind)),
                    UI.Button("+ Group", () => AddNode(PresetNodeKind.Group)),
                    UI.Button("Delete", DeleteSelected)));
        }

        private Element NodeRow(PresetNode node, int depth)
        {
            var isGroup = node.Kind == PresetNodeKind.Group;
            var title = UI.Button(
                UI.Label(() => Icon(node.Kind) + "  " + node.Name + CopiesSuffix(node)),
                () => SelectNode(node.Id));
            _hierarchyDrag ??= new PresetHierarchyDrag(id => id != Document.RootGroup.Id, CanDropNode, DropNode);
            title.RegisterUpdateCallback(element =>
            {
                _hierarchyDrag.Track(UIToolkitBuilder.Instance.GetUIObj(element), node.Id);
                element.Style.BackgroundColor = RowColor(node.Id);
                element.Style.Color = node.Id == _hierarchyDrag.DraggingId ? PresetEditorPalette.DragSource : null;
            });

            Element expander = isGroup
                ? UI.Button(UI.Label(() => _expandedGroups.Contains(node.Id) ? "▼" : "►"), () =>
                {
                    if (!_expandedGroups.Remove(node.Id)) _expandedGroups.Add(node.Id);
                }).SetWidth(24)
                : UI.Space().SetWidth(24);
            var row = UI.Row(UI.Space().SetWidth(depth * 14), expander, title.SetFlexGrow(1));
            if (!isGroup) return row;

            return UI.Column(
                row,
                UI.DynamicElementIf(
                    () => _expandedGroups.Contains(node.Id),
                    () => UI.DynamicElementOnStatusChanged(
                        () => ChildRevision(node.Id),
                        _ => UI.Column(node.Children.Select(child => NodeRow(child, depth + 1))))));
        }

        /// <summary>While dragging, colors where the Node can go; otherwise marks the selection.</summary>
        private Color? RowColor(string id)
        {
            var dragging = _hierarchyDrag.DraggingId;
            if (dragging == null) return id == _selectedId ? PresetEditorPalette.Selected : null;
            if (id == _hierarchyDrag.TargetId) return PresetEditorPalette.DropTarget;

            return CanDropAnywhere(dragging, id) ? PresetEditorPalette.DropCandidate : null;
        }

        private bool CanDropAnywhere(string source, string target)
        {
            return CanDropNode(source, target, PresetDropPosition.Inside)
                   || CanDropNode(source, target, PresetDropPosition.Before)
                   || CanDropNode(source, target, PresetDropPosition.After);
        }

        private static string CopiesSuffix(PresetNode node)
        {
            return node.Layout.Kind == PresetLayoutKind.None ? "" : $"  x{node.Layout.Count}";
        }

        private static string Icon(PresetNodeKind kind)
        {
            // Only glyphs present in RosettaUI's default font.
            if (kind == PresetNodeKind.Circle) return "○";
            if (kind == PresetNodeKind.Rectangle) return "◻";

            return kind == PresetNodeKind.Polygon ? "▲" : "■";
        }

        private bool DropDestination(
            string sourceId,
            string targetId,
            PresetDropPosition position,
            out PresetNode parent,
            out int index)
        {
            parent = null;
            index = 0;
            var nodes = Document.Nodes().ToArray();
            var source = nodes.FirstOrDefault(node => node.Id == sourceId);
            var target = nodes.FirstOrDefault(node => node.Id == targetId);
            if (source == null || target == null || source == Document.RootGroup || source == target) return false;

            if (position == PresetDropPosition.Inside)
            {
                if (target.Kind != PresetNodeKind.Group) return false;

                parent = target;
                index = target.Children.Count;
            }
            else
            {
                parent = nodes.FirstOrDefault(node => node.Children.Contains(target));
                if (parent == null) return false;

                index = parent.Children.IndexOf(target) + (position == PresetDropPosition.After ? 1 : 0);
            }

            var destination = parent;
            bool Contains(PresetNode node) => node == destination || node.Children.Any(Contains);
            return !Contains(source);
        }

        private bool CanDropNode(string source, string target, PresetDropPosition position)
        {
            return DropDestination(source, target, position, out _, out _);
        }

        private void DropNode(string source, string target, PresetDropPosition position)
        {
            Guard(() =>
            {
                if (!DropDestination(source, target, position, out var parent, out var index)) return;

                var oldParent = ParentOf(source);
                Document.MoveNode(source, parent.Id, index);
                ChildrenChanged(oldParent.Id);
                if (parent != oldParent) ChildrenChanged(parent.Id);

                _expandedGroups.Add(parent.Id);
                SelectNode(source);
                Changed();
            });
        }
    }
}
