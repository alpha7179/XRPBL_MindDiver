//------------------------------------------------------------------------------------------------------------------
// Tunnel FX
// Copyright (c) Kronnect
//------------------------------------------------------------------------------------------------------------------

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;


namespace TunnelEffect {


    [ExecuteInEditMode]
    public partial class TunnelFX2 : MonoBehaviour {


        static class ShaderParams {
            public static int FallOffStart = Shader.PropertyToID("_FallOffStart");
            public static int Aperture = Shader.PropertyToID("_Aperture");
            public static int Params5 = Shader.PropertyToID("_Params5");
            public static int MixParams = Shader.PropertyToID("_MixParams");
            public static int TextureTilings = Shader.PropertyToID("_TextureTilings");
            public static int CurveParams = Shader.PropertyToID("_CurveParams");
            public static int Color = Shader.PropertyToID("_Color");
            public static int BackgroundColor = Shader.PropertyToID("_BackgroundColor");
            public static int Behind = Shader.PropertyToID("_Behind");

            public const string SKW_TUNNEL_BLEND_IN_ORDER = "TUNNEL_BLEND_IN_ORDER";
            public const string SKW_TUNNEL_LAYER_COUNT_1 = "TUNNEL_LAYER_COUNT_1";
            public const string SKW_TUNNEL_LAYER_COUNT_2 = "TUNNEL_LAYER_COUNT_2";
            public const string SKW_TUNNEL_LAYER_COUNT_3 = "TUNNEL_LAYER_COUNT_3";
            public const string SKW_TUNNEL_LAYER_COUNT_4 = "TUNNEL_LAYER_COUNT_4";
        }



#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object/TunnelFX2", false)]
        static void CreateTunnelMenuOption(MenuCommand menuCommand) {
            // Create a custom game object
            GameObject go = new GameObject("Tunnel", typeof(MeshFilter), typeof(MeshRenderer));
            go.name = "Tunnel";
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            if (Selection.activeTransform != null) {
                go.transform.SetParent(Selection.activeTransform, false);
                go.transform.localPosition = Vector3.zero;
            }
            go.transform.localRotation = Quaternion.Euler(0, 0, 0);
            go.transform.localScale = new Vector3(10f, 10f, 950f);
            Selection.activeObject = go;
            go.AddComponent<TunnelFX2>();
        }


#endif

        [HideInInspector]
        public bool isDirty;


        // internal fields
        List<Vector3> vertices;
        List<Vector2> uv;
        List<int> indicesCap;
        Material[] tunnelMats;
        Material tunnelMat, tunnelMatTransNonCurved, tunnelMatTransCurved, tunnelMatOpaque, tunnelCapMat, tunnelCapTrans, tunnelCapOpaque;
        static readonly int[] textureNames = { Shader.PropertyToID("_TunnelTex1"), Shader.PropertyToID("_TunnelTex2"), Shader.PropertyToID("_TunnelTex3"), Shader.PropertyToID("_TunnelTex4") };
        static readonly int[] paramNames = { Shader.PropertyToID("_Params1"), Shader.PropertyToID("_Params2"), Shader.PropertyToID("_Params3"), Shader.PropertyToID("_Params4") };
        Dictionary<string, Texture2D> dict;

        List<string> shaderKeywords;
        float animationTime;
        readonly Vector4[] tunnelParams = new Vector4[4];
        bool needTunnelGeoRefresh;
        bool shouldUpdateMaterialProperties;
        int generatedSides, generatedSegments;
        bool generatedCap, generatedCapBothEnds;

        #region Game loop events

