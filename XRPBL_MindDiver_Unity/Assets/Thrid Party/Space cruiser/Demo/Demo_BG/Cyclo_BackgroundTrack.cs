using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.ComponentModel;

namespace hello
{
    [TrackColor(0.2947383f, 1f, 0.2783019f)]
    [TrackClipType(typeof(Cyclo_BackgroundClip))]
    [TrackBindingType(typeof(GlobalShaderCycloRenderSettings))]
    [DisplayName("Cinematic Studio Sample/Cyclo Background Track")]
    public class Cyclo_BackgroundTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<Cyclo_BackgroundMixerBehaviour>.Create(graph, inputCount);
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
#if UNITY_EDITOR
            GlobalShaderCycloRenderSettings trackBinding = director.GetGenericBinding(this) as GlobalShaderCycloRenderSettings;
            if (trackBinding == null)
                return;

            driver.AddFromName<GlobalShaderCycloRenderSettings>(trackBinding.gameObject, "_CycloBottomColor");
            driver.AddFromName<GlobalShaderCycloRenderSettings>(trackBinding.gameObject, "_CycloTopColor");
            driver.AddFromName<GlobalShaderCycloRenderSettings>(trackBinding.gameObject, "_CycloHorizonOrigin");
            driver.AddFromName<GlobalShaderCycloRenderSettings>(trackBinding.gameObject, "_CycloGradiantSpread");
#endif
            base.GatherProperties(director, driver);
        }
    }

    [System.Serializable]
    public class Cyclo_BackgroundClip : PlayableAsset, ITimelineClipAsset
    {
        public Cyclo_BackgroundBehaviour template = new Cyclo_BackgroundBehaviour();

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<Cyclo_BackgroundBehaviour>.Create(graph, template);
            return playable;
        }
    }

    public class Cyclo_BackgroundMixerBehaviour : PlayableBehaviour
    {
        Color m_Default_CycloBottomColor;
        Color m_Default_CycloTopColor;
        float m_Default_CycloHorizonOrigin;
        float m_Default_CycloGradiantSpread;

        Color m_Assigned_CycloBottomColor;
        Color m_Assigned_CycloTopColor;
        float m_Assigned_CycloHorizonOrigin;
        float m_Assigned_CycloGradiantSpread;

        GlobalShaderCycloRenderSettings m_TrackBinding;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            m_TrackBinding = playerData as GlobalShaderCycloRenderSettings;

            if (m_TrackBinding == null)
                return;

            if (m_TrackBinding._CycloBottomColor != m_Assigned_CycloBottomColor)
                m_Default_CycloBottomColor = m_TrackBinding._CycloBottomColor;
            if (m_TrackBinding._CycloTopColor != m_Assigned_CycloTopColor)
                m_Default_CycloTopColor = m_TrackBinding._CycloTopColor;
            if (!Mathf.Approximately(m_TrackBinding._CycloHorizonOrigin, m_Assigned_CycloHorizonOrigin))
                m_Default_CycloHorizonOrigin = m_TrackBinding._CycloHorizonOrigin;
            if (!Mathf.Approximately(m_TrackBinding._CycloGradiantSpread, m_Assigned_CycloGradiantSpread))
                m_Default_CycloGradiantSpread = m_TrackBinding._CycloGradiantSpread;

            int inputCount = playable.GetInputCount();

            Color blended_CycloBottomColor = Color.clear;
            Color blended_CycloTopColor = Color.clear;
            float blended_CycloHorizonOrigin = 0f;
            float blended_CycloGradiantSpread = 0f;
            float totalWeight = 0f;

            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                var inputPlayable = (ScriptPlayable<Cyclo_BackgroundBehaviour>)playable.GetInput(i);
                var input = inputPlayable.GetBehaviour();

                blended_CycloBottomColor += input._CycloBottomColor * inputWeight;
                blended_CycloTopColor += input._CycloTopColor * inputWeight;
                blended_CycloHorizonOrigin += input._CycloHorizonOrigin * inputWeight;
                blended_CycloGradiantSpread += input._CycloGradiantSpread * inputWeight;
                totalWeight += inputWeight;
            }

            m_Assigned_CycloBottomColor = blended_CycloBottomColor + m_Default_CycloBottomColor * (1f - totalWeight);
            m_TrackBinding._CycloBottomColor = m_Assigned_CycloBottomColor;

            m_Assigned_CycloTopColor = blended_CycloTopColor + m_Default_CycloTopColor * (1f - totalWeight);
            m_TrackBinding._CycloTopColor = m_Assigned_CycloTopColor;

            m_Assigned_CycloHorizonOrigin = blended_CycloHorizonOrigin + m_Default_CycloHorizonOrigin * (1f - totalWeight);
            m_TrackBinding._CycloHorizonOrigin = m_Assigned_CycloHorizonOrigin;

            m_Assigned_CycloGradiantSpread = blended_CycloGradiantSpread + m_Default_CycloGradiantSpread * (1f - totalWeight);
            m_TrackBinding._CycloGradiantSpread = m_Assigned_CycloGradiantSpread;

            m_TrackBinding.OnUpdateRenderSettings();
        }
    }

    [System.Serializable]
    public class Cyclo_BackgroundBehaviour : PlayableBehaviour
    {
        [ColorUsage(false, true)] public Color _CycloBottomColor;
        [ColorUsage(false, true)] public Color _CycloTopColor;
        public float _CycloHorizonOrigin;
        public float _CycloGradiantSpread;
    }

    public class GlobalShaderCycloRenderSettings : MonoBehaviour
    {
        public Color _CycloBottomColor;
        public Color _CycloTopColor;
        public float _CycloHorizonOrigin;
        public float _CycloGradiantSpread;

        public void OnUpdateRenderSettings()
        {
            // Update shader or settings logic
        }
    }
}
