using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IngameUIManager : MonoBehaviour
{
    #region Singleton
    public static IngameUIManager Instance { get; private set; }
    #endregion

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    #region Inspector Fields - HUD & Panels
    [Header("XR CAVE Elements")]
    [SerializeField] private Canvas[] ingameCanvases;

    [Header("Panels (Multi-Screen Support)")]
    [SerializeField] private List<GameObject> mainPanels;
    [SerializeField] private List<GameObject> infoPanels;
    [SerializeField] private List<GameObject> instructionPanels;
    [SerializeField] private List<GameObject> manualPanels;
    [SerializeField] private List<GameObject> pausePanels;
    [SerializeField] private List<GameObject> failPanels;
    [SerializeField] private List<GameObject> blackoutPanels;
    [SerializeField] private List<GameObject> takenDamagePanels;

    [Header("Panels UI Elements")]
    [SerializeField] public List<TextMeshProUGUI> scoreTexts;
    [SerializeField] public TextMeshProUGUI progressText;
    [SerializeField] public Image progressSlider;
    [SerializeField] public List<TextMeshProUGUI> HPTexts;
    [SerializeField] public List<Slider> HPSliders;
    [SerializeField] public List<TextMeshProUGUI> ShieldTexts;
    [SerializeField] public List<Slider> ShieldSliders;
    [SerializeField] public TextMeshProUGUI BulletText;
    [SerializeField] public Slider BulletSlider;
    [SerializeField] public TextMeshProUGUI BuffText;
    [SerializeField] public Slider BuffSlider;
    [SerializeField] public TextMeshProUGUI DeBuffText;
    [SerializeField] public Slider DeBuffSlider;
    [SerializeField] private List<GameObject> arrowPanels;

    [Header("Character Display Settings")]
    [Tooltip("체크하면 영상을 재생하고, 해제하면 이미지를 사용합니다.")]
    [SerializeField] public bool useVideoMode = false;

    [Header("Character Images (0:Default, 1:Damage, 2:Fail, 3:Success)")]
    [SerializeField] public List<Image> CharacterFrontImage;
    [SerializeField] public List<Image> CharacterLeftImage;
    [SerializeField] public List<Image> CharacterRightImage;

    [Header("Character Videos (0:Default, 1:Damage, 2:Fail, 3:Success)")]
    [SerializeField] public List<VideoClip> CharacterFrontVideo;
    [SerializeField] public List<VideoClip> CharacterLeftVideo;
    [SerializeField] public List<VideoClip> CharacterRightVideo;

    [Header("Video Players & Screens (Required for Video Mode)")]
    [Tooltip("전면 스크린용 비디오 플레이어")]
    [SerializeField] public VideoPlayer frontVideoPlayer;
    [Tooltip("좌측 스크린용 비디오 플레이어")]
    [SerializeField] public VideoPlayer leftVideoPlayer;
    [Tooltip("우측 스크린용 비디오 플레이어")]
    [SerializeField] public VideoPlayer rightVideoPlayer;

    [Tooltip("비디오가 출력될 RawImage (이미지 모드일 때 숨겨짐)")]
    [SerializeField] public RawImage frontRawImage;
    [SerializeField] public RawImage leftRawImage;
    [SerializeField] public RawImage rightRawImage;

    [Tooltip("비디오 재생 속도 (1.0 = 정배속, 2.0 = 2배속)")]
    [Range(0.1f, 5.0f)]
    [SerializeField] public float videoPlaybackSpeed = 1.0f; // [New] 영상 속도 조절 변수
    #endregion

    #region Inspector Fields - Settings
    [Header("UI Fade Settings")]
    [SerializeField] private float panelFadeDuration = 0.2f;

    [Header("Vignette Effect Settings")]
    [SerializeField] private List<Image> vignetteImages;
    private List<Material> vignetteMats = new List<Material>();

    private readonly int RadiusProp = Shader.PropertyToID("_Radius");
    private readonly int ColorProp = Shader.PropertyToID("_VignetteColor");

    [Header("Vignette Colors")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color shieldDamageColor = Color.blue;
    [SerializeField] private Color bufferColor = Color.yellow;
    [SerializeField] private Color debufferColor = new Color(0.6f, 0f, 0.8f, 1f);

    [Header("Vignette Configuration")]
    [SerializeField] private float maxRadius = 0.6f;
    [SerializeField] private float minRadius = -0.3f;

    [Header("Skill Effect Settings")]
    [SerializeField] private float bufferEffectDuration = 3.0f;
    [SerializeField] private float debufferEffectDuration = 5.0f;
    [SerializeField] private float skillFadeDuration = 0.5f;
    [SerializeField] private float skillTargetRadius = 0.35f;

    [Header("Damage Feedback Settings")]
    [Tooltip("이미지 모드일 때 피격 이미지 유지 시간")]
    [SerializeField] private float damageImageDuration = 2.0f;
    [SerializeField] private float hitVignetteDuration = 2.0f;

    [Header("Debug Settings")]
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Private Fields
    private OuttroUIManager outtroUIManager;
    private int currentHealth;
    private int maxHealth;
    private int currentShield;
    private int maxShield;

    private bool isBufferEffectActive = false;
    private float bufferTimer = 0f;
    private float currentBufferMaxDuration = 0f;

    private bool isDeBufferEffectActive = false;
    private float debufferTimer = 0f;
    private float currentDebufferMaxDuration = 0f;

    private bool isHitEffectActive = false;
    private float hitEffectTimer = 0f;

    // 코루틴 참조 변수
    private Coroutine characterResetRoutine;

    private bool isDisplayPanel = false;
    private Dictionary<GameObject, Coroutine> panelCoroutines = new Dictionary<GameObject, Coroutine>();
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        outtroUIManager = GetComponent<OuttroUIManager>();

        // 비네팅 초기화
        foreach (var img in vignetteImages)
        {
            if (img != null)
            {
                Material mat = img.material;
                mat.SetFloat(RadiusProp, maxRadius);
                vignetteMats.Add(mat);
            }
        }

        // 초기 모드 설정 (이미지 vs 비디오 화면 정리)
        InitializeCharacterDisplayMode();

        // [중요] 캐릭터 초기화 (기본 상태: 0번) - 시작하자마자 기본 영상 재생
        SetCharacterState(0);

        if (DataManager.Instance != null)
        {
            currentHealth = DataManager.Instance.GetShipHealth();
            maxHealth = DataManager.Instance.maxShipHealth;
            currentShield = DataManager.Instance.GetShipShield();
            maxShield = DataManager.Instance.maxShipShield;

            GameManager.Instance.OnPauseStateChanged += HandlePauseState;
            GameManager.Instance.OnFailStateChanged += HandleFailState;
            DataManager.Instance.OnHealthChanged += HandleHealthChange;
            DataManager.Instance.OnShieldChanged += HandleShieldChange;
            DataManager.Instance.OnDeBufferAdded += HandleDeBufferAdded;
            DataManager.Instance.OnBufferAdded += HandleBufferAdded;
        }

        InitializeSliders();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPauseStateChanged -= HandlePauseState;
            GameManager.Instance.OnFailStateChanged -= HandleFailState;
        }

        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnHealthChanged -= HandleHealthChange;
            DataManager.Instance.OnShieldChanged -= HandleShieldChange;
            DataManager.Instance.OnDeBufferAdded -= HandleDeBufferAdded;
            DataManager.Instance.OnBufferAdded -= HandleBufferAdded;
        }
    }

    private void Update()
    {
        UpdateVignetteState();
    }
    #endregion

    #region Event Handlers
    private void HandleShieldChange(int current, int max)
    {
        if (current < currentShield) TriggerDamageEffect();
        currentShield = current;
        maxShield = max;
        UpdateShield(current);
    }

    private void HandleHealthChange(int current, int max)
    {
        if (current < currentHealth) TriggerDamageEffect();
        currentHealth = current;
        maxHealth = max;
        UpdateHP(current);
    }

    private void HandleBufferAdded()
    {
        currentBufferMaxDuration = bufferEffectDuration;
        bufferTimer = currentBufferMaxDuration;
        isBufferEffectActive = true;
        isDeBufferEffectActive = false;
        UpdateBuff(DataManager.Instance.GetBuffer());
    }

    private void HandleDeBufferAdded()
    {
        currentDebufferMaxDuration = debufferEffectDuration;
        debufferTimer = currentDebufferMaxDuration;
        isDeBufferEffectActive = true;
        isBufferEffectActive = false;
        UpdateDeBuff(DataManager.Instance.GetDeBuffer());
    }

    private void HandlePauseState(bool isPaused)
    {
        if (isPaused) OpenPausePanel();
        else ClosePausePanel();

        // 비디오 일시정지 처리
        if (useVideoMode)
        {
            if (isPaused) PauseAllVideos();
            else ResumeAllVideos();
        }
    }

    private void HandleFailState(bool isFailed)
    {
        if (isFailed)
        {
            OpenFailPanel();
            SetCharacterState(2); // 실패 상태 (영상/이미지)
        }
        else CloseFailPanel();
    }
    #endregion

    #region Character Logic (Image & Video)

    // 모드에 따라 UI 오브젝트 활성/비활성 초기화
    private void InitializeCharacterDisplayMode()
    {
        // 비디오 모드라면 RawImage를 켜고, Image 리스트의 모든 오브젝트를 끔
        if (useVideoMode)
        {
            if (frontRawImage) frontRawImage.gameObject.SetActive(true);
            if (leftRawImage) leftRawImage.gameObject.SetActive(true);
            if (rightRawImage) rightRawImage.gameObject.SetActive(true);

            HideAllCharacterImages();
        }
        else // 이미지 모드라면 RawImage를 끄고 로직 시작
        {
            if (frontRawImage) frontRawImage.gameObject.SetActive(false);
            if (leftRawImage) leftRawImage.gameObject.SetActive(false);
            if (rightRawImage) rightRawImage.gameObject.SetActive(false);
        }
    }

    // 데미지 효과 발동 (공통 진입점)
    private void TriggerDamageEffect()
    {
        isHitEffectActive = true;
        hitEffectTimer = hitVignetteDuration;

        // 피격 상태(Index 1)로 변경
        SetCharacterState(1);
    }

    // 캐릭터 상태 변경 통합 함수
    public void SetCharacterState(int index)
    {
        // 기존 코루틴이 있다면 중지 (피격 대기 중 상태 변경 시)
        if (characterResetRoutine != null) StopCoroutine(characterResetRoutine);

        if (useVideoMode)
        {
            UpdateCharacterVideo(index);
        }
        else
        {
            UpdateCharacterImage(index);
        }
    }

    // [이미지 모드] 처리 로직
    private void UpdateCharacterImage(int index)
    {
        UpdateImageList(CharacterFrontImage, index);
        UpdateImageList(CharacterLeftImage, index);
        UpdateImageList(CharacterRightImage, index);

        // 데미지(1번)인 경우 일정 시간 후 복귀
        if (index == 1)
        {
            characterResetRoutine = StartCoroutine(ResetCharacterStateRoutine(damageImageDuration));
        }
    }

    private void UpdateImageList(List<Image> images, int targetIndex)
    {
        if (images == null) return;
        for (int i = 0; i < images.Count; i++)
        {
            if (images[i] != null) images[i].gameObject.SetActive(i == targetIndex);
        }
    }

    private void HideAllCharacterImages()
    {
        foreach (var img in CharacterFrontImage) if (img) img.gameObject.SetActive(false);
        foreach (var img in CharacterLeftImage) if (img) img.gameObject.SetActive(false);
        foreach (var img in CharacterRightImage) if (img) img.gameObject.SetActive(false);
    }

    // [비디오 모드] 처리 로직
    private void UpdateCharacterVideo(int index)
    {
        // 1. 영상 클립 가져오기 (범위 체크)
        VideoClip frontClip = (index < CharacterFrontVideo.Count) ? CharacterFrontVideo[index] : null;
        VideoClip leftClip = (index < CharacterLeftVideo.Count) ? CharacterLeftVideo[index] : null;
        VideoClip rightClip = (index < CharacterRightVideo.Count) ? CharacterRightVideo[index] : null;

        // 2. 반복 재생 여부 결정 (1번=데미지는 반복 X, 나머지는 O)
        bool isLooping = (index != 1);

        // 3. 영상 재생
        PlayVideo(frontVideoPlayer, frontClip, isLooping);
        PlayVideo(leftVideoPlayer, leftClip, isLooping);
        PlayVideo(rightVideoPlayer, rightClip, isLooping);

        // 4. 데미지(1번)인 경우 영상 길이만큼 대기 후 0번(기본)으로 복귀
        if (index == 1 && frontClip != null)
        {
            // 전면 영상 길이를 기준으로 대기 (셋 다 비슷하다고 가정, 속도 고려하여 시간 계산)
            float waitTime = (float)frontClip.length / videoPlaybackSpeed;
            characterResetRoutine = StartCoroutine(ResetCharacterStateRoutine(waitTime));
        }
    }

    private void PlayVideo(VideoPlayer player, VideoClip clip, bool loop)
    {
        if (player == null || clip == null) return;

        player.source = VideoSource.VideoClip;
        player.clip = clip;
        player.isLooping = loop;

        // [New] 재생 속도 설정
        player.playbackSpeed = videoPlaybackSpeed;

        player.Play();
    }

    // 비디오 일시정지 유틸
    private void PauseAllVideos()
    {
        if (frontVideoPlayer) frontVideoPlayer.Pause();
        if (leftVideoPlayer) leftVideoPlayer.Pause();
        if (rightVideoPlayer) rightVideoPlayer.Pause();
    }
    private void ResumeAllVideos()
    {
        if (frontVideoPlayer) frontVideoPlayer.Play();
        if (leftVideoPlayer) leftVideoPlayer.Play();
        if (rightVideoPlayer) rightVideoPlayer.Play();
    }

    // 상태 복귀 코루틴 (이미지/영상 공용)
    private IEnumerator ResetCharacterStateRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 체력이 남아있고, 아직 데미지 효과 중이라면 기본 상태로 복귀
        // (죽었거나 게임이 끝났으면 복귀 안 함)
        if (currentHealth > 0 && GameManager.Instance.currentState == GameManager.GameState.GameStage)
        {
            SetCharacterState(0); // 0: Default
        }
    }

    #endregion

    #region Vignette Logic
    private void UpdateVignetteState()
    {
        if (vignetteMats.Count == 0) return;

        float targetRadius = maxRadius;
        Color targetColor = damageColor;

        if (isHitEffectActive)
        {
            targetColor = (currentShield > 0) ? shieldDamageColor : damageColor;
            hitEffectTimer -= Time.deltaTime;

            if (hitEffectTimer <= 0) isHitEffectActive = false;
            else
            {
                float hitProgress = 1.0f - (hitEffectTimer / hitVignetteDuration);
                targetRadius = Mathf.Lerp(minRadius, maxRadius, hitProgress);
            }
        }
        else if (isBufferEffectActive)
        {
            bufferTimer -= Time.deltaTime;
            if (bufferTimer <= 0) isBufferEffectActive = false;
            else
            {
                targetColor = bufferColor;
                float intensity = CalculateFadeIntensity(bufferTimer, currentBufferMaxDuration);
                targetRadius = Mathf.Lerp(maxRadius, skillTargetRadius, intensity);
                targetColor.a = intensity;
            }
        }
        else if (isDeBufferEffectActive)
        {
            debufferTimer -= Time.deltaTime;
            if (debufferTimer <= 0) isDeBufferEffectActive = false;
            else
            {
                targetColor = debufferColor;
                float intensity = CalculateFadeIntensity(debufferTimer, currentDebufferMaxDuration);
                targetRadius = Mathf.Lerp(maxRadius, skillTargetRadius, intensity);
                targetColor.a = intensity;
            }
        }
        else
        {
            float healthRatio = (maxHealth > 0) ? (float)currentHealth / maxHealth : 0f;
            if (healthRatio <= 0.5f)
            {
                targetColor = damageColor;
                float t = 1.0f - (healthRatio / 0.5f);
                float healthBasedRadius = Mathf.Lerp(maxRadius, minRadius, t);
                float pulseSpeed = Mathf.Lerp(2.0f, 8.0f, t);
                float pulseAmp = Mathf.Lerp(0.0f, 0.05f, t);
                targetRadius = healthBasedRadius + Mathf.Sin(Time.time * pulseSpeed) * pulseAmp;
            }
            else
            {
                targetRadius = maxRadius;
            }
        }

        foreach (var mat in vignetteMats)
        {
            if (mat != null)
            {
                mat.SetColor(ColorProp, targetColor);
                mat.SetFloat(RadiusProp, targetRadius);
            }
        }
    }

    private float CalculateFadeIntensity(float currentTimer, float maxDuration)
    {
        float timeElapsed = maxDuration - currentTimer;
        float intensity = 1.0f;

        if (timeElapsed < skillFadeDuration) intensity = Mathf.SmoothStep(0f, 1f, timeElapsed / skillFadeDuration);
        else if (currentTimer < skillFadeDuration) intensity = Mathf.SmoothStep(0f, 1f, currentTimer / skillFadeDuration);

        return intensity;
    }
    #endregion

    #region UI Control Methods
    public void InitializePanels()
    {
        SetPanelsActive(mainPanels, false);
        SetPanelsActive(infoPanels, false);
        SetPanelsActive(instructionPanels, false);
        SetPanelsActive(manualPanels, false);
        SetPanelsActive(pausePanels, false);
        SetPanelsActive(failPanels, false);
        SetPanelsActive(takenDamagePanels, true);

        if (DataManager.Instance != null)
        {
            UpdateProgress(DataManager.Instance.GetProgress());
            UpdateScore(DataManager.Instance.GetScore());
            UpdateBullet(DataManager.Instance.GetBullet());
            UpdateHP(DataManager.Instance.GetShipHealth());
            UpdateShield(DataManager.Instance.GetShipShield());
            UpdateBuff(DataManager.Instance.GetBuffer());
            UpdateDeBuff(DataManager.Instance.GetDeBuffer());
        }
    }

    private void InitializeSliders()
    {
        foreach (var sliders in HPSliders) if (sliders) { sliders.minValue = 0; sliders.maxValue = 100; sliders.wholeNumbers = true; }
        foreach (var sliders in ShieldSliders) if (sliders) { sliders.minValue = 0; sliders.maxValue = 100; sliders.wholeNumbers = true; }
        BulletSlider.minValue = 0; BulletSlider.maxValue = 100; BulletSlider.wholeNumbers = true;
        BuffSlider.minValue = 0; BuffSlider.maxValue = 100; BuffSlider.wholeNumbers = true;
        DeBuffSlider.minValue = 0; DeBuffSlider.maxValue = 100; DeBuffSlider.wholeNumbers = true;
    }

    private void SetPanelsActive(List<GameObject> panels, bool isActive)
    {
        foreach (var panel in panels) if (panel != null) panel.SetActive(isActive);
    }

    public void OnClickPauseButton() { if (GameManager.Instance != null && !GetDisplayPanel()) { OpenPausePanel(); GameManager.Instance.TogglePause(); } }
    public void OnClickFailButton() { if (GameManager.Instance != null) { OpenFailPanel(); GameManager.Instance.ToggleFail(); } }
    public void OnClickContinueButton() { if (GameManager.Instance != null) GameManager.Instance.TogglePause(); ClosePausePanel(); }
    public void OnClickBackButton() { Time.timeScale = 1f; if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.MainMenu); }
    public void OnClickRetryButton() { Time.timeScale = 1f; if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.GameStage); }

    public void OpenInstructionPanel() { FadePanels(instructionPanels, true); SetDisplayPanel(true); }
    public void CloseInstructionPanel() { FadePanels(instructionPanels, false); SetDisplayPanel(false); }
    public void OpenManualPanel() { FadePanels(manualPanels, true); SetDisplayPanel(true); }
    public void CloseManualPanel() { FadePanels(manualPanels, false); SetDisplayPanel(false); }
    public void OpenPausePanel() { foreach (var panel in pausePanels) panel.SetActive(true); SetDisplayPanel(true); }
    public void ClosePausePanel() { foreach (var panel in pausePanels) panel.SetActive(false); SetDisplayPanel(false); }
    public void OpenFailPanel() { foreach (var panel in failPanels) panel.SetActive(true); SetDisplayPanel(true); }
    public void CloseFailPanel() { foreach (var panel in failPanels) panel.SetActive(false); SetDisplayPanel(false); }
    public void OpenMainPanel() { FadePanels(mainPanels, true); }
    public void CloseMainPanel() { FadePanels(mainPanels, false); }
    public void OpenInfoPanel() { FadePanels(infoPanels, true); }
    public void CloseInfoPanel() { FadePanels(infoPanels, false); }
    public void OpenTakenDamagePanel() { FadePanels(takenDamagePanels, true); }
    public void CloseTakenDamagePanel() { FadePanels(takenDamagePanels, false); }
    public void OpenArrowPanel(int value) { FadePanels(arrowPanels, true, true, value); }
    public void CloseArrowPanel() { FadePanels(arrowPanels, false); }

    public void UpdateScore(int value) { foreach (var text in scoreTexts) if (text) text.text = value.ToString(); }
    public void UpdateHP(int value) { foreach (var text in HPTexts) if (text) text.text = value.ToString(); foreach (var slider in HPSliders) if (slider) slider.value = value; }
    public void UpdateShield(int value) { foreach (var text in ShieldTexts) if (text) text.text = value.ToString(); foreach (var slider in ShieldSliders) if (slider) slider.value = value; }
    public void UpdateBullet(int value) { BulletText.text = value.ToString(); BulletSlider.value = value; }
    public void UpdateBuff(int value) { BuffText.text = value.ToString(); BuffSlider.value = value; }
    public void UpdateDeBuff(int value) { DeBuffText.text = value.ToString(); DeBuffSlider.value = value; }
    public void UpdateProgress(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0f, 100f);
        if (progressSlider) progressSlider.fillAmount = clampedValue / 100f;
        if (progressText) progressText.text = $"{((int)clampedValue)} %";
    }
    public void SetDisplayPanel(bool state) { isDisplayPanel = state; }
    public bool GetDisplayPanel() { return isDisplayPanel; }

    private void FadePanels(List<GameObject> panels, bool show, bool onlyOne = false, int onlyOneChoice = 0)
    {
        int count = 0;
        foreach (var panel in panels)
        {
            count++;
            if (panel == null) continue;
            if (onlyOne) { if (onlyOneChoice != count) continue; }

            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();

            if (panelCoroutines.ContainsKey(panel) && panelCoroutines[panel] != null) StopCoroutine(panelCoroutines[panel]);
            panelCoroutines[panel] = StartCoroutine(FadePanelRoutine(panel, cg, show));
        }
    }

    private IEnumerator FadePanelRoutine(GameObject panel, CanvasGroup cg, bool show)
    {
        float targetAlpha = show ? 1.0f : 0.0f;
        float startAlpha = cg.alpha;
        float elapsed = 0f;

        if (show) { panel.SetActive(true); cg.alpha = 0f; startAlpha = 0f; }

        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / panelFadeDuration);
            yield return null;
        }
        cg.alpha = targetAlpha;
        if (!show) panel.SetActive(false);
    }

    public void Log(string message) { if (isDebugMode) Debug.Log(message); }
    public void ShowOuttroUI()
    {
        if (outtroUIManager != null)
        {
            outtroUIManager.ShowResult();
            foreach (var Panel in blackoutPanels)
            {
                if (Panel) Panel.SetActive(true);
            }
        }
        else
        {
            Debug.LogError("OuttroUIManager가 씬에 없습니다!");
        }
    }
    #endregion
}