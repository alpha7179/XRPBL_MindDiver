using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// Include the namespace where Cyclo_BackgroundBehaviour is defined
using Behavior;

namespace Bgm
{
    [Serializable]
    public class Cyclo_BackgroundClip : PlayableAsset, ITimelineClipAsset
    {
        public Cyclo_BackgroundBehaviour template = new Cyclo_BackgroundBehaviour();

        public ClipCaps clipCaps
        {
            get { return ClipCaps.Blending; }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<Cyclo_BackgroundBehaviour>.Create(graph, template);
            return playable;
        }
    }
}

