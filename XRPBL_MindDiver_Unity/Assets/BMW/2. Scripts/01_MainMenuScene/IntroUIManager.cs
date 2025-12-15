using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static GameManager;
using static GamePhaseManager;

/// <summary>
/// 게임 시작 화면(인트로 및 메인 메뉴)의 UI 상호작용 및 패널 전환을 관리하는 클래스
/// </summary>
public class IntroUIManager : MonoBehaviour
{
    public static IntroUIManager Instance { get; private set; }

    #region Inspector Fields
    [Header("UI 패널 (Parents)")]
    // 인트로 패널 참조
    [SerializeField] private GameObject introPanel;
    // 선택(메인 메뉴) 패널 참조
    [SerializeField] private GameObject choicePanel;

    [Header("StartPanel의 하위 패널들")]
    // 장소 선택 패널 참조
    [SerializeField] private GameObject placePanel;
    // 매뉴얼 패널 참조
    [SerializeField] private GameObject manualPanel;
    // 설정 패널 참조
    [SerializeField] private GameObject settingPanel;
    // 종료/팁 패널 참조
    [SerializeField] private GameObject endPanel;
    // 활성화 패널 경계
    [SerializeField] private GameObject[] activePanelBorder;
    // 활성화 패널 이름
    [SerializeField] private GameObject[] activePanelName;

    [SerializeField] private GameObject[] fadePanel;

    [Header("Place 패널 구성요소")]
    // 활성화 챕터 번호 패널
    [SerializeField] private GameObject[] activePhaseNumPanel;
    // 활성화 챕터 이름 패널
    [SerializeField] private GameObject[] activePhaseNamePanel;

    [Header("Setting 패널 구성요소")]
    // BGM 볼륨 텍스트
    [SerializeField] private TextMeshProUGUI BGMText;
    // BGM 볼륨 슬라이더
    [SerializeField] private Slider BGMSlider;
    // SFX 볼륨 텍스트
    [SerializeField] private TextMeshProUGUI SFXText;
    // SFX 볼륨 슬라이더
    [SerializeField] private Slider SFXSlider;
    // Video 볼륨 텍스트
    [SerializeField] private TextMeshProUGUI VideoText;
    // Video 볼륨 슬라이더
    [SerializeField] private Slider VideoSlider;
    // Nar 볼륨 텍스트
    [SerializeField] private TextMeshProUGUI NARText;
    // Nar 볼륨 슬라이더
    [SerializeField] private Slider NARSlider;

    [Header("Animation Settings")]
    [Tooltip("패널이 켜지고 꺼지는 페이드 시간")]
    [SerializeField] private float panelFadeDuration = 0.5f;

    [Header("Debug Settings")]
    // 디버그 로그 출력 여부
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Private Fields
    // 현재 활성화된 최상위 패널 추적
    private GameObject currentTopPanel;
    // 현재 활성화된 메인(하위) 패널 추적
    private GameObject currentMainPanel;
    private int currentActiveMainPanel;
    // 현재 활성화된 장소 패널 추적
    private int currentPlacePanel;
    private string currentPhaseName = null ;

    // 현재 BGM 볼륨 값
    private int BGMValue;
    // 현재 SFX 볼륨 값
    private int SFXValue;
    // 현재 Video 볼륨 값
    private int videoValue;
    // 현재 NAR 볼륨 값
    private int NARValue;

    private Dictionary<GameObject, Coroutine> panelCoroutines = new Dictionary<GameObject, Coroutine>();
    #endregion

