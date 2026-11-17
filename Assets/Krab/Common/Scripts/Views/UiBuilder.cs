using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PrefsGUI;
using RosettaUI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimpleContainer;

namespace Krab.Common.Views
{
    [RequireComponent(typeof(RosettaUIRoot))]
    public class UiBuilder : MonoBehaviour
    {
        [SerializeField] private string _title = "Settings";
        [SerializeField] private InputAction _prefsSaveAction;
        [SerializeField] private InputAction _toggleUIAction;
        private RosettaUIRoot _root;

        private List<IUiTarget> _targets;

        private void Start()
        {
            _root = GetComponent<RosettaUIRoot>();
            _root.Build(CreateElement());
        }

        private void Update()
        {
            if (_toggleUIAction.WasPressedThisFrame()) _root.enabled = !_root.enabled;
            if (_prefsSaveAction.WasPressedThisFrame()) SaveAll();
        }

        private void OnEnable()
        {
            _toggleUIAction.Enable();
            _prefsSaveAction.Enable();
        }

        private void OnDisable()
        {
            _toggleUIAction.Disable();
            _prefsSaveAction.Disable();
        }

        [Inject]
        public void Constructs(IEnumerable<IUiTarget> targets)
        {
            _targets = targets.ToList();
        }

        private void SaveAll()
        {
            Prefs.Save();
            if (_targets == null) return;

            foreach (var target in _targets.OfType<IUiSaveTarget>())
            {
                target.Save();
            }
        }

        private Element CreateElement()
        {
            var entries = (_targets ?? new List<IUiTarget>())
                .Select(target => new TargetEntry(target)).ToList();
            var tabs = entries.GroupBy(entry => entry.Path[0])
                .OrderBy(group => group.Min(entry => entry.Order))
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => (group.Key, (Element)UI.Page(CreateCategory(group.ToList(), 1))));

            return UI.Window(_title, UI.Tabs(tabs).SetWidth(360f), UI.Button("Save", Prefs.Save)).SetClosable(false);
        }

        private static IEnumerable<Element> CreateCategory(List<TargetEntry> entries, int depth)
        {
            // Categories use the lowest descendant order; equal orders sort by display name.
            var items = entries.Where(entry => entry.Path.Length == depth)
                .Select(entry => (entry.Order, Name: entry.Title, Element: CreateLauncher(entry)))
                .Concat(entries.Where(entry => entry.Path.Length > depth)
                    .GroupBy(entry => entry.Path[depth])
                    .Select(group => (group.Min(entry => entry.Order), Name: group.Key,
                        Element: (Element)UI.Fold(group.Key, CreateCategory(group.ToList(), depth + 1)))));
            return items.OrderBy(item => item.Item1)
                .ThenBy(item => item.Name, StringComparer.Ordinal)
                .Select(item => item.Element).ToList();
        }

        private static Element CreateLauncher(TargetEntry entry)
        {
            if (!(entry.Target is IUiWindowsTarget windows)) return Launcher(entry.Title, entry.Target.CreateElement());

            return UI.Column(windows.CreateWindows()
                .Select(window => Launcher($"{entry.Title} - {window.Name}", window.Content)));
        }

        private static Element Launcher(string title, Element content) =>
            UI.WindowLauncher(title, UI.Window(title, UI.Page(content)));

        private sealed class TargetEntry
        {
            public IUiTarget Target { get; }
            public string[] Path { get; }
            public string Title { get; }
            public int Order { get; }

            public TargetEntry(IUiTarget target)
            {
                Target = target;
                var type = target.GetType();
                var attribute = type.GetCustomAttribute<UiTargetAttribute>(true);
                Path = (attribute?.CategoryPath ?? string.Empty).Split('/')
                    .Select(segment => segment.Trim()).Where(segment => segment.Length > 0).ToArray();
                if (Path.Length == 0) Path = new[] { "Other" };
                Title = string.IsNullOrWhiteSpace(attribute?.Title) ? type.Name : attribute.Title;
                Order = attribute?.Order ?? 0;
            }
        }
    }
}