        void OnEnable() {
            if (tunnelMatTransNonCurved == null) {
                tunnelMatTransNonCurved = new Material(Shader.Find("TunnelEffect/TunnelFX2Alpha"));
                tunnelMatTransNonCurved.hideFlags = HideFlags.DontSave;
            }
            if (tunnelMatTransCurved == null) {
                tunnelMatTransCurved = new Material(Shader.Find("TunnelEffect/TunnelFX2AlphaCurved"));
                tunnelMatTransCurved.hideFlags = HideFlags.DontSave;
            }
            if (tunnelMatOpaque == null) {
                tunnelMatOpaque = new Material(Shader.Find("TunnelEffect/TunnelFX2"));
                tunnelMatOpaque.hideFlags = HideFlags.DontSave;
            }
            if (tunnelCapTrans == null) {
                tunnelCapTrans = new Material(Shader.Find("TunnelEffect/TunnelCapAlpha"));
                tunnelCapTrans.hideFlags = HideFlags.DontSave;
            }
            if (tunnelCapOpaque == null) {
                tunnelCapOpaque = new Material(Shader.Find("TunnelEffect/TunnelCap"));
                tunnelCapOpaque.hideFlags = HideFlags.DontSave;
            }

            if (dict == null) {
                dict = new Dictionary<string, Texture2D>();
            }

            UpdateMaterialProperties();
        }

        void OnDestroy() {
            if (tunnelMatOpaque != null) {
                DestroyImmediate(tunnelMatOpaque);
                tunnelMatOpaque = null;
            }
            if (tunnelMatTransNonCurved != null) {
                DestroyImmediate(tunnelMatTransNonCurved);
                tunnelMatTransNonCurved = null;
            }
            if (tunnelMatTransCurved != null) {
                DestroyImmediate(tunnelMatTransCurved);
                tunnelMatTransCurved = null;
            }
            if (dict != null) {
                dict.Clear();
                dict = null;
            }
        }

        void Reset() {
            UpdateMaterialProperties();
        }

        void Update() {

            if (shouldUpdateMaterialProperties) {
                UpdateMaterialPropertiesNow();
            }

            if (needTunnelGeoRefresh) {
                UpdateTunnelGeo();
            }

            for (int k = 0; k < 4; k++) {
                if (Application.isPlaying) {
                    tunnelParams[k].x += _travelSpeed[k] * _layersSpeed * Time.deltaTime * 0.05f;
                    tunnelParams[k].y += _rotationSpeed[k] * _layersSpeed * Time.deltaTime * 0.05f;
                }
                tunnelMat.SetVector(paramNames[k], tunnelParams[k]);
            }
            tunnelCapMat.SetVector(paramNames[0], tunnelParams[0]);
            if (Application.isPlaying && _positionAnimated) {
                animationTime += _layersSpeed * Time.deltaTime;
                float ox = Mathf.Sin(animationTime * 0.5f) * _animationAmplitude;
                float oy = Mathf.Cos(animationTime * 0.25f) * _animationAmplitude;
                transform.localRotation = Quaternion.Euler(0, 0, 0);
                Vector3 tpos = transform.TransformPoint(new Vector3(ox, oy, 10f / transform.localScale.z));
                transform.LookAt(tpos);
            }
        }


        #endregion

        public void OnDidApplyAnimationProperties() {   // support for animating property based fields
            shouldUpdateMaterialProperties = true;
        }

