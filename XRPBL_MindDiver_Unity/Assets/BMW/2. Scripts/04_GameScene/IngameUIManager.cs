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
    [Tooltip("쉴드 데미지 시 색상 (파랑/하늘)")]
    [SerializeField] private Color shieldDamageColor = Color.cyan;
    [Tooltip("버프 획득 시 색상 (노랑)")]
    [SerializeField] private Color bufferColor = Color.yellow;

    [Header("Vignette Configuration")]
    [Tooltip("비네팅이 없는 상태의 Radius (기본 0.6)")]
    [SerializeField] private float maxRadius = 0.6f;

    [Tooltip("비네팅이 최대인 상태의 Radius (기본 -0.3)")]
    [SerializeField] private float minRadius = -0.3f;

    [SerializeField] private float bufferEffectDuration = 2.0f;
    [SerializeField] private float debufferEffectDuration = 2.0f;

    [Header("Damage Feedback Settings")]
    [Tooltip("피격 시 데미지 이미지가 유지되는 시간")]
    [SerializeField] private float damageImageDuration = 0.5f;
    [Tooltip("피격 시 비네팅이 확 줄어드는 효과의 지속 시간")]
    [SerializeField] private float hitVignetteDuration = 0.3f;

    [Header("Debug Settings")]
    // 디버그 로그 출력 여부
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Private Fields
    private int currentHealth;
    private int maxHealth;
    private int currentShield;
    private int maxShield;

    // 버프/디버프 관련
    private bool isBufferEffectActive = false;
    private float bufferTimer = 0f;
    private bool isDeBufferEffectActive = false;
    private float debufferTimer = 0f;

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
        InitializePanels();

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
        // [변경] 쉴드가 감소했을 때도 피격 효과 발생
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
        // 체력이 줄어들었을 때 피격 효과 발생
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
        bufferTimer = bufferEffectDuration;
        isBufferEffectActive = true;
        UpdateBuff(DataManager.Instance.GetBuffer());
    }

    private void HandleDeBufferAdded()
    {
        debufferTimer = debufferEffectDuration;
        isDeBufferEffectActive = true;
        UpdateDeBuff(DataManager.Instance.GetDeBuffer());
    }

    private void HandlePauseState(bool isPaused)
    {
        if (isPaused) OpenPausePanel();
        else ClosePausePanel();
    }
    #endregion

    #region Damage & Character Logic
    private void TriggerDamageEffect()
    {
        // 1. 비네팅 피격 효과 활성화
        isHitEffectActive = true;
        hitEffectTimer = hitVignetteDuration;

        // 2. 캐릭터 이미지 '데미지(1번)' 상태로 변경
        SetCharacterImageIndex(1);

        // 3. 기존 리셋 코루틴이 있다면 취소하고 새로 시작
        if (characterImageResetRoutine != null) StopCoroutine(characterImageResetRoutine);
        characterImageResetRoutine = StartCoroutine(ResetCharacterImageRoutine());
    }

    private IEnumerator ResetCharacterImageRoutine()
    {
        yield return new WaitForSeconds(damageImageDuration);

        // 게임 오버 상태가 아니라면 기본 이미지로 복귀
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
            if (images[i] != null)
            {
                images[i].gameObject.SetActive(i == targetIndex);
            }
        }
    }
    #endregion

    #region Vignette Logic
    private void UpdateVignetteState()
    {
        if (vignetteMats.Count == 0) return;

        float targetRadius = maxRadius; // 기본 0.6
        Color targetColor = damageColor;

        // 1. 버프 상태 (노란색)
        if (isBufferEffectActive)
        {
            bufferTimer -= Time.deltaTime;
            if (bufferTimer <= 0) isBufferEffectActive = false;
            else
            {
                targetColor = bufferColor;
                float pulse = Mathf.Sin(Time.time * 10.0f) * 0.1f;
                targetRadius = 0.5f + pulse;
            }
        }
        else
        {
            // 기본은 데미지 색상(빨강)이지만, 피격 순간 쉴드가 있다면 파란색으로 덮어씀
            targetColor = damageColor;

            // --- A. 저체력 로직 (Continuous Warning) ---
            float healthRatio = (maxHealth > 0) ? (float)currentHealth / maxHealth : 0f;
            float healthBasedRadius = maxRadius;

            // 체력이 50% 이하일 때 경고 효과
            if (healthRatio <= 0.5f)
            {
                float t = 1.0f - (healthRatio / 0.5f);
                healthBasedRadius = Mathf.Lerp(maxRadius, minRadius, t);

                float pulseSpeed = Mathf.Lerp(2.0f, 8.0f, t);
                float pulseAmp = Mathf.Lerp(0.0f, 0.05f, t);
                healthBasedRadius += Mathf.Sin(Time.time * pulseSpeed) * pulseAmp;
            }

            // --- B. 피격 효과 로직 (Sudden Flash) ---
            if (isHitEffectActive)
            {
                // [변경] 피격 시 쉴드가 남아있다면 쉴드 컬러(파랑) 사용
                if (currentShield > 0)
                {
                    targetColor = shieldDamageColor;
                }

                hitEffectTimer -= Time.deltaTime;
                if (hitEffectTimer <= 0)
                {
                    isHitEffectActive = false;
                    targetRadius = healthBasedRadius;
                }
                else
                {
                    // 피격 순간: 최소 반지름(-0.3)까지 갔다가 돌아옴
                    float hitProgress = 1.0f - (hitEffectTimer / hitVignetteDuration);
                    targetRadius = Mathf.Lerp(minRadius, healthBasedRadius, hitProgress);
                }
            }
            else
            {
                targetRadius = healthBasedRadius;
            }
        }

        // 3. 쉐이더 적용
        foreach (var mat in vignetteMats)
        {
            if (mat != null)
            {
                mat.SetColor(ColorProp, targetColor);
                mat.SetFloat(RadiusProp, targetRadius);
            }
        }
    }
    #endregion

    #region UI Control Methods & Utils
    private void InitializePanels()
    {
        SetPanelsActive(mainPanels, false);
        SetPanelsActive(infoPanels, false);
        SetPanelsActive(instructionPanels, false);
        SetPanelsActive(manualPanels, false);
        SetPanelsActive(pausePanels, false);
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

    public void OnClickPauseButton() { if (GameManager.Instance != null) { GameManager.Instance.TogglePause(); OpenPausePanel(); } }
    public void OnClickContinueButton() { if (GameManager.Instance != null) GameManager.Instance.TogglePause(); ClosePausePanel(); }
    public void OnClickBackButton() { Time.timeScale = 1f; if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.MainMenu); }

    public void OpenInstructionPanel() { FadePanels(instructionPanels, true); SetDisplayPanel(true); }
    public void CloseInstructionPanel() { FadePanels(instructionPanels, false); SetDisplayPanel(false); }
    public void OpenManualPanel() { FadePanels(manualPanels, true); SetDisplayPanel(true); }
    public void CloseManualPanel() { FadePanels(manualPanels, false); SetDisplayPanel(false); }
    public void OpenPausePanel() { FadePanels(pausePanels, true); SetDisplayPanel(true); }
    public void ClosePausePanel() { FadePanels(pausePanels, false); SetDisplayPanel(false); }
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

        if (progressSlider)
        {
            progressSlider.fillAmount = clampedValue / 100f;
        }

        if (progressText)
        {
            progressText.text = $"{((int)clampedValue)} %";
        }
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
    public void ShowOuttroUI() { if (OuttroUIManager.Instance) { OuttroUIManager.Instance.resultPanel.SetActive(true); foreach (var Panel in blackoutPanels) { if (Panel) Panel.SetActive(true); } } }
    #endregion
}