    #region Unity Lifecycle
    /*
     * 슬라이더 초기화, 패널 상태 설정 및 BGM 재생 수행
     */
    void Start()
    {
        // BGM 슬라이더 설정 및 이벤트 리스너 등록
        BGMSlider.minValue = 0;
        BGMSlider.maxValue = 100;
        BGMSlider.wholeNumbers = true;

        UpdateBGMVolume(DataManager.Instance.GetBGMVolume());

        // SFX 슬라이더 설정 및 이벤트 리스너 등록
        SFXSlider.minValue = 0;
        SFXSlider.maxValue = 100;
        SFXSlider.wholeNumbers = true;

        UpdateSFXVolume(DataManager.Instance.GetSFXVolume());

        // Video 슬라이더 설정 및 이벤트 리스너 등록
        VideoSlider.minValue = 0;
        VideoSlider.maxValue = 100;
        VideoSlider.wholeNumbers = true;

        UpdateVideoVolume(DataManager.Instance.GetVideoVolume());

        // NAR 슬라이더 설정 및 이벤트 리스너 등록
        NARSlider.minValue = 0;
        NARSlider.maxValue = 100;
        NARSlider.wholeNumbers = true;

        UpdateNARVolume(DataManager.Instance.GetNARVolume());

        // 하위 패널 비활성화 초기화
        if (placePanel)
        {
            placePanel.SetActive(false);
            foreach (var panel in activePhaseNumPanel) { if (panel) panel.SetActive(false); }
            foreach (var panel in activePhaseNamePanel) { if (panel) panel.SetActive(false); }
        }
        if (manualPanel) manualPanel.SetActive(false);
        if (settingPanel) settingPanel.SetActive(false);
        if (endPanel) endPanel.SetActive(false);

        // 최상위 패널 초기화 (인트로 활성화)
        if (choicePanel) choicePanel.SetActive(false);
        if (introPanel) introPanel.SetActive(true);

        currentTopPanel = introPanel;
        currentMainPanel = null;
        currentActiveMainPanel = -1;
        currentPlacePanel = -1;
        currentPhaseName = null;
    }
    #endregion

    #region Panel Control Methods
    /*
     * 최상위 패널(Intro <-> Choice) 전환 수행
     */
    private void SwitchTopPanel(GameObject panelToActivate)
    {
        if (currentTopPanel == panelToActivate) return;

        if (currentTopPanel != null)
        {
            //currentTopPanel.SetActive(false);
            foreach (var panel in fadePanel) { if (panel) FadePanel(panel, true); }
        }

        FadePanel(panelToActivate,true);
        currentTopPanel = panelToActivate;
    }

    /*
     * 메인 메뉴 내부의 하위 패널(Place, Manual 등) 전환 수행
     */
    private void SwitchMainPanel(GameObject panelToActivate, int panelNumToActivate = -1)
    {
        if (currentMainPanel == panelToActivate) return;

        if (panelToActivate == placePanel)
        {
            foreach (var panel in activePhaseNumPanel) { if (panel) panel.SetActive(false); }
            foreach (var panel in activePhaseNamePanel) { if (panel) panel.SetActive(false); }
            currentPlacePanel = -1;
            currentPhaseName = null;
        }

        if (currentMainPanel != null && activePanelBorder != null && activePanelName != null)
        {
            currentMainPanel.SetActive(false);
            activePanelBorder[currentActiveMainPanel].SetActive(false);
            activePanelName[currentActiveMainPanel].SetActive(false);
        }

        panelToActivate.SetActive(true);
        activePanelBorder[panelNumToActivate].SetActive(true);
        activePanelName[panelNumToActivate].SetActive(true);
        currentMainPanel = panelToActivate;
        currentActiveMainPanel = panelNumToActivate;

    }

    /*
     * Place 메뉴 전환 수행
     */
    private void SwitchPlacePanel(int panelToActivate)
    {
        if (currentPlacePanel == panelToActivate) return;

        if (activePhaseNumPanel != null && activePhaseNamePanel != null && currentPlacePanel != -1)
        {
            activePhaseNumPanel[currentPlacePanel].SetActive(false);
            activePhaseNamePanel[currentPlacePanel].SetActive(false);
        }

        activePhaseNumPanel[panelToActivate].SetActive(true);
        activePhaseNamePanel[panelToActivate].SetActive(true);
        currentPlacePanel = panelToActivate;
    }
    #endregion

    #region Button Event Handlers
    /*
     * 인트로 화면 클릭 시 메인 메뉴(장소 선택)로 진입 처리
     */
    public void OnClickIntroButton()
    {
        Log("IntroButton Clicked");
        SwitchTopPanel(choicePanel);

        // 메인 메뉴 진입 시 기본 하위 패널 설정
        SwitchMainPanel(placePanel, 0);
    }

    /*
     * 장소 선택 버튼 클릭 시 해당 패널 활성화
     */
    public void OnClickPlaceButton()
    {
        Log("PlaceButton Clicked");
        SwitchMainPanel(placePanel, 0);
    }

    /*
     * 매뉴얼 버튼 클릭 시 해당 패널 활성화
     */
    public void OnClickManualButton()
    {
        Log("ManualButton Clicked");
        SwitchMainPanel(manualPanel, 1);
    }

    /*
     * 설정 버튼 클릭 시 해당 패널 활성화
     */
    public void OnClickSettingButton()
    {
        Log("SettingButton Clicked");
        SwitchMainPanel(settingPanel, 2);
    }

