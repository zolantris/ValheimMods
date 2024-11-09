using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = System.Numerics.Quaternion;

namespace ValheimRAFT.Unity.Scene
{
    public class AnimatedSwitch : MonoBehaviour
    {
        public GameObject pivotPoint;
        public GameObject attachPoint;
        public GameObject lever;
        public bool IsForward = false;

        void Awake()
        {
            if (!pivotPoint)
            {
                pivotPoint = transform.Find("pivotpoint")?.gameObject;
            }
            if (!pivotPoint)
            {
                attachPoint = transform.Find("lever/attachpoint")?.gameObject;
            }
            if (!lever)
            {
                lever = transform.Find("lever")?.gameObject;
            }
        }


        public float GetIncrement()
        {
            return (IsForward ? 1 : -1) * 0.1f;
        }

        public float SpeedFactor = 10f;
        public bool CanUpdate = false;

        public float lastAngle = 0f;
        public float currentAngle = 0f;
        public void UpdateDirection(float increment)
        {
            lastAngle = currentAngle;
            currentAngle = lastAngle + increment;
            // Use a reasonable speed factor, such as 1f or any other value to control the speed

            // Rotate the lever around the pivot point

            // Check for forward/backward direction
            if (currentAngle > -80)
            {
                IsForward = false;
            }
            if (currentAngle < 80)
            {
                IsForward = true;
            }
        }
        
        // Utility function to normalize an angle to the range of 0 to 360 degrees
        private float NormalizeAngle(float angle)
        {
            if (angle < 0) angle += 360f;  // Ensure positive angle
            if (angle >= 360) angle -= 360f;  // Wrap the angle back within 360
            return angle;
        }
        
        public void UpdateRotation()
        {
            var normalizedXangle = NormalizeAngle(lever.transform.rotation.eulerAngles.x);
            // if (normalizedXangle <= 300f && normalizedXangle > 30f)
            // {
            //     if (NormalizeAngle(normalizedXangle) >= 30f)
            //     {
            //         IsForward = false;
            //         // lever.transform.RotateAround(pivotPoint.transform.position, Vector3.right, GetIncrement()*SpeedFactor * Time.deltaTime);
            //     } else if (normalizedXangle <= 300f)
            //     {
            //         IsForward = true;
            //     }
            //     return;
            // }
            lever.transform.RotateAround(pivotPoint.transform.position, Vector3.left, GetIncrement()*SpeedFactor * Time.deltaTime);
            var normalizedYangle =
                lever.transform.rotation.eulerAngles.normalized;

            // lever.transform.rotation = UnityEngine.Quaternion.Euler(Mathf.Clamp(lever.transform.rotation.eulerAngles.x, 60, 270), lever.transform.rotation.eulerAngles.y, lever.transform.rotation.eulerAngles.z);

            // var currentX = lever.transform.rotation.eulerAngles.x;

            // if (currentX >= 0 && currentX <= 90f || currentX > 270 && currentX <= 360f)
            // {
            //     IsForward = false;
            //     // do nothing
            // }
            //
            // if (currentX > 90f && currentX <= 270f)
            // {
            //      IsForward = true;
            //     
            // }
        }

        
        // Update is called once per frame
        void FixedUpdate()
        {
            // if (CanUpdate)
            // { 
            //     UpdateDirection(GetIncrement());
            // }
            // var angle = Mathf.Lerp(lastAngle, currentAngle, Time.fixedDeltaTime * SpeedFactor);
            // if (!Mathf.Approximately(angle, currentAngle))
            // {
            //     lever.transform.RotateAround(pivotPoint.transform.position, Vector3.forward, 0.001f * Time.deltaTime);
            //     // lever.transform.rotation = Quaternion.Euler(new Vector3(angle + 90f,0f, 0f));
            // }
            // lever.transform.RotateAround(pivotPoint.transform.position, Vector3.left, SpeedFactor * Time.deltaTime);
            UpdateRotation();

        }
    }
}


