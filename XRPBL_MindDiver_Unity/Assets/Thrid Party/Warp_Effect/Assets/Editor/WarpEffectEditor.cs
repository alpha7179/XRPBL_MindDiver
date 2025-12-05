using UnityEngine;
using UnityEditor;

/// <summary>
/// This is the editor for the Warp Effect without particle effect stars.
/// (c) 2018 Dirk Jacobasch
/// dirk.jacobasch@outlook.com
/// </summary>
namespace com.ggames4u {

	[CustomEditor(typeof(WarpEffect))]
    public class WarpEffectEditor : Editor {
		private SerializedObject serObj;

        public string[] EmissionMapModes = new string[] { "Add", "Multiply" };

        [SerializeField] private Renderer warpRenderer;

        [SerializeField] private Material warpMaterial;

        [SerializeField] private Color mainColor;
        [SerializeField] private Color mixColor;
		
        // Texture 1
		[SerializeField] private Texture2D mainTexture1;
        [SerializeField] private float mainTexture1TilingX;
        [SerializeField] private float mainTexture1TilingY;
        [SerializeField] private float mainTextureRotationSpeed1;

        // Texture 2
        [SerializeField] private Texture2D mainTexture2;
        [SerializeField] private float mainTexture2TilingX;
        [SerializeField] private float mainTexture2TilingY;
        [SerializeField] private float mainTextureRotationSpeed2;

        // Texture 3
        [SerializeField] private Texture2D mainTexture3;
        [SerializeField] private float mainTexture3TilingX;
        [SerializeField] private float mainTexture3TilingY;
        [SerializeField] private float mainTextureRotationSpeed3;

        // Noise texture
        [SerializeField] Texture2D noiseTexture;
        [SerializeField] private float noiseTextureTilingX;
        [SerializeField] private float noiseTextureTilingY;
		
		[SerializeField] private Texture2D backgroundBumpTexture;
        [SerializeField] private float backgroundBumpTextureTilingX;
        [SerializeField] private float backgroundBumpTextureTilingY;

        [SerializeField] private float distortionStrength;
        [SerializeField] private float distortionFrequence;

		[SerializeField] private float speedX;
        [SerializeField] private float speedY;
        [SerializeField] private float scale;
        [SerializeField] private float tileX;
        [SerializeField] private float tileY;

        [SerializeField] private float warpSpeed;

        // Emission maps

        [ColorUsage(false, true)]
        [SerializeField] private Color emissionColor;

		[SerializeField] private Texture2D emissionMap_01;
		[SerializeField] private float emissionMap_01_TilingX;
		[SerializeField] private float emissionMap_01_TilingY;
        [SerializeField] private float emissionMap_Rotation_Speed_01;

        [SerializeField] private Texture2D emissionMap_02;
		[SerializeField] private float emissionMap_02_TilingX;
		[SerializeField] private float emissionMap_02_TilingY;
		[SerializeField] private float emissionMap_Rotation_Speed_02;

        [SerializeField] private int multiplayEmissionMaps;

        // Emission map distortion
        [SerializeField] private bool emissionMapDistortion;
        [SerializeField] private float emissionMapDistortionScale;
        [SerializeField] private float emissionMapDistortionSpeedY;

        // Stars

        [SerializeField] private Texture2D starTextureLayer1;
        [SerializeField]  private float starTextureLayer1TilingX;
        [SerializeField] private float starTextureLayer1TilingY;

        [SerializeField] private Texture2D starTextureLayer2;
        [SerializeField] private float starTextureLayer2TilingX;
        [SerializeField] private float starTextureLayer2TilingY;

        [SerializeField] private Color starColorLayer1;
        [SerializeField] private float starBrightnessLayer1;
        [SerializeField] private float starsStartPositionLayer1;
        [SerializeField] private float starsEndPositionLayer1;
        [SerializeField] private float starSpeedFactorLayer1;
		[SerializeField] private float starLayerRotationSpeed1;
        [SerializeField] private bool renderStarLayer1;
        
        [SerializeField] private Color starColorLayer2;
        [SerializeField] private float starBrightnessLayer2;
        [SerializeField] private float starsStartPositionLayer2;
        [SerializeField] private float starsEndPositionLayer2;
        [SerializeField] private float starSpeedFactorLayer2;
		[SerializeField] private float starLayerRotationSpeed2;
		[SerializeField] private bool renderStarLayer2;

        [SerializeField] private Vector2 scrollPos;
        [SerializeField] private GameObject gameObject;

        private WarpEffect warpSphereScript;

        private GUIStyle defaultStyle;
        private GUIStyle titleStyle;