    /*
     * 종료/팁 버튼 클릭 시 해당 패널 활성화
     */
    public void OnClickEndButton()
    {
        Log("EndButton Clicked");
        SwitchMainPanel(endPanel, 3);
    }

    public void OnClickPhase1Button()
    {
        Log("Phase1Button Clicked");
        currentPhaseName = "Phase1";
        SwitchPlacePanel(0);
    }
    public void OnClickPhase2Button()
    {
        Log("Phase2Button Clicked");
        currentPhaseName = "Phase2";
        SwitchPlacePanel(1);
    }
    public void OnClickPhase3Button()
    {
        Log("Phase3Button Clicked");
        currentPhaseName = "Phase3";
        SwitchPlacePanel(2);
    }

    /*
     * 게임 종료 버튼 클릭 시 어플리케이션 종료 처리
     */
    public void OnClickQuitButton()
    {
        Log("QuitButton Clicked");
        GameManager.Instance.QuitGame();
    }

    /*
     * 체험 시작(플레이) 버튼 클릭 시 인트로 비디오 씬으로 전환
     */
    public void OnClickPlayButton()
    {
        if (currentPhaseName != "Phase1") return;

        Log("체험을 시작합니다.");

        // GameManager를 통한 씬 상태 전환
        if (GameManager.Instance.currentPlayState == GameManager.PlayState.Test) { if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.GameStage); }
        else { if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.CharacterSelect); }
    }

    public void UpdateBGMVolume(int value)
    {
        BGMValue = value;
        BGMSlider.value = BGMValue;
        BGMSlider.onValueChanged.AddListener(OnBGMSliderValueChanged);

        OnBGMSliderValueChanged(BGMValue);
    }

    public void UpdateSFXVolume(int value)
    {
        SFXValue = value;
        SFXSlider.value = SFXValue;
        SFXSlider.onValueChanged.AddListener(OnSFXSliderValueChanged);

        OnSFXSliderValueChanged(SFXValue);
    }

    public void UpdateVideoVolume(int value)
    {
        videoValue = value;
        VideoSlider.value = videoValue;
        VideoSlider.onValueChanged.AddListener(OnVideoSliderValueChanged);

        OnVideoSliderValueChanged(videoValue);
    }

    public void UpdateNARVolume(int value)
    {
        NARValue = value;
        NARSlider.value = NARValue;
        NARSlider.onValueChanged.AddListener(OnNARSliderValueChanged);

        OnNARSliderValueChanged(NARValue);
    }
    #endregion

    #region Slider Event Handlers
    /*
     * BGM 슬라이더 값 변경 시 텍스트 갱신 및 내부 값 저장
     */
    private void OnBGMSliderValueChanged(float value)
    {
        BGMValue = Mathf.RoundToInt(value);
        if (BGMText != null) BGMText.text = BGMValue.ToString();
        if (DataManager.Instance != null) DataManager.Instance.SetBGMVolume(BGMValue);
    }

    /*
     * SFX 슬라이더 값 변경 시 텍스트 갱신 및 내부 값 저장
     */
    private void OnSFXSliderValueChanged(float value)
    {
        SFXValue = Mathf.RoundToInt(value);
        if (SFXText != null) SFXText.text = SFXValue.ToString();
        if (DataManager.Instance != null) DataManager.Instance.SetSFXVolume(SFXValue);
    }

    /*
     * Video 슬라이더 값 변경 시 텍스트 갱신 및 내부 값 저장
     */
    private void OnVideoSliderValueChanged(float value)
    {
        videoValue = Mathf.RoundToInt(value);
        if (VideoText != null) VideoText.text = videoValue.ToString();
        if (DataManager.Instance != null) DataManager.Instance.SetVideoVolume(videoValue);
    }

    /*
     * NAR 슬라이더 값 변경 시 텍스트 갱신 및 내부 값 저장
     */
    private void OnNARSliderValueChanged(float value)
    {
        NARValue = Mathf.RoundToInt(value);
        if (NARText != null) NARText.text = NARValue.ToString();
        if (DataManager.Instance != null) DataManager.Instance.SetNARVolume(NARValue);
    }
    #endregion

    private void FadePanel(GameObject panel, bool show)
    {
        if (panel == null) return;

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        if (panelCoroutines.ContainsKey(panel) && panelCoroutines[panel] != null)
        {
            StopCoroutine(panelCoroutines[panel]);
        }
        panelCoroutines[panel] = StartCoroutine(FadePanelRoutine(panel, cg, show));
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

    #region Helper Methods
    /*
     * 디버그 모드 시 로그 출력 수행
     */
    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
    #endregion
}