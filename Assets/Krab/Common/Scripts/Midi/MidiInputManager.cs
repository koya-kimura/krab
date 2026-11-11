using Minis;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Krab.Common.Midi
{
    public class MidiInputManager : MonoBehaviour
    {
        private void Start()
        {
            var devices = InputSystem.devices;
            foreach (var device in devices)
                if (device is MidiDevice midi)
                {
                    Debug.Log(device.name);
                    midi.onWillNoteOn += null;
                }
        }
    }
}