        /// <summary>
        /// Setup all properties. This method reads the values from
        /// the warp material shader.
        /// </summary>
        private void OnEnable() {
            // Get target material
            this.warpSphereScript = this.target as WarpEffect;
            this.gameObject = this.warpSphereScript.gameObject.transform.Find("WarpSphereContainer").Find("WarpSphere").gameObject;
            this.warpRenderer = this.gameObject.GetComponent<Renderer>();
            if (this.warpRenderer != null) {
                this.warpMaterial = this.warpRenderer.sharedMaterial;
            }

			this.serObj = new SerializedObject(this.target);
			
            // Get values from material
            if (this.warpMaterial != null) {
                if (this.warpMaterial.HasProperty("_Color")) {
                    this.mainColor = this.warpMaterial.GetColor("_Color");
                }

                if (this.warpMaterial.HasProperty("_MixColor")) {
                    this.mixColor = this.warpMaterial.GetColor("_MixColor");
                }

                if (this.warpMaterial.HasProperty("_MainTex1")) {
                    this.mainTexture1 = this.warpMaterial.GetTexture("_MainTex1") as Texture2D;
                    this.mainTexture1TilingX = this.warpMaterial.GetTextureScale("_MainTex1").x;
                    this.mainTexture1TilingY = this.warpMaterial.GetTextureScale("_MainTex1").y;
                }

                if (this.warpMaterial.HasProperty("_MainTex2")) {
                    this.mainTexture2 = this.warpMaterial.GetTexture("_MainTex2") as Texture2D;
                    this.mainTexture2TilingX = this.warpMaterial.GetTextureScale("_MainTex2").x;
                    this.mainTexture2TilingY = this.warpMaterial.GetTextureScale("_MainTex2").y;
                }

                if (this.warpMaterial.HasProperty("_MainTex3")) {
                    this.mainTexture3 = this.warpMaterial.GetTexture("_MainTex3") as Texture2D;
                    this.mainTexture3TilingX = this.warpMaterial.GetTextureScale("_MainTex3").x;
                    this.mainTexture3TilingY = this.warpMaterial.GetTextureScale("_MainTex3").y;
                }
                
                if (this.warpMaterial.HasProperty("_NoiseTex")) {
                    this.noiseTexture = this.warpMaterial.GetTexture("_NoiseTex") as Texture2D;
                    this.noiseTextureTilingX = this.warpMaterial.GetTextureScale("_NoiseTex").x;
                    this.noiseTextureTilingY = this.warpMaterial.GetTextureScale("_NoiseTex").y;
                }

				// Main texture rotation
				if (this.warpMaterial.HasProperty("_MainTexRotationSpeed1")) {
					this.mainTextureRotationSpeed1 = this.warpMaterial.GetFloat("_MainTexRotationSpeed1");
				}

				if (this.warpMaterial.HasProperty("_MainTexRotationSpeed2")) {
					this.mainTextureRotationSpeed2 = this.warpMaterial.GetFloat("_MainTexRotationSpeed2");
				}

				if (this.warpMaterial.HasProperty("_MainTexRotationSpeed3")) {
					this.mainTextureRotationSpeed3 = this.warpMaterial.GetFloat("_MainTexRotationSpeed3");
				}

				// Emission maps
				if (this.warpMaterial.HasProperty("_EmissionMap_01")) {
					this.emissionMap_01 = this.warpMaterial.GetTexture("_EmissionMap_01") as Texture2D;
					this.emissionMap_01_TilingX = this.warpMaterial.GetTextureScale("_EmissionMap_01").x;
					this.emissionMap_01_TilingY = this.warpMaterial.GetTextureScale("_EmissionMap_01").y;
				}

				if (this.warpMaterial.HasProperty("_EmissionMap_02")) {
					this.emissionMap_02 = this.warpMaterial.GetTexture("_EmissionMap_02") as Texture2D;
					this.emissionMap_02_TilingX = this.warpMaterial.GetTextureScale("_EmissionMap_02").x;
					this.emissionMap_02_TilingY = this.warpMaterial.GetTextureScale("_EmissionMap_02").y;
				}

				if (this.warpMaterial.HasProperty("_EmissionColor")) {
					this.emissionColor = this.warpMaterial.GetColor("_EmissionColor");
				}

                if (this.warpMaterial.HasProperty("_MultiplyEmissionMaps")) {
                    this.multiplayEmissionMaps = (this.warpMaterial.GetFloat("_MultiplyEmissionMaps") > 0) ? 1 : 0;
                }

                if (this.warpMaterial.HasProperty("_EmissionMapDistortion")) {
                    this.emissionMapDistortion = (this.warpMaterial.GetFloat("_EmissionMapDistortion") > 0) ? true : false;
                }

                if (this.warpMaterial.HasProperty("_EmissionMapDistortionScale")) {
                    this.emissionMapDistortionScale = this.warpMaterial.GetFloat("_EmissionMapDistortionScale");
                }

                if (this.warpMaterial.HasProperty("_EmissionMapDistortionSpeedY")) {
                    this.emissionMapDistortionSpeedY = this.warpMaterial.GetFloat("_EmissionMapDistortionSpeedY");
                }

                // Emission map rotation
                if (this.warpMaterial.HasProperty("_EmissionMapRotationSpeed_01")) {
					this.emissionMap_Rotation_Speed_01 = this.warpMaterial.GetFloat("_EmissionMapRotationSpeed_01");
				}

				if (this.warpMaterial.HasProperty("_EmissionMapRotationSpeed_02")) {
					this.emissionMap_Rotation_Speed_02 = this.warpMaterial.GetFloat("_EmissionMapRotationSpeed_02");
				}

                // Distortion
                if (this.warpMaterial.HasProperty("_DistortionBumpMap")) {
                    this.backgroundBumpTexture = this.warpMaterial.GetTexture("_DistortionBumpMap") as Texture2D;
                    this.backgroundBumpTextureTilingX = this.warpMaterial.GetTextureScale("_DistortionBumpMap").x;
                    this.backgroundBumpTextureTilingY = this.warpMaterial.GetTextureScale("_DistortionBumpMap").y;
                }

                if (this.warpMaterial.HasProperty("_BackgroundDistortionStrength")) {
                    this.distortionStrength = this.warpMaterial.GetFloat("_BackgroundDistortionStrength");
                }

                if (this.warpMaterial.HasProperty("_BackgroundDistortionFrequence")) {
                    this.distortionFrequence = this.warpMaterial.GetFloat("_BackgroundDistortionFrequence");
                }

                if (this.warpMaterial.HasProperty("_SpeedX")) {
                    this.speedX = this.warpMaterial.GetFloat("_SpeedX");
                }

                if (this.warpMaterial.HasProperty("_SpeedY")) {
                    this.speedY = this.warpMaterial.GetFloat("_SpeedY");
                }

                if (this.warpMaterial.HasProperty("_Scale")) {
                    this.scale = this.warpMaterial.GetFloat("_Scale");
                }

                if (this.warpMaterial.HasProperty("_TileX")) {
                    this.tileX = this.warpMaterial.GetFloat("_TileX");
                }

                if (this.warpMaterial.HasProperty("_TileY")) {
                    this.tileY = this.warpMaterial.GetFloat("_TileY");
                }

                if (this.warpMaterial.HasProperty("_WarpSpeed")) {
                    this.warpSpeed = this.warpMaterial.GetFloat("_WarpSpeed");
                }

                // Get stars layer 1 properties
                if (this.warpMaterial.HasProperty("_StarTexLayer1")) {
                    this.starTextureLayer1 = this.warpMaterial.GetTexture("_StarTexLayer1") as Texture2D;
                    this.starTextureLayer1TilingX = this.warpMaterial.GetTextureScale("_StarTexLayer1").x;
                    this.starTextureLayer1TilingY = this.warpMaterial.GetTextureScale("_StarTexLayer1").y;
                }

                if (this.warpMaterial.HasProperty("_StarsStartPosLayer1")) {
                    this.starsStartPositionLayer1 = this.warpMaterial.GetFloat("_StarsStartPosLayer1");
                }

                if (this.warpMaterial.HasProperty("_StarsEndPosLayer1")) {
                    this.starsEndPositionLayer1 = this.warpMaterial.GetFloat("_StarsEndPosLayer1");
                }

                if (this.warpMaterial.HasProperty("_StarColorLayer1")) {
                    this.starColorLayer1 = this.warpMaterial.GetColor("_StarColorLayer1");
                }

                if (this.warpMaterial.HasProperty("_StarTexLayer1Brightness")) {
                    this.starBrightnessLayer1 = this.warpMaterial.GetFloat("_StarTexLayer1Brightness");
                }

                if (this.warpMaterial.HasProperty("_StarSpeedFactorLayer1")) {
                    this.starSpeedFactorLayer1 = this.warpMaterial.GetFloat("_StarSpeedFactorLayer1");
                }

				if (this.warpMaterial.HasProperty("_StarLayerRotationSpeed1")) {
					this.starLayerRotationSpeed1 = this.warpMaterial.GetFloat("_StarLayerRotationSpeed1");
				}

                if (this.warpMaterial.HasProperty("_RenderStarLayer1")) {
                    this.renderStarLayer1 = (this.warpMaterial.GetFloat("_RenderStarLayer1") > 0) ? true : false;
                }

                // Get stars layer 2 properties
                if (this.warpMaterial.HasProperty("_StarTexLayer2")) {
                    this.starTextureLayer2 = this.warpMaterial.GetTexture("_StarTexLayer2") as Texture2D;
                    this.starTextureLayer2TilingX = this.warpMaterial.GetTextureScale("_StarTexLayer2").x;
                    this.starTextureLayer2TilingY = this.warpMaterial.GetTextureScale("_StarTexLayer2").y;
                }

                if (this.warpMaterial.HasProperty("_StarsStartPosLayer2")) {
                    this.starsStartPositionLayer2 = this.warpMaterial.GetFloat("_StarsStartPosLayer2");
                }

                if (this.warpMaterial.HasProperty("_StarsEndPosLayer2")) {
                    this.starsEndPositionLayer2 = this.warpMaterial.GetFloat("_StarsEndPosLayer2");
                }

                if (this.warpMaterial.HasProperty("_StarColorLayer2")) {
                    this.starColorLayer2 = this.warpMaterial.GetColor("_StarColorLayer2");
                }

                if (this.warpMaterial.HasProperty("_StarTexLayer2Brightness")) {
                    this.starBrightnessLayer2 = this.warpMaterial.GetFloat("_StarTexLayer2Brightness");
                }

                if (this.warpMaterial.HasProperty("_StarSpeedFactorLayer2")) {
                    this.starSpeedFactorLayer2 = this.warpMaterial.GetFloat("_StarSpeedFactorLayer2");
                }

				if (this.warpMaterial.HasProperty("_StarLayerRotationSpeed2")) {
					this.starLayerRotationSpeed2 = this.warpMaterial.GetFloat("_StarLayerRotationSpeed2");
				}

				if (this.warpMaterial.HasProperty("_RenderStarLayer2")) {
                    this.renderStarLayer2 = (this.warpMaterial.GetFloat("_RenderStarLayer2") > 0) ? true : false;
                }
            }
        }

