using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotorController : MonoBehaviour
{
    [SerializeField]
    public Rigidbody m_rigidbody;
    [SerializeField]
public    Transform rotationTransform;
    void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    public Quaternion deltaDegrees = new Quaternion();
    public float deltaTimeDiff = 0f;
    private void FixedUpdate()
    {
        deltaTimeDiff += Time.fixedDeltaTime * 50;
        if (deltaTimeDiff > 360f)
        {
            deltaTimeDiff = 0f;
        }
        
        deltaDegrees = Quaternion.Euler(deltaTimeDiff,0,0);
        // m_rigidbody.transform.localRotation = deltaDegrees;
        m_rigidbody.transform.RotateAround(rotationTransform.position, new Vector3(1f, 0f,0f), Time.deltaTime * 50f);
    }

    // Start is called before the first frame update
    void Start()
    {
        // HingeJoint hinge = GetComponent<HingeJoint>();
        //
        // // Make the hinge motor rotate with 90 degrees per second and a strong force.
        // JointMotor motor = hinge.motor;
        // motor.force = 100;
        // motor.targetVelocity = 900;
        // motor.freeSpin = true;
        // hinge.motor = motor;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
