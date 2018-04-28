using UnityEngine;
using UnityEditor;

namespace ASE_to_Unity {
    public static class AseGUILayout {

        public static Rect GUIRect(float width, float height) {
            return GUILayoutUtility.GetRect(width, height,
                GUILayout.ExpandWidth(width <= 0),
                GUILayout.ExpandHeight(height <= 0));
        }


        /// <summary> A foldout with a box indentation. </summary>
        public static bool BeginFold(bool state, string label) {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(3);
            EditorGUI.indentLevel++;

            bool foldState = EditorGUI.Foldout(
                EditorGUILayout.GetControlRect(),
                state, label, true);

            EditorGUI.indentLevel--;
            if (foldState) GUILayout.Space(8);

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

        /// <summary> Begin a boxed in area of elements </summary>
        public static void BeginArea() {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(3);
            //EditorGUI.indentLevel++;

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(1);
            EditorGUILayout.BeginVertical();
        }

        public static void EndArea() {
            EditorGUILayout.EndVertical();
            GUILayout.Space(1);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);
            EditorGUILayout.EndVertical();
            GUILayout.Space(0);
        }
    }
}
