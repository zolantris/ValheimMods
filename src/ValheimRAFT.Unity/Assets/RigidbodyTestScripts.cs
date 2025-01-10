using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidbodyTestScripts : MonoBehaviour
{
    public Rigidbody rbTarget;
    // Start is called before the first frame update
    private void Awake()
    {
        if (!rbTarget)
        {
            rbTarget = GetComponentInChildren<Rigidbody>();
        }
    }

    void FixedUpdate()
    {
        rbTarget.MovePosition(rbTarget.position + Vector3.forward * Time.fixedDeltaTime * 0.1f);
        var rotation = rbTarget.rotation;
        rbTarget.MoveRotation(Quaternion.Euler(rotation.eulerAngles.x,rotation.eulerAngles.y + 15f * Time.fixedDeltaTime,rotation.eulerAngles.z));
    }
}
