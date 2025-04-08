using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Doorway : MonoBehaviour
{
    [Tooltip("Unique identifier for matching doorways")]
    public string socketType = "default";

    [Tooltip("Door dimensions: X = width, Y = height.")]
    public Vector2 doorSize = new Vector2(1.0f, 2.0f);

    [Tooltip("Indicates if this door is connected.")]
    public bool connected = false;

    [Tooltip("If enabled, snap this doorway to the nearest edge of a floor.")]
    public bool snapToFloorEdge = true;

    [Tooltip("Layer mask for detecting the floor")]
    public LayerMask floorLayer;

    public static readonly Dictionary<string, Vector2> SocketTypeToDoorSize = new Dictionary<string, Vector2>{
        {"default", new Vector2(1.0f, 2.0f)},
        {"small", new Vector2(0.8f, 2.0f)},
        {"large", new Vector2(1.5f, 2.5f)}
    };

    void OnValidate()
    {
        if(SocketTypeToDoorSize.ContainsKey(socketType))
        {
            doorSize = SocketTypeToDoorSize[socketType];
        }

        if(snapToFloorEdge) {
            SnapToFloorEdge();
        }
    }

    void SnapToFloorEdge() {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Vector3 rayDirection = Vector3.down;
        float rayDistance = 10f;

        // Draw the raycast in blue.
        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.blue, 0.2f);

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, rayDistance, floorLayer)) {
            // Draw a green line at the hit point.
            Debug.DrawLine(hit.point, hit.point + Vector3.up, Color.green, 0.2f);

            Collider floorCollider = hit.collider;
            if (floorCollider != null) {
                Bounds floorBounds = floorCollider.bounds;

                // Convert the door's position into the floor's local space.
                Vector3 localPos = floorCollider.transform.InverseTransformPoint(transform.position);
                
                // For simplicity, snap based on the local X-axis of the floor.
                float halfWidth = floorBounds.size.x * 0.5f;
                // Snap to the right edge if the door is on the positive side; otherwise, snap to the left edge.
                localPos.x = (localPos.x >= 0) ? halfWidth : -halfWidth;
                
                // Transform back to world coordinates.
                Vector3 snappedPos = floorCollider.transform.TransformPoint(localPos);

                // Draw a yellow line showing how far the door moves.
                Debug.DrawLine(transform.position, new Vector3(snappedPos.x, transform.position.y, snappedPos.z), Color.yellow, 0.2f);

                // Update the door's position while preserving its original y-level if desired.
                transform.position = new Vector3(snappedPos.x, transform.position.y, snappedPos.z);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = connected ? Color.green : Color.red;

        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;

        Vector3 rectSize = new Vector3(doorSize.x, doorSize.y, 0.1f);
        Gizmos.DrawWireCube(Vector3.zero, rectSize);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1f);
    }
}
