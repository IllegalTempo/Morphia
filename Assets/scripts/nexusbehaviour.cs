using UnityEngine;

public class nexusbehaviour : MonoBehaviour
{
    [Header("Follow Settings")]
    public Vector3 offsetFromPlayer = new Vector3(2f, 1f, 0f); // Side offset position relative to player
    public float followSpeed = 5f; // How fast the nexus follows the player
    public float minFollowDistance = 0.5f; // Minimum distance before starting to follow
    
    [Header("Rotation Settings")]
    public Vector3 spinSpeed = new Vector3(100f, 100f,100f); // Rotation speed per axis (degrees per second)
    
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
        
        // Continuously spin the nexus
        SpinNexus();
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
        }
    }
    
    private void SpinNexus()
    {
        // Continuously rotate the nexus based on spinSpeed
        transform.Rotate(spinSpeed * Time.deltaTime, Space.Self);
    }
}
