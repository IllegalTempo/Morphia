using UnityEngine;

[RequireComponent(typeof(NetworkObject))]

public class item : Selectable
{
    public string ItemName;
    public string ItemDescription;
    public Sprite ItemIcon;
    public int ItemID = -1;
    private Transform OriginalParent;
    Collider itemCollider;
    public static Vector3 inventoryOffset = new Vector3(0, 0, 16); // Offset from camera when in inventory
    public bool followCameraRotation = false; // Whether item rotates with camera

    Rigidbody rb;
    public NetworkObject netObj;

    public override void OnClicked()
    {
        base.OnClicked();
        // Check if item is already in inventory
        if (netObj.Owner != -1)
        {
            if (!gamecore.instance.IsLocal(netObj.Owner)) return;

            // Item is in inventory, so drop it
            Drop(transform.position);
        }
        else
        {
            // Item is not in inventory, so pick it up
            PickUpItem();
        }
    }

    private void PickUpItem()
    {

        rb.linearVelocity = Vector3.zero;
        gamecore.instance.LocalPlayer.playerMovement.OnPickUpItem(this);
        outline.OutlineColor = Color.aquamarine;
        LookedAt = true;
        // Get collider in itself or children and disable it
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
        netObj.Owner = gamecore.instance.LocalPlayer.NetworkID;

        if (NetworkSystem.instance.IsServer)
        {
            PacketSend.Server_Send_DistributePickUpItem(netObj.Identifier, netObj.Owner);
        }
        else
        {
            PacketSend.Client_Send_PickUpItem(netObj.Identifier, netObj.Owner);
        }

    }
    public void Drop(Vector3 dropPosition)
    {


        // Remove from inventory
        gamecore.instance.LocalPlayer.playerMovement.OnDropItem(this);
        outline.OutlineColor = Color.white;

        this.transform.position = dropPosition;
        this.transform.parent = OriginalParent;

        itemCollider.enabled = true;
        netObj.Owner = -1;

        if (NetworkSystem.instance.IsServer)
        {
            PacketSend.Server_Send_DistributePickUpItem(netObj.Identifier, -1);
        }
        else
        {
            PacketSend.Client_Send_PickUpItem(netObj.Identifier, -1);
        }

    }

    private void Start()
    {
        if (ItemID == -1) { Destroy(this.gameObject); Debug.LogError($"Destroyed {gameObject.name}"); }
        OriginalParent = this.transform.parent;
        itemCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        netObj = GetComponent<NetworkObject>();
        if(!NetworkSystem.instance.IsServer)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (netObj.Owner == -1)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        } else
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        if (gamecore.instance.IsLocal(netObj.Owner) && Camera.main != null)
        {
            // Update position to follow camera
            transform.position = Camera.main.transform.position + Camera.main.transform.rotation * inventoryOffset;

            // Optionally update rotation to follow camera
            if (followCameraRotation)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }
    }


}
