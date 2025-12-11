using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Image/Video Players Settings")]
    [SerializeField] public VideoPlayer[] videoPlayers;
    [SerializeField] public List<GameObject> VideoPanels;

    [Header("Character Images (0:Default, 1:Damage, 2:Fail, 3:Success)")]
    [SerializeField] public List<Image> CharacterFrontImage;
    [SerializeField] public List<Image> CharacterLeftImage;
    [SerializeField] public List<Image> CharacterRightImage;
    [SerializeField] public List<VideoClip> CharacterFrontVideo;
    [SerializeField] public List<VideoClip> CharacterLeftVideo;
    [SerializeField] public List<VideoClip> CharacterRightVideo;
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
    [Tooltip("체력 데미지 시 색상 (빨강)")]
    [SerializeField] private Color damageColor = Color.red;
    [Tooltip("쉴드 데미지 시 색상 (파란)")]
    [SerializeField] private Color shieldDamageColor = Color.blue;
    [Tooltip("버프 사용시 시 색상 (노랑)")]
    [SerializeField] private Color bufferColor = Color.yellow;
    [Tooltip("디버프 사용시 색상 (보라)")]
    [SerializeField] private Color debufferColor = new Color(0.6f, 0f, 0.8f, 1f);

    [Header("Vignette Configuration")]
    [Tooltip("비네팅이 없는 상태의 Radius (기본 0.6)")]
    [SerializeField] private float maxRadius = 0.6f;

    [Tooltip("비네팅이 최대인 상태의 Radius (기본 -0.3)")]
    [SerializeField] private float minRadius = -0.3f;

    [Header("Skill Effect Settings")]
    [Tooltip("버프 효과 지속 시간 (초)")]
    [SerializeField] private float bufferEffectDuration = 3.0f;
    [Tooltip("디버프 효과 지속 시간 (초)")]
    [SerializeField] private float debufferEffectDuration = 5.0f;
    [Tooltip("스킬 효과 페이드 인/아웃 시간")]
    [SerializeField] private float skillFadeDuration = 0.5f;
    [Tooltip("스킬 발동 시 비네팅 반경 (은은한 정도, 0.6~-0.3 사이)")]
    [SerializeField] private float skillTargetRadius = 0.35f;

    [Header("Damage Feedback Settings")]
    [Tooltip("피격 시 데미지 이미지가 유지되는 시간")]
    [SerializeField] private float damageImageDuration = 2.0f;
    [Tooltip("피격 시 비네팅이 확 줄어드는 효과의 지속 시간")]
    [SerializeField] private float hitVignetteDuration = 2.0f;

    [Header("Debug Settings")]
    // 디버그 로그 출력 여부
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Private Fields
    private OuttroUIManager outtroUIManager;
    private int currentHealth;
    private int maxHealth;
    private int currentShield;
    private int maxShield;

    // 버프/디버프 관련
    private bool isBufferEffectActive = false;
    private float bufferTimer = 0f;
    private float currentBufferMaxDuration = 0f; // 현재 실행중인 버프의 총 시간 저장

    private bool isDeBufferEffectActive = false;
    private float debufferTimer = 0f;
    private float currentDebufferMaxDuration = 0f; // 현재 실행중인 디버프의 총 시간 저장

    // 피격 효과 관련
    private bool isHitEffectActive = false;
    private float hitEffectTimer = 0f;
    private Coroutine characterImageResetRoutine;

    private bool isDisplayPanel = false;
    private Dictionary<GameObject, Coroutine> panelCoroutines = new Dictionary<GameObject, Coroutine>();
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        outtroUIManager = GetComponent<OuttroUIManager>();

        // 1. 비네팅 머티리얼 초기화
        foreach (var img in vignetteImages)
        {
            if (img != null)
            {
                Material mat = img.material;
                mat.SetFloat(RadiusProp, maxRadius); // 초기값 0.6
                vignetteMats.Add(mat);
            }
        }

        // 2. 캐릭터 이미지 초기화 (기본 상태: 0번)
        SetCharacterImageIndex(0);

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
            GameManager.Instance.OnPauseStateChanged -= HandlePauseState;
            GameManager.Instance.OnFailStateChanged -= HandleFailState;

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
        if (current < currentShield)
        {
            TriggerDamageEffect();
        }
        currentShield = current;
        maxShield = max;
        UpdateShield(current);
    }

    private void HandleHealthChange(int current, int max)
    {
        if (current < currentHealth)
        {
            TriggerDamageEffect();
        }
        currentHealth = current;
        maxHealth = max;
        UpdateHP(current);
    }

    private void HandleBufferAdded()
    {
        // 버프: 3초 (Inspector 설정값 사용)
        currentBufferMaxDuration = bufferEffectDuration;
        bufferTimer = currentBufferMaxDuration;
        isBufferEffectActive = true;

        // 디버프 효과가 있다면 덮어쓰거나 우선순위 결정 (여기선 최근 발동 우선)
        isDeBufferEffectActive = false;

        UpdateBuff(DataManager.Instance.GetBuffer());
    }

    private void HandleDeBufferAdded()
    {
        // 디버프: 스킬 지속 시간 (Inspector 설정값 혹은 변수 사용)
        currentDebufferMaxDuration = debufferEffectDuration;
        debufferTimer = currentDebufferMaxDuration;
        isDeBufferEffectActive = true;

        // 버프 효과가 있다면 끔
        isBufferEffectActive = false;

        UpdateDeBuff(DataManager.Instance.GetDeBuffer());
    }

    private void HandlePauseState(bool isPaused)
    {
        if (isPaused) OpenPausePanel();
        else ClosePausePanel();
    }

    private void HandleFailState(bool isFailed)
    {
        if (isFailed) OpenFailPanel();
        else CloseFailPanel();
    }
    #endregion

    #region Damage & Character Logic
    private void TriggerDamageEffect()
    {
        isHitEffectActive = true;
        hitEffectTimer = hitVignetteDuration;

        SetCharacterImageIndex(1);

        if (characterImageResetRoutine != null) StopCoroutine(characterImageResetRoutine);
        characterImageResetRoutine = StartCoroutine(ResetCharacterImageRoutine());
    }

    private IEnumerator ResetCharacterImageRoutine()
    {
        yield return new WaitForSeconds(damageImageDuration);
        if (currentHealth > 0)
        {
            SetCharacterImageIndex(0);
        }
    }

    public void SetCharacterImageIndex(int index)
    {
        UpdateCharacterList(CharacterFrontImage, index);
        UpdateCharacterList(CharacterLeftImage, index);
        UpdateCharacterList(CharacterRightImage, index);
    }

    private void UpdateCharacterList(List<Image> images, int targetIndex)
    {
        if (images == null) return;
        for (int i = 0; i < images.Count; i++)
        {
            if (images[i] != null) images[i].gameObject.SetActive(i == targetIndex);
        }
    }
    #endregion

    #region Vignette Logic
    private void UpdateVignetteState()
    {
        if (vignetteMats.Count == 0) return;

        float targetRadius = maxRadius; // 기본 0.6 (효과 없음)
        Color targetColor = damageColor;

        // 우선순위 1: 피격 (Hit) - 가장 강렬하고 즉각적
        if (isHitEffectActive)
        {
            // 피격 시 쉴드 유무에 따라 색상 결정
            targetColor = (currentShield > 0) ? shieldDamageColor : damageColor;

            hitEffectTimer -= Time.deltaTime;
            if (hitEffectTimer <= 0)
            {
                isHitEffectActive = false;
                // 피격이 끝나면 아래의 다른 상태 로직으로 넘어감
            }
            else
            {
                // 피격 순간: 최소 반지름(-0.3)까지 갔다가 돌아옴
                float hitProgress = 1.0f - (hitEffectTimer / hitVignetteDuration);
                targetRadius = Mathf.Lerp(minRadius, maxRadius, hitProgress); // 강하게 조임
            }
        }
        // 우선순위 2: 버프 (Buff) - 노란색, 은은하게
        else if (isBufferEffectActive)
        {
            bufferTimer -= Time.deltaTime;

            if (bufferTimer <= 0)
            {
                isBufferEffectActive = false;
            }
            else
            {
                targetColor = bufferColor;

                // 은은하게 페이드 인/아웃 계산
                float intensity = CalculateFadeIntensity(bufferTimer, currentBufferMaxDuration);

                // radius: maxRadius(0.6) -> skillTargetRadius(0.35) -> maxRadius(0.6)
                targetRadius = Mathf.Lerp(maxRadius, skillTargetRadius, intensity);

                // 알파값도 조절하여 더 부드럽게 (Color 자체의 투명도 조절)
                targetColor.a = intensity;
            }
        }
        // 우선순위 3: 디버프 (DeBuff) - 보라색, 은은하게
        else if (isDeBufferEffectActive)
        {
            debufferTimer -= Time.deltaTime;

            if (debufferTimer <= 0)
            {
                isDeBufferEffectActive = false;
            }
            else
            {
                targetColor = debufferColor;

                // 은은하게 페이드 인/아웃 계산
                float intensity = CalculateFadeIntensity(debufferTimer, currentDebufferMaxDuration);

                // radius: maxRadius(0.6) -> skillTargetRadius(0.35) -> maxRadius(0.6)
                targetRadius = Mathf.Lerp(maxRadius, skillTargetRadius, intensity);

                targetColor.a = intensity;
            }
        }
        // 우선순위 4: 저체력 경고 (Low Health)
        else
        {
            float healthRatio = (maxHealth > 0) ? (float)currentHealth / maxHealth : 0f;

            // 체력이 50% 이하일 때 경고 효과
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
                // 평상시
                targetRadius = maxRadius;
            }
        }

        // 쉐이더 적용
        foreach (var mat in vignetteMats)
        {
            if (mat != null)
            {
                mat.SetColor(ColorProp, targetColor);
                mat.SetFloat(RadiusProp, targetRadius);
            }
        }
    }

    /// <summary>
    /// 남은 시간과 전체 시간을 기반으로 페이드 인/아웃 강도(0~1)를 반환합니다.
    /// </summary>
    private float CalculateFadeIntensity(float currentTimer, float maxDuration)
    {
        float timeElapsed = maxDuration - currentTimer;
        float intensity = 1.0f;

        // 페이드 인 (시작 부분)
        if (timeElapsed < skillFadeDuration)
        {
            intensity = Mathf.SmoothStep(0f, 1f, timeElapsed / skillFadeDuration);
        }
        // 페이드 아웃 (끝 부분)
        else if (currentTimer < skillFadeDuration)
        {
            intensity = Mathf.SmoothStep(0f, 1f, currentTimer / skillFadeDuration);
        }
        // 유지 (중간)
        else
        {
            intensity = 1.0f;
        }

        return intensity;
    }
    #endregion

    #region UI Control Methods & Utils
    // ... (이전 코드와 동일, 생략 없이 사용하시면 됩니다)
    public void InitializePanels()
    {
        SetPanelsActive(mainPanels, false);
        SetPanelsActive(infoPanels, false);
        SetPanelsActive(instructionPanels, false);
        SetPanelsActive(manualPanels, false);
        SetPanelsActive(pausePanels, false);
        SetPanelsActive(failPanels, false);
        SetPanelsActive(takenDamagePanels, true); // 피격 패널 초기화

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
        // 1. OuttroUIManager가 있으면 결과창 표시 요청
        if (outtroUIManager != null)
        {
            outtroUIManager.ShowResult();

            // [중요] Outtro UI 캔버스가 Ingame UI 캔버스보다 위에 그려져야 합니다.
            // 만약 BlackoutPanel이 ResultPanel을 가린다면 아래 반복문을 주석 처리하세요.
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

    IEnumerator DelayRoutine(float n)
    {
        Debug.Log("시작");

        // 3초 대기 (게임 시간이 멈추면 같이 멈춤)
        yield return new WaitForSeconds(n);

        // 만약 일시정지 상관없이 실제 시간으로 3초를 기다리려면:
        // yield return new WaitForSecondsRealtime(3.0f);

        Debug.Log($"{n}초 후 실행됨");
    }
    #endregion
}