using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    public enum PresetDropPosition { Before, Inside, After }

    /// <summary>Pointer capture and drop feedback only; the editor owns validated document mutations.</summary>
    public sealed class PresetHierarchyDrag
    {
        private readonly Dictionary<VisualElement, string> _rows = new();
        private readonly Func<string, bool> _canDrag;
        private readonly Func<string, string, PresetDropPosition, bool> _canDrop;
        private readonly Action<string, string, PresetDropPosition> _drop;
        private readonly Action<string> _status;
        private VisualElement _source;
        private VisualElement _root;
        private VisualElement _marker;
        private Label _hint;
        private string _sourceId;
        private string _targetId;
        private PresetDropPosition _position;
        private int _pointerId = -1;
        private Vector2 _start;
        private bool _dragging;

        public PresetHierarchyDrag(
            Func<string, bool> canDrag,
            Func<string, string, PresetDropPosition, bool> canDrop,
            Action<string, string, PresetDropPosition> drop,
            Action<string> status)
        {
            _canDrag = canDrag;
            _canDrop = canDrop;
            _drop = drop;
            _status = status;
        }

        public Action Bind(VisualElement view, string id)
        {
            _rows[view] = id;
            view.name = "preset-node-" + id;

            void Down(PointerDownEvent evt)
            {
                if (evt.button != 0 || !_canDrag(id)) return;

                Cancel();
                _source = view;
                _sourceId = id;
                _pointerId = evt.pointerId;
                _start = evt.position;
                _root = view.panel.visualTree;
                _root.RegisterCallback<KeyDownEvent>(KeyDown, TrickleDown.TrickleDown);
            }

            view.RegisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            view.RegisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            view.RegisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            view.RegisterCallback<PointerCaptureOutEvent>(CaptureOut);
            view.RegisterCallback<PointerCancelEvent>(PointerCancel);
            return () =>
            {
                if (_source == view) Cancel();

                _rows.Remove(view);
                view.UnregisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
                view.UnregisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
                view.UnregisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
                view.UnregisterCallback<PointerCaptureOutEvent>(CaptureOut);
                view.UnregisterCallback<PointerCancelEvent>(PointerCancel);
            };
        }

        private void Move(PointerMoveEvent evt)
        {
            if (_source == null || evt.pointerId != _pointerId) return;
            if (!_dragging && Vector2.Distance(_start, evt.position) < 6) return;

            if (!_dragging) BeginDrag();

            FindTarget(evt.position);
            evt.StopImmediatePropagation();
        }

        private void BeginDrag()
        {
            _dragging = true;
            _source.CapturePointer(_pointerId);
            _marker = new VisualElement { name = "preset-drop-marker", pickingMode = PickingMode.Ignore };
            _marker.style.position = Position.Absolute;
            _hint = new Label { name = "preset-drag-hint", pickingMode = PickingMode.Ignore };
            _hint.style.position = Position.Absolute;
            _hint.style.backgroundColor = new Color(0.08f, 0.1f, 0.14f, 1);
            _hint.style.color = Color.white;
            _hint.style.paddingLeft = 8;
            _hint.style.paddingRight = 8;
            _hint.style.paddingTop = 4;
            _hint.style.paddingBottom = 4;
            _root.Add(_marker);
            _root.Add(_hint);
        }

        private void FindTarget(Vector2 position)
        {
            _targetId = null;
            var picked = _source.panel.Pick(position);
            while (picked != null && !_rows.ContainsKey(picked)) picked = picked.parent;

            if (picked != null)
            {
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
                _marker.style.backgroundColor = inside ? new Color(0.2f, 0.75f, 0.9f, 0.35f) : new Color(0.3f, 0.85f, 1);
            }
            else
            {
                _marker.style.display = DisplayStyle.None;
            }

            var point = _root.WorldToLocal(position);
            _hint.style.left = Mathf.Min(point.x + 14, _root.layout.width - 230);
            _hint.style.top = Mathf.Min(point.y + 14, _root.layout.height - 35);
            _hint.text = HintText();
        }

        private static PresetDropPosition DropPositionAt(float fraction)
        {
            if (fraction < 0.25f) return PresetDropPosition.Before;

            return fraction > 0.75f ? PresetDropPosition.After : PresetDropPosition.Inside;
        }

        private string HintText()
        {
            if (_targetId == null) return "Cannot drop here · Esc to cancel";

            return _position == PresetDropPosition.Inside ? "Move into Group" : "Move " + _position.ToString().ToLowerInvariant();
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
            else _status("Move cancelled — choose a valid hierarchy row.");

            evt.StopImmediatePropagation();
        }

        private void KeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape) return;

            Cancel();
            _status("Move cancelled.");
            evt.StopImmediatePropagation();
        }

        private void CaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId == _pointerId) Cancel();
        }

        private void PointerCancel(PointerCancelEvent evt)
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
            _hint?.RemoveFromHierarchy();
            _marker = null;
            _hint = null;
            _root = null;
            if (source != null && source.HasPointerCapture(pointerId)) source.ReleasePointer(pointerId);
        }
    }
}
