using UnityEngine;

public class nexusbehaviour : MonoBehaviour
{
    [Header("Follow Settings")]
    public Vector3 offsetFromPlayer = new Vector3(2f, 1f, 0f); // Side offset position relative to player
    public float followSpeed = 5f; // How fast the nexus follows the player
    public float rotationSpeed = 5f; // How fast the nexus rotates to face movement direction
    public float minFollowDistance = 0.5f; // Minimum distance before starting to follow
    
    private NetworkPlayerObject localPlayer;
    
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    void Update()
    {
        // Get local player reference if we don't have it
        if (localPlayer == null && gamecore.instance != null)
        {
            localPlayer = gamecore.instance.LocalPlayer;
        }
        
        // Follow the local player if it exists
        if (localPlayer != null)
        {
            FollowPlayer();
        }
    }
    
    private void FollowPlayer()
    {
        // Calculate target position to the side of the player
        Vector3 playerForward = localPlayer.transform.forward;
        Vector3 playerRight = localPlayer.transform.right;
        
        // Calculate offset position relative to player's rotation
        Vector3 targetPosition = localPlayer.transform.position 
            + playerRight * offsetFromPlayer.x 
            + Vector3.up * offsetFromPlayer.y 
            + playerForward * offsetFromPlayer.z;
        
        // Check distance to target
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        // Only move if distance is greater than minimum
        if (distanceToTarget > minFollowDistance)
        {
            // Smoothly move towards target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            
            // Rotate to face the direction of movement
            Vector3 moveDirection = targetPosition - transform.position;
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
