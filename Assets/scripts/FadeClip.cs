using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class FadeClip : PlayableAsset, ITimelineClipAsset
{
    public FadeBehaviour template = new FadeBehaviour();

    public ClipCaps clipCaps
    {
        get { return ClipCaps.Blending; }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<FadeBehaviour>.Create(graph, template);
        return playable;
    }
}
