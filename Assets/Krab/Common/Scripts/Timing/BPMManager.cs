using System;
using Krab.Common.Views;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;

namespace Krab.Common.Timing
{
    public class BPMManager : MonoBehaviour, IBPMManager, IUiTarget
    {
        private const float _minBpm = 10f;
        private const float _maxBpm = 500f;

        private readonly PrefsFloat _bpm = new("BPM", 120);
        private float _currentBpm;

        private double _lastBeat;
        private float _pendingBpm;

        private void Awake()
        {
            _currentBpm = _bpm;
            _pendingBpm = _bpm;
        }

        private void Update()
        {
            if (!Mathf.Approximately(_bpm, _pendingBpm)) _pendingBpm = _bpm;

            Beat += Time.deltaTime * (_currentBpm / 60.0);

            if (Math.Floor(Beat) > Math.Floor(_lastBeat))
            {
                _currentBpm = _pendingBpm;
                OnBeat?.Invoke();
            }

            _lastBeat = Beat;
        }

        public event Action OnBeat;

        public double Beat { get; private set; }

        // Effective tempo; pending edits take effect at the next beat boundary.
        public float CurrentBpm => _currentBpm;

        public void SetBpm(float bpm)
        {
            if (bpm < _minBpm || _maxBpm < bpm) return;

            _bpm.Set(bpm);
            _pendingBpm = bpm;
        }

        public Element CreateElement()
        {
            return UI.Column(_bpm.CreateElement());
        }
    }
}