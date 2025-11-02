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

    [Header("Nametag Settings")]
    public bool showNameTag = true;
    public Vector3 nameTagOffset = new Vector3(0, 1.5f, 0);
    private GameObject nameTagObject;
    private ItemNameTag nameTag;

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
            Drop(transform.position,true);
            if (NetworkSystem.instance.IsServer)
            {
                PacketSend.Server_Send_DistributeDrop(netObj.Identifier);
            }
            else
            {
                PacketSend.Client_Send_PickUpItem(netObj.Identifier, -1);
            }
        }
        else
        {
            // Item is not in inventory, so pick it up
            PickUpItem(gamecore.instance.LocalPlayer.NetworkID);
            if (NetworkSystem.instance.IsServer)
            {
                PacketSend.Server_Send_DistributePickUpItem(netObj.Identifier, netObj.Owner);
            }
            else
            {
                PacketSend.Client_Send_PickUpItem(netObj.Identifier, netObj.Owner);
            }
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
        Drop(transform.position,true);
        StickingTo = other;

        // Ignore collision between this item and the item it's sticking to
        if (itemCollider != null && other.itemCollider != null)
        {
            Physics.IgnoreCollision(itemCollider, other.itemCollider, true);
        }

        StickEffect();
    }
    public void UnStick()
    {
        Debug.Log($"Unsticking item: {ItemName}");
        netObj.Sync_Position = true;
        netObj.Sync_Rotation = true;

        // Re-enable collision between this item and the item it was sticking to
        if (StickingTo != null && itemCollider != null && StickingTo.itemCollider != null)
        {
            Physics.IgnoreCollision(itemCollider, StickingTo.itemCollider, false);
        }

        UnStickEffect(); // Call before setting StickingTo to null

        StickingTo = null;
    }
    public void PickUpItem(int networkID)
    {
        Debug.Log($"Picking up item: {ItemName}");
        gameObject.layer = 0;

        bool local = gamecore.instance.IsLocal(networkID);
        if (local)
        {
            gamecore.instance.LocalPlayer.playerMovement.OnPickUpItem(this);
            gamecore.instance.I_interactionSelector.PickingUp_Item = this;
        }
        rb.linearVelocity = Vector3.zero;
        
        UnStick();
        outline.OutlineColor = Color.aquamarine;
        LookedAt = true;
        // Get collider in itself or children and disable it
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
        netObj.Owner = networkID;
        
        // Hide nametag when picked up
        if (nameTag != null)
        {
            nameTag.SetVisibility(false);
        }
    }
    
    public void Drop(Vector3 dropPosition,bool local)
    {
        gameObject.layer = 6;

        // Remove from inventory
        if(local)
        {
            gamecore.instance.LocalPlayer.playerMovement.OnDropItem(this);
            gamecore.instance.I_interactionSelector.PickingUp_Item = null;
        }
        
        outline.OutlineColor = Color.white;

        this.transform.position = dropPosition;

        itemCollider.enabled = true;
        netObj.Owner = -1;
        
        // Show nametag when dropped
        if (nameTag != null)
        {
            nameTag.SetVisibility(true);
        }
    }

    private void Start()
    {
        if (ItemID == -1) { Destroy(this.gameObject); Debug.LogError($"Destroyed {gameObject.name}"); }
        itemCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        netObj = GetComponent<NetworkObject>();
        if (!NetworkSystem.instance.IsServer)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        
        // Spawn a nametag above the item displaying itemname
        if (showNameTag && !string.IsNullOrEmpty(ItemName))
        {
            SpawnNameTag();
        }
    }
    
    private void SpawnNameTag()
    {
        // Create nametag GameObject
        nameTagObject = new GameObject($"{ItemName}_NameTag");
        nameTagObject.transform.SetParent(transform);
        nameTagObject.transform.localPosition = nameTagOffset;
        nameTagObject.transform.localRotation = Quaternion.identity;
        nameTagObject.transform.localScale = Vector3.one;
        
        // Add and initialize ItemNameTag component
        nameTag = nameTagObject.AddComponent<ItemNameTag>();
        nameTag.Initialize(ItemName, this);
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
        if (PickedUp)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;

        }
        else
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;

        }
        if (StickingTo != null)
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