        /// <summary>
        /// Create editor UI.
        /// The editor is separeated into different blocks.
        /// </summary>
        public override void OnInspectorGUI() {
			this.serObj.Update();

			// Styles
			this.defaultStyle = new GUIStyle(EditorStyles.textField);
            this.defaultStyle.normal.textColor = new Color(0.25f, 0.25f, 0.25f);
            this.defaultStyle.normal.background = null;
            this.defaultStyle.fontSize = 11;
            this.defaultStyle.fontStyle = FontStyle.Bold;
            this.defaultStyle.stretchHeight = false;
            this.defaultStyle.clipping = TextClipping.Clip;

            this.titleStyle = new GUIStyle(EditorStyles.textField);
            this.titleStyle.normal.textColor = new Color(0.85f, 0.15f, 0.1f);
            this.titleStyle.normal.background = null;
            this.titleStyle.fontSize = 12;
            this.titleStyle.fontStyle = FontStyle.Bold;
            this.titleStyle.stretchHeight = true;
            this.titleStyle.clipping = TextClipping.Overflow;

            this.scrollPos = EditorGUILayout.BeginScrollView(this.scrollPos);
            EditorGUILayout.BeginVertical("box");

            GUILayout.Space(5f);
            EditorGUILayout.LabelField("Warp Colors", this.titleStyle);
            GUILayout.Space(5f);

			EditorGUI.BeginChangeCheck();
			Undo.RecordObject(this, "Undo Warp Settings");

			EditorGUI.indentLevel += 1;
            this.mainColor = EditorGUILayout.ColorField("Main Color", this.mainColor);

            this.mixColor = EditorGUILayout.ColorField("Mix Color", this.mixColor);
            EditorGUI.indentLevel -= 1;
            GUILayout.Space(5f);

            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10f);

