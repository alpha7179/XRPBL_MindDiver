using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Behavior
{
    [Serializable]
    public class Cyclo_BackgroundBehaviour : PlayableBehaviour
    {
        [ColorUsage(false, true)] public Color _CycloBottomColor;
        [ColorUsage(false, true)] public Color _CycloTopColor;
        public float _CycloHorizonOrigin;
        public float _CycloGradiantSpread;
    }
}

