using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

/// <summary>
/// 게임의 전반적인 상태, 씬 전환, 전역 이벤트를 관리하는 매니저 클래스
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
    #endregion

    #region Inspector Fields
    [Header("Debug Settings")]
    public bool isDebugMode = true;

    [Header("Game State")]
    // 현재 일시정지 상태 여부 확인
    public bool IsPaused = false;
    public bool IsFailed = false;
    // 현재 활성화된 씬 이름 저장
    public string CurrentSceneName;
    // 현재 게임 상태 저장
    public GameState currentState;
    // 현재 게임 상태 저장
    public PlayState currentPlayState = PlayState.Play;
    #endregion

    #region Events
    // --- 전역 이벤트 (다른 매니저들이 구독) ---
    // 일시정지 상태 변경 시 발생
    public event Action<bool> OnPauseStateChanged;
    // 실패 상태 변경 시 발생
    public event Action<bool> OnFailStateChanged;
    // 씬 로드 완료 시 발생 (초기화용)
    public event Action<string> OnSceneLoaded;
    // 게임 클리어 (탈출 성공) 시 발생
    public event Action OnGameClear;
    // 게임 오버 (실패) 시 발생
    public event Action OnGameOver;
    #endregion

    #region Enums
    public enum GameState
    {
        MainMenu,
        IntroVideo,
        CharacterSelect,
        GameStage,
        OutroVideo,
        Result
    }

    public enum PlayState
    {
        Play,
        Test,
    }
    #endregion

    #region Unity Lifecycle
    /*
     * 싱글톤 인스턴스 초기화 및 파괴 방지 설정 수행
     */
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.parent = null;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /*
     * 씬 로드 이벤트 구독 등록
     */
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    /*
     * 씬 로드 이벤트 구독 해제
     */
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }
    #endregion

    #region Public Methods
    /*
     * 게임의 일시정지 및 재개 상태를 전환하는 함수
     */
    public void TogglePause()
    {
        // 인트로 씬 등 일시정지가 불필요한 씬 예외 처리
        if (CurrentSceneName.Equals("IntroScene", StringComparison.OrdinalIgnoreCase)) return;

        IsPaused = !IsPaused;

        // 물리 연산 및 시간 정지/재개 (TimeScale 조절)
        Time.timeScale = IsPaused ? 0f : 1f;

        // 상태 변경 이벤트 발생
        OnPauseStateChanged?.Invoke(IsPaused);

        Log($"[GameManager] Pause State: {IsPaused}");
    }

    public void ToggleFail()
    {
        // 인트로 씬 등 일시정지가 불필요한 씬 예외 처리
        if (CurrentSceneName.Equals("IntroScene", StringComparison.OrdinalIgnoreCase)) return;

        IsFailed = !IsFailed;

        // 물리 연산 및 시간 정지/재개 (TimeScale 조절)
        Time.timeScale = IsFailed ? 0f : 1f;

        // 상태 변경 이벤트 발생
        OnFailStateChanged?.Invoke(IsFailed);

        Log($"[GameManager] Fail State: {IsFailed}");
    }

    /*
     * 게임 상태를 변경하고 해당 상태에 맞는 씬으로 전환하는 함수
     */
    public void ChangeState(GameState newState)
    {
        AudioManager.Instance.StopBGM();
        currentState = newState;
        Log($"[GameManager] State Changed to: {newState}");

        switch (newState)
        {
            case GameState.MainMenu:
                LoadScene("01_MainMenuScene");
                AudioManager.Instance.PlayBGM(currentState);

                break;

            case GameState.IntroVideo:
                DataManager.Instance.SetVideoType(DataManager.VideoType.Intro);
                LoadScene("02_VideoScene");
                break;

            case GameState.CharacterSelect:
                LoadScene("03_CharacterSelectScene");
                AudioManager.Instance.PlayBGM(currentState);
                break;

            case GameState.GameStage:
                LoadScene("04_GameScene");
                break;

            case GameState.OutroVideo:
                DataManager.Instance.SetVideoType(DataManager.VideoType.Outro);
                LoadScene("05_VideoScene");
                break;

            case GameState.Result:
                LoadScene("ResultScene");
                AudioManager.Instance.PlayBGM(currentState);
                break;
        }

        CurrentSceneName = SceneManager.GetActiveScene().name;
    }

    /*
     * SceneTransitionManager를 통해 씬 전환을 요청하는 함수
     */
    public void LoadScene(string sceneName)
    {
        // 페이드 매니저가 존재할 경우 페이드 효과와 함께 전환
        if (SceneTransitionManager.Instance != null)
        {
            Log($"[GameManager] Requesting Fade Transition to: {sceneName}");
            SceneTransitionManager.Instance.LoadScene(sceneName);
        }
        else
        {
            // 페이드 매니저 부재 시 직접 전환 (비상용)
            Log("[GameManager] SceneTransitionManager not found. Loading directly.");
            StartCoroutine(LoadSceneRoutine(sceneName));
        }
    }

    /*
     * 게임 클리어 이벤트를 발생시키는 함수
     */
    public void TriggerGameClear()
    {
        Log("[GameManager] Mission Clear!");
        OnGameClear?.Invoke();
    }

    /*
     * 게임 오버 이벤트를 발생시키는 함수
     */
    public void TriggerGameOver()
    {
        Log("[GameManager] Game Over!");
        OnGameOver?.Invoke();
    }

    /*
     * 애플리케이션을 종료하거나 에디터 플레이 모드를 중단하는 함수
     */
    public void QuitGame()
    {
        Log("[GameManager] Quitting Application...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /*
     * 디버그 모드일 때만 로그를 출력하는 유틸리티 함수
     */
    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
    #endregion

    #region Private Methods & Coroutines
    /*
     * SceneTransitionManager 부재 시 사용하는 비상용 씬 로드 코루틴
     */
    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        // 로드 완료 처리는 HandleSceneLoaded 이벤트에서 일괄 수행
    }

    /*
     * 씬 로드 완료 시 자동으로 호출되어 게임 상태를 초기화하는 콜백 함수
     */
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CurrentSceneName = scene.name;

        // 씬 전환 시 게임 시간 및 일시정지 상태 초기화
        Time.timeScale = 1f;
        IsPaused = false;

        // 씬 로드 완료 이벤트 전파
        OnSceneLoaded?.Invoke(scene.name);

        Log($"[GameManager] Scene Loaded & State Reset: {scene.name}");
    }
    #endregion
}