using UnityEngine;

public class UpdriftFan : item
{
    private float upwardSpeed = 5f; // Speed at which the item moves upward
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
            // Stop the upward motion
            stickingToRb.linearVelocity = Vector3.zero;
            stickingToRb = null;
        }
    }

    public override void DuringStickEffect()
    {
        base.DuringStickEffect();
        
        if (StickingTo != null && stickingToRb != null)
        {
            // Apply upward velocity in the world up direction
            stickingToRb.linearVelocity = Vector3.up * upwardSpeed;
        }
        //keep spining
        transform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);
    }
}
