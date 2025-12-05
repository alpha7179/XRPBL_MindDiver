using UnityEngine;



namespace Behavior{

    [ExecuteInEditMode]

public class GlobalShaderCycloRenderSettings : MonoBehaviour
{
    // This script controls the Cyclo & the Grid Color. Simple Global variables are exposed in the Shaders and tweaked here.


    [Header("CYCLO BACKGROUND")]
    public Color _CycloTopColor = Color.grey;
    public Color _CycloBottomColor = Color.grey;
    [Range(-2, 2)]
    public float _CycloHorizonOrigin = 0;
    [Range((float)0.1, 2)]
    public float _CycloGradiantSpread = (float)0.1;
    //[Space(20)]
    [Header("GRID COLOR")]
    public Color _GridColor = Color.grey;

    public void OnUpdateRenderSettings()
    {
        SetRender();
    }

    private void Awake()
    {
        SetRender();
    }

    private void OnEnable()
    {
        SetRender();
    }

    private void OnValidate()
    {
        SetRender();
    }

    private void SetRender()
    {
        Shader.SetGlobalColor("_CycloTopColor", _CycloTopColor);
        Shader.SetGlobalColor("_CycloBottomColor", _CycloBottomColor);
        Shader.SetGlobalFloat("_CycloHorizonOrigin", _CycloHorizonOrigin);
        Shader.SetGlobalFloat("_CycloGradiantSpread", _CycloGradiantSpread);
        Shader.SetGlobalColor("_GridColor", _GridColor);
    }
}
}