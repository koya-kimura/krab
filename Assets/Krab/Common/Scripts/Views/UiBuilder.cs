using System.Collections.Generic;
using System.Linq;
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
            if (_prefsSaveAction.WasPressedThisFrame()) Prefs.Save();
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

        private Element CreateElement()
        {
            List<Element> elements = new();
            foreach (var target in _targets)
            {
                var title = target.GetType().Name;
                elements.Add(UI.WindowLauncher(title, UI.Window(title, UI.Page(target.CreateElement()))));
            }

            elements.Add(UI.Button("Save", Prefs.Save));

            return UI.Window(_title, elements).SetClosable(false);
        }
    }
}