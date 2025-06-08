using UnityEditor;
using UnityEngine;

namespace Optim.ScenePartitioning.Editor
{
    [CustomEditor(typeof(ScenePartitioner))]
    public class ScenePartitionerEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var sp = (ScenePartitioner)target;
            if (sp.Method != ScenePartitioner.PartitionMethod.Voronoi2D && sp.Method != ScenePartitioner.PartitionMethod.Voronoi3D)
                return;

            var serializedSeeds = serializedObject.FindProperty("voronoiSeeds");
            for (int i = 0; i < serializedSeeds.arraySize; ++i)
            {
                var element = serializedSeeds.GetArrayElementAtIndex(i);
                var posProp = element.FindPropertyRelative("position");
                var weightProp = element.FindPropertyRelative("weight");

                EditorGUI.BeginChangeCheck();
                Vector3 pos = posProp.vector3Value;
                Handles.Label(pos + Vector3.up * 0.2f, $"{i}");
                pos = Handles.PositionHandle(pos, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    posProp.vector3Value = pos;
                    serializedObject.ApplyModifiedProperties();
                }

                Handles.BeginGUI();
                Vector2 guiPos = HandleUtility.WorldToGUIPoint(pos + Vector3.up * 0.3f);
                Rect rect = new Rect(guiPos.x - 50, guiPos.y - 10, 100, 20);
                float w = weightProp.floatValue;
                w = GUI.HorizontalSlider(rect, w, 0.1f, 5f);
                weightProp.floatValue = w;
                Handles.EndGUI();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
