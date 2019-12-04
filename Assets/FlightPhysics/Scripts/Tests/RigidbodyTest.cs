using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyTest : MonoBehaviour
{
    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 force = transform.up * Physics.gravity.magnitude * _rb.mass;
        _rb.AddForce(force);
        Debug.DrawRay(transform.position, force, Color.green);
    }
}
