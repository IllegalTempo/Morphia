using UnityEngine;

public class Mushroom : item
{
    private Vector3 originalScale;
    private float scaleMultiplier = 1.5f; // Makes the stuck item 50% larger

    public override void StickEffect()
    {
        base.StickEffect();
        
        if (StickingTo != null)
        {
            originalScale = StickingTo.transform.localScale;
            StickingTo.transform.localScale = originalScale * scaleMultiplier;
        }
    }

    public override void UnStickEffect()
    {
        base.UnStickEffect();
        
        if (StickingTo != null)
        {
            StickingTo.transform.localScale = originalScale;
        }
    }

    public override void DuringStickEffect()
    {
        base.DuringStickEffect();
        
        // Maintain the scale during the stick effect
        if (StickingTo != null)
        {
            StickingTo.transform.localScale = originalScale * scaleMultiplier;
        }
    }
}
