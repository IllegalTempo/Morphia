using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class FadeBehaviour : PlayableBehaviour
{
    public enum FadeType
    {
        FadeToBlack,
        FadeFromBlack,
        Hold,
        Custom
    }

    [Header("Fade Settings")]
    public FadeType fadeType = FadeType.FadeToBlack;
    public Color fadeColor = Color.black;
    
    [Header("Animation")]
    [Range(0f, 1f)]
    public float startAlpha = 0f;
    [Range(0f, 1f)]
    public float endAlpha = 1f;
    
    [Header("Curve")]
    public bool useCustomCurve = false;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
}
