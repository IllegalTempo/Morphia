using UnityEngine;

public class Blackhole : item
{
    [Header("Blackhole Settings")]
    public float attractionRadius = 10f; // Radius within which players are attracted
    public float attractionForce = 20f; // Force applied to attract players
    public float maxAttractionSpeed = 15f; // Maximum speed at which players are pulled
    
    private LayerMask playerLayer;

    private void Start()
    {
        // Get the player layer from gamecore (assuming it's defined there)
        // If not, you can set it manually in the inspector or use a default layer
        playerLayer = LayerMask.GetMask("Player");
    }

    protected override void Update()
    {
        base.Update();
        
        // Only apply attraction when the item is not picked up
        if (netObj.Owner == -1)
        {
            AttractPlayers();
        }
    }

    private void AttractPlayers()
    {
        // Find all player objects within the attraction radius
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, attractionRadius);
        
        foreach (Collider col in nearbyObjects)
        {
            // Check if the object has a PlayerMovement component
            PlayerMovement player = col.GetComponent<PlayerMovement>();
            
            if (player != null)
            {
                // Get the player's rigidbody
                Rigidbody playerRb = col.GetComponent<Rigidbody>();
                
                if (playerRb != null)
                {
                    // Calculate direction from player to blackhole
                    Vector3 directionToBlackhole = (transform.position - playerRb.position).normalized;
                    float distance = Vector3.Distance(transform.position, playerRb.position);
                    
                    // Calculate attraction force (stronger when closer)
                    // Inverse square law for more realistic attraction
                    float forceMagnitude = attractionForce * (1f - (distance / attractionRadius));
                    forceMagnitude = Mathf.Max(0, forceMagnitude); // Ensure non-negative
                    
                    // Apply force towards the blackhole
                    Vector3 attractionVector = directionToBlackhole * forceMagnitude;
                    playerRb.AddForce(attractionVector, ForceMode.Force);
                    
                    // Optional: Limit the maximum speed towards the blackhole
                    Vector3 velocityTowardBlackhole = Vector3.Project(playerRb.linearVelocity, directionToBlackhole);
                    if (velocityTowardBlackhole.magnitude > maxAttractionSpeed)
                    {
                        Vector3 excessVelocity = velocityTowardBlackhole - (directionToBlackhole * maxAttractionSpeed);
                        playerRb.linearVelocity -= excessVelocity;
                    }
                }
            }
        }
    }

    // Optional: Visualize the attraction radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.3f); // Purple transparent
        Gizmos.DrawSphere(transform.position, attractionRadius);
        
        Gizmos.color = new Color(0.5f, 0f, 0.5f, 1f); // Purple solid
        Gizmos.DrawWireSphere(transform.position, attractionRadius);
    }
}
