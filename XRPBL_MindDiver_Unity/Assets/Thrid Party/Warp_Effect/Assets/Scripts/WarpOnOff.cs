using UnityEngine;
using System.Collections;

namespace com.ggames4u {
    /// <summary>
    /// Leave On Off script.
    /// If you have any questions feel free to cantact me at dirk.jacobasch@outlook.com
    /// </summary>
    public class WarpOnOff : MonoBehaviour {
        private Material warpMaterial;

        [Tooltip("The container from the prefab which contains the warp sphere and the particle effects")]
        [SerializeField] private GameObject warpSphereContainer;

        [Tooltip("The camera inside the warp effect. The camera can be rotated by the left/right arrows")]
        [SerializeField] private Camera warpCamera;

        [SerializeField] private AudioClip warpLoopAudio;
        private AudioSource warpLoopAudiosSource;

        [SerializeField] private AudioClip leaveWarpAudio;
        private AudioSource leaveWarpAudiosSource;

        [Tooltip("Drag the camera with the - Leave Warp Effect Script - inside here.")]
        [SerializeField] private LeaveWarpEffect leaveWarpEffectScript;

        private float cameraRotationSpeed = 40f;

        [SerializeField] private GameObject warpSphere;

        [Tooltip("Delay to start render star layer 1. Works only with the WarpPrefabShaderStars.")]
        [Range(0, 20)]
        [SerializeField] private float starLayerDelay01 = 1.5f;

        [Tooltip("Delay to start render star layer 2. Works only with the WarpPrefabShaderStars.")]
        [Range(0, 20)]
        [SerializeField] private float starLayerDelay02 = 2.5f;

        // Get warp material
        private void Awake() {
            if (this.warpSphere != null) {
                this.warpMaterial = this.warpSphere.GetComponent<Renderer>().sharedMaterial;
            }
        }

        // Use this for initialization
        void Start() {
            // Hide warp effect
            this.warpSphereContainer.SetActive(false);

            // Setup audio sources
            this.warpLoopAudiosSource = this.gameObject.AddComponent<AudioSource>();
            this.warpLoopAudiosSource.clip = this.warpLoopAudio;
            this.warpLoopAudiosSource.playOnAwake = false;
            this.warpLoopAudiosSource.loop = true;

            this.leaveWarpAudiosSource = this.gameObject.AddComponent<AudioSource>();
            this.leaveWarpAudiosSource.clip = this.leaveWarpAudio;
            this.leaveWarpAudiosSource.playOnAwake = false;
            this.leaveWarpAudiosSource.loop = false;

            // Deactivate star layer rendering
            DisableStarLayers();
        }

        /// <summary>
        /// Disable star layer rendering.
        /// </summary>
        private void DisableStarLayers() {
            if (this.warpMaterial != null && this.warpMaterial.HasProperty("_RenderStarLayer1")) {
                this.warpMaterial.SetFloat("_RenderStarLayer1", 0f);
            }

            if (this.warpMaterial != null && this.warpMaterial.HasProperty("_RenderStarLayer2")) {
                this.warpMaterial.SetFloat("_RenderStarLayer2", 0f);
            }
        }

        /// <summary>
        /// Enable star layer rendering.
        /// </summary>
        private void EnableStarLayers() {
            if (this.warpMaterial != null && this.warpMaterial.HasProperty("_RenderStarLayer1")) {
                this.warpMaterial.SetFloat("_RenderStarLayer1", 1f);
            }

            if (this.warpMaterial != null && this.warpMaterial.HasProperty("_RenderStarLayer2")) {
                this.warpMaterial.SetFloat("_RenderStarLayer2", 1f);
            }
        }

        /// <summary>
        /// Start or stop the warp effect.
        /// </summary>
        void Update() {
            if (Input.GetKey(KeyCode.UpArrow)) {
                if (this.warpSphereContainer.activeSelf == false) {
                    this.warpSphereContainer.SetActive(true);
                    this.warpLoopAudiosSource.Play();
                    this.leaveWarpEffectScript.StopEffect();

                    if (this.warpMaterial != null && this.warpMaterial.HasProperty("_RenderStarLayer1")) {
                        StartCoroutine(StartRenderStarLayerWithDelay(this.starLayerDelay01, "_RenderStarLayer1"));
                    }

                    if (this.warpMaterial != null && this.warpMaterial.HasProperty("_RenderStarLayer2")) {
                        StartCoroutine(StartRenderStarLayerWithDelay(this.starLayerDelay02, "_RenderStarLayer2"));
                    }
                }

            } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
                if (this.warpSphereContainer.activeSelf == true) {
                    this.warpSphereContainer.SetActive(false);
                    this.warpLoopAudiosSource.Stop();
                    this.leaveWarpAudiosSource.Play();

                    // Start distortion image effect
                    if (this.leaveWarpEffectScript != null) {
                        this.leaveWarpEffectScript.StartEffect();
                    }

                    // Stop star layer rendering with delay
                    DisableStarLayers();
                }
            }

            // Camera rotation
            if (Input.GetKey(KeyCode.LeftArrow)) {
                this.warpCamera.transform.Rotate(Vector3.down * Time.deltaTime * this.cameraRotationSpeed);

            } else if (Input.GetKey(KeyCode.RightArrow)) {
                this.warpCamera.transform.Rotate(Vector3.up * Time.deltaTime * this.cameraRotationSpeed);
            }
        }

        /// <summary>
        /// Start the star layer rendering with the given delay.
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="shaderProperty"></param>
        /// <returns></returns>
        private IEnumerator StartRenderStarLayerWithDelay(float delay, string shaderProperty) {
            yield return new WaitForSeconds(delay);

            if (this.warpMaterial != null && this.warpMaterial.HasProperty(shaderProperty)) {
                this.warpMaterial.SetFloat(shaderProperty, 1f);
            }
        }

        /// <summary>
        /// Reset to default.
        /// </summary>
        private void OnDestroy() {
            EnableStarLayers();
        }
    }
}
