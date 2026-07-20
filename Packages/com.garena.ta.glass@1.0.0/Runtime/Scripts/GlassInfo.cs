using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace XKnight.Glass
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class GlassInfo : MonoBehaviour
    {
        [Header("Glass Culling Box")]
        [SerializeField] private Vector3 boxCenter = Vector3.zero;
        [SerializeField] private Vector3 boxSize = Vector3.one;

        [Header("Gizmo")]
        [SerializeField] private bool drawGizmo = true;

        private void Update()
        {
            if (!XKnightFrostedGlass.ShouldApplyFrustumCulling())
                return;
            
            if (IsVisibleByCamera(GetMainCamera()))
                XKnightFrostedGlass.NotifyGlassInFrustum();
        }

        private Camera _cachedMainCamera;
        private readonly Plane[] _planes = new Plane[6];
        private readonly Vector3[] _corners = new Vector3[8];
        
        private Camera GetMainCamera()
        {
            if (_cachedMainCamera == null)
                _cachedMainCamera = Camera.main;
            return _cachedMainCamera;
        }
        
        private bool IsVisibleByCamera(Camera cam)
        {
            if (cam == null) return false;
            GeometryUtility.CalculateFrustumPlanes(cam, _planes);
            var bounds = GetWorldBounds();
            return GeometryUtility.TestPlanesAABB(_planes, bounds);
        }

        private Bounds GetWorldBounds()
        {
            Vector3 localCenter = boxCenter;
            Vector3 localExtents = boxSize * 0.5f;
            var m = transform.localToWorldMatrix;
            
            int i = 0;
            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                Vector3 local = localCenter + Vector3.Scale(localExtents, new Vector3(xi, yi, zi));
                _corners[i++] = m.MultiplyPoint3x4(local);
            }

            Bounds b = new Bounds(_corners[0], Vector3.zero);
            for (int c = 1; c < _corners.Length; c++) b.Encapsulate(_corners[c]);
            return b;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;
            var b = GetWorldBounds();
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}

