using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(StaticOutline))]
public partial class Selectable : MonoBehaviour
{

    public StaticOutline outline;
    public bool LookedAt = false;
    public float ClickTimer = 0f;
    
    private InputAction clickAction;

    protected virtual void OnEnable()
    {
        // Create and configure the click action
        clickAction = new InputAction("Click", InputActionType.Button);
        clickAction.AddBinding("<Mouse>/leftButton");
        
        // Subscribe to the performed event
        clickAction.performed += OnClickPerformed;
        
        // Enable the action
        clickAction.Enable();
    }

    protected virtual void OnDisable()
    {
        // Clean up: unsubscribe and disable the action
        if (clickAction != null)
        {
            clickAction.performed -= OnClickPerformed;
            clickAction.Disable();
            clickAction.Dispose();
        }
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        // Only trigger if we're being looked at
        if (LookedAt)
        {
            OnClicked();
        }
    }

    protected virtual void Update()
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
            LookedAt = false;
        }
        else
        {
            outline.enabled = false;
        }
    }
    
    public virtual void OnClicked()
    {
        ClickTimer = 0.2f;
    }
}
