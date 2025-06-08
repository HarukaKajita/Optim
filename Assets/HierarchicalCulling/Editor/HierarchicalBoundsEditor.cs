using UnityEditor;
using UnityEngine;

namespace Optim.HierarchicalCulling.Editor
{
    [CustomEditor(typeof(HierarchicalBounds))]
    internal class HierarchicalBoundsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Recalculate Bounds"))
            {
                foreach (var t in targets)
                {
                    var hb = (HierarchicalBounds)t;
                    Undo.RecordObject(hb, "Recalculate Bounds");
                    hb.RecalculateBounds();
                    EditorUtility.SetDirty(hb);
                }
            }
        }
    }
}
