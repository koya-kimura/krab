using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    public enum PresetDropPosition
    {
        Before,
        Inside,
        After
    }

    /// <summary>Pointer capture and a drop marker only; the editor owns validated document mutations.</summary>
    public sealed class PresetHierarchyDrag
    {
        private const float DragThreshold = 6;

        private static readonly Color InsideColor = new(0.2f, 0.75f, 0.9f, 0.35f);
        private static readonly Color EdgeColor = new(0.3f, 0.85f, 1f);

        private readonly Dictionary<VisualElement, string> _rows = new();
        private readonly Func<string, bool> _canDrag;
        private readonly Func<string, string, PresetDropPosition, bool> _canDrop;
        private readonly Action<string, string, PresetDropPosition> _drop;

        private VisualElement _source;
        private VisualElement _root;
        private VisualElement _marker;
        private string _sourceId;
        private string _targetId;
        private PresetDropPosition _position;
        private int _pointerId = -1;
        private Vector2 _start;
        private bool _dragging;

        public PresetHierarchyDrag(
            Func<string, bool> canDrag,
            Func<string, string, PresetDropPosition, bool> canDrop,
            Action<string, string, PresetDropPosition> drop)
        {
            _canDrag = canDrag ?? throw new ArgumentNullException(nameof(canDrag));
            _canDrop = canDrop ?? throw new ArgumentNullException(nameof(canDrop));
            _drop = drop ?? throw new ArgumentNullException(nameof(drop));
        }

        /// <summary>The Node being dragged, or null when no drag is in progress.</summary>
        public string DraggingId => _dragging ? _sourceId : null;

        /// <summary>The Node that would receive the current drop, or null.</summary>
        public string TargetId => _dragging ? _targetId : null;

        /// <summary>Registers a row view once; the registration ends when the view leaves its panel.</summary>
        public void Track(VisualElement view, string id)
        {
            if (view == null) return;
            if (_rows.ContainsKey(view))
            {
                _rows[view] = id;
                return;
            }

            _rows[view] = id;
            view.RegisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            view.RegisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            view.RegisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            view.RegisterCallback<PointerCaptureOutEvent>(CaptureOut);
            view.RegisterCallback<DetachFromPanelEvent>(Detach);
        }

        private void Detach(DetachFromPanelEvent evt)
        {
            var view = (VisualElement)evt.currentTarget;
            if (_source == view) Cancel();

            _rows.Remove(view);
            view.UnregisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            view.UnregisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            view.UnregisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            view.UnregisterCallback<PointerCaptureOutEvent>(CaptureOut);
            view.UnregisterCallback<DetachFromPanelEvent>(Detach);
        }

        private void Down(PointerDownEvent evt)
        {
            var view = (VisualElement)evt.currentTarget;
            if (evt.button != 0 || !_rows.TryGetValue(view, out var id) || !_canDrag(id)) return;

            Cancel();
            _source = view;
            _sourceId = id;
            _pointerId = evt.pointerId;
            _start = evt.position;
            _root = view.panel.visualTree;
            _root.RegisterCallback<KeyDownEvent>(KeyDown, TrickleDown.TrickleDown);
        }

        private void Move(PointerMoveEvent evt)
        {
            if (_source == null || evt.pointerId != _pointerId) return;
            if (!_dragging && Vector2.Distance(_start, evt.position) < DragThreshold) return;

            if (!_dragging) BeginDrag();

            FindTarget(evt.position);
            evt.StopImmediatePropagation();
        }

        private void Up(PointerUpEvent evt)
        {
            if (_source == null || evt.pointerId != _pointerId || evt.button != 0) return;

            if (!_dragging)
            {
                Cancel();
                return;
            }

            FindTarget(evt.position);
            var source = _sourceId;
            var target = _targetId;
            var position = _position;
            Cancel();
            if (target != null) _drop(source, target, position);

            evt.StopImmediatePropagation();
        }

        private void BeginDrag()
        {
            _dragging = true;
            _source.CapturePointer(_pointerId);
            _marker = new VisualElement { pickingMode = PickingMode.Ignore };
            _marker.style.position = Position.Absolute;
            _root.Add(_marker);
        }

        private void FindTarget(Vector2 position)
        {
            _targetId = null;
            var picked = _source.panel.Pick(position);
            while (picked != null && !_rows.ContainsKey(picked)) picked = picked.parent;

            if (picked == null)
            {
                _marker.style.display = DisplayStyle.None;
                return;
            }

            var rect = picked.worldBound;
            var fraction = (position.y - rect.yMin) / Mathf.Max(1, rect.height);
            _position = DropPositionAt(fraction);
            var candidate = _rows[picked];
            if (_canDrop(_sourceId, candidate, _position)) _targetId = candidate;

            // Shape centers accept an adjacent drop, while Group centers accept a child.
            if (_targetId == null && _position == PresetDropPosition.Inside)
            {
                _position = fraction < 0.5f ? PresetDropPosition.Before : PresetDropPosition.After;
                if (_canDrop(_sourceId, candidate, _position)) _targetId = candidate;
            }

            var local = _root.WorldToLocal(rect.position);
            var inside = _position == PresetDropPosition.Inside;
            _marker.style.display = _targetId == null ? DisplayStyle.None : DisplayStyle.Flex;
            _marker.style.left = local.x;
            _marker.style.top = local.y + (_position == PresetDropPosition.After ? rect.height - 2 : 0);
            _marker.style.width = rect.width;
            _marker.style.height = inside ? rect.height : 2;
            _marker.style.backgroundColor = inside ? InsideColor : EdgeColor;
        }

        private static PresetDropPosition DropPositionAt(float fraction)
        {
            if (fraction < 0.25f) return PresetDropPosition.Before;

            return fraction > 0.75f ? PresetDropPosition.After : PresetDropPosition.Inside;
        }

        private void KeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape) return;

            Cancel();
            evt.StopImmediatePropagation();
        }

        private void CaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId == _pointerId) Cancel();
        }

        private void Cancel()
        {
            var source = _source;
            var pointerId = _pointerId;
            _source = null;
            _sourceId = null;
            _targetId = null;
            _pointerId = -1;
            _dragging = false;
            _root?.UnregisterCallback<KeyDownEvent>(KeyDown, TrickleDown.TrickleDown);
            _marker?.RemoveFromHierarchy();
            _marker = null;
            _root = null;
            if (source != null && source.HasPointerCapture(pointerId)) source.ReleasePointer(pointerId);
        }
    }
}
