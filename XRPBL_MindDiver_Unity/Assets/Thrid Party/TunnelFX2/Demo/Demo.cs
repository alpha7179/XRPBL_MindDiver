using UnityEngine;
using UnityEngine.UI;

namespace TunnelEffect {
	public class Demo : MonoBehaviour {
		
		public bool initialAlphaTransition = true;
		public Text modeText;

		int demoMode = 1;

		void Start () {
			TunnelFX2.instance.preset = TUNNEL_PRESET.SpaceTravel;
			if (initialAlphaTransition) {
				TunnelFX2.instance.globalAlpha = 0;
			}
			UpdatePresetString ();
		}

		void Update () {
			// Make a smooth alpha transition from fully transparent to full opaque
			if (initialAlphaTransition) {
				float elapsed = Time.time * 0.2f;
				if (elapsed > 1f) {
					elapsed = 1f;
					initialAlphaTransition = false;
				}
				TunnelFX2.instance.globalAlpha = elapsed;
			}
			if (Input.GetKeyDown (KeyCode.Space) || Input.GetMouseButtonDown (0)) {
				demoMode++;
				if (demoMode > 14) {
					demoMode = 1;
				}
				TunnelFX2.instance.preset = (TUNNEL_PRESET)demoMode;
				UpdatePresetString ();
			}
		}

		void UpdatePresetString () {
			modeText.text = "Press SPACE or click to change preset (currently: " + TunnelFX2.instance.preset.ToString () + ")";
		}

	}

}