using UnityEngine;
using UnityEditor;

namespace Pathfinding
{
    [CustomEditor(typeof(InitializePathfinding))]
    //[CanEditMultipleObjects]
    public class InitializerEditor : Editor
    {
        InitializePathfinding pathfinding => (InitializePathfinding)target;

        SerializedProperty numberOfRandomPaths => serializedObject.FindProperty("numOfRandomPaths");
        SerializedProperty startManualPath => serializedObject.FindProperty("start");
        SerializedProperty endManualPath => serializedObject.FindProperty("end");

        SerializedProperty numberOfBlocksToAdd => serializedObject.FindProperty("numbOfRandomBlockedNodes");
        SerializedProperty manualBlockNode => serializedObject.FindProperty("blockedNode");

        SerializedProperty size;

        bool showBlocked;
        bool showPaths;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            showPaths = EditorGUILayout.Foldout(showPaths, "Paths");

            if (showPaths)
                ShowPaths();

            showBlocked = EditorGUILayout.Foldout(showBlocked, "Blocked Nodes");

            if (showBlocked)
                ShowBlockedNodes();
        }

        void ShowPaths()
        {
            EditorGUILayout.PropertyField(numberOfRandomPaths, new GUIContent("Number To Generate"));
            if (GUILayout.Button("Add Random Paths"))
            {
                pathfinding.searchRandomPaths = true;
            }

            EditorGUILayout.PropertyField(startManualPath, new GUIContent("Start Node"));
            EditorGUILayout.PropertyField(endManualPath, new GUIContent("End Node"));
            if (GUILayout.Button("Add Manual Path"))
                pathfinding.searchManualPath = true;

            serializedObject.ApplyModifiedProperties();
        }

        void ShowBlockedNodes()
        {
            EditorGUILayout.PropertyField(numberOfBlocksToAdd, new GUIContent("Number To Generate"));
            if (GUILayout.Button("Add Random Blocked Nodes"))
                pathfinding.addRandomBlockedNode = true;
            
            EditorGUILayout.PropertyField(manualBlockNode);

            if (GUILayout.Button("Add Manual Blocked Node"))
                pathfinding.addManualBlockedNode = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}