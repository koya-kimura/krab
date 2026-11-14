using Minis;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Krab.Common.Midi
{
    public class MidiInputManager : MonoBehaviour
    {
        private void Start()
        {
            foreach (var device in InputSystem.devices)
            {
                if (device is not MidiDevice midi) continue;

                Debug.Log(device.name, this);
                midi.onWillNoteOn += null;
            }
        }
    }
}