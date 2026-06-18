using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandCon : MonoBehaviour
{
    [Header("References")]
    public GameObject sp1; 
    public GameObject sp2; 
    public GameObject upPoint; 
    public GameObject bone; 
    
    [Header("Rotation")]
    public Vector3 rotationOffset = new Vector3(0, 0, 0); 
    private Vector3 currentPalmNormal;

    public Vector3 GetPalmNormal() {
        return currentPalmNormal;
    }

    void Update()
    {
        if (sp1 == null || sp2 == null || upPoint == null || bone == null) return;
        Vector3 fingerDirection = (sp2.transform.position - sp1.transform.position).normalized;

        Vector3 sideDirection = (upPoint.transform.position - sp1.transform.position).normalized;

        currentPalmNormal = Vector3.Cross(fingerDirection, sideDirection).normalized;
        DrawDebugLines();
        if (fingerDirection != Vector3.zero && currentPalmNormal != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentPalmNormal, fingerDirection);
            bone.transform.rotation = targetRotation * Quaternion.Euler(rotationOffset);
        }
        bone.transform.position = sp1.transform.position;
    }
    void DrawDebugLines()
    {
        Vector3 origin = sp1.transform.position;

        Debug.DrawRay(origin, (sp2.transform.position - sp1.transform.position), Color.white); 
        Debug.DrawRay(origin, (upPoint.transform.position - sp1.transform.position), Color.yellow); 

        Debug.DrawRay(origin, bone.transform.forward * 0.1f, Color.blue); 
        Debug.DrawRay(origin, bone.transform.up * 0.1f, Color.green);
        Debug.DrawRay(origin, bone.transform.right * 0.1f, Color.red);

        Debug.DrawRay(origin, currentPalmNormal * 0.15f, Color.cyan);
    }
}