            EditorGUILayout.BeginVertical("box");

            // Main textures
            EditorGUILayout.LabelField("Warp Textures", this.titleStyle);
            GUILayout.Space(10f);

            EditorGUI.indentLevel += 1;

            // Main texture 1
            this.mainTexture1 = (Texture2D)EditorGUILayout.ObjectField("Main Texture 1", this.mainTexture1, typeof(Texture2D), true);
            this.mainTexture1TilingX = EditorGUILayout.FloatField("Tiling X", this.mainTexture1TilingX);
            this.mainTexture1TilingY = EditorGUILayout.FloatField("Tiling Y", this.mainTexture1TilingY);
			GUILayout.Space(5f);

			this.mainTextureRotationSpeed1 = EditorGUILayout.Slider(
				new GUIContent("Main Texture 1 Rotation Speed", ""),
				this.mainTextureRotationSpeed1,
				-25.0f, 25.0f);
			GUILayout.Space(10f);

            // Main texture 2
            this.mainTexture2 = (Texture2D)EditorGUILayout.ObjectField("Main Texture 2", this.mainTexture2, typeof(Texture2D), true);
            this.mainTexture2TilingX = EditorGUILayout.FloatField("Tiling X", this.mainTexture2TilingX);
            this.mainTexture2TilingY = EditorGUILayout.FloatField("Tiling Y", this.mainTexture2TilingY);
			GUILayout.Space(5f);

