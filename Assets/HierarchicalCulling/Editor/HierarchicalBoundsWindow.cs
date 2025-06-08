using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Optim.HierarchicalCulling.Editor
{
    public class HierarchicalBoundsWindow : EditorWindow
    {
        private Vector2 scroll;
        private List<HierarchicalBounds> roots = new List<HierarchicalBounds>();

        [MenuItem("Window/Hierarchical Bounds Viewer")]
        static void Open()
        {
            GetWindow<HierarchicalBoundsWindow>("Hierarchical Bounds");
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Refresh"))
            {
                Refresh();
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var root in roots)
            {
                DrawHierarchy(root, 0);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawHierarchy(HierarchicalBounds hb, int indent)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 16);
            bool enabled = hb.Rendered;
            bool toggled = EditorGUILayout.ToggleLeft(hb.name, enabled);
            if (toggled != enabled)
            {
                hb.ManualUpdate();
            }
            if (GUILayout.Button("Recalc", GUILayout.Width(60)))
            {
                hb.RecalculateBounds();
            }
            EditorGUILayout.EndHorizontal();

            foreach (var child in hb.Children)
            {
                DrawHierarchy(child, indent + 1);
            }
        }

        private void Refresh()
        {
            roots.Clear();
            foreach (var hb in GameObject.FindObjectsOfType<HierarchicalBounds>())
            {
                if (hb.Parent == null)
                    roots.Add(hb);
            }
        }
    }
}

