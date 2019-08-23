using UnityEngine;
using UnityEditor;

namespace Pathfinding
{
    [CustomEditor(typeof(PathfindingManager))]
    //[CanEditMultipleObjects]
    public class InitializerEditor : Editor
    {
        PathfindingManager PathfindingManager => (PathfindingManager)target;

        SerializedProperty numberOfRandomPaths => serializedObject.FindProperty("numOfRandomPaths");
        SerializedProperty startManualPath => serializedObject.FindProperty("start");
        SerializedProperty endManualPath => serializedObject.FindProperty("end");

        SerializedProperty numberOfBlocksToAdd => serializedObject.FindProperty("numbOfRandomBlockedNodes");
        SerializedProperty manualBlockNode => serializedObject.FindProperty("blockedNode");

        SerializedProperty size;

        SerializedProperty cellMaterial => serializedObject.FindProperty("instanceCellMaterial");
        SerializedProperty cellBlockedMaterial => serializedObject.FindProperty("instanceCellBlockedMaterial");
        SerializedProperty instancedMeshWalkable => serializedObject.FindProperty("instancedMeshWalkable");
        SerializedProperty instancedMeshBlocked => serializedObject.FindProperty("instancedMeshBlocked");
        SerializedProperty instancedSpacing => serializedObject.FindProperty("instancedSpacing");
        SerializedProperty instancedScale => serializedObject.FindProperty("instancedScale");

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
            EditorGUILayout.PropertyField(numberOfRandomPaths, new GUIContent("Number To Generate"));
            if (GUILayout.Button("Add Random Paths"))
            {
                PathfindingManager.searchRandomPaths = true;
            }

            EditorGUILayout.PropertyField(startManualPath, new GUIContent("Start Node"));
            EditorGUILayout.PropertyField(endManualPath, new GUIContent("End Node"));
            if (GUILayout.Button("Add Manual Path"))
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
                PathfindingManager.addRandomBlockedNode = true;
            
            EditorGUILayout.PropertyField(manualBlockNode);

            EditorGUILayout.Space();
            if (GUILayout.Button("Add Manual Blocked Node"))
                PathfindingManager.addManualBlockedNode = true;

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
                PathfindingManager.InitMatrices();
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