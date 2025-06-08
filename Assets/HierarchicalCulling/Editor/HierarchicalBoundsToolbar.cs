using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Optim.HierarchicalCulling.Editor
{
#if UNITY_2021_2_OR_NEWER
    [Overlay(typeof(SceneView), "Hierarchical Bounds", true)]
    public class HierarchicalBoundsToolbar : Overlay
    {
        public override VisualElement CreatePanelContent()
        {
            var button = new ToolbarButton(() => HierarchicalBoundsGizmosVisible = !HierarchicalBoundsGizmosVisible)
            {
                text = "Toggle Bounds"
            };
            return button;
        }

        private static bool HierarchicalBoundsGizmosVisible
        {
            get => SessionState.GetBool("HB_GizmosVisible", true);
            set => SessionState.SetBool("HB_GizmosVisible", value);
        }
    }
#endif
}
