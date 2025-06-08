using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Optim.MaterialViewer.Editor
{
    internal class SceneMaterialsWindow : EditorWindow
    {
        private class MaterialInfo
        {
            public Material Material;
            public int SubMeshCount;
            public HashSet<Renderer> Renderers = new();
            public bool UsedStatic;
            public bool UsedDynamic;
        }

        private class MaterialTreeView : TreeView
        {
            private readonly List<MaterialInfo> items;
            private readonly MultiColumnHeader header;
            public event Action<MaterialInfo> SelectionChanged;

            public MaterialTreeView(TreeViewState state, MultiColumnHeader header, List<MaterialInfo> items)
                : base(state, header)
            {
                this.items = items;
                this.header = header;
                rowHeight = EditorGUIUtility.singleLineHeight + 2f;
                showAlternatingRowBackgrounds = true;
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem { id = 0, depth = -1, displayName = "root" };
                var all = new List<TreeViewItem>();
                for (int i = 0; i < items.Count; ++i)
                {
                    all.Add(new TreeViewItem { id = i + 1, depth = 0, displayName = items[i].Material.name });
                }
                SetupParentsAndChildrenFromDepths(root, all);
                return root;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = items[args.item.id - 1];
                for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    Rect rect = args.GetCellRect(i);
                    CenterRectUsingSingleLineHeight(ref rect);
                    int col = args.GetColumn(i);
                    switch (col)
                    {
                        case 0:
                            EditorGUI.LabelField(rect, item.Material.name);
                            break;
                        case 1:
                            EditorGUI.LabelField(rect, item.SubMeshCount.ToString());
                            break;
                        case 2:
                            EditorGUI.LabelField(rect, item.Material.shader != null ? item.Material.shader.name : "");
                            break;
                        case 3:
                            EditorGUI.LabelField(rect, item.Material.renderQueue.ToString());
                            break;
                        case 4:
                            EditorGUI.LabelField(rect, item.Material.enableInstancing ? "On" : "Off");
                            break;
                        case 5:
                            EditorGUI.LabelField(rect, GetStaticState(item));
                            break;
                        case 6:
                            EditorGUI.LabelField(rect, item.Material.shaderKeywords.Length.ToString());
                            break;
                        case 7:
                            EditorGUI.LabelField(rect, GetZWrite(item.Material));
                            break;
                        case 8:
                            EditorGUI.LabelField(rect, GetCull(item.Material));
                            break;
                        case 9:
                            EditorGUI.LabelField(rect, GetBlend(item.Material));
                            break;
                        case 10:
                            EditorGUI.LabelField(rect, GetSRPBatching(item.Material));
                            break;
                    }
                }
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                if (selectedIds.Count > 0)
                    SelectionChanged?.Invoke(items[selectedIds[0] - 1]);
                else
                    SelectionChanged?.Invoke(null);
            }

            protected override void DoubleClickedItem(int id)
            {
                var info = items[id - 1];
                EditorGUIUtility.PingObject(info.Material);
                Selection.activeObject = info.Material;
            }

            protected override void SortByMultipleColumns()
            {
                if (items.Count == 0)
                    return;

                var columns = header.state.sortedColumns;
                Comparison<MaterialInfo> comparison = (a, b) => 0;
                foreach (var col in columns)
                {
                    bool asc = header.IsSortedAscending(col);
                    comparison = Combine(comparison, GetCompare(col, asc));
                }
                items.Sort(comparison);
                Reload();
            }

            private static Comparison<MaterialInfo> Combine(Comparison<MaterialInfo> a, Comparison<MaterialInfo> b)
            {
                return (x, y) =>
                {
                    int res = a(x, y);
                    return res != 0 ? res : b(x, y);
                };
            }

            private static Comparison<MaterialInfo> GetCompare(int column, bool asc)
            {
                int Sign(int r) => asc ? r : -r;
                return column switch
                {
                    0 => (a, b) => Sign(string.Compare(a.Material.name, b.Material.name, StringComparison.OrdinalIgnoreCase)),
                    1 => (a, b) => Sign(a.SubMeshCount.CompareTo(b.SubMeshCount)),
                    2 => (a, b) => Sign(string.Compare(a.Material.shader?.name, b.Material.shader?.name, StringComparison.OrdinalIgnoreCase)),
                    3 => (a, b) => Sign(a.Material.renderQueue.CompareTo(b.Material.renderQueue)),
                    4 => (a, b) => Sign(a.Material.enableInstancing.CompareTo(b.Material.enableInstancing)),
                    5 => (a, b) => Sign(string.Compare(GetStaticState(a), GetStaticState(b), StringComparison.Ordinal)),
                    6 => (a, b) => Sign(a.Material.shaderKeywords.Length.CompareTo(b.Material.shaderKeywords.Length)),
                    7 => (a, b) => Sign(string.Compare(GetZWrite(a.Material), GetZWrite(b.Material), StringComparison.Ordinal)),
                    8 => (a, b) => Sign(string.Compare(GetCull(a.Material), GetCull(b.Material), StringComparison.Ordinal)),
                    9 => (a, b) => Sign(string.Compare(GetBlend(a.Material), GetBlend(b.Material), StringComparison.Ordinal)),
                    10 => (a, b) => Sign(string.Compare(GetSRPBatching(a.Material), GetSRPBatching(b.Material), StringComparison.Ordinal)),
                    _ => (x, y) => 0
                };
            }
        }

        private MaterialTreeView treeView;
        private MultiColumnHeader header;
        private SearchField searchField;
        private List<MaterialInfo> materials = new();
        private MaterialInfo selected;

        [MenuItem("Window/Scene Materials Viewer")]
        private static void Open()
        {
            GetWindow<SceneMaterialsWindow>("Scene Materials");
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            CollectMaterials();
            var state = new TreeViewState();
            header = new MultiColumnHeader(CreateHeaderState());
            header.canSort = true;
            header.sortingChanged += treeView?.OnSortingChanged;
            treeView = new MaterialTreeView(state, header, materials);
            treeView.SelectionChanged += info => selected = info;
            searchField = new SearchField();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    Refresh();
                }
                GUILayout.FlexibleSpace();
                treeView.searchString = searchField.OnToolbarGUI(treeView.searchString);
            }

            Rect rect = new Rect(0, 20, position.width, position.height - 20);
            float detailWidth = 250f;
            Rect left = new Rect(rect.x, rect.y, detailWidth, rect.height);
            Rect right = new Rect(rect.x + detailWidth, rect.y, rect.width - detailWidth, rect.height);

            DrawDetailPane(left);
            treeView.OnGUI(right);
        }

        private void DrawDetailPane(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            if (selected != null)
            {
                EditorGUILayout.LabelField(selected.Material.name, EditorStyles.boldLabel);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Keywords:");
                foreach (var kw in selected.Material.shaderKeywords.OrderBy(k => k))
                {
                    EditorGUILayout.LabelField("  " + kw);
                }
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Used By:");
                foreach (var r in selected.Renderers)
                {
                    EditorGUILayout.ObjectField(r.gameObject.name, r, typeof(Renderer), true);
                }
            }
            else
            {
                GUILayout.Label("No material selected");
            }
            GUILayout.EndArea();
        }

        private void CollectMaterials()
        {
            materials.Clear();
            var renderers = FindObjectsOfType<Renderer>(true);
            var dict = new Dictionary<Material, MaterialInfo>();
            foreach (var r in renderers)
            {
                bool isStatic = r.gameObject.isStatic;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; ++i)
                {
                    var m = mats[i];
                    if (m == null)
                        continue;
                    if (!dict.TryGetValue(m, out var info))
                    {
                        info = new MaterialInfo { Material = m };
                        dict[m] = info;
                    }
                    info.SubMeshCount++;
                    info.Renderers.Add(r);
                    if (isStatic) info.UsedStatic = true; else info.UsedDynamic = true;
                }
            }
            materials.AddRange(dict.Values);
        }

        private static MultiColumnHeaderState CreateHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Name"), width = 150, minWidth = 100, allowToggleVisibility = true},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("SubMeshes"), width = 70, minWidth = 50, allowToggleVisibility = true},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Shader"), width = 150, minWidth = 100, allowToggleVisibility = true},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Queue"), width = 60, minWidth = 50, allowToggleVisibility = true},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Instancing"), width = 70, minWidth = 60, allowToggleVisibility = true},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Static"), width = 60, minWidth = 50, allowToggleVisibility = true},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Keywords"), width = 70, minWidth = 60, allowToggleVisibility = true},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("ZWrite"), width = 60, minWidth = 50, allowToggleVisibility = true},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Cull"), width = 60, minWidth = 50, allowToggleVisibility = true},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Blend"), width = 80, minWidth = 60, allowToggleVisibility = true},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("SRP"), width = 60, minWidth = 50, allowToggleVisibility = true}
            };
            return new MultiColumnHeaderState(columns);
        }

        private static string GetZWrite(Material mat)
        {
            if (mat.HasProperty("_ZWrite"))
                return mat.GetInt("_ZWrite") != 0 ? "On" : "Off";
            return "";
        }

        private static string GetCull(Material mat)
        {
            if (mat.HasProperty("_Cull"))
            {
                int c = mat.GetInt("_Cull");
                return ((UnityEngine.Rendering.CullMode)c).ToString();
            }
            return "";
        }

        private static string GetBlend(Material mat)
        {
            if (mat.HasProperty("_SrcBlend") && mat.HasProperty("_DstBlend"))
            {
                var src = (UnityEngine.Rendering.BlendMode)mat.GetInt("_SrcBlend");
                var dst = (UnityEngine.Rendering.BlendMode)mat.GetInt("_DstBlend");
                return $"{src}/{dst}";
            }
            return "";
        }

        private static string GetSRPBatching(Material mat)
        {
#if UNITY_EDITOR
            var code = UnityEditor.Rendering.ShaderUtil.GetSRPBatcherCompatibilityCode(mat.shader);
            return code == UnityEditor.Rendering.ShaderUtil.SRPBatcherCompatibilityCode.SRPBatcherCompatible ? "Compatible" : "Not";
#else
            return "";
#endif
        }

        private static string GetStaticState(MaterialInfo info)
        {
            if (info.UsedStatic && info.UsedDynamic)
                return "Mixed";
            if (info.UsedStatic)
                return "Static";
            return "Dynamic";
        }
    }
}