			this.mainTextureRotationSpeed2 = EditorGUILayout.Slider(
				new GUIContent("Main Texture 2 Rotation Speed", ""),
				this.mainTextureRotationSpeed2,
				-25.0f, 25.0f);
			GUILayout.Space(10f);

            // Main texture 3
            this.mainTexture3 = (Texture2D)EditorGUILayout.ObjectField("Main Texture 3", this.mainTexture3, typeof(Texture2D), true);
            this.mainTexture3TilingX = EditorGUILayout.FloatField("Tiling X", this.mainTexture3TilingX);
            this.mainTexture3TilingY = EditorGUILayout.FloatField("Tiling Y", this.mainTexture3TilingY);
			GUILayout.Space(5f);

			this.mainTextureRotationSpeed3 = EditorGUILayout.Slider(
				new GUIContent("Main Texture 3 Rotation Speed", ""),
				this.mainTextureRotationSpeed3,
				-25.0f, 25.0f);
			GUILayout.Space(10f);
            
            // Noise texture
            this.noiseTexture = (Texture2D)EditorGUILayout.ObjectField("Noise Texture", this.noiseTexture, typeof(Texture2D), true);
            this.noiseTextureTilingX = EditorGUILayout.FloatField("Tiling X", this.noiseTextureTilingX);
            this.noiseTextureTilingY = EditorGUILayout.FloatField("Tiling Y", this.noiseTextureTilingY);
            GUILayout.Space(10f);

            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

			GUILayout.Space(10f);

			// Emission maps
			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.LabelField("Emission Maps", this.titleStyle);
			GUILayout.Space(10f);
			EditorGUI.indentLevel += 1;

			// Emission map 1
			this.emissionMap_01 = (Texture2D)EditorGUILayout.ObjectField("Emission Map 1", this.emissionMap_01, typeof(Texture2D), true);
			this.emissionMap_01_TilingX = EditorGUILayout.FloatField("Tiling X", this.emissionMap_01_TilingX);
			this.emissionMap_01_TilingY = EditorGUILayout.FloatField("Tiling Y", this.emissionMap_01_TilingY);
			GUILayout.Space(5f);

			this.emissionMap_Rotation_Speed_01 = EditorGUILayout.Slider(
				new GUIContent("Emission Map 1 Rotation Speed", ""),
				this.emissionMap_Rotation_Speed_01,
				-25.0f, 25.0f);
			GUILayout.Space(10f);

			// Emission map 2
			this.emissionMap_02 = (Texture2D)EditorGUILayout.ObjectField("Emission Map 2", this.emissionMap_02, typeof(Texture2D), true);
			this.emissionMap_02_TilingX = EditorGUILayout.FloatField("Tiling X", this.emissionMap_02_TilingX);
			this.emissionMap_02_TilingY = EditorGUILayout.FloatField("Tiling Y", this.emissionMap_02_TilingY);

            GUILayout.Space(5f);

			this.emissionMap_Rotation_Speed_02 = EditorGUILayout.Slider(
				new GUIContent("Emission Map 2 Rotation Speed", ""),
				this.emissionMap_Rotation_Speed_02,
				-25.0f, 25.0f);

            GUILayout.Space(5f);

            // Emission color
            GUILayoutOption[] options = new GUILayoutOption[] {};
			this.emissionColor = EditorGUILayout.ColorField(new GUIContent("Emission Color"), this.emissionColor, true, false,  true, options);

            GUILayout.Space(5f);

            // Emission map mode. Add or multiply
            this.multiplayEmissionMaps = EditorGUILayout.Popup(new GUIContent("Emission Map Mode", "Add or multiply emission maps togesther."), multiplayEmissionMaps, this.EmissionMapModes);

            GUILayout.Space(15f);

            // Emission Map Distaortion
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.LabelField("Emission Map Distaortion", this.defaultStyle);
            EditorGUI.indentLevel += 1;
            this.emissionMapDistortion = EditorGUILayout.Toggle(new GUIContent("Enable", "Enable or disable the emission map distortion."), this.emissionMapDistortion);

            GUILayout.Space(5f);

