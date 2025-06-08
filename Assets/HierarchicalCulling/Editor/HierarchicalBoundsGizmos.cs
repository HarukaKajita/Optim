using UnityEditor;
using UnityEngine;

namespace Optim.HierarchicalCulling.Editor
{
    [InitializeOnLoad]
    internal static class HierarchicalBoundsGizmos
    {
        static HierarchicalBoundsGizmos()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView view)
        {
            if (!SessionState.GetBool("HB_GizmosVisible", true))
                return;

            foreach (var hb in GameObject.FindObjectsOfType<HierarchicalBounds>())
            {
                Handles.color = hb.Rendered ? Color.green : Color.red;
                Handles.DrawWireCube(hb.Bounds.center, hb.Bounds.size);
            }
        }
    }
}

