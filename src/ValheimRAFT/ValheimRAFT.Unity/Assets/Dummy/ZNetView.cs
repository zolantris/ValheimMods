using System;
using UnityEngine;

namespace ValheimRAFT
{
    public class ZNetView : MonoBehaviour
    {
        internal static bool m_forceDisableInit = false;
        internal ZDO m_zdo = null;
    }

    public class ZDO
    {
        internal int GetInt(string v)
        {
            return 0;
        }

        internal Vector3 GetVec3(string v, Vector3 zero)
        {
            return zero;
        }

        internal void Set(string v, int count)
        {
            
        }

        internal void Set(string v, Vector3 vector3)
        {
            
        }
    }
}