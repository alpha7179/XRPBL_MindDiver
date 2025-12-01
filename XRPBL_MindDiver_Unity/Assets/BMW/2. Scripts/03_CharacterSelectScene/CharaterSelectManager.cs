using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 선택 화면에서 버튼 입력, 타이머, 비디오 시퀀스 재생을 관리하는 클래스
/// </summary>
public class CharaterSelectManager : MonoBehaviour
{
    #region Inspector Fields
    [Header("Video Players Settings (Order: 0=Front, 1=Left, 2=Right)")]
    // 3면 비디오 플레이어 배열
    public VideoPlayer[] videoPlayers;

    [Header("Video Clips Settings (Order: 0=Front, 1=Left, 2=Right)")]
    public List<GameObject> VideoPanels;
    // 재생할 아웃트로 비디오 클립 배열
    public VideoClip[] outroClips;

    [Header("Settings")]
    [Tooltip("동시 입력 유지 시간 (초)")]
    // 버튼 동시 입력 유지 시간 설정
    [SerializeField] private float requiredHoldTime = 3.0f;

    [Header("Text 구성요소")]
    // 정면 버튼 텍스트 오브젝트
    [SerializeField] private GameObject FrontButtonText;
    // 좌측 버튼 텍스트 오브젝트
    [SerializeField] private GameObject LeftButtonText;
    // 우측 버튼 텍스트 오브젝트
    [SerializeField] private GameObject RightButtonText;

    [Header("UI Fade Settings")]
    // 패널 페이드 효과 지속 시간
    [SerializeField] private float panelFadeDuration = 0.2f;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float masterVolume;
    [SerializeField] private bool isSideDisplaySoundMute = true;

    [Header("Debug Settings")]
    // 디버그 로그 출력 여부
    [SerializeField] private bool isDebugMode = true;

    [Header("디버그 옵션 (테스트용)")]
    [Tooltip("체크 시 정면 버튼만 눌러도 타이머가 작동합니다.")]
    // 단일 버튼 디버그 모드 활성화 여부
    [SerializeField] private bool enableSingleButtonDebug = false;
    #endregion

    #region Private Fields
    // 패널별 실행 중인 코루틴 관리 딕셔너리
    private Dictionary<GameObject, Coroutine> panelCoroutines = new Dictionary<GameObject, Coroutine>();

    // 버튼 입력 상태 관리 배열 (0: Front, 1: Left, 2: Right)
    private bool[] isButtonPressed = new bool[3];
    // 현재 홀드 타이머 누적 시간
    private float currentHoldTimer = 0f;
    // 비디오 재생 시작 여부 확인
    private bool isVideoPlayed = false;
    #endregion

    #region Unity Lifecycle
    /*
     * 컴포넌트 초기화 및 유효성 검사 수행
     */
    private void Start()
    {
        foreach (var video in VideoPanels) if (video) video.SetActive(false);
        masterVolume = ((float)DataManager.Instance.GetVideoVolume()) / 100;

        if (!CheckArrayValid(videoPlayers, "Video Players")) return;

        // 메인 비디오 플레이어에 종료 이벤트 연결
        if (videoPlayers[0] != null)
        {
            videoPlayers[0].loopPointReached += OnVideoFinished;
        }

        // 디버그 모드 활성화 시 경고 로그 출력
        if (enableSingleButtonDebug)
        {
            Debug.LogWarning("!!! [Test Mode] 정면 버튼만 눌러도 진행되는 모드가 켜져있습니다 !!!");
        }

    }

    /*
     * 입력 상태 모니터링 및 타이머 갱신 수행
     */
    private void Update()
    {
        if (isVideoPlayed) return;

        // 모든 버튼이 눌렸는지 확인
        if (IsAllButtonsPressed())
        {
            currentHoldTimer += Time.deltaTime;

            // 1초 단위 로그 출력
            if (currentHoldTimer % 1.0f < Time.deltaTime)
                Log($"Charging... {currentHoldTimer:F1} / {requiredHoldTime}");

            // 유지 시간 도달 시 비디오 시퀀스 시작
            if (currentHoldTimer >= requiredHoldTime)
            {
                currentHoldTimer = 0f;
                isVideoPlayed = true;

                // 안내 텍스트 숨김 처리
                FadePanel(FrontButtonText, false);
                FadePanel(RightButtonText, false);
                FadePanel(LeftButtonText, false);
                foreach (var video in VideoPanels) if (video) FadePanel(video,true);

                AudioManager.Instance.StopBGM();

                StartVideoSequence();
            }
        }
        else
        {
            // 버튼 해제 시 타이머 초기화
            if (currentHoldTimer > 0)
            {
                currentHoldTimer = 0f;
                Log("Button released. Timer reset.");
            }
        }
    }
    #endregion

    #region Input Logic
    /*
     * 모든 버튼의 눌림 상태 확인 (디버그 모드 고려)
     */
    private bool IsAllButtonsPressed()
    {
        if (enableSingleButtonDebug)
        {
            return isButtonPressed[0];
        }

        return isButtonPressed.All(x => x);
    }
    #endregion

