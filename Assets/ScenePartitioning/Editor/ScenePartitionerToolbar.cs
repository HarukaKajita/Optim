using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Optim.ScenePartitioning.Editor
{
#if UNITY_2021_2_OR_NEWER
    [Overlay(typeof(SceneView), "Scene Partitioner", true)]
    public class ScenePartitionerToolbar : Overlay
    {
        public override VisualElement CreatePanelContent()
        {
            var button = new ToolbarButton(Toggle)
            {
                text = "Toggle Cells"
            };
            return button;
        }

        private static void Toggle()
        {
            bool v = SessionState.GetBool("SP_ShowCells", true);
            SessionState.SetBool("SP_ShowCells", !v);
            SceneView.RepaintAll();
        }
    }
#endif
}
