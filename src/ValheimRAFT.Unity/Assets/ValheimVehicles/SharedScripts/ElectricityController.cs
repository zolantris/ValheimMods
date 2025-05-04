// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
    public class ElectricityController : SingletonBehaviour<ElectricityController>
    {
        private static readonly int MaxPoints = 50;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private static Material _materialInstance;

        [Header("Wire Configuration")]
        public Material wireMaterial;
        public float maxConnectionDistance = 50f;
        public bool debugRefreshEveryFixedUpdate;
        private readonly List<LineRenderer> _activeLines = new();
        private readonly WaitForSeconds _refreshDelay = new(0.05f);
        private Coroutine _delayedRefreshRoutine;
        private ElectricitySparkManager _sparkManager;

        private void FixedUpdate()
        {
            if (debugRefreshEveryFixedUpdate)
            {
                ScheduleRefresh();
            }
        }

        private void OnDestroy()
        {
            ElectricityPylonRegistry.OnPylonListChanged -= ScheduleRefresh;
        }

        public override void OnAwake()
        {
            _sparkManager = gameObject.AddComponent<ElectricitySparkManager>();
            ElectricityPylonRegistry.OnPylonListChanged += ScheduleRefresh;
            ScheduleRefresh();
        }

        public void ScheduleRefresh()
        {
            if (_delayedRefreshRoutine != null) return;
            if (!this || !gameObject || !Application.isPlaying) return;

            _delayedRefreshRoutine = StartCoroutine(DelayedRefresh());
        }

        private IEnumerator DelayedRefresh()
        {
            yield return _refreshDelay;

            if (!this || !gameObject || !Application.isPlaying) yield break;

            _delayedRefreshRoutine = null;
            RefreshConnections();
        }

        private void RefreshConnections()
        {
            foreach (var line in _activeLines)
            {
                if (line != null) Destroy(line.gameObject);
            }
            _activeLines.Clear();

            var allPylons = ElectricityPylonRegistry.All;
            var unvisited = new HashSet<ElectricityPylon>(allPylons.Where(p => p && p.wireConnector));

            while (unvisited.Count > 0)
            {
                var chain = new List<ElectricityPylon>();
                var pending = new Queue<ElectricityPylon>();

                var start = unvisited.First();
                pending.Enqueue(start);
                unvisited.Remove(start);

                while (pending.Count > 0)
                {
                    var current = pending.Dequeue();
                    chain.Add(current);

                    foreach (var neighbor in unvisited.ToList())
                    {
                        if (neighbor == null || neighbor.wireConnector == null) continue;

                        float distance = Vector3.Distance(current.wireConnector.position, neighbor.wireConnector.position);
                        if (distance <= maxConnectionDistance)
                        {
                            pending.Enqueue(neighbor);
                            unvisited.Remove(neighbor);
                        }
                    }
                }

                if (chain.Count >= 2)
                {
                    GenerateChainLine(chain);
                }
            }
        }

        private void GenerateChainLine(List<ElectricityPylon> chain)
        {
            var obj = new GameObject("PylonChainConnector");
            obj.transform.position = chain[0].wireConnector.position;

            var line = obj.AddComponent<LineRenderer>();
            _activeLines.Add(line);

            line.material = GetWireMaterial();
            line.widthMultiplier = 0.02f;
            line.textureMode = LineTextureMode.Tile;
            line.useWorldSpace = true;

            var points = new List<Vector3>();
            foreach (var pylon in chain)
            {
                points.Add(pylon.wireConnector.position);
            }

            var curvedPoints = new List<Vector3>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 start = points[i];
                Vector3 end = points[i + 1];

                for (int j = 0; j < MaxPoints; j++)
                {
                    float t = j / (float)(MaxPoints - 1);
                    Vector3 point = Vector3.Lerp(start, end, t);
                    point += Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.2f;
                    curvedPoints.Add(point);
                }
            }

            line.positionCount = curvedPoints.Count;
            line.SetPositions(curvedPoints.ToArray());
        }

        private Material GetWireMaterial()
        {
            if (wireMaterial != null) return wireMaterial;

            if (_materialInstance == null)
            {
                _materialInstance = new Material(Shader.Find("Sprites/Default"))
                {
                    color = Color.black
                };
                _materialInstance.EnableKeyword("_EMISSION");
                _materialInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                _materialInstance.SetColor(EmissionColor, Color.black * 1.5f);
            }

            return _materialInstance;
        }
    }
}
