using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class SubtitleBehaviour : PlayableBehaviour
{
    [TextArea(3, 10)]
    public string subtitleText;
    
    [Header("Formatting")]
    public Color textColor = Color.white;
    public int fontSize = 24;
    
    [Header("Animation")]
    public bool fadeIn = true;
    public bool fadeOut = true;
    public float fadeDuration = 0.3f;
}
