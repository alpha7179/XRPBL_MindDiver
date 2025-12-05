using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// Import the Behavior namespace where Cyclo_BackgroundBehaviour is defined
using Behavior;

namespace B
{
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

            // Check and assign new default values
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
            float greatestWeight = 0f;

            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                ScriptPlayable<Cyclo_BackgroundBehaviour> inputPlayable = (ScriptPlayable<Cyclo_BackgroundBehaviour>)playable.GetInput(i);
                Cyclo_BackgroundBehaviour input = inputPlayable.GetBehaviour();

                blended_CycloBottomColor += input._CycloBottomColor * inputWeight;
                blended_CycloTopColor += input._CycloTopColor * inputWeight;
                blended_CycloHorizonOrigin += input._CycloHorizonOrigin * inputWeight;
                blended_CycloGradiantSpread += input._CycloGradiantSpread * inputWeight;
                totalWeight += inputWeight;

                if (inputWeight > greatestWeight)
                {
                    greatestWeight = inputWeight;
                }
            }

            // Correct the assignment here
            m_Assigned_CycloBottomColor = blended_CycloBottomColor + m_Default_CycloBottomColor * (1f - totalWeight);
            m_Assigned_CycloTopColor = blended_CycloTopColor + m_Default_CycloTopColor * (1f - totalWeight);
            m_Assigned_CycloHorizonOrigin = blended_CycloHorizonOrigin + m_Default_CycloHorizonOrigin * (1f - totalWeight);
            m_Assigned_CycloGradiantSpread = blended_CycloGradiantSpread + m_Default_CycloGradiantSpread * (1f - totalWeight);

            // Correct the final update here
            m_TrackBinding._CycloBottomColor = m_Assigned_CycloBottomColor;
            m_TrackBinding._CycloTopColor = m_Assigned_CycloTopColor;
            m_TrackBinding._CycloHorizonOrigin = m_Assigned_CycloHorizonOrigin;
            m_TrackBinding._CycloGradiantSpread = m_Assigned_CycloGradiantSpread;

            // Apply the changes to the global shader
            m_TrackBinding.OnUpdateRenderSettings();
        }
    }
}
