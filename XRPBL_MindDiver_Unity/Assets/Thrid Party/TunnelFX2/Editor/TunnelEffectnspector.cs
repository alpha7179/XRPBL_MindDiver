using UnityEngine;
using UnityEditor;
using System.Collections;

namespace TunnelEffect {
    [CustomEditor(typeof(TunnelFX2))]
    public class TunnelFX2Inspector : Editor {

        TunnelFX2 _effect;
        static GUIStyle titleLabelStyle, sectionHeaderStyle;
        static Color titleColor;
        static readonly string[] layerNames = {
            "Layer 1",
            "Layer 2",
            "Layer 3",
            "Layer 4"
        };
        static bool[] expandLayer = new bool[4];

        void OnEnable() {
            titleColor = EditorGUIUtility.isProSkin ? new Color(0.52f, 0.66f, 0.9f) : new Color(0.12f, 0.16f, 0.4f);
            _effect = (TunnelFX2)target;
            for (int k = 0; k < 4; k++) {
                expandLayer[k] = EditorPrefs.GetBool("TunnelExpandLayer" + k, false);
            }
        }

        void OnDestroy() {
            // Save folding sections state
            for (int k = 0; k < 4; k++) {
                EditorPrefs.SetBool("TunnelExpandLayer" + k, expandLayer[k]);
            }
        }

        public override void OnInspectorGUI() {
            if (_effect == null)
                return;
            _effect.isDirty = false;

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            DrawLabel("General Settings");
            if (GUILayout.Button("Help", GUILayout.Width(40))) {
                if (!EditorUtility.DisplayDialog("Tunnel FX", "To learn more about a property in this inspector move the mouse over the label for a quick description (tooltip).\n\nPlease check README file in the root of the asset for details and contact support.\n\nIf you like Tunnel FX, please rate it on the Asset Store. For feedback and suggestions visit our support forum on kronnect.com.", "Close", "Visit Support Forum")) {
                    Application.OpenURL("https://kronnect.com/support");
                }
            }

            EditorGUILayout.EndHorizontal();

            _effect.preset = (TUNNEL_PRESET)EditorGUILayout.EnumPopup(new GUIContent("Preset", "Quick configurations."), _effect.preset);
            _effect.sides = EditorGUILayout.IntSlider("Sides", _effect.sides, 3, 32);
            _effect.segments = EditorGUILayout.IntField(new GUIContent("Segments", "Tube segments. Increase only if you need to produce a curved tunnel."), _effect.segments);
            _effect.aperture = EditorGUILayout.FloatField(new GUIContent("Aperture", "Increases size of end tunnel."), _effect.aperture);
            if (_effect.segments <= 1 && _effect.aperture != 0) {
                EditorGUILayout.HelpBox("Increase number of segments when using aperture for a better result.", MessageType.Warning);
            }
            _effect.curvedTunnel = EditorGUILayout.Toggle(new GUIContent("Curved Tunnel"), _effect.curvedTunnel);
            if (_effect.curvedTunnel) {
                EditorGUI.indentLevel++;
                _effect.curvedTunnelFrequency = EditorGUILayout.Slider("Frequency", _effect.curvedTunnelFrequency, 0, 0.1f);
                _effect.curvedTunnelAmplitudeX = EditorGUILayout.FloatField("Amplitude X", _effect.curvedTunnelAmplitudeX);
                _effect.curvedTunnelAmplitudeY = EditorGUILayout.FloatField("Amplitude Y", _effect.curvedTunnelAmplitudeY);
                EditorGUI.indentLevel--;
            }
            _effect.positionAnimated = EditorGUILayout.Toggle(new GUIContent("Animated", "Should center be animated?"), _effect.positionAnimated);
            if (_effect.positionAnimated) {
                EditorGUI.indentLevel++;
                _effect.animationAmplitude = EditorGUILayout.Slider(new GUIContent("Amplitude", "Radius for the animation of the tunnel center"), _effect.animationAmplitude, 0, 1f);
                EditorGUI.indentLevel--;
            }
            _effect.layersSpeed = EditorGUILayout.FloatField(new GUIContent("Global Speed", "Speed multiplier applied to all layers."), _effect.layersSpeed);
            _effect.hyperSpeed = EditorGUILayout.Slider(new GUIContent("Hyper Speed", "Increase to produce a hyperspeed effect."), _effect.hyperSpeed, 0, 1f);
            _effect.uvScale = EditorGUILayout.FloatField("UV Scale", _effect.uvScale);

            _effect.enableTransparency = EditorGUILayout.Toggle(new GUIContent("Transparency", "Enables transparency on tunnel."), _effect.enableTransparency);
            if (_effect.enableTransparency) {
                EditorGUI.indentLevel++;
                _effect.drawBehindEverything = EditorGUILayout.Toggle(new GUIContent("Draw Behind All", "Renders tunnel behind everything."), _effect.drawBehindEverything);
                _effect.globalAlpha = EditorGUILayout.Slider(new GUIContent("Global Alpha", "Alpha blending between effect and your scene image."), _effect.globalAlpha, 0f, 1f);
                EditorGUI.indentLevel--;
            }

            _effect.tintColor = EditorGUILayout.ColorField(new GUIContent("Tint (RGB)", "Color tint for the complete tunnel. Tunnel color is multiplied by this value."), _effect.tintColor);

            _effect.capped = EditorGUILayout.Toggle(new GUIContent("Capped", "Generate front and back tunnel ends."), _effect.capped);
            if (_effect.capped) {
                EditorGUI.indentLevel++;
                _effect.cappedBothEnds = EditorGUILayout.Toggle(new GUIContent("Both Ends", "Disable to close just one end of the tunnel."), _effect.cappedBothEnds);
                EditorGUI.indentLevel--;
            }
            _effect.backgroundColor = EditorGUILayout.ColorField("Back Color (RGBA)", _effect.backgroundColor);

            _effect.fallOff = EditorGUILayout.Slider(new GUIContent("Fall Off End", "End of the gradient to the background color."), _effect.fallOff, 0, 1f);
            _effect.fallOffStart = EditorGUILayout.Slider(new GUIContent("Fall Off Start", "Starting distance for the gradient to the background color."), _effect.fallOffStart, 0, 1f);

            _effect.renderQueue = EditorGUILayout.IntField(new GUIContent("Render Queue", "Render queue of the tunnel material. By default it's 3100 (Transparent+100)."), _effect.renderQueue);

            EditorGUILayout.Separator();
            DrawLabel("Layer Options");

            _effect.layerCount = EditorGUILayout.IntSlider(new GUIContent("Layer Count", "Number of textures used for the tunnel effect. Each layer can be customized separately."), _effect.layerCount, 1, 4);
            _effect.textureScale = EditorGUILayout.IntSlider("Global Tiling", _effect.textureScale, 1, 8);

            if (_effect.layerCount > 1) {
                _effect.blendInOrder = EditorGUILayout.Toggle(new GUIContent("Blend In Order", "If this option is enabled, a layer will occlude previous one based on its alpha component. For example, a cutout effect will require this option enabled."), _effect.blendInOrder);
            }

            if (sectionHeaderStyle == null) {
                sectionHeaderStyle = new GUIStyle(EditorStyles.foldout);
            }
            sectionHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.52f, 0.66f, 0.9f) : new Color(0.12f, 0.16f, 0.4f);
            sectionHeaderStyle.margin = new RectOffset(12, 0, 0, 0);
            sectionHeaderStyle.fontStyle = FontStyle.Bold;

