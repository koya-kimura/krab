using Krab.Common.Views;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;

namespace Krab.Common.Settings
{
    public class ApplicationFrameSettings : MonoBehaviour, IUiTarget
    {
        private const int MinTargetFrameRate = 15;
        private const int MaxTargetFrameRate = 240;

        private readonly PrefsBool _capFrameRate = new("CapFrameRate");
        private readonly PrefsBool _runInBackground = new("RunInBackground", true);
        private readonly PrefsInt _targetFrameRate = new("TargetFrameRate", 60);
        private readonly PrefsInt _vSyncCount = new("VSyncCount");

        private AppliedSnapshot _applied;

        private void Start()
        {
            ApplyIfChanged();
        }

        private void Update()
        {
            ApplyIfChanged();
        }

        public Element CreateElement()
        {
            return UI.Column(_capFrameRate.CreateElement(),
                _targetFrameRate.CreateSlider(MinTargetFrameRate, MaxTargetFrameRate), _vSyncCount.CreateElement(),
                _runInBackground.CreateElement());
        }

        private void ApplyIfChanged()
        {
            AppliedSnapshot snapshot = new(_capFrameRate.Get(), _targetFrameRate.Get(), _vSyncCount.Get(),
                _runInBackground.Get());

            if (snapshot.Equals(_applied)) return;

            _applied = snapshot;
            Apply(snapshot);
        }

        private static void Apply(AppliedSnapshot snapshot)
        {
            Application.runInBackground = snapshot.RunInBackground;

            QualitySettings.vSyncCount = snapshot.VSyncCount;

            if (snapshot.CapFrameRate)
                Application.targetFrameRate = Mathf.Clamp(
                    snapshot.TargetFrameRate, MinTargetFrameRate, MaxTargetFrameRate);
            else
                Application.targetFrameRate = -1;
        }

        private readonly struct AppliedSnapshot
        {
            public readonly bool CapFrameRate;
            public readonly int TargetFrameRate;
            public readonly int VSyncCount;
            public readonly bool RunInBackground;

            public AppliedSnapshot(bool capFrameRate, int targetFrameRate, int vSyncCount, bool runInBackground)
            {
                CapFrameRate = capFrameRate;
                TargetFrameRate = targetFrameRate;
                VSyncCount = Mathf.Clamp(vSyncCount, 0, 2);
                RunInBackground = runInBackground;
            }

            public bool Equals(AppliedSnapshot other)
            {
                return CapFrameRate == other.CapFrameRate && TargetFrameRate == other.TargetFrameRate &&
                       VSyncCount == other.VSyncCount && RunInBackground == other.RunInBackground;
            }
        }
    }
}