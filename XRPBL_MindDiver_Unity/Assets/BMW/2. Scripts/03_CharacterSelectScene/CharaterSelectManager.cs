using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 선택 화면 매니저 (수정됨: 토글 방식 + 3초 대기)
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
    [Tooltip("모두 활성화 된 후 대기해야 하는 시간 (초)")]
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
    [Tooltip("체크 시 정면 버튼만 활성화되어도 타이머가 작동합니다.")]
    // 단일 버튼 디버그 모드 활성화 여부
    [SerializeField] private bool enableSingleButtonDebug = false;
    #endregion

    #region Private Fields
    // 패널별 실행 중인 코루틴 관리 딕셔너리
    private Dictionary<GameObject, Coroutine> panelCoroutines = new Dictionary<GameObject, Coroutine>();

    // 버튼 활성화 상태 관리 배열 (0: Front, 1: Left, 2: Right)
    // true = 선택됨(활성화), false = 선택안됨(비활성화)
    private bool[] isButtonActive = new bool[3];

    // 현재 타이머 누적 시간
    private float currentHoldTimer = 0f;
    // 비디오 재생 시작 여부 확인
    private bool isVideoPlayed = false;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        foreach (var video in VideoPanels) if (video) video.SetActive(false);

        // 데이터 매니저 연동 (없으면 기본값 사용을 위해 try-catch 혹은 null 체크 권장하지만 기존 코드 유지)
        if (DataManager.Instance != null)
            masterVolume = ((float)DataManager.Instance.GetVideoVolume()) / 100;

        if (!CheckArrayValid(videoPlayers, "Video Players")) return;

        // 메인 비디오 플레이어에 종료 이벤트 연결
        if (videoPlayers[0] != null)
        {
            videoPlayers[0].loopPointReached += OnVideoFinished;
        }

        // 디버그 모드 경고
        if (enableSingleButtonDebug)
        {
            Debug.LogWarning("!!! [Test Mode] 정면 버튼만 눌러도 진행되는 모드가 켜져있습니다 !!!");
        }
    }

    private void Update()
    {
        if (isVideoPlayed) return;

        // 조건 충족(모두 활성화 or 디버그 모드 충족) 확인
        if (IsReadyToCharge())
        {
            currentHoldTimer += Time.deltaTime;

            // 1초 단위 로그 출력
            if (currentHoldTimer % 1.0f < Time.deltaTime)
                Log($"Ready... Wait for start: {currentHoldTimer:F1} / {requiredHoldTime}");

            // 대기 시간 도달 시 비디오 시퀀스 시작
            if (currentHoldTimer >= requiredHoldTime)
            {
                currentHoldTimer = 0f;
                isVideoPlayed = true;

                // 텍스트 완전 숨김 확인 및 비디오 패널 활성화
                FadePanel(FrontButtonText, false);
                FadePanel(RightButtonText, false);
                FadePanel(LeftButtonText, false);
                foreach (var video in VideoPanels) if (video) FadePanel(video, true);

                if (AudioManager.Instance != null) AudioManager.Instance.StopBGM();

                StartVideoSequence();
            }
        }
        else
        {
            // 조건 불충족 시 타이머 초기화
            if (currentHoldTimer > 0)
            {
                currentHoldTimer = 0f;
                Log("Condition broken. Timer reset.");
            }
        }
    }
    #endregion

    #region Input Logic
    /*
     * 현재 상태가 타이머를 돌릴 준비가 되었는지 확인
     */
    private bool IsReadyToCharge()
    {
        // 디버그 모드: 정면(0번)만 활성화되어 있어도 OK
        if (enableSingleButtonDebug)
        {
            return isButtonActive[0];
        }

        // 일반 모드: 모든 버튼이 활성화(true)되어야 OK
        return isButtonActive.All(x => x);
    }
    #endregion

    #region Video Logic
    private void StartVideoSequence()
    {
        Log($"[VideoManager] {requiredHoldTime}s Ready! Playing Outro Video...");

        if (CheckArrayValid(outroClips, "Outro Clips"))
        {
            PlayPanoramicVideo(outroClips);
        }
    }

    private void PlayPanoramicVideo(VideoClip[] clips)
    {
        for (int i = 0; i < 3; i++)
        {
            // 배열 범위 안전 체크
            if (i >= videoPlayers.Length || i >= clips.Length) break;

            if (videoPlayers[i] != null && clips[i] != null)
            {
                videoPlayers[i].clip = clips[i];
                ApplyVolume(videoPlayers[i], i);
                videoPlayers[i].Play();
            }
        }

        // 비디오 플레이어 0번 문제 시 즉시 종료 처리
        if (videoPlayers[0] == null || clips[0] == null)
        {
            OnVideoFinished(null);
        }
    }

    private void ApplyVolume(VideoPlayer vp, int screenIndex)
    {
        if (vp.audioOutputMode == VideoAudioOutputMode.Direct)
        {
            float finalVolume = (isSideDisplaySoundMute && screenIndex > 0) ? 0f : masterVolume;
            vp.SetDirectAudioVolume(0, finalVolume);
        }
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
    //  UI 이벤트 연결 함수 (토글 방식으로 변경됨)
    // ---------------------------------------------------------

    // [중요] EventTrigger의 Pointer Down에 연결하세요.
    public void OnPointerDownFront() { ToggleButtonState(0, FrontButtonText); }
    public void OnPointerDownLeft() { ToggleButtonState(1, LeftButtonText); }
    public void OnPointerDownRight() { ToggleButtonState(2, RightButtonText); }

    // [중요] 기존 설정 유지를 위해 남겨두었으나, 토글 방식이므로 뗐을 때(Up) 상태를 끄지 않습니다.
    // EventTrigger에서 Pointer Up 이벤트를 제거하셔도 되고, 연결해 두셔도 무방합니다.
    public void OnPointerUpFront() { /* Do Nothing */ }
    public void OnPointerUpLeft() { /* Do Nothing */ }
    public void OnPointerUpRight() { /* Do Nothing */ }


    /*
     * 버튼 상태 토글 (ON <-> OFF) 처리 및 UI 갱신
     */
    private void ToggleButtonState(int index, GameObject textObj)
    {
        // 이미 비디오가 시작되었으면 입력 무시
        if (isVideoPlayed) return;

        // 상태 반전 (True -> False, False -> True)
        isButtonActive[index] = !isButtonActive[index];
        bool isActive = isButtonActive[index];

        // 로그 출력
        Log($"Display {index} Toggled : {(isActive ? "ACTIVE" : "INACTIVE")}");

        // 활성화 되면 -> 텍스트 숨김 (Fade Out)
        // 비활성화 되면 -> 텍스트 보임 (Fade In)
        FadePanel(textObj, !isActive);
    }

    #endregion

    #region Helper Methods
    private bool CheckArrayValid(System.Array array, string arrayName)
    {
        if (array == null || array.Length < 3)
        {
            Log($"[VideoManager] Error: {arrayName} array must have at least 3 elements.");
            if (videoPlayers != null && videoPlayers.Length > 0 && videoPlayers[0] != null)
            {
                OnVideoFinished(videoPlayers[0]);
            }
            return false;
        }
        return true;
    }

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
            // 이미 알파가 목표치와 비슷하면 깜빡임 방지를 위해 0으로 초기화하지 않음 (필요 시 수정)
            if (startAlpha == 1.0f && show) { /* 이미 보여짐 */ }
            else if (startAlpha == 0.0f && !show) { /* 이미 숨겨짐 */ }
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

    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
    #endregion
}