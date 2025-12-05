using System.Collections;
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

    [Header("Debug Settings")]
    // 디버그 로그 출력 여부
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Private Fields
    // 현재 활성화된 최상위 패널 추적
    private GameObject currentTopPanel;
    // 현재 활성화된 메인(하위) 패널 추적
    private GameObject currentMainPanel;

    // 현재 BGM 볼륨 값
    private int BGMValue;
    // 현재 SFX 볼륨 값
    private int SFXValue;
    // 현재 Video 볼륨 값
    private int videoValue;
    // 현재 NAR 볼륨 값
    private int NARValue;
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

        UpdateVideoVolume(DataManager.Instance.GetNARVolume());

        // 하위 패널 비활성화 초기화
        if (placePanel) placePanel.SetActive(false);
        if (manualPanel) manualPanel.SetActive(false);
        if (settingPanel) settingPanel.SetActive(false);
        if (endPanel) endPanel.SetActive(false);

        // 최상위 패널 초기화 (인트로 활성화)
        if (choicePanel) choicePanel.SetActive(false);
        if (introPanel) introPanel.SetActive(true);

        currentTopPanel = introPanel;
        currentMainPanel = null;
    }
    #endregion

    #region Panel Control Methods
    /*
     * 최상위 패널(Intro <-> Choice) 전환 수행
     */
    private void SwitchTopPanel(GameObject panelToActivate)
    {
        if (currentTopPanel == panelToActivate) return;

        if (currentTopPanel != null) currentTopPanel.SetActive(false);

        panelToActivate.SetActive(true);
        currentTopPanel = panelToActivate;
    }

    /*
     * 메인 메뉴 내부의 하위 패널(Place, Manual 등) 전환 수행
     */
    private void SwitchMainPanel(GameObject panelToActivate)
    {
        if (currentMainPanel == panelToActivate) return;

        if (currentMainPanel != null) currentMainPanel.SetActive(false);

        panelToActivate.SetActive(true);
        currentMainPanel = panelToActivate;
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
        SwitchMainPanel(placePanel);
    }

    /*
     * 장소 선택 버튼 클릭 시 해당 패널 활성화
     */
    public void OnClickPlaceButton()
    {
        Log("PlaceButton Clicked");
        SwitchMainPanel(placePanel);
    }

    /*
     * 매뉴얼 버튼 클릭 시 해당 패널 활성화
     */
    public void OnClickManualButton()
    {
        Log("ManualButton Clicked");
        SwitchMainPanel(manualPanel);
    }

    /*
     * 설정 버튼 클릭 시 해당 패널 활성화
     */
    public void OnClickSettingButton()
    {
        Log("SettingButton Clicked");
        SwitchMainPanel(settingPanel);
    }

    /*
     * 종료/팁 버튼 클릭 시 해당 패널 활성화
     */
    public void OnClickEndButton()
    {
        Log("EndButton Clicked");
        SwitchMainPanel(endPanel);
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
        Log("체험을 시작합니다.");

        // GameManager를 통한 씬 상태 전환
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.IntroVideo);
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
        NARSlider.value = videoValue;
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