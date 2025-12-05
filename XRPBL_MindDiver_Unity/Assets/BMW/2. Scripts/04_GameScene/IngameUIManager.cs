using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    // [변경] Canvas도 필요하다면 배열로 관리 (선택 사항)
    [Header("XR CAVE Elements")]
    [Tooltip("Front, Left, Right 화면 순서대로 모두 넣으세요")]
    [SerializeField] private Canvas[] ingameCanvases;

    [Header("Panels (Multi-Screen Support)")]
    [Tooltip("Front, Left, Right 화면 순서대로 모두 넣으세요")]
    // [변경] 각 방향별 패널들을 모두 담을 리스트
    [SerializeField] private List<GameObject> mainPanels;
    [SerializeField] private List<GameObject> infoPanels;
    [SerializeField] private List<GameObject> instructionPanels;
    [SerializeField] private List<GameObject> manualPanels;
    [SerializeField] private List<GameObject> pausePanels;
    [SerializeField] private List<GameObject> takenDamagePanels;

    [Header("Panels UI Elements (Multi-Screen Support)")]
    // [변경] 단일 변수 -> 리스트로 변경하여 3개의 화면 UI를 모두 등록
    [Tooltip("Front, Left, Right 화면 순서대로 모두 넣으세요")]
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

    [Header("Images")]
    [SerializeField] public List<Image> CharacterFrontImage;
    [SerializeField] public List<Image> CharacterLeftImage;
    [SerializeField] public List<Image> CharacterRightImage;
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
    [SerializeField] private Color bufferColor = Color.yellow;

    [Header("Vignette Pulse Config")]
    [SerializeField] private float maxRadius = 1;
    [SerializeField] private float minRadius = 0f;
    [SerializeField] private float bufferEffectDuration = 2.0f;
    [SerializeField] private float debufferEffectDuration = 2.0f;

    [Header("Debug Settings")]
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Private Fields
    private int currentHealth;
    private int maxHealth;
    private int currentShield;
    private int maxShield;
    private bool isBufferEffectActive = false;
    private float bufferTimer = 0f;
    private bool isDeBufferEffectActive = false;
    private float debufferTimer = 0f;
    private bool isDisplayPanel = false;

    // 코루틴 관리를 위한 딕셔너리
    private Dictionary<GameObject, Coroutine> panelCoroutines = new Dictionary<GameObject, Coroutine>();
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializePanels();

        // [변경] 모든 비네팅 이미지의 머티리얼 초기화
        foreach (var img in vignetteImages)
        {
            if (img != null)
            {
                Material mat = img.material; // 인스턴스화된 머티리얼 가져오기
                mat.SetFloat(RadiusProp, maxRadius);
                vignetteMats.Add(mat);
            }
        }

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

        foreach (var sliders in HPSliders) if (sliders) sliders.minValue = 0;
        foreach (var sliders in HPSliders) if (sliders) sliders.maxValue = 100;
        foreach (var sliders in HPSliders) if (sliders) sliders.wholeNumbers = true;

        foreach (var sliders in ShieldSliders) if (sliders) sliders.minValue = 0;
        foreach (var sliders in ShieldSliders) if (sliders) sliders.maxValue = 100;
        foreach (var sliders in ShieldSliders) if (sliders) sliders.wholeNumbers = true;

        BulletSlider.minValue = 0;
        BulletSlider.maxValue = 100;
        BulletSlider.wholeNumbers = true;

        BuffSlider.minValue = 0;
        BuffSlider.maxValue = 100;
        BuffSlider.wholeNumbers = true;

        DeBuffSlider.minValue = 0;
        DeBuffSlider.maxValue = 100;
        DeBuffSlider.wholeNumbers = true;
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
        currentShield = current;
        maxShield = max;
        UpdateShield(current);
    }

    private void HandleHealthChange(int current, int max)
    {
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
        // [변경] 일시정지 패널 리스트 전체 제어
        if (isPaused) OpenPausePanel();
        else ClosePausePanel();
    }
    #endregion

    #region Vignette Logic
    private void UpdateVignetteState()
    {
        if (vignetteMats.Count == 0) return;

        float targetRadius = maxRadius;
        Color targetColor = damageColor;

        // 1. 상태에 따른 반지름 및 색상 계산
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
            float hpRatio = (maxShield > 0) ? (float)currentShield / maxShield : 0;
            if (hpRatio < 1.0f)
            {
                targetColor = damageColor;
                float dangerLevel = 1.0f - hpRatio;
                float baseRadius = Mathf.Lerp(maxRadius, minRadius, dangerLevel);
                float currentPulseSpeed = Mathf.Lerp(2.0f, 15.0f, dangerLevel);
                float pulseAmplitude = Mathf.Lerp(0.02f, 0.08f, dangerLevel);
                float pulse = Mathf.Sin(Time.time * currentPulseSpeed) * pulseAmplitude;
                targetRadius = baseRadius + pulse;
            }
            else
            {
                targetRadius = maxRadius;
            }
        }

        // 2. [변경] 모든 화면의 쉐이더에 값 적용
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

    #region UI Control Methods
    /*
     * [변경] 모든 패널 리스트 비활성화 및 초기값 설정
     */
    private void InitializePanels()
    {
        SetPanelsActive(mainPanels, false);
        SetPanelsActive(infoPanels, false);
        SetPanelsActive(instructionPanels, false);
        SetPanelsActive(manualPanels, false);
        SetPanelsActive(pausePanels, false);
        SetPanelsActive(takenDamagePanels, false);

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

    // 헬퍼 함수: 리스트 내 모든 패널 활성/비활성
    private void SetPanelsActive(List<GameObject> panels, bool isActive)
    {
        foreach (var panel in panels)
        {
            if (panel != null) panel.SetActive(isActive);
        }
    }

    // --- Button Event Handlers ---
    public void OnClickPauseButton()
    {
        Log("PauseButton Clicked");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
            // HandlePauseState 이벤트에서 UI 처리를 하므로 여기선 호출 생략 가능하지만 명시적 호출
            OpenPausePanel();
        }
    }

    public void OnClickContinueButton()
    {
        Log("ContinueButton Clicked");
        if (GameManager.Instance != null) GameManager.Instance.TogglePause();
        ClosePausePanel();
    }

    public void OnClickBackButton()
    {
        Log("BackButton Clicked");
        Time.timeScale = 1f;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.MainMenu);
    }

    // --- Panel Open/Close Wrappers [변경: 리스트 전체 적용] ---
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

    public void UpdateScore(int value)
    {
        foreach (var text in scoreTexts) if (text) text.text = value.ToString();
    }

    public void UpdateHP(int value)
    {
        foreach (var text in HPTexts) if (text) text.text = value.ToString();
        foreach (var slider in HPSliders) if (slider) slider.value = value;
    }

    public void UpdateShield(int value)
    {
        foreach (var text in ShieldTexts) if (text) text.text = value.ToString();
        foreach (var slider in ShieldSliders) if (slider) slider.value = value;
    }

    public void UpdateBullet(int value)
    {
        BulletText.text = value.ToString();
        BulletSlider.value = value;
    }

    public void UpdateBuff(int value)
    {
        BuffText.text = value.ToString();
        BuffSlider.value = value;
    }

    public void UpdateDeBuff(int value)
    {
        DeBuffText.text = value.ToString();
        DeBuffSlider.value = value;
    }

    public void UpdateProgress(float value)
    {
        if (progressSlider) { progressSlider.fillAmount = value; progressText.text = $"{((int)value)} %"; }
    }

    // --- State Accessors ---
    public void SetDisplayPanel(bool state) { isDisplayPanel = state; }
    public bool GetDisplayPanel() { return isDisplayPanel; }
    #endregion

    #region Coroutines & Animations
    /*
     * [변경] 여러 패널을 동시에 페이드 처리
     */
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

            if (panelCoroutines.ContainsKey(panel) && panelCoroutines[panel] != null)
            {
                StopCoroutine(panelCoroutines[panel]);
            }
            panelCoroutines[panel] = StartCoroutine(FadePanelRoutine(panel, cg, show));
        }
    }

    private IEnumerator FadePanelRoutine(GameObject panel, CanvasGroup cg, bool show)
    {
        float targetAlpha = show ? 1.0f : 0.0f;
        float startAlpha = cg.alpha;
        float elapsed = 0f;

        if (show)
        {
            panel.SetActive(true);
            cg.alpha = 0f;
            startAlpha = 0f;
        }

        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / panelFadeDuration);
            yield return null;
        }

        cg.alpha = targetAlpha;

        if (!show) panel.SetActive(false);
    }
    #endregion

    #region Utils
    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
    #endregion
}