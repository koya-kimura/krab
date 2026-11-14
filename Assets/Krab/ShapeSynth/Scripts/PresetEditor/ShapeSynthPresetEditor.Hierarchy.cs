using System;
using System.Linq;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using UnityEngine;
using UnityEngine.UIElements;
using T = Krab.ShapeSynth.PresetEditor.PresetEditorTheme;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private static readonly PresetNodeKind[] ShapeKinds = { PresetNodeKind.Circle, PresetNodeKind.Rectangle, PresetNodeKind.Polygon };
        private PresetHierarchyDrag _hierarchyDrag;
        private PresetNodeKind _addKind = PresetNodeKind.Circle;

        private Element BuildNavigation() => UI.Column(
            T.PaneTitle("Hierarchy", EnabledButton("Delete", DeleteSelected, () => CanDeleteNode).Styled(SmallButton)),
            UI.ScrollViewVerticalAndHorizontal(null, null,
                UI.DynamicElementOnStatusChanged(() => _documentRevision, _ => NodeRow(Document.RootGroup, 0))).SetFlexGrow(1).SetMinHeight(0)
                .Styled(style => style.paddingTop = 4),
            T.Text(() => "Add to: " + AddTarget.Name + "  ·  drag edges reorder, center nests", 10, T.Muted)
                .Styled(style => T.Padding(style, 6, 10)),
            UI.Row(
                T.Text("Shape", 10, T.Muted).Styled(style => { style.marginRight = 6; style.alignSelf = Align.Center; }),
                Choice(null, () => Array.IndexOf(ShapeKinds, _addKind), value => _addKind = ShapeKinds[value],
                    ShapeKinds.Select(kind => kind.ToString())).SetFlexGrow(1)).Styled(style => T.Padding(style, 0, 8)),
            T.ActionStrip(T.Button("+ Group", () => AddNode(PresetNodeKind.Group)), T.Button("+ Shape", () => AddNode(_addKind))));

        private static void SmallButton(IStyle style)
        {
            T.Padding(style, 2, 7);
            style.minHeight = 20;
            style.height = 22;
            style.fontSize = 10;
        }

        private static string Icon(PresetNodeKind kind) =>
            kind == PresetNodeKind.Circle ? "○" : kind == PresetNodeKind.Rectangle ? "□" : kind == PresetNodeKind.Polygon ? "△" : "▸";

        private Element NodeRow(PresetNode node, int depth)
        {
            var title = UI.Button(T.Text(() => node.Name, 12), () => SelectNode(node.Id)).SetFlexGrow(1).Styled(style =>
            {
                style.backgroundColor = Color.clear;
                T.Border(style, Color.clear, 0);
                T.Margin(style, 0, 0);
                T.Padding(style, 0, 2);
                style.justifyContent = Justify.FlexStart;
                style.unityTextAlign = TextAnchor.MiddleLeft;
            });
            if (_hierarchyDrag == null)
                _hierarchyDrag = new PresetHierarchyDrag(id => id != Document.RootGroup.Id, CanDropNode, DropNode, message => _status = message);
            PresetWorkspaceLayout.BindView(title, view => _hierarchyDrag.Bind(view, node.Id));
            Element expander = node.Kind == PresetNodeKind.Group
                ? UI.Button(T.Text(() => _expandedGroups.Contains(node.Id) ? "▾" : "▸", 12, T.Icon), () =>
                    { if (!_expandedGroups.Remove(node.Id)) _expandedGroups.Add(node.Id); }).Styled(IconCell)
                : T.Text(Icon(node.Kind), 11, T.Icon).Styled(IconCell);
            var row = PresetButtonStyle.Selectable(UI.Row(expander, title), () => node.Id == _selectedId).Styled(style =>
            {
                style.height = 28;
                style.alignItems = Align.Center;
                style.paddingLeft = 6 + depth * 15;
                style.paddingRight = 8;
            });
            var popup = UI.Popup(row, () => new IMenuItem[]
            {
                new MenuItem("Select", () => SelectNode(node.Id)),
                new MenuItem("Add Group", node.Kind == PresetNodeKind.Group ? () => { SelectNode(node.Id); AddNode(PresetNodeKind.Group); } : (Action)null),
                new MenuItem("Add Circle", node.Kind == PresetNodeKind.Group ? () => { SelectNode(node.Id); AddNode(PresetNodeKind.Circle); } : (Action)null),
                new MenuItem("Delete Node", node != Document.RootGroup ? () => { SelectNode(node.Id); DeleteSelected(); } : (Action)null)
            });
            if (node.Kind != PresetNodeKind.Group) return popup;
            return UI.Column(popup,
                UI.DynamicElementIf(() => _expandedGroups.Contains(node.Id), () =>
                    UI.DynamicElementOnStatusChanged(() => ChildRevision(node.Id), _ =>
                        UI.Column(node.Children.Select(child => NodeRow(child, depth + 1))))));
        }

        private static void IconCell(IStyle style)
        {
            style.width = 18; style.minWidth = 18;
            style.backgroundColor = Color.clear;
            T.Border(style, Color.clear, 0);
            T.Margin(style, 0, 0);
            T.Padding(style, 0, 0);
            style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        private bool DropDestination(string sourceId, string targetId, PresetDropPosition position, out PresetNode parent, out int index)
        {
            parent = null; index = 0;
            var nodes = Document.Nodes().ToArray();
            var source = nodes.FirstOrDefault(node => node.Id == sourceId);
            var target = nodes.FirstOrDefault(node => node.Id == targetId);
            if (source == null || target == null || source == Document.RootGroup || source == target) return false;
            if (position == PresetDropPosition.Inside)
            {
                if (target.Kind != PresetNodeKind.Group) return false;
                parent = target; index = target.Children.Count;
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
        private bool CanDropNode(string source, string target, PresetDropPosition position) => DropDestination(source, target, position, out _, out _);
        private void DropNode(string source, string target, PresetDropPosition position) => Guard(() =>
        {
            if (!DropDestination(source, target, position, out var parent, out var index)) return;
            var oldParent = ParentOf(source);
            Document.MoveNode(source, parent.Id, index);
            _childrenRevisions[oldParent.Id] = ChildRevision(oldParent.Id) + 1;
            if (parent != oldParent) _childrenRevisions[parent.Id] = ChildRevision(parent.Id) + 1;
            _expandedGroups.Add(parent.Id);
            SelectNode(source);
            Changed();
            _status = "Moved " + Selected.Name + " into " + parent.Name;
        });
    }
}
