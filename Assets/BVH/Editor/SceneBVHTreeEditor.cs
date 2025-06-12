using UnityEditor;
using UnityEngine;
using Optim.BVH;

namespace Optim.BVH.Editor
{
    /// <summary>
    /// SceneBVHTree 用のカスタムインスペクタ。BVH ビューアを開くボタンを提供します。
    /// </summary>
    [CustomEditor(typeof(SceneBVHTree))]
    internal class SceneBVHTreeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Open BVH Viewer"))
            {
                BVHViewerWindow.Open((SceneBVHTree)target);
            }
        }
    }
}
