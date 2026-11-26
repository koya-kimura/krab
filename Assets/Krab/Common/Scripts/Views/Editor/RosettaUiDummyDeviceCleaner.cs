using System.Collections.Generic;
using UnityEditor;
using UnityEngine.InputSystem;

namespace Krab.Common.Views.Editor
{
    /// <summary>
    /// RosettaUI's InputDeviceBlocker adds a dummy Keyboard, Mouse and Pointer in every Play session and never
    /// removes them. In the Editor they survive Play Mode, and once dozens pile up the Input System fails to
    /// resolve keyboard bindings (more than 255 controls per binding). Remove them whenever the Editor is idle.
    /// </summary>
    [InitializeOnLoad]
    internal static class RosettaUiDummyDeviceCleaner
    {
        static RosettaUiDummyDeviceCleaner()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (!EditorApplication.isPlayingOrWillChangePlaymode) RemoveDummyDevices();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode) RemoveDummyDevices();
        }

        private static void RemoveDummyDevices()
        {
            var dummies = new List<InputDevice>();
            foreach (var device in InputSystem.devices)
            {
                if (IsDummy(device)) dummies.Add(device);
            }

            foreach (var device in dummies) InputSystem.RemoveDevice(device);
        }

        // The dummies are created with AddDevice<T>(), so they are non-native devices of the base layouts
        // without a description. Real keyboards and mice are native and carry a description.
        private static bool IsDummy(InputDevice device)
        {
            if (device.native || !device.description.empty) return false;

            return device.layout is "Keyboard" or "Mouse" or "Pointer";
        }
    }
}
