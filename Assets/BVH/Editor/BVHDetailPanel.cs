using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Optim.BVH.Editor
{
    /// <summary>
    /// 選択されたBVHノードの詳細情報を表示するパネル
    /// </summary>
    internal class BVHDetailPanel
    {
        private readonly VisualElement detailPane;
        private readonly Label detailLabel;
        private readonly ListView rendererList;

        public VisualElement PanelElement => detailPane;

        public BVHDetailPanel()
        {
            detailPane = new VisualElement();
            detailPane.style.flexGrow = 1;
            detailPane.style.paddingLeft = 4;
            
            detailLabel = new Label();
            
            rendererList = new ListView();
            rendererList.style.flexGrow = 1;
            rendererList.style.flexShrink = 1;
            rendererList.style.paddingLeft = 16;
            rendererList.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            
            detailPane.Add(detailLabel);
            detailPane.Add(rendererList);
        }

        public void UpdateDetails(BVHNode node)
        {
            if (node == null)
            {
                detailLabel.text = string.Empty;
                rendererList.itemsSource = null;
                rendererList.Rebuild();
                return;
            }

            var renderers = CollectRenderers(node);
            detailLabel.text = $"Center: {node.Bounds.center}\nSize: {node.Bounds.size}\nRenderers: {renderers.Count}";

            if (rendererList.makeItem == null)
                rendererList.makeItem = () => new Label();
            if (rendererList.bindItem == null)
                rendererList.bindItem = (ve, i) => ((Label)ve).text = ((Renderer)rendererList.itemsSource[i]).name;
            rendererList.itemsSource = renderers;
            rendererList.Rebuild();
        }

        private static List<Renderer> CollectRenderers(BVHNode node)
        {
            var list = new List<Renderer>();
            CollectRecursive(node, list);
            return list;
        }

        private static void CollectRecursive(BVHNode node, List<Renderer> list)
        {
            if (node == null) return;
            if (node.IsLeaf)
            {
                list.AddRange(node.Renderers);
            }
            else
            {
                CollectRecursive(node.Left, list);
                CollectRecursive(node.Right, list);
            }
        }
    }
}