            this.emissionMapDistortionScale = EditorGUILayout.FloatField(new GUIContent("Emission Map Distortion Scale", "This value set the strength of the emission map distortion. It's better to try small values."), this.emissionMapDistortionScale);
            this.emissionMapDistortionSpeedY = EditorGUILayout.FloatField(new GUIContent("Emission Map Distortion Speed", "Defines how fast the emission distortion is moving along the warp sphere."), this.emissionMapDistortionSpeedY);

            GUILayout.Space(10f);

            EditorGUI.indentLevel -= 1;
			EditorGUILayout.EndVertical();

			GUILayout.Space(10f);
			
			// Distortion and speed settings
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Distortion and Speed", this.titleStyle);
            GUILayout.Space(10f);

            EditorGUI.indentLevel += 1;
            this.speedX = EditorGUILayout.FloatField("Speed X", this.speedX);
            this.speedY = EditorGUILayout.FloatField("Speed Y", this.speedY);
            this.tileX = EditorGUILayout.FloatField(new GUIContent("Tile X", "Tiling X size of the effect"), this.tileX);
            this.tileY = EditorGUILayout.FloatField(new GUIContent("Tile Y", "Tiling Y size of the effect"), this.tileY);
            GUILayout.Space(5f);
            this.scale = EditorGUILayout.Slider(
                new GUIContent("Scale", "The strength of the texture distortion. The higher the value the more waves will impact the warp textures."), 
                this.scale, 
                0.0f, 0.015f);
            this.warpSpeed = EditorGUILayout.Slider(
                new GUIContent("Warp Speed", "The overall speed of the Warp Effect. The warp speed also influences the speed of the star layers."), 
                this.warpSpeed, 
                1f, 25f);
            EditorGUI.indentLevel -= 1;

            EditorGUILayout.EndVertical();

            // Background distortion
            GUILayout.Space(10f);
            
            EditorGUILayout.BeginVertical("box");

            GUILayout.Space(5f);
            EditorGUILayout.LabelField("Background Distortion", this.titleStyle);
            GUILayout.Space(5f);
            EditorGUI.indentLevel += 1;

            // Background distortion texture
            this.backgroundBumpTexture = (Texture2D)EditorGUILayout.ObjectField("Normalmap", this.backgroundBumpTexture, typeof(Texture2D), true);
            this.backgroundBumpTextureTilingX = EditorGUILayout.FloatField("Tiling X", this.backgroundBumpTextureTilingX);
            this.backgroundBumpTextureTilingY = EditorGUILayout.FloatField("Tiling Y", this.backgroundBumpTextureTilingY);
            GUILayout.Space(10f);
            
            // Background distortion strength
            this.distortionStrength = EditorGUILayout.Slider("Distortion Strength", this.distortionStrength, 0.0f, 100f);
            GUILayout.Space(5f);

            // Background distortion frequence
            this.distortionFrequence = EditorGUILayout.Slider("Distortion Frequence", this.distortionFrequence, 0.01f, 0.5f);
            GUILayout.Space(5f);

            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            // Stars
            GUILayout.Space(10f);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Stars", this.titleStyle);

            EditorGUI.indentLevel += 1;
            GUILayout.Space(10f);

            // Layer 1
            this.starTextureLayer1 = (Texture2D)EditorGUILayout.ObjectField("Star Layer 1", this.starTextureLayer1, typeof(Texture2D), true);
            this.starTextureLayer1TilingX = EditorGUILayout.FloatField("Tiling X", this.starTextureLayer1TilingX);
            this.starTextureLayer1TilingY = EditorGUILayout.FloatField("Tiling Y", this.starTextureLayer1TilingY);
            GUILayout.Space(5f);

            // Start / end position
            this.starsStartPositionLayer1 = EditorGUILayout.Slider(
                new GUIContent("Start Position", "0 is the center of the warp sphere and 1 is the front of the warp sphere."), 
                this.starsStartPositionLayer1, 
                0f, 0.5f);
            GUILayout.Space(5f);

            this.starsEndPositionLayer1 = EditorGUILayout.Slider(
                new GUIContent("End Position", "0 is the end of the warp sphere and 1 is the center of the warp sphere."), 
                this.starsEndPositionLayer1, 
                0f, 0.5f);
            GUILayout.Space(5f);

            this.starColorLayer1 = EditorGUILayout.ColorField("Star Color", this.starColorLayer1);
            GUILayout.Space(5f);
            this.starBrightnessLayer1 = EditorGUILayout.Slider(new GUIContent("Brightness", "The brightness of the stars. The higher the value, the brighter the stars are."), this.starBrightnessLayer1, 0f, 50f);
            GUILayout.Space(5f);
            this.starSpeedFactorLayer1 = EditorGUILayout.Slider(
                new GUIContent("Star Speed Factor", "The higher the value the faster the stars"), 
                this.starSpeedFactorLayer1, 
                0.01f, 0.5f);
            GUILayout.Space(5f);

