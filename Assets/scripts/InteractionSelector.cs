using UnityEngine;
using UnityEngine.InputSystem; // Use the new Input System
using System;

public class InteractionSelector : MonoBehaviour
{
    PlayerMovement movement;
    public Selectable seenOutline = null;
    public item PickingUp_Item = null;
    private void Start()
    {
        if (!GetComponent<NetworkPlayerObject>().IsLocal)
        {
            Destroy(this);
        }
        else
        {
            movement = GetComponent<PlayerMovement>();

            gamecore.instance.I_interactionSelector = this;

        }

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
                item seenitem = (item)seenOutline;

                PickingUp_Item.StickTo(seenitem);
                if (NetworkSystem.instance.IsServer)
                {
                    PacketSend.Server_Send_Distribute_stickItem(PickingUp_Item.netObj.Identifier, seenitem.netObj.Identifier);
                }
                else
                {
                    PacketSend.Client_Send_stickItem(PickingUp_Item.netObj.Identifier, seenitem.netObj.Identifier);
                }
            }
                catch (Exception e)
                {
                Debug.LogError($"Failed to send stick item packet: {e.Message}");
            }
        }

    }


}
}
