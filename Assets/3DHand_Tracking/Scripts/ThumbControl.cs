// using UnityEngine;
//NOT USE ANYMORE
// public class ThumbControl : MonoBehaviour
// {
//     [Header("Landmark References")]
//     [SerializeField] private Transform[] thumbPoints; 

//     [Header("Bone Chain")]
//     [SerializeField] private Transform[] thumbBones; 

//     [Header("Settings")]
//     public Vector3 rotationOffset;
//     public float smoothSpeed = 20f;

//     void Update()
//     {
//         if (thumbPoints == null || thumbBones == null || thumbPoints.Length < 2) return;

//         for (int i = 0; i < thumbBones.Length; i++)
//         {
//             if (thumbBones[i] == null || i + 1 >= thumbPoints.Length) continue;

//             Vector3 p1 = thumbPoints[i].position;
//             Vector3 p2 = thumbPoints[i + 1].position;

//             Vector3 direction = (p2 - p1).normalized;

//             if (direction != Vector3.zero)
//             {
//                 Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, direction);
//                 Quaternion finalRotation = targetRotation * Quaternion.Euler(rotationOffset);
                
//                 thumbBones[i].rotation = Quaternion.Slerp(thumbBones[i].rotation, finalRotation, Time.deltaTime * smoothSpeed);
//             }
//         }
//     }
// }