			// Stars rotation speed
			this.starLayerRotationSpeed1 = EditorGUILayout.Slider(
				new GUIContent("Star Rotation Speed Factor", "Defines how fast the stars rotate around the warp sphere. The higher the value the faster the rotation"),
				this.starLayerRotationSpeed1,
				-10.0f, 10.0f);
			GUILayout.Space(5f);

			// Render toggle
			this.renderStarLayer1 = EditorGUILayout.Toggle(new GUIContent("Render Star Layer", "Enable or disable star layer"), this.renderStarLayer1);
            GUILayout.Space(20f);

            // Layer 2
            this.starTextureLayer2 = (Texture2D)EditorGUILayout.ObjectField("Star Layer 2", this.starTextureLayer2, typeof(Texture2D), true);
            this.starTextureLayer2TilingX = EditorGUILayout.FloatField("Tiling X", this.starTextureLayer2TilingX);
            this.starTextureLayer2TilingY = EditorGUILayout.FloatField("Tiling Y", this.starTextureLayer2TilingY);
            GUILayout.Space(5f);

            // Start / end position
            this.starsStartPositionLayer2 = EditorGUILayout.Slider(
                new GUIContent("Start Position", "0 is the center of the warp sphere and 0.5 is the front of the warp sphere."), 
                this.starsStartPositionLayer2, 
                0f, 0.5f);
            GUILayout.Space(5f);

            this.starsEndPositionLayer2 = EditorGUILayout.Slider(
                new GUIContent("End Position", "0 is the end of the warp sphere and 0.5 is the center of the warp sphere."), 
                this.starsEndPositionLayer2, 
                0f, 0.5f);
            GUILayout.Space(5f);

            this.starColorLayer2 = EditorGUILayout.ColorField("Star Color", this.starColorLayer2);
            GUILayout.Space(5f);
            this.starBrightnessLayer2 = EditorGUILayout.Slider(new GUIContent("Brightness", "The brightness of the stars. The higher the value, the brighter the stars are."), this.starBrightnessLayer2, 0f, 50f);
            GUILayout.Space(5f);
            this.starSpeedFactorLayer2 = EditorGUILayout.Slider(
                new GUIContent("Star Speed Factor", "The higher the value the faster the stars"), 
                this.starSpeedFactorLayer2, 
                0.01f, 0.5f);
            GUILayout.Space(5f);

			// Stars rotation speed
			this.starLayerRotationSpeed2 = EditorGUILayout.Slider(
				new GUIContent("Star Rotation Speed Factor", "Defines how fast the stars rotate around the warp sphere. The higher the value the faster the rotation"),
				this.starLayerRotationSpeed2,
				-10.0f, 10.0f);
			GUILayout.Space(5f);

