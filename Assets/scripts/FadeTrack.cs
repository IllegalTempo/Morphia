using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.1f, 0.1f, 0.1f)]
[TrackClipType(typeof(FadeClip))]
[TrackBindingType(typeof(FadeScreen))]
public class FadeTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<FadeMixerBehaviour>.Create(graph, inputCount);
    }
}
