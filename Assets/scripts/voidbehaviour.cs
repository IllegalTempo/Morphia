using UnityEngine;

public class voidbehaviour : MonoBehaviour
{
    [Header("Bounce Settings")]
    [Tooltip("The force multiplier applied when bouncing objects back")]
    public float bounceForce = 20f;
    
    [Tooltip("Minimum force applied regardless of collision speed")]
    public float minBounceForce = 10f;

    private void OnCollisionEnter(Collision collision)
    {
        // Get the rigidbody of the object that collided with the void
        Rigidbody rb = collision.rigidbody;
        
        if (rb != null)
        {
            //teleport to spawnpoint
            transform.position = gamecore.instance.CurrentStage.Spawnpoint[0].position;
        }
    }
}