        public void UpdateMaterialProperties() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                UpdateMaterialPropertiesNow();
                return;
            }
#endif
            shouldUpdateMaterialProperties = true;
        }

        void UpdateMaterialPropertiesNow() {
            shouldUpdateMaterialProperties = false;

            if (_enableTransparency) {
                _renderQueue = 2999;
                tunnelMat = _curvedTunnel ? tunnelMatTransCurved : tunnelMatTransNonCurved;
                tunnelCapMat = tunnelCapTrans;
            } else {
                if (_renderQueue >= 3000) {
                    _renderQueue = 2080;
                }
                tunnelMat = tunnelMatOpaque;
                tunnelCapMat = tunnelCapOpaque;
            }
            if (tunnelMat == null || tunnelCapMat == null)
                return;

            switch (_preset) {
                case TUNNEL_PRESET.SpaceTravel:
                    _layerCount = 4;
                    for (int k = 0; k < 4; k++) {
                        _layerTextures[k] = GetTexture("starfield");
                    }
                    _alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 2f, 1.2f, 0.7f, 0.25f };
                    _rotationSpeed = new float[] { 0.7f, 0.35f, 0.2f, 0.1f };
                    _twist = new float[] { 0.1f, 0.1f, 0.1f, 0.1f };
                    _exposure = new float[] { 0.85f, 0.85f, 0.85f, 0.85f };
                    _fallOff = 1f;
                    _backgroundColor = new Color(100f / 255f, 253f / 255f, 206f / 255f);
                    _positionAnimated = true;
                    _animationAmplitude = 0.15f;
                    _blendInOrder = false;
                    _sides = 16;
                    _hyperSpeed = 0.634f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 1;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.MagmaTunnel:
                    _layerCount = 4;
                    _layerTextures[0] = GetTexture("chromatic");
                    for (int k = 1; k < 4; k++) {
                        _layerTextures[k] = GetTexture("fire");
                    }
                    _alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 0.7f, 0.4f, 0.2f, 0.1f };
                    _rotationSpeed = new float[] { 0f, -0.002f, 0.003f, -0.005f };
                    _twist = new float[] { 0.2f, -0.2f, 0.2f, -0.2f };
                    _exposure = new float[] { 0.55f, 0.55f, 0.6f, 0.65f };
                    _fallOff = 1f;
                    _backgroundColor = new Color(1f, 1f, 0.89f);
                    _positionAnimated = false;
                    _animationAmplitude = 0.05f;
                    _blendInOrder = false;
                    _sides = 16;
                    _hyperSpeed = 0f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 2;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.FireTornado:
                    _layerCount = 4;
                    for (int k = 0; k < 4; k++) {
                        _layerTextures[k] = GetTexture("fire");
                    }
                    _alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 0.7f, 0.4f, 0.2f, 0.15f };
                    _rotationSpeed = new float[] { 4f, -5f, 6f, -7f };
                    _twist = new float[] { 0.5f, 0.5f, 0.5f, 0.5f };
                    _exposure = new float[] { 0.5f, 0.6f, 0.7f, 0.8f };
                    _fallOff = 1f;
                    _backgroundColor = new Color(1f, 1f, 0.89f);
                    _positionAnimated = true;
                    _animationAmplitude = 0.075f;
                    _blendInOrder = false;
                    _sides = 32;
                    _hyperSpeed = 0f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 1;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.CloudAscension:
                    _layerCount = 4;
                    for (int k = 0; k < 4; k++) {
                        _layerTextures[k] = GetTexture("clouds");
                    }
                    _alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 0.7f, 0.3f, 0.2f, 0.1f };
                    _rotationSpeed = new float[] { 0.2f, 0.18f, 0.16f, 0.1f };
                    _twist = new float[] { 0.05f, 0.04f, 0.03f, 0.02f };
                    _exposure = new float[] { 0.45f, 0.5f, 0.55f, 0.6f };
                    _fallOff = 1f;
                    _backgroundColor = new Color(0, 1f, 1f);
                    _positionAnimated = true;
                    _animationAmplitude = 0.05f;
                    _blendInOrder = false;
                    _sides = 16;
                    _hyperSpeed = 0f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 1;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.MetalStructure:
                    _layerCount = 1;
                    _alpha = new float[] { 0.75f, 0.26664f, 0.13332f, 0.06666f };
                    _layerTextures[0] = GetTexture("metalstructure");
                    _travelSpeed = new float[] { 5f, 0.3f, 0.2f, 0.1f };
                    _rotationSpeed = new float[] { 0.2f, 0.18f, 0.16f, 0.1f };
                    _twist = new float[] { 0f, 0.04f, 0.03f, 0.02f };
                    _exposure = new float[] { 0.77f, 0.75f, 0.8f, 0.85f };
                    _fallOff = 0.7f;
                    _backgroundColor = Color.black;
                    _positionAnimated = true;
                    _animationAmplitude = 0.1f;
                    _blendInOrder = false;
                    _sides = 4;
                    _hyperSpeed = 0f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 6;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.WaterTunnel:
                    _layerCount = 4;
                    _layerTextures[0] = GetTexture("water");
                    _layerTextures[1] = GetTexture("water2");
                    _layerTextures[2] = GetTexture("water");
                    _layerTextures[3] = GetTexture("water2");
                    _alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 0.7f, 0.3f, 0.2f, 0.1f };
                    _rotationSpeed = new float[] { 0.2f, 0.18f, 0.16f, 0.1f };
                    _twist = new float[] { 0.05f, 0.04f, 0.03f, 0.02f };
                    _exposure = new float[] { 0.5f, 0.6f, 0.6f, 0.6f };
                    _fallOff = 0.8f;
                    _backgroundColor = new Color(0, 1f, 1f);
                    _positionAnimated = true;
                    _animationAmplitude = 0.05f;
                    _blendInOrder = false;
                    _sides = 16;
                    _hyperSpeed = 0f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 1;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.CavePassage:
                    _layerCount = 1;
                    _layerTextures[0] = GetTexture("rocks");
                    _alpha = new float[] { 1f, 0.26664f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 2f, 0.3f, 0.2f, 0.1f };
                    _rotationSpeed = new float[] { 0f, 0.18f, 0.16f, 0.1f };
                    _twist = new float[] { 0f, 0.04f, 0.03f, 0.02f };
                    _exposure = new float[] { 0.7f, 0.75f, 0.8f, 0.85f };
                    _fallOff = 0.05f;
                    _backgroundColor = new Color(34 / 255f, 22 / 255f, 22 / 255f);
                    _positionAnimated = false;
                    _animationAmplitude = 0.1f;
                    _blendInOrder = false;
                    _sides = 4;
                    _hyperSpeed = 0f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 4;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.Stripes:
                    _layerCount = 1;
                    _layerTextures[0] = GetTexture("stripes");
                    _alpha = new float[] { 1f, 0.26664f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 2f, 0.3f, 0.2f, 0.1f };
                    _rotationSpeed = new float[] { 1f, 0.18f, 0.16f, 0.1f };
                    _twist = new float[] { 0f, 0.04f, 0.03f, 0.02f };
                    _exposure = new float[] { 0.57f, 0.75f, 0.8f, 0.85f };
                    _fallOff = 0.3f;
                    _backgroundColor = Color.black;
                    _positionAnimated = false;
                    _animationAmplitude = 0.1f;
                    _blendInOrder = false;
                    _sides = 32;
                    _hyperSpeed = 0f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 3;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.Twightlight:
                    _layerCount = 3;
                    _layerTextures[0] = GetTexture("starfield");
                    _layerTextures[1] = GetTexture("lights2");
                    _layerTextures[2] = GetTexture("lights1");
                    _alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 2f, 0.3f, 0.2f, 0.1f };
                    _rotationSpeed = new float[] { 1f, 0.18f, 0.16f, 0.1f };
                    _twist = new float[] { 1f, 0.13f, -2.24f, 0.02f };
                    _exposure = new float[] { 0.7f, 0.832f, 0.7f, 0.85f };
                    _fallOff = 0.5f;
                    _backgroundColor = Color.white;
                    _positionAnimated = false;
                    _animationAmplitude = 0.1f;
                    _blendInOrder = false;
                    _sides = 32;
                    _hyperSpeed = 0f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 2;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.MysticTravel:
                    _layerCount = 4;
                    _layerTextures[0] = GetTexture("starfield");
                    for (int k = 1; k < 4; k++) {
                        _layerTextures[k] = GetTexture("clouds");
                    }
                    _alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 0.7f, 0.3f, 0.2f, 0.1f };
                    _rotationSpeed = new float[] { 0f, 0.18f, 0.16f, 0.1f };
                    _twist = new float[] { 0f, 0.04f, 0.03f, 0.02f };
                    _exposure = new float[] { 0.7f, 0.65f, 0.65f, 0.65f };
                    _fallOff = 1f;
                    _backgroundColor = new Color(155f / 255f, 225f / 255f, 1f);
                    _positionAnimated = false;
                    _animationAmplitude = 0.05f;
                    _blendInOrder = false;
                    _sides = 16;
                    _hyperSpeed = 0f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 1;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.Chromatic:
                    _layerCount = 1;
                    _layerTextures[0] = GetTexture("chromatic");
                    _alpha = new float[] { 1f, 0.26664f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 0.7f, 0.3f, 0.2f, 0.1f };
                    _rotationSpeed = new float[] { -0.15f, 0.18f, 0.16f, 0.1f };
                    _twist = new float[] { -0.4f, 0.04f, 0.03f, 0.02f };
                    _exposure = new float[] { 0.5f, 0.5f, 0.55f, 0.6f };
                    _fallOff = 1f;
                    _backgroundColor = Color.white;
                    _positionAnimated = false;
                    _animationAmplitude = 0.05f;
                    _blendInOrder = false;
                    _sides = 32;
                    _hyperSpeed = 0f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 4;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.IcePassage:
                    _layerCount = 1;
                    _layerTextures[0] = GetTexture("ice");
                    _alpha = new float[] { 0.75f, 0.26664f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 0.7f, 0.3f, 0.2f, 0.1f };
                    _rotationSpeed = new float[] { 0f, 0.18f, 0.16f, 0.1f };
                    _twist = new float[] { 0f, 0f, 0f, 0f };
                    _exposure = new float[] { 0.67f, 0.5f, 0.55f, 0.6f };
                    _fallOff = 1f;
                    _backgroundColor = Color.white;
                    _positionAnimated = false;
                    _animationAmplitude = 0.05f;
                    _blendInOrder = false;
                    _sides = 4;
                    _hyperSpeed = 0f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 4;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.ExhaustPort:
                    _layerCount = 2;
                    _layerTextures[0] = GetTexture("rustedmetal");
                    _layerTextures[1] = GetTexture("clouds");
                    _alpha = new float[] { 0.5f, 0.1f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 0.5f, 1f, 0.35f, 0.2f };
                    _rotationSpeed = new float[] { 0f, 0.5f, -0.5f, 0.5f };
                    _twist = new float[] { 0f, -0.2f, 0.2f, -0.2f };
                    _exposure = new float[] { 0.7f, 0.6f, 0.6f, 0.65f };
                    _fallOff = 0.4f;
                    _backgroundColor = Color.black;
                    _positionAnimated = false;
                    _animationAmplitude = 0.075f;
                    _blendInOrder = false;
                    _sides = 6;
                    _hyperSpeed = 0f;
                    _layersSpeed = 1f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 3;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
                case TUNNEL_PRESET.CutOutRail:
                    _layerCount = 2;
                    _layerTextures[0] = GetTexture("coloredsand");
                    _layerTextures[1] = GetTexture("stripescutout");
                    _alpha = new float[] { 1f, 1f, 0.13332f, 0.06666f };
                    _travelSpeed = new float[] { 1f, 9f, 0.35f, 0.2f };
                    _rotationSpeed = new float[] { 0.1f, 0.1f, -0.5f, 0.5f };
                    _twist = new float[] { 0f, -1f, 0.2f, -0.2f };
                    _exposure = new float[] { 0.5f, 0.6f, 0.6f, 0.65f };
                    _fallOff = 10f;
                    _backgroundColor = Color.black;
                    _positionAnimated = false;
                    _animationAmplitude = 0.075f;
                    _blendInOrder = true;
                    _sides = 32;
                    _hyperSpeed = 0f;
                    _layersSpeed = 0.25f;
                    _textureScales = new int[] { 1, 1, 1, 1 };
                    _textureScale = 1;
                    _capped = true;
                    _fallOffStart = 0;
                    _tintColor = Color.white;
                    _uvScale = 1f;
                    break;
            }

            if (_curvedTunnel) {
                if (_segments < 200) {
                    _segments = 200;
                }
            } else if (_segments >= 200) {
                _segments = 1;
            }

            for (int k = 0; k < _layerTextures.Length; k++) {
                if (_layerTextures[k] == null) {
                    // try to get the texture from the material itself
                    _layerTextures[k] = (Texture2D)tunnelMat.GetTexture(textureNames[k]);
                    // or restore default texture (should not happen)
                    if (_layerTextures[k] == null)
                        _layerTextures[k] = GetTexture("starfield");
                }
                tunnelMat.SetTexture(textureNames[k], _layerTextures[k]);

                float expos;
                if (_exposure[k] < 0.5) {
                    expos = _exposure[k] * 2f;
                } else {
                    expos = 1f / (1.0001f - (_exposure[k] - 0.5f) * 2f);
                }
                tunnelParams[k].w = expos;
                tunnelParams[k].z = _twist[k] * 0.01f;
            }

            Color backgroundColor = _backgroundColor;
            backgroundColor.a *= _globalAlpha;
            _tintColor.a = _globalAlpha;
            tunnelMat.SetInt(ShaderParams.Behind, _drawBehindEverything ? 1 : 0);
            tunnelCapMat.SetInt(ShaderParams.Behind, _drawBehindEverything ? 1 : 0);
            tunnelMat.SetColor(ShaderParams.Color, _tintColor);
            tunnelMat.SetColor(ShaderParams.BackgroundColor, backgroundColor);
            tunnelCapMat.SetColor(ShaderParams.BackgroundColor, backgroundColor);
            Vector4 contributions = new Vector4(_alpha[0], _alpha[1], _alpha[2], _alpha[3]);
            tunnelMat.SetVector(ShaderParams.Params5, contributions);
            Vector4 textureScales = new Vector4(_textureScales[0] * _textureScale, _textureScales[1] * _textureScale, _textureScales[2] * _textureScale, _textureScales[3] * _textureScale);
            float fallOffStart = _fallOffStart * transform.localScale.z;
            tunnelMat.SetFloat(ShaderParams.FallOffStart, fallOffStart);
            tunnelMat.SetFloat(ShaderParams.Aperture, _aperture);
            tunnelCapMat.SetFloat(ShaderParams.Aperture, _aperture);

            float hsValue = Mathf.Clamp(1f - _hyperSpeed, 0.001f, 1f);
            float fallOffEnd = _fallOff * transform.localScale.z;
            if (fallOffEnd < fallOffStart)
                fallOffEnd = fallOffStart;
            Vector4 mixParams = new Vector4(fallOffEnd,
                                             hsValue * _uvScale * transform.localScale.z / 100f,
                                             _textureScale,
                                             _uvScale);
            tunnelMat.SetVector(ShaderParams.MixParams, mixParams);
            tunnelMat.SetVector(ShaderParams.TextureTilings, new Vector4(_textureScales[0], _textureScales[1], _textureScales[2], _textureScales[3]));

            Vector4 curveParams = new Vector4(0, 0, _curvedTunnelFrequency, transform.localScale.z);
            if (_curvedTunnel) {
                curveParams.x = _curvedTunnelAmplitudeX * 0.1f;
                curveParams.y = _curvedTunnelAmplitudeY * 0.1f;
            }
            tunnelMat.SetVector(ShaderParams.CurveParams, curveParams);
            tunnelCapMat.SetVector(ShaderParams.CurveParams, curveParams);

            if (shaderKeywords == null) {
                shaderKeywords = new List<string>();
            } else {
                shaderKeywords.Clear();
            }

            if (_blendInOrder && _layerCount > 1) {
                shaderKeywords.Add(ShaderParams.SKW_TUNNEL_BLEND_IN_ORDER);
            }

            switch (_layerCount) {
                case 4:
                    shaderKeywords.Add(ShaderParams.SKW_TUNNEL_LAYER_COUNT_4);
                    break;
                case 3:
                    shaderKeywords.Add(ShaderParams.SKW_TUNNEL_LAYER_COUNT_3);
                    break;
                case 2:
                    shaderKeywords.Add(ShaderParams.SKW_TUNNEL_LAYER_COUNT_2);
                    break;
                default:
                    shaderKeywords.Add(ShaderParams.SKW_TUNNEL_LAYER_COUNT_1);
                    break;
            }

            tunnelMat.shaderKeywords = shaderKeywords.ToArray();

            tunnelMat.renderQueue = _renderQueue;

            if (generatedSides != _sides || generatedCap != _capped || generatedCapBothEnds != _cappedBothEnds || generatedSegments != _segments) {
#if UNITY_EDITOR
                if (Application.isPlaying) {
                    needTunnelGeoRefresh = true;
                } else {
                    UpdateTunnelGeo();
                }
#else
                needTunnelGeoRefresh = true;
#endif
            }

            MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
            if (mr != null) {
                mr.enabled = _globalAlpha > 0;
            }

            isDirty = true;
        }


        Texture2D GetTexture(string name) {
            Texture2D tex;
            if (dict.TryGetValue(name, out tex)) return tex;
            dict[name] = tex = Resources.Load<Texture2D>("Textures/" + name);
            return tex;
        }


        void UpdateTunnelGeo() {

            MeshFilter mf = gameObject.GetComponent<MeshFilter>();
            MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
            if (mf == null || mr == null) {
                Debug.LogError("Create TunnelFX using top menu GameObject > 3D Object > TunnelFX");
                DestroyImmediate(this);
                return;
            }

            generatedCap = _capped;
            generatedCapBothEnds = _cappedBothEnds;
            generatedSides = _sides;
            generatedSegments = _segments;
            needTunnelGeoRefresh = false;

            Mesh mesh;
            if (mf.sharedMesh != null) {
                mesh = mf.sharedMesh;
                mesh.Clear();
            } else {
                mesh = new Mesh();
            }
            mesh.subMeshCount = 3;

            int vertexCount = sides * 2;
            if (vertices == null) {
                vertices = new List<Vector3>();
            } else {
                vertices.Clear();
            }
            if (uv == null) {
                uv = new List<Vector2>();
            } else {
                uv.Clear();
            }

            // Add sides
            for (int i = 0; i < _segments; i++) {
                for (int v = 0, s = 0; s <= _sides; s++, v += 2) {
                    float a = s * Mathf.PI * 2f / _sides + Mathf.PI * 0.25f;
                    float x = Mathf.Cos(a);
                    float y = Mathf.Sin(a);
                    vertices.Add(new Vector3(x, y, -1f + (2f * i) / _segments));
                    vertices.Add(new Vector3(x, y, -1f + (2f * (i + 1f) / _segments)));
                    uv.Add(new Vector2((float)i / _segments, (1f / vertexCount) * v));
                    uv.Add(new Vector2(((float)i + 1) / _segments, (1f / vertexCount) * v));
                }
            }
            int triangleCount = (_sides + 1) * 6;
            int[] indices = new int[triangleCount * _segments];
            for (int i = 0, s = 0, k = 0; k < _segments; k++) {
                for (int t = 0; t < _sides; t++, i += 6, s += 2) {
                    indices[i] = s;
                    indices[i + 1] = s + 1;
                    indices[i + 2] = s + 2;
                    indices[i + 3] = s + 2;
                    indices[i + 4] = s + 1;
                    indices[i + 5] = s + 3;
                }
                s += 2;
            }

            // Add cap front and back
            int capIndex = vertices.Count;
            vertices.Add(new Vector3(0, 0, -1f));
            vertices.Add(new Vector3(0, 0, 1f));
            uv.Add(Vector2.zero);
            uv.Add(Vector2.zero);

            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            if (_capped) {
                if (indicesCap == null) {
                    indicesCap = new List<int>();
                } else {
                    indicesCap.Clear();
                }
                int secondCapSubMesh = 2;
                if (_cappedBothEnds) {
                    for (int s = 0; s < _sides * 2; s += 2) {
                        indicesCap.Add(s);
                        indicesCap.Add(s + 2);
                        indicesCap.Add(capIndex);
                    }
                    mesh.SetTriangles(indicesCap, 1);
                } else {
                    secondCapSubMesh = 1;
                }

                indicesCap.Clear();
                int offset = (_segments - 1) * (_sides + 1) * 2 + 1;
                for (int s = 0; s < _sides * 2; s += 2) {
                    indicesCap.Add(s + offset);
                    indicesCap.Add(s + offset + 2);
                    indicesCap.Add(capIndex + 1);
                }
                mesh.SetTriangles(indicesCap, secondCapSubMesh);
            }

            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();

            mf.sharedMesh = mesh;
            if (tunnelMats == null || tunnelMats.Length == 0) {
                tunnelMats = new Material[3];
            }
            tunnelMats[0] = tunnelMat;
            tunnelMats[1] = tunnelCapMat;
            tunnelMats[2] = tunnelCapMat;
            mr.sharedMaterials = tunnelMats;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            MeshCollider mc = gameObject.GetComponent<MeshCollider>();
            if (mc != null) {
                mc.sharedMesh = mesh;
            }

        }


    }

}