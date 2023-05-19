using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
    public Vector3 cameraOffset;

    [Range(-8, 8)] public float cameraZoom;

    [Range(-1, 5)] public float sideAngleStrength = 1;

    [SerializeField] private bool forceRight = false;
    [SerializeField] private bool forceLeft = false;

    public bool isCameraTarget;
    [HideInInspector] public Transform cameraTarget;

    private void Start()
    {
        cameraTarget = transform.GetChild(0);
        if (isCameraTarget) TargetGroupControllerSystem.AddTarget(cameraTarget, 0, 0, 0);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(GetComponent<BoxCollider>().bounds.center, GetComponent<BoxCollider>().bounds.size);
    }
}