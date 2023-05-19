using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICollisionIgnore
{
    Collider GetCollider();
}

public class CollisionIgnorer : MonoBehaviour
{
    private Collider myCollider;
    public Action<Vector3> onCollisionDetected;

    private void Awake()
    {
        myCollider = GetComponent<Collider>();
    }

    private bool collisionIgnored = false;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.TryGetComponent(out ICollisionIgnore otherCol))
            Physics.IgnoreCollision(myCollider, otherCol.GetCollider());
    }

    private void OnTriggerEnter(Collider other)
    {
        onCollisionDetected?.Invoke(other.transform.position);
        if (other.gameObject.TryGetComponent(out ICollisionIgnore otherCol))
            Physics.IgnoreCollision(myCollider, otherCol.GetCollider());
    }
}