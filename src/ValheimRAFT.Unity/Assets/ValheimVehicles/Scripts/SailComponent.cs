using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimRAFT
{
    public class SailComponent : MonoBehaviour
    {
        public ZNetView m_znetview;
        public SkinnedMeshRenderer m_mesh;
        public Cloth m_sailCloth;
        public List<Vector3> m_sailCorners = new List<Vector3>();
        public float m_sailSubdivision = 0.5f;

        public static List<SailComponent> m_sailComponents = new List<SailComponent>();

        [Flags]
        public enum SailLockedSide
        {
            None = 0,

            A = 1,
            B = 2,
            C = 4,
            D = 8,

            Everything = A | B | C | D,
        };

        public SailLockedSide m_lockedSailSides;
        public SailLockedSide m_lockedSailCorners;

        public void Awake()
        {
            m_sailComponents.Add(this);
            m_sailCloth = GetComponent<Cloth>();
            m_sailCloth.useTethers = false;
            m_mesh = GetComponent<SkinnedMeshRenderer>();
            m_znetview = GetComponent<ZNetView>();

            LoadZDO();

            CreateSailMesh();
        }

        public void OnDestroy()
        {
            m_sailComponents.Remove(this);
        }
        
        public void LoadZDO()
        {
            if (!m_znetview || m_znetview.m_zdo == null)
                return;

            var corners = m_znetview.m_zdo.GetInt($"MBSail_m_sailCorners_count");

            m_sailCorners.Clear();

            for (int i = 0; i < corners; i++)
            {
                m_sailCorners.Add(m_znetview.m_zdo.GetVec3($"MBSail_m_sailCorners_{i}", Vector3.zero));
            }

            m_lockedSailSides = (SailLockedSide)m_znetview.m_zdo.GetInt($"MBSail_m_lockedSailSides");
            m_lockedSailCorners = (SailLockedSide)m_znetview.m_zdo.GetInt($"MBSail_m_lockedSailCorners");
        }

        public void SaveZDO()
        {
            if (!m_znetview || m_znetview.m_zdo == null)
                return;

            m_znetview.m_zdo.Set($"MBSail_m_sailCorners_count", m_sailCorners.Count);

            for (int i = 0; i < m_sailCorners.Count; i++)
            {
                m_znetview.m_zdo.Set($"MBSail_m_sailCorners_{i}", m_sailCorners[i]);
            }

            m_znetview.m_zdo.Set($"MBSail_m_lockedSailSides", (int)m_lockedSailSides);
            m_znetview.m_zdo.Set($"MBSail_m_lockedSailCorners", (int)m_lockedSailCorners);
        }

        public void CreateSailMesh()
        {
            m_sailCloth.enabled = false;

            if (m_sailCorners.Count < 3)
                return;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            if (m_sailCorners.Count == 3)
            {
                vertices.Add(m_sailCorners[0]);
                vertices.Add(m_sailCorners[1]);
                vertices.Add(m_sailCorners[2]);
                triangles.Add(0);
                triangles.Add(1);
                triangles.Add(2);
                uvs.Add(new Vector2() { x = 0, y = 0 });
                uvs.Add(new Vector2() { x = 1, y = 0 });
                uvs.Add(new Vector2() { x = 1, y = 1 });
            }

            else if (m_sailCorners.Count == 4)
            {
                var dx = (m_sailCorners[1] - m_sailCorners[0]).magnitude;
                var dy = (m_sailCorners[2] - m_sailCorners[0]).magnitude;

                var dxs = Mathf.Round(dx / m_sailSubdivision);
                var dys = Mathf.Round(dy / m_sailSubdivision);

                for (int x = 0; x <= dxs; x++)
                {
                    for (int y = 0; y <= dys; y++)
                    {
                        var xs1 = Vector3.Lerp(m_sailCorners[0], m_sailCorners[1], x / dxs);
                        var xs2 = Vector3.Lerp(m_sailCorners[3], m_sailCorners[2], x / dxs);

                        var ys1 = Vector3.Lerp(xs1, xs2, y / dys);

                        vertices.Add(ys1);
                        uvs.Add(new Vector2() { x = x / dxs, y = y / dys });
                    }
                }

                dxs++;
                dys++;

                for (int x = 0; x < dxs - 1; x++)
                {
                    for (int y = 0; y < dys - 1; y++)
                    {
                        triangles.Add((int)((dys * x) + y) + 1);
                        triangles.Add((int)((dys * x) + y));
                        triangles.Add((int)((dys * x) + y) + (int)dys);

                        triangles.Add((int)((dys * x) + y) + 1);
                        triangles.Add((int)((dys * x) + y) + (int)dys);
                        triangles.Add((int)((dys * x) + y) + (int)dys + 1);
                    }
                }
            }

            Mesh mesh = new Mesh();

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.Optimize();
            mesh.RecalculateNormals();

            if (m_sailCorners.Count == 3)
            {
                var sqrSubDist = m_sailSubdivision * m_sailSubdivision;
                while (true)
                {
                    var dist = (mesh.vertices[mesh.triangles[0]] - mesh.vertices[mesh.triangles[1]]).sqrMagnitude;

                    if (dist < sqrSubDist)
                        break;

                    MeshHelper.Subdivide(mesh);
                }
            }

            m_mesh.sharedMesh = mesh;

            ClothSkinningCoefficient[] coefficients = new ClothSkinningCoefficient[mesh.vertexCount];

            for (int i = 0; i < coefficients.Length; i++)
            {
                coefficients[i].maxDistance = float.MaxValue;
                coefficients[i].collisionSphereDistance = float.MaxValue;
            }

            if (m_sailCorners.Count == 3)
            {
                m_lockedSailCorners &= ~SailLockedSide.D;
                m_lockedSailSides &= ~SailLockedSide.D;

                if (m_lockedSailCorners == SailLockedSide.None && m_lockedSailSides == SailLockedSide.None)
                    m_lockedSailCorners = SailLockedSide.Everything;

                var sideA = (m_sailCorners[0] - m_sailCorners[1]).normalized;
                var sideB = (m_sailCorners[1] - m_sailCorners[2]).normalized;
                var sideC = (m_sailCorners[2] - m_sailCorners[0]).normalized;

                for (int i = 0; i < mesh.vertices.Length; i++)
                {
                    if (m_lockedSailCorners.HasFlag(SailLockedSide.A) && mesh.vertices[i] == m_sailCorners[0]) coefficients[i].maxDistance = 0;
                    if (m_lockedSailCorners.HasFlag(SailLockedSide.B) && mesh.vertices[i] == m_sailCorners[1]) coefficients[i].maxDistance = 0;
                    if (m_lockedSailCorners.HasFlag(SailLockedSide.C) && mesh.vertices[i] == m_sailCorners[2]) coefficients[i].maxDistance = 0;

                    if (m_lockedSailSides.HasFlag(SailLockedSide.A) && Mathf.Abs(Vector3.Dot((m_sailCorners[0] - mesh.vertices[i]).normalized, sideA)) >= 0.9999f)
                        coefficients[i].maxDistance = 0;
                    if (m_lockedSailSides.HasFlag(SailLockedSide.B) && Mathf.Abs(Vector3.Dot((m_sailCorners[1] - mesh.vertices[i]).normalized, sideB)) >= 0.9999f)
                        coefficients[i].maxDistance = 0;
                    if (m_lockedSailSides.HasFlag(SailLockedSide.C) && Mathf.Abs(Vector3.Dot((m_sailCorners[2] - mesh.vertices[i]).normalized, sideC)) >= 0.9999f)
                        coefficients[i].maxDistance = 0;
                }
            }
            else if (m_sailCorners.Count == 4)
            {
                if (m_lockedSailCorners == SailLockedSide.None && m_lockedSailSides == SailLockedSide.None)
                    m_lockedSailCorners = SailLockedSide.Everything;

                var sideA = (m_sailCorners[0] - m_sailCorners[1]).normalized;
                var sideB = (m_sailCorners[1] - m_sailCorners[2]).normalized;
                var sideC = (m_sailCorners[2] - m_sailCorners[3]).normalized;
                var sideD = (m_sailCorners[3] - m_sailCorners[0]).normalized;

                for (int i = 0; i < mesh.vertices.Length; i++)
                {
                    if (m_lockedSailCorners.HasFlag(SailLockedSide.A) && mesh.vertices[i] == m_sailCorners[0]) coefficients[i].maxDistance = 0;
                    if (m_lockedSailCorners.HasFlag(SailLockedSide.B) && mesh.vertices[i] == m_sailCorners[1]) coefficients[i].maxDistance = 0;
                    if (m_lockedSailCorners.HasFlag(SailLockedSide.C) && mesh.vertices[i] == m_sailCorners[2]) coefficients[i].maxDistance = 0;
                    if (m_lockedSailCorners.HasFlag(SailLockedSide.D) && mesh.vertices[i] == m_sailCorners[3]) coefficients[i].maxDistance = 0;

                    if (m_lockedSailSides.HasFlag(SailLockedSide.A) && Mathf.Abs(Vector3.Dot((m_sailCorners[0] - mesh.vertices[i]).normalized, sideA)) >= 0.9999f)
                        coefficients[i].maxDistance = 0;
                    if (m_lockedSailSides.HasFlag(SailLockedSide.B) && Mathf.Abs(Vector3.Dot((m_sailCorners[1] - mesh.vertices[i]).normalized, sideB)) >= 0.9999f)
                        coefficients[i].maxDistance = 0;
                    if (m_lockedSailSides.HasFlag(SailLockedSide.C) && Mathf.Abs(Vector3.Dot((m_sailCorners[2] - mesh.vertices[i]).normalized, sideC)) >= 0.9999f)
                        coefficients[i].maxDistance = 0;
                    if (m_lockedSailSides.HasFlag(SailLockedSide.D) && Mathf.Abs(Vector3.Dot((m_sailCorners[3] - mesh.vertices[i]).normalized, sideD)) >= 0.9999f)
                        coefficients[i].maxDistance = 0;
                }
            }

            m_sailCloth.coefficients = coefficients;
            m_sailCloth.useGravity = true;
            m_sailCloth.enabled = true;
        }

        private void OnDrawGizmos()
        {
            for (int i = 0; i < m_sailCorners.Count; i++)
            {
                if (i == 0) Gizmos.color = Color.red;
                if (i == 1) Gizmos.color = Color.green;
                if (i == 2) Gizmos.color = Color.blue;
                if (i == 3) Gizmos.color = Color.white;

                Gizmos.DrawSphere(transform.position + m_sailCorners[i], 0.1f);
            }
        }
    }

}