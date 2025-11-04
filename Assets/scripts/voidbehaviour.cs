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
            // Calculate bounce direction based on collision normal
            Vector3 bounceDirection = Vector3.up;
            
            // Calculate force magnitude based on relative velocity
            float velocityMagnitude = collision.relativeVelocity.magnitude;
            float forceMagnitude = Mathf.Max(velocityMagnitude * bounceForce, minBounceForce);
            
            // Apply the bounce force
            rb.linearVelocity = Vector3.zero; // Reset current velocity 
            rb.AddForce(bounceDirection * forceMagnitude, ForceMode.Impulse);
        }
    }
}