			// Render toggle
			this.renderStarLayer2 = EditorGUILayout.Toggle(new GUIContent("Render Star Layer", "Enable or disable star layer"), this.renderStarLayer2);
            EditorGUI.indentLevel -= 1;

            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10f);
			
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10f);

			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(this.warpMaterial, "Undo Warp Settings");

				OnApply();
            }
        }
		
        /// <summary>
        /// Apply values to the material if something was changed in the editor.
        /// </summary>
        private void OnApply() {
            if (this.warpMaterial != null) {
				// Colors
				this.warpMaterial.SetColor("_Color", this.mainColor);
                this.warpMaterial.SetColor("_MixColor", this.mixColor);

                // Main textures
                this.warpMaterial.SetTexture("_MainTex1", this.mainTexture1);
                this.warpMaterial.SetTextureScale("_MainTex1", new Vector2(this.mainTexture1TilingX, this.mainTexture1TilingY));
				this.warpMaterial.SetFloat("_MainTexRotationSpeed1", this.mainTextureRotationSpeed1);

                this.warpMaterial.SetTexture("_MainTex2", this.mainTexture2);
                this.warpMaterial.SetTextureScale("_MainTex2", new Vector2(this.mainTexture2TilingX, this.mainTexture2TilingY));
				this.warpMaterial.SetFloat("_MainTexRotationSpeed2", this.mainTextureRotationSpeed2);

				this.warpMaterial.SetTexture("_MainTex3", this.mainTexture3);
                this.warpMaterial.SetTextureScale("_MainTex3", new Vector2(this.mainTexture3TilingX, this.mainTexture3TilingY));
				this.warpMaterial.SetFloat("_MainTexRotationSpeed3", this.mainTextureRotationSpeed3);

				this.warpMaterial.SetTexture("_NoiseTex", this.noiseTexture);
                this.warpMaterial.SetTextureScale("_NoiseTex", new Vector2(this.noiseTextureTilingX, this.noiseTextureTilingY));

				// Emission maps
				this.warpMaterial.SetTexture("_EmissionMap_01", this.emissionMap_01);
				this.warpMaterial.SetTextureScale("_EmissionMap_01", new Vector2(this.emissionMap_01_TilingX, this.emissionMap_01_TilingY));
				this.warpMaterial.SetFloat("_EmissionMapRotationSpeed_01", this.emissionMap_Rotation_Speed_01);

				this.warpMaterial.SetTexture("_EmissionMap_02", this.emissionMap_02);
				this.warpMaterial.SetTextureScale("_EmissionMap_02", new Vector2(this.emissionMap_02_TilingX, this.emissionMap_02_TilingY));
				this.warpMaterial.SetFloat("_EmissionMapRotationSpeed_02", this.emissionMap_Rotation_Speed_02);

                this.warpMaterial.SetFloat("_MultiplyEmissionMaps", this.multiplayEmissionMaps);

                // Emission map distortion
                this.warpMaterial.SetFloat("_EmissionMapDistortion", (this.emissionMapDistortion == true) ? 1f : 0f);
                this.warpMaterial.SetFloat("_EmissionMapDistortionScale", this.emissionMapDistortionScale);
                this.warpMaterial.SetFloat("_EmissionMapDistortionSpeedY", this.emissionMapDistortionSpeedY);

                this.warpMaterial.SetColor("_EmissionColor", this.emissionColor);

				// Warp settings
				this.warpMaterial.SetFloat("_SpeedX", this.speedX);
                this.warpMaterial.SetFloat("_SpeedY", this.speedY);
                this.warpMaterial.SetFloat("_Scale", this.scale);
                this.warpMaterial.SetFloat("_TileX", this.tileX);
                this.warpMaterial.SetFloat("_TileY", this.tileY);
                this.warpMaterial.SetFloat("_WarpSpeed", this.warpSpeed);

                // Background distortion
                this.warpMaterial.SetTexture("_DistortionBumpMap", this.backgroundBumpTexture);
                this.warpMaterial.SetTextureScale("_DistortionBumpMap", new Vector2(this.backgroundBumpTextureTilingX, this.backgroundBumpTextureTilingY));

                this.warpMaterial.SetFloat("_BackgroundDistortionStrength", this.distortionStrength);
                this.warpMaterial.SetFloat("_BackgroundDistortionFrequence", this.distortionFrequence);

                // Stars layer 1
                this.warpMaterial.SetTexture("_StarTexLayer1", this.starTextureLayer1);
                this.warpMaterial.SetTextureScale("_StarTexLayer1", new Vector2(this.starTextureLayer1TilingX, this.starTextureLayer1TilingY));
                this.warpMaterial.SetFloat("_StarsStartPosLayer1", this.starsStartPositionLayer1);
                this.warpMaterial.SetFloat("_StarsEndPosLayer1", this.starsEndPositionLayer1);
                this.warpMaterial.SetColor("_StarColorLayer1", this.starColorLayer1);
                this.warpMaterial.SetFloat("_StarTexLayer1Brightness", this.starBrightnessLayer1);
                this.warpMaterial.SetFloat("_StarSpeedFactorLayer1", this.starSpeedFactorLayer1);
				this.warpMaterial.SetFloat("_StarLayerRotationSpeed1", this.starLayerRotationSpeed1);
				this.warpMaterial.SetFloat("_RenderStarLayer1", (this.renderStarLayer1 == true) ? 1f : 0f);

                // Stars layer 2
                this.warpMaterial.SetTexture("_StarTexLayer2", this.starTextureLayer2);
                this.warpMaterial.SetTextureScale("_StarTexLayer2", new Vector2(this.starTextureLayer2TilingX, this.starTextureLayer2TilingY));
                this.warpMaterial.SetFloat("_StarsStartPosLayer2", this.starsStartPositionLayer2);
                this.warpMaterial.SetFloat("_StarsEndPosLayer2", this.starsEndPositionLayer2);
                this.warpMaterial.SetColor("_StarColorLayer2", this.starColorLayer2);
                this.warpMaterial.SetFloat("_StarTexLayer2Brightness", this.starBrightnessLayer2);
                this.warpMaterial.SetFloat("_StarSpeedFactorLayer2", this.starSpeedFactorLayer2);
				this.warpMaterial.SetFloat("_StarLayerRotationSpeed2", this.starLayerRotationSpeed2);
				this.warpMaterial.SetFloat("_RenderStarLayer2", (this.renderStarLayer2 == true) ? 1f : 0f);

				EditorUtility.SetDirty(this.warpMaterial);
			}
        }
    }
}
