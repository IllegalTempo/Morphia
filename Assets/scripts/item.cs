using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]

public class item : Selectable
{
    public string ItemName;
    public string ItemDescription;
    public Sprite ItemIcon;
    public int ItemID = -1;
    Collider itemCollider;
    public static Vector3 inventoryOffset = new Vector3(0, 0, 16); // Offset from camera when in inventory
    public bool followCameraRotation = false; // Whether item rotates with camera
    public item StickingTo;
    Rigidbody rb;
    public NetworkObject netObj;
    
    [Header("Stick Settings")]
    public float maxStickForce = 10f; // Maximum force when far away
    public float minStickDistance = 0.5f; // Distance at which force becomes zero

    private bool PickedUp
    {
        get
        {
            return netObj.Owner != -1;
        }
    }
    private bool PickedUp_Local
    {
        get
        {
            return netObj.Owner != -1 && gamecore.instance.IsLocal(netObj.Owner);
        }
    }


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
    public virtual void StickEffect()
    {
    }
    public virtual void UnStickEffect()
    {
    }
    public virtual void DuringStickEffect()
    {

    }
    public void StickTo(item other)
    {
        netObj.Sync_Position = false;
        netObj.Sync_Rotation = false;
        Drop(transform.position);
        StickingTo = other;
        StickEffect();
    }
    public void UnStick()
    {
        netObj.Sync_Position = true;
        netObj.Sync_Rotation = true;
        StickingTo = null;
        UnStickEffect();
    }
    private void PickUpItem()
    {
        gameObject.layer = 0;
        
        rb.linearVelocity = Vector3.zero;
        gamecore.instance.LocalPlayer.playerMovement.OnPickUpItem(this);
        gamecore.instance.I_interactionSelector.PickingUp_Item = this;
        StickingTo = null;
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

        gameObject.layer = 6;

        // Remove from inventory
        gamecore.instance.LocalPlayer.playerMovement.OnDropItem(this);
        gamecore.instance.I_interactionSelector.PickingUp_Item = null;
        outline.OutlineColor = Color.white;

        this.transform.position = dropPosition;

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
        if (ClickTimer > 0)
        {
            ClickTimer -= Time.deltaTime;
            outline.OutlineWidth = 10f;
        }
        else
        {
            ClickTimer = 0f;
            outline.OutlineWidth = 5f;

        }
        
        if (LookedAt)
        {

            outline.enabled = true;
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                OnClicked();
            }
            LookedAt = false || netObj.Owner != -1;
        }
        else
        {
            outline.enabled = false;

        }
        if(PickedUp)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;

        } else
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;

        }
        if(StickingTo != null)
        {
            // Calculate distance and direction to stick target
            Vector3 directionToStick = StickingTo.transform.position - transform.position;
            float distance = directionToStick.magnitude;
            
            // Scale force based on distance - force is 0 at minStickDistance and maxStickForce when far away
            if (distance > minStickDistance)
            {
                float normalizedDistance = (distance - minStickDistance);
                float force = Mathf.Min(normalizedDistance * maxStickForce, maxStickForce);
                rb.AddForce(directionToStick.normalized * force);
            }
        }
        DuringStickEffect();

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
