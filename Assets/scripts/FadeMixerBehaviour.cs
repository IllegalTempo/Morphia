using UnityEngine;
using UnityEngine.Playables;

public class FadeMixerBehaviour : PlayableBehaviour
{
    private FadeScreen fadeScreen;
    private Color defaultColor;

    public override void OnPlayableCreate(Playable playable)
    {
        defaultColor = Color.black;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        fadeScreen = playerData as FadeScreen;

        if (fadeScreen == null)
            return;

        int inputCount = playable.GetInputCount();
        float finalAlpha = 0f;
        Color finalColor = Color.black;
        bool anyActiveClip = false;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            
            if (inputWeight > 0.0001f)
            {
                ScriptPlayable<FadeBehaviour> scriptPlayable = (ScriptPlayable<FadeBehaviour>)playable.GetInput(i);
                FadeBehaviour behaviour = scriptPlayable.GetBehaviour();
                
                if (behaviour != null)
                {
                    anyActiveClip = true;
                    double clipTime = scriptPlayable.GetTime();
                    double clipDuration = scriptPlayable.GetDuration();
                    float normalizedTime = clipDuration > 0 ? (float)(clipTime / clipDuration) : 0f;
                    
                    float clipAlpha = CalculateAlpha(behaviour, normalizedTime);
                    finalAlpha += clipAlpha * inputWeight;
                    finalColor = behaviour.fadeColor;
                }
            }
        }

        if (anyActiveClip)
        {
            fadeScreen.SetFade(finalColor, finalAlpha);
        }
        else
        {
            fadeScreen.SetAlpha(0f);
        }
    }

    private float CalculateAlpha(FadeBehaviour behaviour, float normalizedTime)
    {
        float alpha = 0f;

        switch (behaviour.fadeType)
        {
            case FadeBehaviour.FadeType.FadeToBlack:
                alpha = Mathf.Lerp(0f, 1f, normalizedTime);
                break;
                
            case FadeBehaviour.FadeType.FadeFromBlack:
                alpha = Mathf.Lerp(1f, 0f, normalizedTime);
                break;
                
            case FadeBehaviour.FadeType.Hold:
                alpha = 1f;
                break;
                
            case FadeBehaviour.FadeType.Custom:
                if (behaviour.useCustomCurve)
                {
                    float curveValue = behaviour.fadeCurve.Evaluate(normalizedTime);
                    alpha = Mathf.Lerp(behaviour.startAlpha, behaviour.endAlpha, curveValue);
                }
                else
                {
                    alpha = Mathf.Lerp(behaviour.startAlpha, behaviour.endAlpha, normalizedTime);
                }
                break;
        }

        return Mathf.Clamp01(alpha);
    }

    public override void OnGraphStop(Playable playable)
    {
        if (fadeScreen != null)
        {
            fadeScreen.SetAlpha(0f);
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (fadeScreen != null)
        {
            fadeScreen.SetAlpha(0f);
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (fadeScreen != null && !info.seekOccurred)
        {
            fadeScreen.SetAlpha(0f);
        }
    }
}
