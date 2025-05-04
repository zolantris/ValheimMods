// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
    [RequireComponent(typeof(LineRenderer))]
    public class DynamicLineConnector : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        public Transform? startTransform;
        public Transform? endTransform;
        public bool useCurvedWire;
        public bool enableGlow = true;

        [SerializeField] private float wireSagAmount = 0.2f;
        [SerializeField] private int segmentCount = 20;
        [SerializeField] private Color wireColor = Color.black;

        [SerializeField] private float smoothTime = 0.05f;
        private float _emissionTime;
        private Vector3 _endVelocity;

        private LineRenderer _line;
        private Vector3[] _points;
        private Vector3 _smoothedEnd;
        public Func<RaycastHit, bool>? RaycastFilter;

        public Func<bool>? ShouldRender;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
            _points = new Vector3[segmentCount];
            _line.positionCount = segmentCount;
            _line.startWidth = 0.02f;
            _line.endWidth = 0.02f;
            _line.useWorldSpace = false;

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = wireColor;

            if (enableGlow)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (mat.HasProperty(EmissionColorId))
                {
                    mat.SetColor(EmissionColorId, wireColor);
                }
            }

            _line.material = mat;
        }

        private void FixedUpdate()
        {
            bool shouldRender = startTransform && endTransform;
            if (ShouldRender != null)
                shouldRender = ShouldRender.Invoke();

            if (!shouldRender)
            {
                _line.enabled = false;
                return;
            }

            _line.enabled = true;

            Vector3 start = startTransform.localPosition;
            Vector3 targetWorldEnd = endTransform.position;
            _smoothedEnd = Vector3.SmoothDamp(_smoothedEnd, targetWorldEnd, ref _endVelocity, smoothTime);
            Vector3 end = transform.InverseTransformPoint(_smoothedEnd);

            for (int i = 0; i < segmentCount; i++)
            {
                float t = i / (float)(segmentCount - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                if (useCurvedWire) point += transform.InverseTransformDirection(Vector3.up) * Mathf.Sin(t * Mathf.PI) * wireSagAmount;
                _points[i] = point;
            }

            _line.SetPositions(_points);

            if (enableGlow && _line.material.HasProperty(EmissionColorId))
            {
                _emissionTime += Time.fixedDeltaTime;
                float pulse = Mathf.PingPong(_emissionTime * 2f, 1f);
                Color glow = wireColor * (0.75f + pulse * 0.75f);
                _line.material.SetColor(EmissionColorId, glow);
            }
        }

        public void SetEndpoints(Transform? start, Transform? end)
        {
            startTransform = start;
            endTransform = end;
        }
    }
}
