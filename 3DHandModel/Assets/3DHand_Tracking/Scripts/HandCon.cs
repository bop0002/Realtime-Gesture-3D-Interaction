using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandCon : MonoBehaviour
{
    [Header("References")]
    public GameObject sp1; // Wrist Point 0
    public GameObject sp2; // Middle MCP Point 9
    public GameObject upPoint; //Index MCP Point 5
    public GameObject bone; // Root Bone
    
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

        // Vẽ các Vector tính toán từ Landmark (Đường mảnh)
        Debug.DrawRay(origin, (sp2.transform.position - sp1.transform.position), Color.white); // Cổ tay -> Ngón giữa
        Debug.DrawRay(origin, (upPoint.transform.position - sp1.transform.position), Color.yellow); // Cổ tay -> Ngón trỏ

        // Vẽ hệ trục tọa độ thực tế của xương sau khi áp Rotation (Đường đậm)
        // Trục Forward (Z) của xương
        Debug.DrawRay(origin, bone.transform.forward * 0.1f, Color.blue); 
        // Trục Up (Y) của xương
        Debug.DrawRay(origin, bone.transform.up * 0.1f, Color.green);
        // Trục Right (X) của xương - Đây là trục bạn cần nằm ngang để gập ngón
        Debug.DrawRay(origin, bone.transform.right * 0.1f, Color.red);

        // Vẽ Palm Normal vừa tính được
        Debug.DrawRay(origin, currentPalmNormal * 0.15f, Color.cyan);
    }
}