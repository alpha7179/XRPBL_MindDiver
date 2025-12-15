using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 선택 화면 매니저 (최종_Ver_FadeIn)
/// - 기능: 토글 입력, 3초 대기, 연속 클릭 방지, 클립 개수 자동 인식
/// - 추가: 영상 시작 시 부드러운 페이드인 효과 적용
/// </summary>
public class CharacterSelectManager : MonoBehaviour
{
    #region Inspector Fields

    [Header("Video Players Settings")]
    public VideoPlayer[] videoPlayers;

    [Header("Video Panels Settings")]
    public GameObject[] videoPanels;

    [Header("Video Clips Settings")]
    public VideoClip[] outroClips;

    [Header("Timing Settings")]
    [Tooltip("모두 활성화 된 후 대기해야 하는 시간 (초)")]
    [SerializeField] private float requiredHoldTime = 3.0f;
    [Tooltip("버튼 연속 클릭 방지를 위한 쿨타임 (초)")]
    [SerializeField] private float clickCooldown = 1.0f;

    [Header("UI Components")]
    [SerializeField] private GameObject frontButtonText;
    [SerializeField] private GameObject leftButtonText;
    [SerializeField] private GameObject rightButtonText;

    [Header("Fade Effect Settings")]
    [Tooltip("버튼 텍스트가 사라질 때 걸리는 시간")]
    [SerializeField] private float textFadeDuration = 0.2f;

    [Tooltip("영상이 나타날 때(페이드인) 걸리는 시간")]
    [SerializeField] private float videoFadeDuration = 1.5f; // 영상은 좀 더 천천히 뜨도록 설정

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;
    [SerializeField] private bool isSideDisplaySoundMute = true;

    [Header("Debug Settings")]
    [SerializeField] private bool isDebugMode = true;
    [SerializeField] private bool enableSingleButtonDebug = false;

    #endregion

    #region Private Fields
    private Dictionary<GameObject, Coroutine> uiFadeCoroutines = new Dictionary<GameObject, Coroutine>();
    private bool[] isButtonActive = new bool[3];
    private bool isVideoPlayed = false;
    private float lastClickTime = -99f;
    private Coroutine sequenceTimerCoroutine;
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // 1. 비디오 패널 초기화 (중요: Alpha를 0으로 맞춰놔야 페이드인이 먹힘)
        if (videoPanels != null)
        {
            foreach (var panel in videoPanels)
            {
                if (panel)
                {
                    // CanvasGroup이 없으면 추가하고, Alpha를 0으로 강제 설정
                    CanvasGroup cg = panel.GetComponent<CanvasGroup>();
                    if (cg == null) cg = panel.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;

                    panel.SetActive(false);
                }
            }
        }

        // 2. 볼륨 데이터 연동
        if (DataManager.Instance != null)
        {
            masterVolume = ((float)DataManager.Instance.GetVideoVolume()) / 100f;
        }

        // 3. 하드웨어 체크
        if (!CheckHardwareArrayValid(videoPlayers, "Video Players")) return;
        if (!CheckHardwareArrayValid(videoPanels, "Video Panels")) return;

        // 4. 이벤트 연결
        if (videoPlayers[0] != null)
        {
            videoPlayers[0].loopPointReached += OnVideoFinished;
        }

