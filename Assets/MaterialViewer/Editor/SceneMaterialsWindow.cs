using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Optim.MaterialViewer.Editor
{
    // SceneMaterialsWindow shows all materials in the current scene using
    // MultiColumnListView. Columns can be sorted by custom multi column rules
    // specified via GUI elements placed above the list view. A detail pane is
    // displayed on the left side of the window.
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

        enum SortType
        {
            Ascending,
            Descending
        }

        class SortSettingRowListData : ScriptableObject
        {
            public string[] columnName;
            public int[] priority;
            public bool[] enabled;
            public SortType[] ascending;

            public void Set(string[] columnName, int[] priority, bool[] enabled, SortType[] ascending)
            {
                this.columnName = columnName;
                this.priority = priority;
                this.enabled = enabled;
                this.ascending = ascending;
            }
        }

        private static readonly string[] ColumnNames =
        {
            "Name", "SubMeshes", "Shader", "Queue", "Instancing", "Static",
            "Keywords", "ZWrite", "Cull", "Blend", "SRP"
        };

        private const float DetailWidth = 250f;

        private readonly List<MaterialInfo> materials = new();
        private MultiColumnListView listView;
        private ScrollView detailContainer;
        private MaterialInfo selected;
        private SortSettingRowListData sortSettings;

        [MenuItem("Window/Scene Materials Viewer")]
        private static void Open()
        {
            GetWindow<SceneMaterialsWindow>("Scene Materials");
        }

        private void OnEnable()
        {
            Refresh();
            BuildUI();
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();

            var toolbar = new IMGUIContainer(DrawToolbar);
            rootVisualElement.Add(toolbar);

            var content = new VisualElement();
            content.style.flexDirection = FlexDirection.Row;
            content.style.flexGrow = 1f;

            // マルチカラムを組み合わせたソートを制御する為の GUI
            content.Add(CreateSortSettingsGUI());
            
            // ListView
            listView = CreateListView();
            listView.style.flexGrow = 1f;
            content.Add(listView);
            
            // Detail pane
            detailContainer = new ScrollView();
            detailContainer.style.width = DetailWidth;
            detailContainer.style.flexShrink = 0f;
            content.Add(detailContainer);

            UpdateDetailPane();

            rootVisualElement.Add(content);
        }

        private MultiColumnListView CreateListView()
        {
            var columns = new Columns();
            for (int i = 0; i < ColumnNames.Length; ++i)
            {
                int index = i;
                columns.Add(new Column
                {
                    title = ColumnNames[i],
                    width = 100,
                    makeCell = () => new Label(),
                    bindCell = (e, row) =>
                    {
                        var info = materials[row];
                        var label = (Label)e;
                        switch (index)
                        {
                            case 0: label.text = info.Material.name; break;
                            case 1: label.text = info.SubMeshCount.ToString(); break;
                            case 2: label.text = info.Material.shader ? info.Material.shader.name : string.Empty; break;
                            case 3: label.text = info.Material.renderQueue.ToString(); break;
                            case 4: label.text = info.Material.enableInstancing ? "On" : "Off"; break;
                            case 5: label.text = GetStaticState(info); break;
                            case 6: label.text = info.Material.shaderKeywords.Length.ToString(); break;
                            case 7: label.text = GetZWrite(info.Material); break;
                            case 8: label.text = GetCull(info.Material); break;
                            case 9: label.text = GetBlend(info.Material); break;
                            case 10: label.text = GetSRPBatching(info.Material); break;
                        }
                    }
                });
            }

            var lv = new MultiColumnListView(columns)
            {
                itemsSource = materials,
                showBorder = true,
                reorderable = true,
                selectionType = SelectionType.Single,
                fixedItemHeight = EditorGUIUtility.singleLineHeight + 2f,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
            };
            lv.selectionChanged += objs =>
            {
                selected = objs.FirstOrDefault() as MaterialInfo;
                UpdateDetailPane();
            };
            return lv;
        }

        private void Refresh()
        {
            CollectMaterials();
            sortSettings = CreateInstance<SortSettingRowListData>();
            sortSettings.Set(
                ColumnNames,
                new int[ColumnNames.Length],
                Enumerable.Repeat(true, ColumnNames.Length).ToArray(),
                Enumerable.Repeat(SortType.Ascending, ColumnNames.Length).ToArray());
            
            if (listView != null)
            {
                listView.itemsSource = materials;
                listView.Rebuild();
            }
            selected = null;
            UpdateDetailPane();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                    Refresh();
                GUILayout.FlexibleSpace();
            }
        }
        
        private VisualElement CreateSortSettingsGUI()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.paddingLeft = 5f;
            container.style.paddingRight = 5f;

            var label = new Label("Multi Column Sort Settings");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(label);
            
            // Debug.Log($"Creating ListView with {rows.Count} rows");
            var listView = new ListView(ColumnNames, 20, () => new VisualElement(), (e, i) =>
            {
                var row = e;
                row.Clear();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;

                // リストの並び順がソートの優先度
                sortSettings.priority[i] = i;
                // ソートの優先度を表示するためのラベル
                var priorityLabel = new Label((i + 1).ToString());
                priorityLabel.style.width = 20;
                row.Add(priorityLabel);
                
                // ソートの有効/無効
                var toggleToEnable = new Toggle();
                toggleToEnable.style.width = 20;
                toggleToEnable.value = sortSettings.enabled[i];

                toggleToEnable.RegisterValueChangedCallback(evt =>
                {
                    sortSettings.enabled[i] = evt.newValue;
                    ApplySort();
                });
                row.Add(toggleToEnable);

                // カラム名
                var columnLabel = new Label(ColumnNames[i]);
                columnLabel.style.width = 100;
                row.Add(columnLabel);
                
                // ソートの種類
                var sortType = new PopupField<SortType>(
                    "",
                    new List<SortType> {SortType.Ascending, SortType.Descending},
                    sortSettings.ascending[i]);
                sortType.style.width = 100;
                sortType.RegisterValueChangedCallback(evt =>
                {
                    sortSettings.ascending[i] = evt.newValue;
                    ApplySort();
                });
                row.Add(sortType);
            })
            {
                style =
                {
                    flexGrow = 1f,
                    flexShrink = 0f,
                    height = 100f
                }
            };
            var so = new SerializedObject(sortSettings);
            listView.Bind(so);
            
            listView.reorderable = true;
            listView.showBorder = true;
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            container.Add(listView);
            return container;
        }

        private void UpdateDetailPane()
        {
            detailContainer.Clear();

            if (selected == null)
            {
                detailContainer.Add(new Label("No material selected"));
                return;
            }

            var nameLabel = new Label(selected.Material.name)
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold }
            };
            detailContainer.Add(nameLabel);

            var usedByContainer = new VisualElement();
            detailContainer.Add(usedByContainer); // Add usedByContainer to detailContainer
            usedByContainer.Add(new Label("Used By:"));
            foreach (var r in selected.Renderers)
            {
                var field = new ObjectField(r.gameObject.name)
                {
                    objectType = typeof(Renderer),
                    value = r
                };
                field.SetEnabled(false);
                usedByContainer.Add(field);
            }

            detailContainer.Add(new Label("Keywords:"));
            foreach (var kw in selected.Material.shaderKeywords.OrderBy(k => k))
                detailContainer.Add(new Label(kw));
        }

        private void ApplySort()
        {
            var order = Enumerable.Range(0, ColumnNames.Length)
                .Where(i => sortSettings.priority[i] > 0)
                .OrderBy(i => sortSettings.priority[i]);

            Comparison<MaterialInfo> cmp = (a, b) => 0;
            foreach (var col in order)
            {
                var asc = sortSettings.ascending[col];
                cmp = Combine(cmp, GetCompare(col, asc == SortType.Ascending));
            }
            materials.Sort(cmp);
            listView.Rebuild();
        }

        private static Comparison<MaterialInfo> Combine(Comparison<MaterialInfo> a, Comparison<MaterialInfo> b)
        {
            return (x, y) =>
            {
                int r = a(x, y);
                return r != 0 ? r : b(x, y);
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

        private static string GetZWrite(Material mat)
        {
            if (mat.HasProperty("_ZWrite"))
                return mat.GetInt("_ZWrite") != 0 ? "On" : "Off";
            return string.Empty;
        }

        private static string GetCull(Material mat)
        {
            if (mat.HasProperty("_Cull"))
            {
                int c = mat.GetInt("_Cull");
                return ((UnityEngine.Rendering.CullMode)c).ToString();
            }
            return string.Empty;
        }

        private static string GetBlend(Material mat)
        {
            if (mat.HasProperty("_SrcBlend") && mat.HasProperty("_DstBlend"))
            {
                var src = (UnityEngine.Rendering.BlendMode)mat.GetInt("_SrcBlend");
                var dst = (UnityEngine.Rendering.BlendMode)mat.GetInt("_DstBlend");
                return $"{src}/{dst}";
            }
            return string.Empty;
        }

        private static string GetSRPBatching(Material mat)
        {
#if UNITY_EDITOR
            return "-";
#else
            return string.Empty;
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
