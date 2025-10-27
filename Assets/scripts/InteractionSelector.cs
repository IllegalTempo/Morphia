using UnityEngine;
using UnityEngine.InputSystem; // Use the new Input System

public class InteractionSelector : MonoBehaviour
{
    PlayerMovement movement;
    public Selectable seenOutline = null;
    public Selectable ClickedOutline = null;
    private void Start()
    {
        movement = GetComponent<PlayerMovement>();
        if (!GetComponent<NetworkPlayerObject>().IsLocal) Destroy(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (gamecore.instance.InLobby) return;
        Ray ray = new Ray(movement.playerCamera.transform.position, movement.playerCamera.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, gamecore.instance.Interactable))
        {
            //search for nearest parent that have selectable component
            seenOutline = hit.collider.gameObject.GetComponent<Selectable>();
            if (seenOutline == null) return;
            seenOutline.LookedAt = true;
            // Use the new Input System for mouse click
            
        }
        if (seenOutline != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if(seenOutline.LookedAt)
            {
                ClickedOutline = seenOutline;
                ClickedOutline.OnClicked();
            }
            
        }
    }
}
