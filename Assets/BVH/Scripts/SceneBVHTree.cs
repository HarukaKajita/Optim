using UnityEngine;

namespace Optim.BVH
{
    /// <summary>
    /// シーン全体の BVH を保持し、他のスクリプトから参照できるようにするコンポーネント。
    /// </summary>
    [ExecuteAlways]
    public class SceneBVHTree : MonoBehaviour
    {
        [SerializeField]
        private bool buildOnStart = true;

        [SerializeField]
        private int leafSize = 4;

        [SerializeField]
        private BVHTree tree = new BVHTree();

        /// <summary>構築済み BVH への参照。</summary>
        public BVHTree Tree => tree;

        private void Start()
        {
            if (buildOnStart)
                Build();
        }

        /// <summary>
        /// シーン内の Renderer から BVH を再構築します。
        /// </summary>
        [ContextMenu("Build BVH From Scene")]
        public void Build()
        {
            tree.BuildFromScene(leafSize);
        }
    }
}
