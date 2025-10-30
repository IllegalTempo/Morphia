using UnityEngine;
using UnityEngine.InputSystem; // Use the new Input System

public class InteractionSelector : MonoBehaviour
{
    PlayerMovement movement;
    public Selectable seenOutline = null;
    public item PickingUp_Item = null;
    private void Start()
    {
        movement = GetComponent<PlayerMovement>();
        if (!GetComponent<NetworkPlayerObject>().IsLocal) Destroy(this);
        gamecore.instance.I_interactionSelector = this;

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
            //If press F this frame
            if (PickingUp_Item != null && Keyboard.current.fKey.wasPressedThisFrame && seenOutline is item)
            {
                PickingUp_Item.StickTo((item)seenOutline);
            }

        }

        
    }
}
