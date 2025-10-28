using UnityEngine;
using UnityEngine.Playables;

public class SubtitleMixerBehaviour : PlayableBehaviour
{
    private SubtitleDisplay subtitleDisplay;
    private bool isFirstFrameHappened;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        subtitleDisplay = playerData as SubtitleDisplay;

        if (subtitleDisplay == null)
            return;

        if (!isFirstFrameHappened)
        {
            subtitleDisplay.HideSubtitle();
            isFirstFrameHappened = true;
        }

        int inputCount = playable.GetInputCount();
        SubtitleBehaviour currentSubtitle = null;
        float currentWeight = 0f;
        float clipProgress = 0f;
        float clipDuration = 0f;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            
            if (inputWeight > 0.0001f)
            {
                ScriptPlayable<SubtitleBehaviour> scriptPlayable = (ScriptPlayable<SubtitleBehaviour>)playable.GetInput(i);
                SubtitleBehaviour behaviour = scriptPlayable.GetBehaviour();
                
                if (behaviour != null)
                {
                    currentSubtitle = behaviour;
                    currentWeight = inputWeight;
                    clipProgress = (float)(scriptPlayable.GetTime() / scriptPlayable.GetDuration());
                    clipDuration = (float)scriptPlayable.GetDuration();
                    break;
                }
            }
        }

        if (currentSubtitle != null && currentWeight > 0.0001f)
        {
            float alpha = CalculateAlpha(currentSubtitle, clipProgress, clipDuration);
            subtitleDisplay.ShowSubtitle(currentSubtitle.subtitleText, currentSubtitle.textColor, currentSubtitle.fontSize, alpha);
        }
        else
        {
            subtitleDisplay.HideSubtitle();
        }
    }

    private float CalculateAlpha(SubtitleBehaviour subtitle, float progress, float duration)
    {
        float alpha = 1f;
        float fadeTime = Mathf.Min(subtitle.fadeDuration, duration * 0.5f);

        if (subtitle.fadeIn && progress < subtitle.fadeDuration / duration)
        {
            alpha = Mathf.Clamp01(progress * duration / fadeTime);
        }

        if (subtitle.fadeOut && progress > 1f - (subtitle.fadeDuration / duration))
        {
            float fadeOutProgress = (1f - progress) * duration;
            alpha = Mathf.Clamp01(fadeOutProgress / fadeTime);
        }

        return alpha;
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (subtitleDisplay != null)
        {
            subtitleDisplay.HideSubtitle();
        }
    }
}
