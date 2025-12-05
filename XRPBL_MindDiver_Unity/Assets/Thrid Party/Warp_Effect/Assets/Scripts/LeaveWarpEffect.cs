using UnityEngine;

namespace com.ggames4u {
    /// <summary>
    /// Leave Warp Effect script.
    /// If you have any questions feel free to cantact me.
    /// (c) 2018 Dirk Jacobasch
    /// dirk.jacobasch@outlook.com
    /// </summary>
    public class LeaveWarpEffect : MonoBehaviour {

        [Tooltip("The material the effect is applied to")]
        [SerializeField] private Material effectMaterial;

        private float magnitude = 0f;
        private bool effectIsRunning;

        [Tooltip("The strength of the effect")]
        [Range(0.001f, 0.1f)]
        [SerializeField] private float leaveWarpEffectMagnitude = 0.028f;

        [Header("Speed values for the effect")]

        [Tooltip("Distortion X-Speed")]
        [Range(-5f, 5f)]
        [SerializeField] private float xSpeed;

        [Tooltip("Distortion Y-Speed")]
        [Range(-5f, 5f)]
        [SerializeField] private float ySpeed;

        [Tooltip("This factor defined how long the effect will take. A small value for longer running effect.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float effectTimeFactor;

        /// <summary>
        /// Render texture on screen if the effct is running.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        private void OnRenderImage(RenderTexture source, RenderTexture destination) {
            if (this.effectIsRunning == true) {
                Graphics.Blit(source, destination, this.effectMaterial);
            } else {
                Graphics.Blit(source, destination);
            }
        }

        /// <summary>
        /// Set shader values.
        /// </summary>
        private void Start() {
            this.effectMaterial.SetFloat("_SpeedX", this.xSpeed);
            this.effectMaterial.SetFloat("_SpeedY", this.ySpeed);
        }

        /// <summary>
        /// Decrease the magnitude if the effect is running.
        /// </summary>
        private void Update() {
            if (this.effectIsRunning == true) {
                if (this.magnitude > 0f) {
                    this.magnitude -= Time.deltaTime * this.effectTimeFactor;
                    if (this.magnitude < 0f) {
                        this.magnitude = 0f;
                    }

                    this.effectMaterial.SetFloat("_Magnitude", this.magnitude);

                } else {
                    this.effectMaterial.SetFloat("_Magnitude", 0f);
                    this.effectIsRunning = false;
                }
            }
        }

        /// <summary>
        /// Start the effect. 
        /// This method is called from another script to start the screen effect.
        /// </summary>
        public void StartEffect() {
            this.magnitude = this.leaveWarpEffectMagnitude;
            this.effectIsRunning = true;
        }

        /// <summary>
        /// Stop the effect.
        /// </summary>
        public void StopEffect() {
            this.effectMaterial.SetFloat("_Magnitude", 0f);
            this.effectIsRunning = false;
        }
    }
}
