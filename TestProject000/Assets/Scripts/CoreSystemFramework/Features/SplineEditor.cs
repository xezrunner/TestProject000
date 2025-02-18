using UnityEditor;
using UnityEngine;

using static CoreSystemFramework.Logging;

namespace CoreSystemFramework {

    [CustomEditor(typeof(Spline))]
    class SplineEditor : Editor {
        Spline spline;

        void OnEnable() => spline = (Spline)target;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            GUILayout.Label($"total length: {spline?.totalLength}");
            if (GUILayout.Button("Refresh spline points")) spline?.refreshSplinePoints();
        }

        static Color   SCENE_SplinePointBaseColor   = Color.white;
        static Color   SCENE_SplinePointHoverColor  = Color.green;
        static Color   SCENE_SplinePointSelectColor = Color.red;
        static float   SCENE_SplinePointSizeValue = 0.15f;
        static Vector3 SCENE_SplinePointSize = new(SCENE_SplinePointSizeValue,
                                                   SCENE_SplinePointSizeValue,
                                                   SCENE_SplinePointSizeValue);

        int hoveredPointID  = -1;
        int selectedPointID = -1;

        void SCENE_DrawSplinePoints() {
            int index = 0;
            int count = spline.points.Count;
            for (int i = 0; i < count; ++i) unsafe {
                var point = spline.points[i];

                Handles.color = SCENE_SplinePointBaseColor;

                if (selectedPointID == point.id) {
                    Handles.color = SCENE_SplinePointSelectColor;
                    DrawSolidCube(point.pos, SCENE_SplinePointSizeValue, SCENE_SplinePointSelectColor, SCENE_SplinePointHoverColor);
                    Handles.Label(point.pos + (Vector3.right * 0.5f), $"spline point [{index}]:\npos: {point.pos}\nrot: {point.rot}");

                    Handles.TransformHandle(ref point.pos, ref point.rot);
                    spline.points[i] = point; // set!
                }
                else {
                    if (hoveredPointID == point.id) Handles.color = SCENE_SplinePointHoverColor;
                    Handles.DrawWireCube(point.pos, SCENE_SplinePointSize);
                }
                
                ++index;
            }
        }

        void SCENE_HandleMouse() {
            var camera = SceneView.currentDrawingSceneView.camera;
            if (!camera) return;

            var mousePos = Event.current.mousePosition;

            foreach (var point in spline.points) {
                var pointPos        = HandleUtility.WorldToGUIPoint(point.pos);
                var pointPosMaxAxis = HandleUtility.WorldToGUIPoint(point.pos + (Vector3.right * SCENE_SplinePointSizeValue));
                var radius          = Vector2.Distance(pointPos, pointPosMaxAxis);

                if (Vector2.Distance(mousePos, pointPos) < radius) {
                    hoveredPointID = point.id; break;
                }
                hoveredPointID = -1;
            }

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0) {
                if (hoveredPointID != -1) {
                    selectedPointID = hoveredPointID;
                    e.Use(); // consume event (don't propagate into scene view)
                }
            } 
        }

        public void OnSceneGUI() {
            SCENE_HandleMouse();
            SCENE_DrawSplinePoints();

            HandleUtility.Repaint();
        }

        void DrawSolidCube(Vector3 center, float size, Color faceColor, Color outlineColor) {
            Vector3 halfSize = Vector3.one * (size * 0.5f);

            // Cube corners
            Vector3[] corners = new Vector3[8] {
                center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z), // 0
                center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z),  // 1
                center + new Vector3(halfSize.x, -halfSize.y, halfSize.z),   // 2
                center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z),  // 3
                center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z),  // 4
                center + new Vector3(halfSize.x, halfSize.y, -halfSize.z),   // 5
                center + new Vector3(halfSize.x, halfSize.y, halfSize.z),    // 6
                center + new Vector3(-halfSize.x, halfSize.y, halfSize.z)    // 7
            };

            // Cube faces (each has 4 corner indices)
            int[][] faces = new int[6][] {
                new int[] { 0, 1, 2, 3 }, // Bottom
                new int[] { 4, 5, 6, 7 }, // Top
                new int[] { 0, 3, 7, 4 }, // Left
                new int[] { 1, 2, 6, 5 }, // Right
                new int[] { 0, 1, 5, 4 }, // Front
                new int[] { 3, 2, 6, 7 }  // Back
            };

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual; // Ensures proper depth sorting

            foreach (var face in faces) {
                Vector3[] faceVertices = new Vector3[] {
                    corners[face[0]], corners[face[1]], corners[face[2]], corners[face[3]]
                };

                Handles.DrawSolidRectangleWithOutline(faceVertices, faceColor, outlineColor);
            }
        }
    }

}