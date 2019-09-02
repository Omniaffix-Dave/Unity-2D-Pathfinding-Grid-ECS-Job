using UnityEngine;
using UnityEditor;

namespace Pathfinding
{
    [CustomEditor(typeof(PathfindingManager))]
    //[CanEditMultipleObjects]
    public class InitializerEditor : Editor
    {
        PathfindingManager PathfindingManager => (PathfindingManager)target;

        SerializedProperty numberOfRandomPaths => serializedObject.FindProperty("randomPathsCount");
        SerializedProperty startManualPath => serializedObject.FindProperty("start");
        SerializedProperty endManualPath => serializedObject.FindProperty("end");

        SerializedProperty numberOfBlocksToAdd => serializedObject.FindProperty("randomObstaclesCount");
        SerializedProperty manualBlockNode => serializedObject.FindProperty("obstacleNode");

        SerializedProperty size;

        SerializedProperty cellMaterial => serializedObject.FindProperty("nodeMaterial");
        SerializedProperty cellBlockedMaterial => serializedObject.FindProperty("obstacleMaterial");
        SerializedProperty instancedMeshWalkable => serializedObject.FindProperty("walkableMesh");
        SerializedProperty instancedMeshBlocked => serializedObject.FindProperty("obstacleMesh");
        SerializedProperty instancedSpacing => serializedObject.FindProperty("gridSpacing");
        SerializedProperty instancedScale => serializedObject.FindProperty("gridScale");
        SerializedProperty noiseLevel => serializedObject.FindProperty("noiseLevel");
        SerializedProperty noiseScale => serializedObject.FindProperty("noiseScale");
        SerializedProperty gizmoPathColor => serializedObject.FindProperty("gizmoPathColor");
        
        SerializedProperty visualMode => serializedObject.FindProperty("visualMode");

        bool showBlocked;
        bool showPathTesting;
        bool showInstancing;
        bool showGizmo;

        private GUIStyle title;
        private GUIStyle foldout;
        private void OnEnable()
        {
            title = new GUIStyle(GUIStyle.none);
            title.fontStyle = FontStyle.Bold;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            
            showBlocked = EditorGUILayout.Foldout(showBlocked, "Obstacles");
            
            if (showBlocked)
                ShowBlockedNodes();
            
            showPathTesting = EditorGUILayout.Foldout(showPathTesting, "Path Testing");

            if (showPathTesting)
                ShowPathTesting();

            EditorGUILayout.Space();
            
            int mode = visualMode.intValue;
            
            if (mode != 1)
            {
                showGizmo = EditorGUILayout.Foldout(showGizmo, "Display (Gizmo)");
                if (showGizmo)
                    ShowGizmoSettings();
            }
            
            if (mode != 0)
            {
                showInstancing = EditorGUILayout.Foldout(showInstancing, "Display (Instancing)");
                if (showInstancing)
                    ShowInstancingSettings();
            }
            
        }

        
        void ShowBlockedNodes()
        {
            EditorGUILayout.LabelField("Additional Obstacles", title);
            EditorGUILayout.PropertyField(numberOfBlocksToAdd, new GUIContent("Count"));
            if (GUILayout.Button($"Add Random Obstacles ({numberOfBlocksToAdd.intValue})")) PathfindingManager.SetRandomObstacles();
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Single Obstacle At Position", title);
            EditorGUILayout.PropertyField(manualBlockNode, new GUIContent("Position"));
            if (GUILayout.Button("Add Obstacle"))
                PathfindingManager.AddObstacleManually();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Perlin Noise", title);
            EditorGUILayout.PropertyField(noiseLevel, new GUIContent("Noise Level"));
            EditorGUILayout.PropertyField(noiseScale, new GUIContent("Noise Scale"));
            if (GUILayout.Button("Generate"))  PathfindingManager.GenerateObstaclesWithPerlinNoise();
            if (GUILayout.Button("Clear Obstacles Map"))         PathfindingManager.ClearObstaclesMap();
            
            serializedObject.ApplyModifiedProperties();
        }

        void ShowPathTesting()
        {
            EditorGUILayout.LabelField("Random Paths", title);
            EditorGUILayout.PropertyField(numberOfRandomPaths, new GUIContent("Count"));
            if (GUILayout.Button($"Add Additional Paths ({numberOfRandomPaths.intValue})")) PathfindingManager.AddRandomPaths();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Custom Path", title);
            EditorGUILayout.PropertyField(startManualPath, new GUIContent("Start Node"));
            EditorGUILayout.PropertyField(endManualPath,   new GUIContent("End Node"));
            if (GUILayout.Button("Add Path Manually")) PathfindingManager.AddPathManually();

            serializedObject.ApplyModifiedProperties();
        }
        
        void ShowInstancingSettings()
        {
            EditorGUILayout.LabelField("Materials", title);
            
            EditorGUILayout.PropertyField(cellMaterial, new GUIContent("Walkable Cells"));
            EditorGUILayout.PropertyField(cellBlockedMaterial, new GUIContent("Blocked Cells"));

            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Geometry", title);
            
            EditorGUILayout.PropertyField(instancedMeshWalkable, new GUIContent("Cell Mesh"));
            EditorGUILayout.PropertyField(instancedMeshBlocked, new GUIContent("Obstacle Mesh"));
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Position", title);
            
            EditorGUILayout.PropertyField(instancedScale, new GUIContent("Scale"));
            EditorGUILayout.PropertyField(instancedSpacing, new GUIContent("Spacing"));
            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Scale and Spacing"))
            {
                PathfindingManager.UpdateMatrices();
                PathfindingManager.UpdatePathsDisplay();
            }
            
            if (GUILayout.Button("Update Paths Display"))
                PathfindingManager.UpdatePathsDisplay();
            
            serializedObject.ApplyModifiedProperties();
        }

        void ShowGizmoSettings()
        {
            EditorGUILayout.PropertyField(gizmoPathColor, new GUIContent("Gizmo Path Color"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}