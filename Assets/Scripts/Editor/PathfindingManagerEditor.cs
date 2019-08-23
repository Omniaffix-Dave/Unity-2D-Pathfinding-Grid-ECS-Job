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

        bool showBlocked;
        bool showPaths;
        bool showInstancing;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            showPaths = EditorGUILayout.Foldout(showPaths, "Paths");

            if (showPaths)
                ShowPaths();

            showBlocked = EditorGUILayout.Foldout(showBlocked, "Blocked Nodes");

            if (showBlocked)
                ShowBlockedNodes();

            showInstancing = EditorGUILayout.Foldout(showInstancing, "Instancing");

            if (showInstancing)
                ShowInstancing();
        }

        void ShowPaths()
        {
            EditorGUILayout.PropertyField(numberOfRandomPaths, new GUIContent("Random Paths Count"));
            if (GUILayout.Button("Add Random Paths"))
            {
                PathfindingManager.searchRandomPaths = true;
            }
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(startManualPath, new GUIContent("Start Node"));
            EditorGUILayout.PropertyField(endManualPath, new GUIContent("End Node"));
            if (GUILayout.Button("Add Path Manually"))
                PathfindingManager.searchManualPath = true;

            serializedObject.ApplyModifiedProperties();
        }

        void ShowBlockedNodes()
        {
            EditorGUILayout.PropertyField(numberOfBlocksToAdd, new GUIContent("Number To Generate"));
            EditorGUILayout.Space();
            if (GUILayout.Button("Clear Obstacles Map"))
                PathfindingManager.ClearObstaclesMap();
            if (GUILayout.Button("Add Random Blocked Nodes"))
                PathfindingManager.addRandomObstacles = true;
            
            EditorGUILayout.PropertyField(manualBlockNode);

            EditorGUILayout.Space();
            if (GUILayout.Button("Add Manual Blocked Node"))
                PathfindingManager.addObstacleManually = true;
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(noiseLevel, new GUIContent("Noise Level"));
            EditorGUILayout.PropertyField(noiseScale, new GUIContent("Noise Scale"));
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Generate With Perlin Noise"))
                PathfindingManager.GeneratePerlinNoiseObstacles();
            
            
            serializedObject.ApplyModifiedProperties();
        }

        void ShowInstancing()
        {
            EditorGUILayout.PropertyField(instancedMeshWalkable, new GUIContent("Cell Mesh"));
            EditorGUILayout.PropertyField(instancedMeshBlocked, new GUIContent("Obstacle Mesh"));
            EditorGUILayout.Space();
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
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Materials");
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(cellMaterial, new GUIContent("Walkable Cells"));
            EditorGUILayout.PropertyField(cellBlockedMaterial, new GUIContent("Blocked Cells"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}