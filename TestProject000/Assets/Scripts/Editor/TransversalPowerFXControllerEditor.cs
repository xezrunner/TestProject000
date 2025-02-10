using UnityEditor;
using UnityEngine; 

[CustomEditor(typeof(TransversalPowerFXController))]
public class TransversalPowerFXControllerEditor: Editor {
    TransversalPowerFXController instance;

    TransversalPowerEffectsState newState;

    void Awake() {
        instance = (TransversalPowerFXController)target;
        newState = instance.state;
        instance.EDITOR_RepaintEvent += Repaint;
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        EditorGUILayout.Separator();

        GUILayout.Label("Info:");
        GUILayout.Label($"State: {instance.state}");

        GUILayout.Label($"Anim target values:");
        GUILayout.Label($"    - Lens distortion: {instance.animData_target.lensDistortion}");

        GUILayout.Label($"Anim values:");
        GUILayout.Label($"    - Lens distortion: {instance.animData.lensDistortion}");

        var t = instance.t;

        (float sine, float value) = instance.getValueForOutStateWobble();

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label($"sine:");  EditorGUILayout.Slider(sine, 0, 1);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label($"value:"); EditorGUILayout.Slider(value, 0, 1);
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Label($"(1 - t): {(1 - t)}");


        if (!instance.IsTest) return;


        // FX:
        GUILayout.Label("Anim target values:");
        // GUILayout.BeginHorizontal();
        {
            instance.animData_target.radialZoom     = EditorGUILayout.FloatField("Radial zoom",     instance.animData_target.radialZoom);
            instance.animData_target.lensDistortion = EditorGUILayout.FloatField("Lens distortion", instance.animData_target.lensDistortion);
            instance.animData_target.fovAddition    = EditorGUILayout.FloatField("FOV addition",    instance.animData_target.fovAddition);
        }
        // GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"T:"); instance.t = EditorGUILayout.Slider(instance.t, 0, 1);
        GUILayout.EndHorizontal();

        // State:
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("State:");
            newState = (TransversalPowerEffectsState)EditorGUILayout.EnumPopup(newState);
            if (GUILayout.Button("Set state")) instance.setState(newState);
        }
        GUILayout.EndHorizontal();
    }
}