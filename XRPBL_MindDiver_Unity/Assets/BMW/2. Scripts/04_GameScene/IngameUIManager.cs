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

        RadiusProp = Shader.PropertyToID("_Radius");
        ColorProp = Shader.PropertyToID("_VignetteColor");
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
    [SerializeField] public float videoPlaybackSpeed = 1.0f;
    #endregion

    #region Inspector Fields - Settings
    [Header("UI Fade Settings")]
    [SerializeField] private float panelFadeDuration = 0.2f;

    [Header("Vignette Effect Settings")]
    [SerializeField] private List<Image> vignetteImages;
    private List<Material> vignetteMats = new List<Material>();

    private int RadiusProp;
    private int ColorProp;

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

    #region Private Fields - Logic
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

    #region Private Fields - Seamless Video
    // 끊김 없는 재생을 위한 채널 클래스 정의
    private class SeamlessVideoChannel
    {
        public VideoPlayer playerA; // 메인
        public VideoPlayer playerB; // 백버퍼 (대기용)
        public RawImage displayImage;
        public Coroutine transitionRoutine;
        public bool isUsingA = true; // 현재 A를 보고 있는지 여부
    }

    private SeamlessVideoChannel frontChannel;
    private SeamlessVideoChannel leftChannel;
    private SeamlessVideoChannel rightChannel;
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

        // [중요] 비디오 채널 초기화 (더블 버퍼링 준비)
        if (useVideoMode)
        {
            frontChannel = InitializeVideoChannel(frontVideoPlayer, frontRawImage);
            leftChannel = InitializeVideoChannel(leftVideoPlayer, leftRawImage);
            rightChannel = InitializeVideoChannel(rightVideoPlayer, rightRawImage);
        }

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
        if (useVideoMode)
        {
            if (frontRawImage) frontRawImage.gameObject.SetActive(true);
            if (leftRawImage) leftRawImage.gameObject.SetActive(true);
            if (rightRawImage) rightRawImage.gameObject.SetActive(true);

            if (frontVideoPlayer) frontVideoPlayer.gameObject.SetActive(true);
            if (leftVideoPlayer) leftVideoPlayer.gameObject.SetActive(true);
            if (rightVideoPlayer) rightVideoPlayer.gameObject.SetActive(true);

            HideAllCharacterImages();
        }
        else
        {
            if (frontRawImage) frontRawImage.gameObject.SetActive(false);
            if (leftRawImage) leftRawImage.gameObject.SetActive(false);
            if (rightRawImage) rightRawImage.gameObject.SetActive(false);
        }
    }

    // 데미지 효과 발동
    private void TriggerDamageEffect()
    {
        isHitEffectActive = true;
        hitEffectTimer = hitVignetteDuration;

        // 피격 상태(Index 1)로 변경
        SetCharacterState(1);
    }

    public void SetCharacterState(int index)
    {
        // 기존 코루틴(데미지 복귀 등) 중지
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

    #endregion

    #region Seamless Video Logic (Double Buffering)

    /// <summary>
    /// 기존 VideoPlayer를 기반으로 백버퍼용 Player를 하나 더 생성하여 채널을 구성합니다.
    /// </summary>
    private SeamlessVideoChannel InitializeVideoChannel(VideoPlayer originalPlayer, RawImage targetImage)
    {
        if (originalPlayer == null || targetImage == null) return null;

        SeamlessVideoChannel channel = new SeamlessVideoChannel();
        channel.playerA = originalPlayer;
        channel.displayImage = targetImage;

        // Player B (백버퍼) 생성: Player A의 설정을 복사
        GameObject ghostObj = new GameObject(originalPlayer.name + "_Ghost");
        ghostObj.transform.SetParent(originalPlayer.transform.parent);
        ghostObj.transform.localPosition = originalPlayer.transform.localPosition;
        ghostObj.transform.localRotation = originalPlayer.transform.localRotation;
        ghostObj.transform.localScale = originalPlayer.transform.localScale;

        VideoPlayer ghostPlayer = ghostObj.AddComponent<VideoPlayer>();
        ghostPlayer.playOnAwake = false;
        ghostPlayer.waitForFirstFrame = true; // 중요: 첫 프레임 준비될 때까지 대기
        ghostPlayer.isLooping = originalPlayer.isLooping;
        ghostPlayer.source = VideoSource.VideoClip;
        ghostPlayer.audioOutputMode = originalPlayer.audioOutputMode;

        // 원본과 동일한 렌더모드 설정
        // [수정] VideoPlayer.RenderMode가 아니라 VideoRenderMode를 사용해야 합니다.
        channel.playerA.renderMode = VideoRenderMode.APIOnly;
        ghostPlayer.renderMode = VideoRenderMode.APIOnly;

        // RT 연결 해제 (APIOnly에서는 targetTexture가 null이어야 안전)
        channel.playerA.targetTexture = null;
        ghostPlayer.targetTexture = null;

        channel.playerB = ghostPlayer;
        channel.isUsingA = true; // 처음엔 A 사용 가정

        return channel;
    }

    private void UpdateCharacterVideo(int index)
    {
        VideoClip frontClip = (index < CharacterFrontVideo.Count) ? CharacterFrontVideo[index] : null;
        VideoClip leftClip = (index < CharacterLeftVideo.Count) ? CharacterLeftVideo[index] : null;
        VideoClip rightClip = (index < CharacterRightVideo.Count) ? CharacterRightVideo[index] : null;

        bool isLooping = (index != 1); // 데미지는 반복 X

        // 각 채널별로 끊김 없는 전환 시작
        PlaySeamless(frontChannel, frontClip, isLooping);
        PlaySeamless(leftChannel, leftClip, isLooping);
        PlaySeamless(rightChannel, rightClip, isLooping);

        // 데미지(1번) 복귀 로직
        if (index == 1 && frontClip != null)
        {
            float waitTime = (float)frontClip.length / videoPlaybackSpeed;
            characterResetRoutine = StartCoroutine(ResetCharacterStateRoutine(waitTime));
        }
    }

    private void PlaySeamless(SeamlessVideoChannel channel, VideoClip clip, bool loop)
    {
        if (channel == null || clip == null) return;

        // 이미 전환 중이라면 이전 작업 중지
        if (channel.transitionRoutine != null) StopCoroutine(channel.transitionRoutine);

        // 끊김 없는 전환 코루틴 시작
        channel.transitionRoutine = StartCoroutine(SeamlessSwitchRoutine(channel, clip, loop));
    }

    private IEnumerator SeamlessSwitchRoutine(SeamlessVideoChannel channel, VideoClip nextClip, bool loop)
    {
        // 1. 현재 사용하지 않는(백그라운드) 플레이어 선택
        VideoPlayer activePlayer = channel.isUsingA ? channel.playerA : channel.playerB;
        VideoPlayer backPlayer = channel.isUsingA ? channel.playerB : channel.playerA;

        // 2. 백그라운드 플레이어 설정 및 준비
        backPlayer.gameObject.SetActive(true);
        backPlayer.source = VideoSource.VideoClip;
        backPlayer.clip = nextClip;
        backPlayer.isLooping = loop;
        backPlayer.playbackSpeed = videoPlaybackSpeed;

        backPlayer.Prepare();

        // 3. 준비 완료될 때까지 대기 (이 동안 앞쪽 activePlayer는 계속 재생 중이므로 끊김 없음)
        while (!backPlayer.isPrepared)
        {
            yield return null;
        }

        // 4. 준비 완료 -> 텍스처 교체 및 재생
        // RawImage의 텍스처를 백그라운드 플레이어의 텍스처로 순간 교체
        if (channel.displayImage != null)
        {
            channel.displayImage.texture = backPlayer.texture;
            channel.displayImage.color = Color.white;
        }

        backPlayer.Play();

        // 5. 기존 플레이어 정지 및 상태 스왑
        // 약간의 딜레이를 주어 새 영상이 확실히 렌더링 된 후 끄면 더 안전함
        yield return null;

        activePlayer.Stop();
        activePlayer.gameObject.SetActive(false); // 성능 위해 끄기

        // 플래그 반전 (이제 backPlayer가 active가 됨)
        channel.isUsingA = !channel.isUsingA;
        channel.transitionRoutine = null;
    }

    private void PauseAllVideos()
    {
        if (frontChannel != null) PauseChannel(frontChannel);
        if (leftChannel != null) PauseChannel(leftChannel);
        if (rightChannel != null) PauseChannel(rightChannel);
    }
    private void PauseChannel(SeamlessVideoChannel channel)
    {
        if (channel.playerA.isPlaying) channel.playerA.Pause();
        if (channel.playerB.isPlaying) channel.playerB.Pause();
    }

    private void ResumeAllVideos()
    {
        if (frontChannel != null) ResumeChannel(frontChannel);
        if (leftChannel != null) ResumeChannel(leftChannel);
        if (rightChannel != null) ResumeChannel(rightChannel);
    }
    private void ResumeChannel(SeamlessVideoChannel channel)
    {
        // 현재 활성화된(보여지고 있는) 플레이어만 재생
        if (channel.isUsingA) channel.playerA.Play();
        else channel.playerB.Play();
    }

    // 상태 복귀 코루틴
    private IEnumerator ResetCharacterStateRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentHealth > 0 && GameManager.Instance.currentState == GameManager.GameState.GameStage)
        {
            SetCharacterState(0);
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

    public void InitializeSliders()
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