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

	public enum TUNNEL_PRESET {
		Custom = 0,
		SpaceTravel = 1,
		MagmaTunnel = 2,
		CloudAscension = 3,
		MetalStructure = 4,
		WaterTunnel = 5,
		CavePassage = 6,
		Stripes = 7,
		Twightlight = 8,
		MysticTravel = 9,
		Chromatic = 10,
		IcePassage = 11,
		FireTornado = 12,
		ExhaustPort = 13,
		CutOutRail = 14
	}

	public partial class TunnelFX2 : MonoBehaviour {

		static TunnelFX2 _tunnel;

		public static TunnelFX2 instance { 
			get { 
				if (_tunnel == null) {
#if UNITY_2023_3_OR_NEWER
					_tunnel = FindAnyObjectByType<TunnelFX2>();
#else
                    _tunnel = FindObjectOfType<TunnelFX2> ();
#endif
				}
				return _tunnel;
			} 
		}

		[SerializeField]
		int _layerCount = 4;

		public int layerCount {
			get { return _layerCount; }
			set {
				if (_layerCount != value) {
					_preset = TUNNEL_PRESET.Custom;
					_layerCount = value;
					UpdateMaterialProperties ();
				}
			}
		}

		[SerializeField]
		TUNNEL_PRESET _preset = TUNNEL_PRESET.SpaceTravel;

		public TUNNEL_PRESET preset {
			get { return _preset; }
			set {
				if (_preset != value) {
					_preset = value;
					UpdateMaterialProperties ();
				}
			}
		}

		[SerializeField]
		int _renderQueue = 2080;

		public int renderQueue {
			get { return _renderQueue; }
			set {
				if (_renderQueue != value) {
					_renderQueue = value;
					UpdateMaterialProperties ();
				}
			}
		}

		[SerializeField]
		Texture2D[] _layerTextures = new Texture2D[4];

		public Texture2D GetTexture (int layerIndex) {
			return _layerTextures [layerIndex];
		}

		public void SetTexture (int layerIndex, Texture2D tex) {
			if (tex != _layerTextures [layerIndex]) {
				_preset = TUNNEL_PRESET.Custom;
				_layerTextures [layerIndex] = tex;
				UpdateMaterialProperties ();
			}
		}

		
		[SerializeField]
		float[] _travelSpeed = { 1.5f, 1.5f, 1.5f, 1.5f };

		public float GetTravelSpeed (int layerIndex) {
			return _travelSpeed [layerIndex];
		}

		public void SetTravelSpeed (int layerIndex, float value) {
			if (_travelSpeed [layerIndex] != value) {
				_preset = TUNNEL_PRESET.Custom;
				_travelSpeed [layerIndex] = value;
				UpdateMaterialProperties (); 
			}
		}

		[SerializeField]
		float[] _rotationSpeed = { 0.5f, 0.5f, 0.5f, 0.5f };

		public float GetRotationSpeed (int layerIndex) {
			return _rotationSpeed [layerIndex];
		}

		public void SetRotationSpeed (int layerIndex, float value) {
			if (_rotationSpeed [layerIndex] != value) {
				_preset = TUNNEL_PRESET.Custom;
				_rotationSpeed [layerIndex] = value;
				UpdateMaterialProperties (); 
			}
		}

		[SerializeField]
		float[] _twist = { 0.1f, 0.1f, 0.1f, 0.1f };

		public float GetTwist (int layerIndex) {
			return _twist [layerIndex];
		}

		public void SetTwist (int layerIndex, float value) {
			if (_twist [layerIndex] != value) {
				_preset = TUNNEL_PRESET.Custom;
				_twist [layerIndex] = value;
				UpdateMaterialProperties (); 
			}
		}

		[SerializeField]
		float[] _exposure = { 0.9f, 0.9f, 0.9f, 0.9f };

		public float GetExposure (int layerIndex) {
			return _exposure [layerIndex];
		}

		public void SetExposure (int layerIndex, float value) {
			if (_exposure [layerIndex] != value) {
				_preset = TUNNEL_PRESET.Custom;
				_exposure [layerIndex] = value;
				UpdateMaterialProperties (); 
			}
		}

		
		[SerializeField]
		float[] _alpha = { 0.533328f, 0.26664f, 0.13332f, 0.06666f };

		public float GetAlpha (int layerIndex) {
			return _alpha [layerIndex];
		}

		public void SetAlpha (int layerIndex, float value) {
			if (_alpha [layerIndex] != value) {
				_preset = TUNNEL_PRESET.Custom;
				_alpha [layerIndex] = value;
				UpdateMaterialProperties (); 
			}
		}



        [SerializeField]
        int[] _textureScales = { 1, 1, 1, 1 };

        public int GetTextureScale(int layerIndex) {
            return _textureScales[layerIndex];
        }

        public void SetTextureScale(int layerIndex, int value) {
            if (_textureScales[layerIndex] != value) {
                _preset = TUNNEL_PRESET.Custom;
                _textureScales[layerIndex] = value;
                UpdateMaterialProperties();
            }
        }

        [SerializeField]
		bool _positionAnimated;

		public bool positionAnimated {
			get { return _positionAnimated; }
			set {
				if (_positionAnimated != value) {
					_preset = TUNNEL_PRESET.Custom;
					_positionAnimated = value;
					UpdateMaterialProperties ();
				}
			}
		}

		[SerializeField]
		float _animationAmplitude = 0.15f;

		public float animationAmplitude {
			get { return _animationAmplitude; }
			set {
				if (_animationAmplitude != value) {
					_preset = TUNNEL_PRESET.Custom;
					_animationAmplitude = value;
					UpdateMaterialProperties ();
				}
			}
		}

		[SerializeField]
		float _hyperSpeed;

		public float hyperSpeed {
			get { return _hyperSpeed; }
			set {
				if (_hyperSpeed != value) {
					_preset = TUNNEL_PRESET.Custom;
					_hyperSpeed = value;
					UpdateMaterialProperties ();
				}
			}
		}

		[SerializeField]
		bool _enableTransparency = true;

		public bool enableTransparency {
			get { return _enableTransparency; }
			set {
				if (_enableTransparency != value) {
					_enableTransparency = value;
					needTunnelGeoRefresh = true;
					UpdateMaterialProperties ();
				}
			}
		}

		[SerializeField]
		bool _drawBehindEverything;

		public bool drawBehindEverything {
			get { return _drawBehindEverything; }
			set {
				if (_drawBehindEverything != value) {
					_drawBehindEverything = value;
					UpdateMaterialProperties();
				}
			}
		}

		[SerializeField]
		float _globalAlpha = 1f;

		public float globalAlpha {
			get { return _globalAlpha; }
			set {
				if (_globalAlpha != value) {
					_globalAlpha = value;
					UpdateMaterialProperties ();
				}
			}
		}

		[SerializeField]
		Color _backgroundColor = Color.white;

		public Color backgroundColor {
			get { return _backgroundColor; }
			set {
				if (_backgroundColor != value) {
					_preset = TUNNEL_PRESET.Custom;
					_backgroundColor = value;
					UpdateMaterialProperties ();
				}
			}
		}

		[SerializeField]
		Color _tintColor = Color.white;

		public Color tintColor {
			get { return _tintColor; }
			set {
				if (_tintColor != value) {
					_preset = TUNNEL_PRESET.Custom;
					_tintColor = value;
					UpdateMaterialProperties ();
				}
			}
		}

		[SerializeField]
		bool _capped = true;

		public bool capped {
			get { return _capped; }
			set {
				if (_capped != value) {
					_preset = TUNNEL_PRESET.Custom;
					_capped = value;
					UpdateMaterialProperties ();
				}
			}
		}

        [SerializeField]
        bool _cappedBothEnds = true;

        public bool cappedBothEnds {
            get { return _cappedBothEnds; }
            set {
                if (_cappedBothEnds != value) {
                    _preset = TUNNEL_PRESET.Custom;
                    _cappedBothEnds = value;
                    UpdateMaterialProperties();
                }
            }
        }
        

        [SerializeField]
		float _fallOff = 0.702f;

		public float fallOff {
			get { return _fallOff; }
			set {
				if (_fallOff != value) {
					_preset = TUNNEL_PRESET.Custom;
					_fallOff = value;
					UpdateMaterialProperties ();
				}
			}
		}

		[SerializeField]
		float _fallOffStart;

		public float fallOffStart {
			get { return _fallOffStart; }
			set {
				if (_fallOffStart != value) {
					_preset = TUNNEL_PRESET.Custom;
					_fallOffStart = value;
					UpdateMaterialProperties ();
				}
			}
		}


		[SerializeField]
		float _aperture;

		public float aperture {
			get { return _aperture; }
			set {
				if (_aperture != value) {
					_preset = TUNNEL_PRESET.Custom;
					_aperture = value;
					UpdateMaterialProperties();
				}
			}
		}



		[SerializeField]
		int _textureScale = 1;

		public int textureScale {
			get { return _textureScale; }
			set {
				if (_textureScale != value) {
					_preset = TUNNEL_PRESET.Custom;
					_textureScale = value;
					UpdateMaterialProperties ();
				}
			}
		}


		[SerializeField]
		bool _blendInOrder;

		public bool blendInOrder {
			get { return _blendInOrder; }
			set {
				if (_blendInOrder != value) {
					_preset = TUNNEL_PRESET.Custom;
					_blendInOrder = value;
					UpdateMaterialProperties ();
				}
			}
		}

		
		[SerializeField]
		float _layersSpeed = 1f;

		public float layersSpeed {
			get { return _layersSpeed; }
			set {
				if (_layersSpeed != value) {
					_preset = TUNNEL_PRESET.Custom;
					_layersSpeed = value;
					UpdateMaterialProperties ();
				}
			}
		}


		[SerializeField]
		int _sides = 16;

		public int sides {
			get { return _sides; }
			set {
				if (_sides != value) {
					_preset = TUNNEL_PRESET.Custom;
					_sides = Mathf.Max (value, 3);
					UpdateMaterialProperties();
				}
			}
		}


        [SerializeField]
        int _segments = 1;

        public int segments {
            get { return _segments; }
            set {
                if (_segments != value) {
                    _segments = Mathf.Max(value, 1);
					UpdateMaterialProperties();
				}
			}
        }

        [SerializeField]
        bool _curvedTunnel;

        public bool curvedTunnel {
            get { return _curvedTunnel; }
            set {
                if (_curvedTunnel != value) {
                    _preset = TUNNEL_PRESET.Custom;
                    _curvedTunnel = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        float _curvedTunnelFrequency = 0.02f;

        public float curvedTunnelFrequency {
            get { return _curvedTunnelFrequency; }
            set {
                if (_curvedTunnelFrequency != value) {
                    _preset = TUNNEL_PRESET.Custom;
                    _curvedTunnelFrequency = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        float _uvScale = 1f;

        public float uvScale {
            get { return _uvScale; }
            set {
                if (_uvScale != value) {
                    _preset = TUNNEL_PRESET.Custom;
					_uvScale = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        float _curvedTunnelAmplitudeX = 0.15f;

        public float curvedTunnelAmplitudeX {
            get { return _curvedTunnelAmplitudeX; }
            set {
                if (_curvedTunnelAmplitudeX != value) {
                    _preset = TUNNEL_PRESET.Custom;
                    _curvedTunnelAmplitudeX = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        float _curvedTunnelAmplitudeY = 0.15f;

        public float curvedTunnelAmplitudeY {
            get { return _curvedTunnelAmplitudeY; }
            set {
                if (_curvedTunnelAmplitudeY != value) {
                    _preset = TUNNEL_PRESET.Custom;
                    _curvedTunnelAmplitudeY = value;
                    UpdateMaterialProperties();
                }
            }
        }



        /// <summary>
        /// Creates a new tunnel using scripting
        /// </summary>
        /// <returns>The tunnel game object.</returns>
        /// <param name="position">Position.</param>
        public static GameObject CreateTunnel (Vector3 position) {
			GameObject go = new GameObject ("Tunnel", typeof(MeshFilter), typeof(MeshRenderer));
			go.transform.position = position;
			go.transform.localRotation = Quaternion.Euler (0, 0, 0);
			go.transform.localScale = new Vector3 (10f, 10f, 950f);
			go.AddComponent<TunnelFX2> ();
			return go;
		}

	}

}