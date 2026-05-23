using UnityEngine;

public class FingerControl : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandCon handMaster; 
    [SerializeField] private Transform[] targetPoints; 
    [SerializeField] private Transform[] fingerBones; 

    [Header("Settings")]
    public bool isThumb = false; 
    public Vector3 rotationOffset = new Vector3(0, 0, 0); 
    public float smoothSpeed = 20f;

    [Header("Limits")]
    public bool applyLimits = true;
    [Range(0f, 120f)]
    public float maxJointBendAngle = 90f;

    void Update()
    {
        if (handMaster == null || targetPoints == null || fingerBones == null) return;

        Vector3 globalPalmNormal = handMaster.GetPalmNormal(); 
        
        Vector3 lastValidDir = Vector3.forward;

        for (int i = 0; i < fingerBones.Length; i++)
        {
            if (fingerBones[i] == null || i + 1 >= targetPoints.Length) continue;

            Vector3 p1 = targetPoints[i].position;
            Vector3 p2 = targetPoints[i + 1].position;

            Vector3 fingerDir = (p2 - p1).normalized;

            if (fingerDir != Vector3.zero)
            {
                if (applyLimits)
                {
                    Vector3 prevDir;
                    if (i == 0) 
                    {
                        if (handMaster.sp1 != null) {
                            prevDir = (p1 - handMaster.sp1.transform.position).normalized;
                        } else {
                            prevDir = fingerDir; 
                        }
                    }
                    else 
                    {
                        prevDir = lastValidDir;
                    }

                    float bendAngle = Vector3.Angle(prevDir, fingerDir);
                    
                    if (bendAngle > maxJointBendAngle)
                    {
                        fingerDir = Vector3.RotateTowards(prevDir, fingerDir, maxJointBendAngle * Mathf.Deg2Rad, 0f);
                    }
                }
                
                lastValidDir = fingerDir;
                Vector3 palmToMCPDir;
                if (handMaster.sp1 != null) {
                    palmToMCPDir = (targetPoints[0].position - handMaster.sp1.transform.position).normalized;
                } else {
                    palmToMCPDir = handMaster.bone.transform.forward; 
                }

                Vector3 lateralDir = Vector3.Cross(globalPalmNormal, palmToMCPDir).normalized;
                

                if (lateralDir == Vector3.zero) {
                    lateralDir = handMaster.bone.transform.right; 
                }
                
                Vector3 upDir = Vector3.Cross(fingerDir, lateralDir).normalized;
                
                if (isThumb)
                {
                    upDir = Vector3.Cross(globalPalmNormal, fingerDir).normalized;
                }

                Quaternion targetRotation = Quaternion.LookRotation(fingerDir, upDir);

                fingerBones[i].rotation = Quaternion.Slerp(
                    fingerBones[i].rotation, 
                    targetRotation * Quaternion.Euler(rotationOffset), 
                    Time.deltaTime * smoothSpeed
                );
            }
        }
    }
}