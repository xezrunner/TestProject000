using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using static CoreSystemFramework.Logging;

[CustomEditor(typeof(TestSMD))]
class TestSMDEditor: Editor {
    TestSMD instance;

    void OnEnable() => instance = (TestSMD)target;
    
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        if (!instance.SMD) return;

        if (!instance.preset) {
            GUILayout.Label("No preset assigned!");
            return;
        }

        if (GUILayout.Button("Precache / recreate object pool")) instance.preCache();
        if (GUILayout.Button("Spawn array")) instance.spawnArrayOfObjs();
    }
}

class TestSMD: MonoBehaviour {
    new Transform transform;
    
    public SplineMeshDeformer SMD;

    public GameObject preset;
    MeshFilter preset_meshFilter;

    public int x = 6;
    public int z = 4;

    // TODO: these are certainly not real
    public float width  = 3.68f;
    public float height = 0.619f;
    public float length = 32.9f;

    struct DeformObjInfo {
        public GameObject obj;
        public Transform  trans;
        public MeshFilter meshFilter;
        public int x;
        public int z;
    }

    DeformObjInfo[] infos;

    void Awake() {
        transform = base.transform;
        
        preset_meshFilter = preset.GetComponent<MeshFilter>();
        if (!preset_meshFilter) preset = null;

        if (!preset) return;

        preCache();

        SMD.onDispatchRequestFinished += callback;
    }

    void callback(int tag) {
        infos[tag].obj.SetActive(true);
        infos[tag].trans.SetPositionAndRotation(
            new(infos[tag].x * width, 0, 0), infos[tag].trans.rotation
        );
        // log($"done with [{tag}]");
    }

    public void preCache() {
        if (infos != null) {
            for (int i = infos.Length-1; i >= 0; --i) Destroy(infos[i].obj);
            infos = null;
        }

        List<DeformObjInfo> list = new(x * z);
        for (int i = 0; i < z; ++i) {     // Z
            for (int j = 0; j < x; ++j) { // X
                var obj = Instantiate(preset, transform);
                obj.name = $"deform obj x:{i} z:{j}";
                obj.SetActive(false);

                var trans = obj.GetComponent<Transform>();

                var meshFilter = obj.GetComponent<MeshFilter>();

                var info = new DeformObjInfo() {
                    obj = obj, trans = trans,
                    meshFilter = meshFilter,
                    x = (x/2) - (j+1), z = i
                };
                list.Add(info);
            }
        }
        infos = list.ToArray();
    }

    public void spawnArrayOfObjs() {
        if (!preset) {
            logError("no preset!"); return;
        }

        int count = infos.Length;
        for (int i = 0; i < count; ++i) {
            SMD.RequestMeshDeform_Shared(new(0,0,infos[i].z * length), infos[i].meshFilter.mesh, i);
        }

        SMD.DispatchAllRequests();
    }
}