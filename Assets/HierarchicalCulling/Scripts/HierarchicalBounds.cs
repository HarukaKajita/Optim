using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Optim.HierarchicalCulling
{
    /// <summary>
    /// Hierarchical bounds container that collects renderers from its hierarchy
    /// and performs view frustum culling.
    /// </summary>
    [DisallowMultipleComponent]
    public class HierarchicalBounds : MonoBehaviour
    {
        public enum UpdateTiming
        {
            FixedUpdate,
            Update,
            Manual
        }

        [Serializable]
        public class RendererInfo
        {
            public Renderer Renderer;
        }

        [SerializeField]
        private UpdateTiming updateTiming = UpdateTiming.Update;

        [SerializeField]
        private float margin = 0f;

        [SerializeField]
        private bool skipIfSameFixedFrame = true;

        [SerializeField]
        private bool rendered = true;

        [SerializeField, HideInInspector]
        private Bounds bounds;

        [SerializeField, HideInInspector]
        private HierarchicalBounds parent;

        [SerializeField, HideInInspector]
        private List<HierarchicalBounds> children = new List<HierarchicalBounds>();

        [SerializeField, HideInInspector]
        private List<RendererInfo> renderers = new List<RendererInfo>();

        private int lastFixedUpdateFrame = -1;

        public Bounds Bounds => bounds;
        public bool Rendered => rendered;
        public HierarchicalBounds Parent => parent;
        public IReadOnlyList<HierarchicalBounds> Children => children;
        public IReadOnlyList<RendererInfo> ManagedRenderers => renderers;

        public event Action<bool> OnRenderedChanged;

        private void Reset()
        {
            RecalculateBounds();
        }

        private void Awake()
        {
            RegisterToParent();
        }

        private void OnDestroy()
        {
            UnregisterFromParent();
        }

        private void RegisterToParent()
        {
            Transform t = transform.parent;
            while (t != null)
            {
                var hb = t.GetComponent<HierarchicalBounds>();
                if (hb)
                {
                    parent = hb;
                    hb.children.Add(this);
                    break;
                }
                t = t.parent;
            }
        }

        private void UnregisterFromParent()
        {
            if (parent)
                parent.children.Remove(this);
        }

        private void Update()
        {
            if (updateTiming == UpdateTiming.Update)
                EvaluateCulling();
        }

        private void FixedUpdate()
        {
            if (updateTiming == UpdateTiming.FixedUpdate)
            {
                if (skipIfSameFixedFrame && lastFixedUpdateFrame == Time.frameCount)
                    return;
                lastFixedUpdateFrame = Time.frameCount;
                EvaluateCulling();
            }
        }

        /// <summary>
        /// Manually trigger culling evaluation.
        /// </summary>
        public void ManualUpdate()
        {
            if (updateTiming == UpdateTiming.Manual)
                EvaluateCulling();
        }

        private void EvaluateCulling()
        {
            if (Camera.main == null)
                return;
            var planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            planes = planes.Take(4).ToArray(); // near clip planeとfar clip planeは無視する
            var intersects = GeometryUtility.TestPlanesAABB(planes, bounds);
            SetRendered(intersects);
        }

        [ContextMenu("Rendered State True")]
        public void SetRenderedTrue() => SetRendered(true);

        [ContextMenu("Rendered State False")]
        public void SetRenderedFalse() => SetRendered(false);
        private void SetRendered(bool value)
        {
            if (rendered == value)
                return;
            rendered = value;

            foreach (var info in renderers)
                if (info.Renderer)
                    info.Renderer.forceRenderingOff = !rendered;

            foreach (var child in children)
                child.SetRendered(rendered);

            OnRenderedChanged?.Invoke(rendered);
        }

        /// <summary>
        /// Recalculate bounds and collect renderers.
        /// </summary>
        [ContextMenu("Recalculate Bounds")]
        public void RecalculateBounds()
        {
            renderers.Clear();
            children.Clear();
            bounds = new Bounds(transform.position, Vector3.zero);

            var childHierarchies = new List<HierarchicalBounds>();
            GetComponentsInChildren(true, childHierarchies);
            childHierarchies.Remove(this);

            var childRendererList = new List<Renderer>();
            GetComponentsInChildren(true, childRendererList);

            foreach (var child in childHierarchies)
            {
                childRendererList.RemoveAll(r => r.transform.IsChildOf(child.transform));
                children.Add(child);
                child.parent = this;
            }

            foreach (var r in childRendererList)
            {
                if (r.GetComponentInParent<HierarchicalBounds>() != this)
                    continue;
                RendererInfo info = new RendererInfo { Renderer = r};
                renderers.Add(info);
                if (bounds.size == Vector3.zero)
                    bounds = r.bounds;
                else
                    bounds.Encapsulate(r.bounds);
            }

            foreach (var child in children)
            {
                child.RecalculateBounds();
                bounds.Encapsulate(child.bounds);
            }

            bounds.Expand(margin);
        }
    }
}

