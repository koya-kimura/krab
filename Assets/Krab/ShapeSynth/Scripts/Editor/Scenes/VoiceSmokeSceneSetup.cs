using System;
using Krab.ShapeSynth.Samples;
using Krab.ShapeSynth.Unity.Rendering;
using Krab.ShapeSynth.Unity.Runtime;
using UnityEditor;
using UnityEngine;

namespace Krab.ShapeSynth.Editor.Scenes
{
    public static class VoiceSmokeSceneSetup
    {
        public static void Configure(GameObject synth, Camera camera)
        {
            if (synth.GetComponentInChildren<VoiceSmokeTest>(true))
            {
                throw new InvalidOperationException(
                    "Voice demo already exists; existing Inspector settings are preserved.");
            }

            var go = new GameObject("Voice Lifetime", typeof(TargetCameraCanvas));
            go.transform.SetParent(synth.transform, false);
            go.GetComponent<TargetCameraCanvas>().TargetCamera = camera;
            var controller = go.AddComponent<ShapeSynthController>();
            var controllerSettings = new SerializedObject(controller);
            controllerSettings.FindProperty("_outputCamera").objectReferenceValue = camera;
            controllerSettings.FindProperty("_invertTargetY").boolValue = true;
            controllerSettings.ApplyModifiedPropertiesWithoutUndo();
            var demo = go.AddComponent<VoiceSmokeTest>();
            var demoSettings = new SerializedObject(demo);
            demoSettings.FindProperty("_timerBeats").floatValue = 8;
            demoSettings.FindProperty("_showInstructions").boolValue = true;
            demoSettings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
