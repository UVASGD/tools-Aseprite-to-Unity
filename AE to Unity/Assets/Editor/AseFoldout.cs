using UnityEngine;
using UnityEditor;

namespace ASE_to_Unity {
    public static class AseFoldout {
        public static bool BeginFold(bool state, string label) {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(3);
            EditorGUI.indentLevel++;

            bool foldState = EditorGUI.Foldout(
                EditorGUILayout.GetControlRect(),
                state, label, true);

            EditorGUI.indentLevel--;
            if (foldState) GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(1);
            EditorGUILayout.BeginVertical();

            return foldState;
        }

        public static void EndFold() {
            EditorGUILayout.EndVertical();
            GUILayout.Space(1);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);
            EditorGUILayout.EndVertical();
            GUILayout.Space(0);
        }
    }

    public static class AseArea {
        public static void Begin() {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(3);
            //EditorGUI.indentLevel++;
            
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(1);
            EditorGUILayout.BeginVertical();
        }

        public static void End() {
            EditorGUILayout.EndVertical();
            GUILayout.Space(1);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);
            EditorGUILayout.EndVertical();
            GUILayout.Space(0);
        }
    }
}
