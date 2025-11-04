using UnityEngine;
using UnityEngine.InputSystem;

public class Seat : item
{
    [Header("Seat Settings")]
    public Vector3 sitPositionOffset = new Vector3(0, 0.5f, 0); // Offset from seat center where player sits
    public Vector3 sitRotationEuler = new Vector3(0, 0, 0); // Rotation when sitting
    
    private PlayerMovement seatedPlayer;
    private Rigidbody seatedPlayerRb;
    private Vector3 originalPosition;
    private RigidbodyConstraints originalConstraints;
    private bool isOccupied = false;

    public override void OnClicked()
    {
        // If seat is occupied, don't allow pickup or sitting
        if (isOccupied)
        {
            Debug.Log("Seat is occupied!");
            return;
        }

        // Check if item is in inventory
        if (netObj.Owner != -1)
        {
            // Normal item pickup/drop behavior
            base.OnClicked();
        }
        else
        {
            // Item is on ground - check if local player wants to sit
            if (gamecore.instance.LocalPlayer != null)
            {
                PlayerMovement localPlayerMovement = gamecore.instance.LocalPlayer.playerMovement;
                
                // Check distance to player
                float distance = Vector3.Distance(transform.position, localPlayerMovement.transform.position);
                if (distance < 8f) // Within 3 units to sit
                {
                    SitDown(localPlayerMovement);
                }
                else
                {
                    Debug.Log("Too far from seat!");
                }
            }
        }
    }

    private void SitDown(PlayerMovement player)
    {
        if (isOccupied || player == null) return;
        gamecore.instance.subtitle.text = "Click E to Exit Seat";


        seatedPlayer = player;
        seatedPlayerRb = player.GetComponent<Rigidbody>();
        
        if (seatedPlayerRb == null) return;

        isOccupied = true;

        // Store original state
        originalConstraints = seatedPlayerRb.constraints;
        originalPosition = player.transform.position;

        // Position player on seat
        Vector3 sitPosition = transform.position + sitPositionOffset;
        player.transform.position = sitPosition;
        player.transform.rotation = Quaternion.Euler(sitRotationEuler);

        // Freeze player in place
        seatedPlayerRb.linearVelocity = Vector3.zero;
        seatedPlayerRb.angularVelocity = Vector3.zero;
        seatedPlayerRb.constraints = RigidbodyConstraints.FreezeAll;

        Debug.Log($"{player.name} sat down on {ItemName}");
    }

    private void StandUp()
    {

        if (!isOccupied || seatedPlayer == null) return;
        gamecore.instance.subtitle.text = "";

        // Restore player movement
        if (seatedPlayerRb != null)
        {
            seatedPlayerRb.constraints = originalConstraints;
            
            // Move player slightly forward from seat
            Vector3 standPosition = transform.position + transform.forward * 1.5f;
            standPosition.y = transform.position.y; // Keep same height
            seatedPlayer.transform.position = standPosition;
        }

        Debug.Log($"{seatedPlayer.name} stood up from {ItemName}");

        // Clear references
        seatedPlayer = null;
        seatedPlayerRb = null;
        isOccupied = false;
    }

    protected override void Update()
    {
        base.Update();

        // Check if seated player wants to stand up
        if (isOccupied && seatedPlayer != null)
        {
            // Keep player positioned on seat
            if (seatedPlayerRb != null)
            {
                Vector3 sitPosition = transform.position + sitPositionOffset;
                seatedPlayer.transform.position = sitPosition;
                seatedPlayerRb.linearVelocity = Vector3.zero;
            }

            // Check for stand up input (E key)
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                StandUp();
            }
        }

        // If seated player is destroyed or null, clean up
        if (isOccupied && seatedPlayer == null)
        {
            isOccupied = false;
            seatedPlayerRb = null;
        }
    }

    public override void StickEffect()
    {
        base.StickEffect();
        
        // If someone is sitting, make them stand up when item is picked up
        if (isOccupied)
        {
            StandUp();
        }
    }

    private void OnDestroy()
    {
        // Make sure player is freed if seat is destroyed
        if (isOccupied)
        {
            StandUp();
        }
    }

    // Optional: Visualize the sit position in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 sitPos = transform.position + sitPositionOffset;
        Gizmos.DrawWireSphere(sitPos, 0.3f);
        
        // Draw forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(sitPos, transform.forward);
        
        // Draw interaction range
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}
