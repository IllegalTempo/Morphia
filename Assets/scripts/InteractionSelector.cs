using UnityEngine;
using UnityEngine.InputSystem; // Use the new Input System

public class InteractionSelector : MonoBehaviour
{
    PlayerMovement movement;
    public Selectable seenOutline = null;
    public Selectable ClickedOutline = null;
    
    private InputAction clickAction;
    
    private void Start()
    {
        movement = GetComponent<PlayerMovement>();
        if (!GetComponent<NetworkPlayerObject>().IsLocal)
        {
            Destroy(this);
            return;
        }
        
        // Initialize click input action
        clickAction = new InputAction("Click", binding: "<Mouse>/leftButton");
        clickAction.AddBinding("<Gamepad>/buttonSouth");
        
        // Subscribe to click event
        clickAction.performed += OnClick;
        clickAction.Enable();
    }
    
    private void OnDestroy()
    {
        if (clickAction != null)
        {
            clickAction.performed -= OnClick;
            clickAction.Disable();
            clickAction.Dispose();
        }
    }
    
    private void OnClick(InputAction.CallbackContext context)
    {
        if (seenOutline != null && seenOutline.LookedAt && !gamecore.instance.InLobby)
        {
            ClickedOutline = seenOutline;
            ClickedOutline.OnClicked();
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
        }
    }
}