            for (int k = 0; k < _effect.layerCount; k++) {
                EditorGUILayout.BeginHorizontal();
                expandLayer[k] = EditorGUILayout.Foldout(expandLayer[k], layerNames[k], sectionHeaderStyle);
                EditorGUILayout.EndHorizontal();
                if (expandLayer[k]) {
                    EditorGUI.indentLevel++;
                    _effect.SetTexture(k, (Texture2D)EditorGUILayout.ObjectField(new GUIContent("Texture", "The texture used in this layer. Alpha channel will be used for transparency if present."), _effect.GetTexture(k), typeof(Texture2D), false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)));
                    _effect.SetAlpha(k, EditorGUILayout.FloatField(new GUIContent("Alpha", "The weight of this layer in the final color mix. The sum of all layers contributions should be 1 aprox. Use along with exposure to create nice color blends."), _effect.GetAlpha(k)));
                    _effect.SetTravelSpeed(k, EditorGUILayout.Slider("Travel Speed", _effect.GetTravelSpeed(k), -20f, 20f));
                    _effect.SetRotationSpeed(k, EditorGUILayout.Slider("Rotation", _effect.GetRotationSpeed(k), -10f, 10f));
                    _effect.SetTwist(k, EditorGUILayout.Slider("Twist", _effect.GetTwist(k), -1f, 1f));
                    _effect.SetTextureScale(k, EditorGUILayout.IntField("Tiling", _effect.GetTextureScale(k)));
                    _effect.SetExposure(k, EditorGUILayout.Slider(new GUIContent("Exposure", "An exposure below 0.5 will make this texture darker. A value above 0.5 will increase the brightness."), _effect.GetExposure(k), 0f, 1f));
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Separator();

            if (_effect.isDirty) {
                EditorUtility.SetDirty(target);
            }


        }

        void DrawLabel(string s) {
            if (titleLabelStyle == null) {
                GUIStyle skurikenModuleTitleStyle = "ShurikenModuleTitle";
                titleLabelStyle = new GUIStyle(skurikenModuleTitleStyle);
                titleLabelStyle.contentOffset = new Vector2(5f, -2f);
                titleLabelStyle.normal.textColor = titleColor;
                titleLabelStyle.fixedHeight = 22;
                titleLabelStyle.fontStyle = FontStyle.Bold;
            }

            GUILayout.Label(s, titleLabelStyle);
        }

    }

}
