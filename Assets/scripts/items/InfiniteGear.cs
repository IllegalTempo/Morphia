using UnityEngine;

public class InfiniteGear : item
{
    private float forwardSpeed = 10f; // Speed at which the item moves forward
    private Rigidbody stickingToRb;

    public override void StickEffect()
    {
        base.StickEffect();
        
        if (StickingTo != null)
        {
            stickingToRb = StickingTo.GetComponent<Rigidbody>();
            if (stickingToRb != null)
            {
                // Unfreeze the rigidbody so it can move
                stickingToRb.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }
    }

    public override void UnStickEffect()
    {
        base.UnStickEffect();
        
        if (stickingToRb != null)
        {
            // Stop the forward motion
            stickingToRb.linearVelocity = Vector3.zero;
            stickingToRb = null;
        }
    }

    public override void DuringStickEffect()
    {
        base.DuringStickEffect();
        
        if (StickingTo != null && stickingToRb != null)
        {
            // Apply forward velocity in the object's forward direction
            stickingToRb.linearVelocity = StickingTo.transform.forward * forwardSpeed;
        }
    }
}
