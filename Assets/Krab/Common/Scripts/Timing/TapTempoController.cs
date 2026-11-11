using System.Collections.Generic;
using Krab.Common.Views;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimpleContainer;

namespace Krab.Common.Timing
{
    [UiTarget("Common/Timing", "Tap Tempo", 10)]
    public class TapTempoController : MonoBehaviour, IUiTarget
    {
        private const double TapTimeoutSeconds = 2.0;

        [SerializeField] private InputAction _tapTempoAction;

        private readonly PrefsInt _tapCount = new("TapCount", 4);

        private readonly List<double> _tapTimes = new();
        private IBPMManager _bpmManager;

        private void Update()
        {
            if (_tapTimes.Count > 0 && Time.unscaledTimeAsDouble - _tapTimes[^1] > TapTimeoutSeconds)
                _tapTimes.Clear();

            if (_tapTempoAction != null && _tapTempoAction.WasPressedThisFrame()) Tap();
        }

        private void OnEnable()
        {
            _tapTempoAction?.Enable();
        }

        private void OnDisable()
        {
            _tapTempoAction?.Disable();
            _tapTimes.Clear();
        }

        public Element CreateElement()
        {
            return UI.Column(_tapCount.CreateElement(), UI.Button("Tap", Tap));
        }

        [Inject]
        public void Construct(IBPMManager manager)
        {
            _bpmManager = manager;
        }

        private void Tap()
        {
            var now = Time.unscaledTimeAsDouble;
            if (_tapTimes.Count > 0 && now - _tapTimes[^1] > TapTimeoutSeconds)
                _tapTimes.Clear();

            _tapTimes.Add(now);
            var tapCount = Mathf.Max(2, _tapCount.Get());
            if (_tapTimes.Count > tapCount) _tapTimes.RemoveAt(0);

            if (_tapTimes.Count < 2) return;

            var bpm = CalculateBpm();
            if (bpm > 0) _bpmManager?.SetBpm(bpm);
        }

        private float CalculateBpm()
        {
            var duration = _tapTimes[^1] - _tapTimes[0];
            if (duration <= 0) return 0;

            return (float)(60.0 * (_tapTimes.Count - 1) / duration);
        }
    }
}