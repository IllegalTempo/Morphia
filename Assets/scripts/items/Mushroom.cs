using UnityEngine;

public class Mushroom : item
{
    private float scaleMultiplier = 1.5f; // Makes the stuck item 50% larger

    public override void StickEffect()
    {
        base.StickEffect();
        
        if (StickingTo != null)
        {
            StickingTo.transform.localScale *= scaleMultiplier;
        }
    }

    public override void UnStickEffect()
    {
        base.UnStickEffect();
        
        Debug.Log($"Unsticking item: {StickingTo?.ItemName} localscale/=scalemultiplier");
        if (StickingTo != null)
        {
            StickingTo.transform.localScale /= scaleMultiplier;
        }
    }

    public override void DuringStickEffect()
    {
        base.DuringStickEffect();
        
    }
}