    #region Video Logic
    /*
     * 비디오 재생 시퀀스 시작 처리
     */
    private void StartVideoSequence()
    {
        Log($"[VideoManager] {requiredHoldTime}s Hold Complete! Playing Outro Video...");

        if (CheckArrayValid(outroClips, "Outro Clips"))
        {
            PlayPanoramicVideo(outroClips);
        }
    }

    /*
     * 파노라마 비디오 클립 재생 수행
     */
    private void PlayPanoramicVideo(VideoClip[] clips)
    {
        for (int i = 0; i < 3; i++)
        {
            if (videoPlayers[i] != null && clips[i] != null)
            {
                videoPlayers[i].clip = clips[i];
                
                ApplyVolume(videoPlayers[i], i);

                videoPlayers[i].Play();
            }
        }

        // 비디오 플레이어 참조 오류 시 즉시 종료 처리
        if (videoPlayers[0] == null || clips[0] == null)
        {
            OnVideoFinished(null);
        }
    }

    private void ApplyVolume(VideoPlayer vp, int screenIndex)
    {
        // 1. Audio Output Mode가 Direct(직접 출력)인지 확인
        if (vp.audioOutputMode == VideoAudioOutputMode.Direct)
        {
            // 좌/우 화면(인덱스 1, 2)이고 음소거 옵션이 켜져있으면 0, 아니면 설정된 볼륨
            float finalVolume = (isSideDisplaySoundMute && screenIndex > 0) ? 0f : masterVolume;

            // 트랙 0번의 볼륨을 설정
            vp.SetDirectAudioVolume(0, finalVolume);
        }
        // 2. Audio Output Mode가 AudioSource인 경우
        else if (vp.audioOutputMode == VideoAudioOutputMode.AudioSource)
        {
            AudioSource source = vp.GetTargetAudioSource(0);
            if (source != null)
            {
                float finalVolume = (isSideDisplaySoundMute && screenIndex > 0) ? 0f : masterVolume;
                source.volume = finalVolume;
            }
        }
    }

    /*
     * 비디오 재생 완료 시 호출되어 씬 전환 수행
     */
    private void OnVideoFinished(VideoPlayer vp)
    {
        Log("[VideoManager] Video Finished.");
        if (videoPlayers != null && videoPlayers.Length > 0 && videoPlayers[0] != null)
        {
            videoPlayers[0].loopPointReached -= OnVideoFinished;
        }

        if (DataManager.Instance != null && GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.GameStage);
        }
    }
    #endregion

    #region UI Event Handlers
    // ---------------------------------------------------------
    //  UI 이벤트 연결 함수
    // ---------------------------------------------------------
    /*
     * 정면 버튼 누름 이벤트 처리
     */
    public void OnPointerDownFront() { SetButtonState(0, true); FadePanel(FrontButtonText, false); Log("Front Display Held"); }
    /*
     * 정면 버튼 뗌 이벤트 처리
     */
    public void OnPointerUpFront() { SetButtonState(0, false); if (!isVideoPlayed) { FadePanel(FrontButtonText, true); } Log("Front Display Released"); }

    /*
     * 좌측 버튼 누름 이벤트 처리
     */
    public void OnPointerDownLeft() { SetButtonState(1, true); FadePanel(LeftButtonText, false); Log("Left Display Held"); }
    /*
     * 좌측 버튼 뗌 이벤트 처리
     */
    public void OnPointerUpLeft() { SetButtonState(1, false); if (!isVideoPlayed) { FadePanel(LeftButtonText, true); } Log("Left Display Released"); }

    /*
     * 우측 버튼 누름 이벤트 처리
     */
    public void OnPointerDownRight() { SetButtonState(2, true); FadePanel(RightButtonText, false); Log("Right Display Held"); }
    /*
     * 우측 버튼 뗌 이벤트 처리
     */
    public void OnPointerUpRight() { SetButtonState(2, false); if (!isVideoPlayed) { FadePanel(RightButtonText, true); } Log("Right Display Released"); }

    /*
     * 버튼 상태 갱신 및 로그 출력 수행
     */
    private void SetButtonState(int index, bool isPressed)
    {
        isButtonPressed[index] = isPressed;
        if (isPressed) Log($"Button {index} Pressed");
        else Log($"Button {index} Released");
    }
    #endregion

    #region Helper Methods
    /*
     * 배열 유효성 검사 수행
     */
    private bool CheckArrayValid(System.Array array, string arrayName)
    {
        if (array == null || array.Length < 3)
        {
            Log($"[VideoManager] Error: {arrayName} array must have at least 3 elements.");

            // 오류 발생 시에도 흐름 유지를 위해 종료 처리 호출 시도
            if (videoPlayers != null && videoPlayers.Length > 0 && videoPlayers[0] != null)
            {
                OnVideoFinished(videoPlayers[0]);
            }
            return false;
        }
        return true;
    }

    /*
     * UI 패널 페이드 효과 코루틴 실행
     */
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

    /*
     * 알파값 조정을 통한 페이드 인/아웃 처리 코루틴
     */
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

        if (!show)
        {
            panel.SetActive(false);
        }
    }

    /*
     * 디버그 모드 시 로그 출력 수행
     */
    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
    #endregion
}