        if (enableSingleButtonDebug)
            LogWarning("!!! [Test Mode] 정면 버튼만 눌러도 진행되는 모드가 켜져있습니다 !!!");
    }

    #endregion

    #region Input Event Handlers

    public void OnPointerDownFront() { ProcessInput(0, frontButtonText); }
    public void OnPointerDownLeft() { ProcessInput(1, leftButtonText); }
    public void OnPointerDownRight() { ProcessInput(2, rightButtonText); }

    private void ProcessInput(int index, GameObject textObj)
    {
        if (isVideoPlayed) return;

        if (Time.time - lastClickTime < clickCooldown)
        {
            Log($"[Input Ignored] Clicked too fast. (Cooldown: {clickCooldown}s)");
            return;
        }

        lastClickTime = Time.time;
        isButtonActive[index] = !isButtonActive[index];
        bool isActive = isButtonActive[index];

        Log($"Display {index} Toggled : {(isActive ? "ACTIVE" : "INACTIVE")}");

        // 버튼 텍스트는 짧은 시간(textFadeDuration) 동안 페이드
        FadePanel(textObj, !isActive, textFadeDuration);

        CheckSequenceCondition();
    }

    #endregion

    #region Sequence Logic

    private void CheckSequenceCondition()
    {
        bool isReady;
        if (enableSingleButtonDebug) isReady = isButtonActive[0];
        else isReady = isButtonActive.All(x => x);

        if (isReady)
        {
            if (sequenceTimerCoroutine == null)
                sequenceTimerCoroutine = StartCoroutine(CountdownRoutine());
        }
        else
        {
            if (sequenceTimerCoroutine != null)
            {
                StopCoroutine(sequenceTimerCoroutine);
                sequenceTimerCoroutine = null;
                Log("Condition broken. Timer reset.");
            }
        }
    }

    private IEnumerator CountdownRoutine()
    {
        float timer = 0f;
        while (timer < requiredHoldTime)
        {
            timer += Time.deltaTime;
            if (timer % 1.0f < Time.deltaTime)
                Log($"Ready... Wait for start: {timer:F1} / {requiredHoldTime}");
            yield return null;
        }

        sequenceTimerCoroutine = null;
        isVideoPlayed = true;

        PrepareAndStartVideo();
    }

    private void PrepareAndStartVideo()
    {
        // 텍스트 버튼 숨김 (즉시 혹은 짧게)
        FadePanel(frontButtonText, false, textFadeDuration);
        FadePanel(leftButtonText, false, textFadeDuration);
        FadePanel(rightButtonText, false, textFadeDuration);

        if (AudioManager.Instance != null) AudioManager.Instance.StopBGM();

        StartSmartVideoSequence();
    }

    #endregion

    #region Smart Video Logic

    private void StartSmartVideoSequence()
    {
        Log($"[VideoManager] Ready! Clip Count: {(outroClips != null ? outroClips.Length : 0)}");

        if (outroClips == null || outroClips.Length == 0)
        {
            LogError("Outro Clips missing!");
            OnVideoFinished(null);
            return;
        }

        bool isPanoramaMode = (outroClips.Length >= 3);
        int targetLoopCount = isPanoramaMode ? 3 : 1;

        if (outroClips.Length == 2)
            LogWarning("Clip count is 2. Playing Front only.");

        // 1. 패널 활성화 (Video Fade Duration 사용)
        if (videoPanels != null)
        {
            for (int i = 0; i < videoPanels.Length; i++)
            {
                if (i < targetLoopCount)
                {
                    // ★ 여기서 비디오 전용 시간(videoFadeDuration)을 사용하여 부드럽게 켭니다.
                    FadePanel(videoPanels[i], true, videoFadeDuration);
                }
                else
                {
                    videoPanels[i].SetActive(false);
                }
            }
        }

        // 2. 비디오 재생
        for (int i = 0; i < targetLoopCount; i++)
        {
            if (i >= videoPlayers.Length) break;

            if (videoPlayers[i] != null && outroClips[i] != null)
            {
                videoPlayers[i].clip = outroClips[i];
                ApplyVolume(videoPlayers[i], i);
                videoPlayers[i].Play();
            }
        }

        if (videoPlayers[0] == null || videoPlayers[0].clip == null)
        {
            OnVideoFinished(null);
        }
    }

    private void ApplyVolume(VideoPlayer vp, int screenIndex)
    {
        float finalVolume = (isSideDisplaySoundMute && screenIndex > 0) ? 0f : masterVolume;
        if (vp.audioOutputMode == VideoAudioOutputMode.Direct)
            vp.SetDirectAudioVolume(0, finalVolume);
        else if (vp.audioOutputMode == VideoAudioOutputMode.AudioSource)
        {
            AudioSource source = vp.GetTargetAudioSource(0);
            if (source != null) source.volume = finalVolume;
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        Log("[VideoManager] Finished.");
        if (videoPlayers != null && videoPlayers.Length > 0 && videoPlayers[0] != null)
            videoPlayers[0].loopPointReached -= OnVideoFinished;

        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameManager.GameState.GameStage);
    }

    #endregion

    #region Helper Methods (Fade & Validations)

    /// <summary>
    /// 페이드 효과 적용 (duration 매개변수 추가됨)
    /// </summary>
    private void FadePanel(GameObject panel, bool show, float duration)
    {
        if (panel == null) return;

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        if (uiFadeCoroutines.ContainsKey(panel) && uiFadeCoroutines[panel] != null)
        {
            StopCoroutine(uiFadeCoroutines[panel]);
        }

        uiFadeCoroutines[panel] = StartCoroutine(FadePanelRoutine(panel, cg, show, duration));
    }

    private IEnumerator FadePanelRoutine(GameObject panel, CanvasGroup cg, bool show, float duration)
    {
        float targetAlpha = show ? 1.0f : 0.0f;
        float startAlpha = cg.alpha;
        float elapsed = 0f;

        if (show) panel.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 부드러운 전환을 위해 Lerp 사용
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        cg.alpha = targetAlpha;

        if (!show) panel.SetActive(false);

        if (uiFadeCoroutines.ContainsKey(panel)) uiFadeCoroutines.Remove(panel);
    }

    private bool CheckHardwareArrayValid(System.Array array, string arrayName)
    {
        if (array == null || array.Length < 3)
        {
            LogError($"[System Error] {arrayName} needs 3 elements.");
            if (videoPlayers != null && videoPlayers.Length > 0 && videoPlayers[0] != null)
                OnVideoFinished(videoPlayers[0]);
            return false;
        }
        return true;
    }

    private void Log(string message) { if (isDebugMode) Debug.Log($"[CharacterSelect] {message}"); }
    private void LogWarning(string message) { if (isDebugMode) Debug.LogWarning($"[CharacterSelect] {message}"); }
    private void LogError(string message) { Debug.LogError($"[CharacterSelect] {message}"); }

    #endregion
}