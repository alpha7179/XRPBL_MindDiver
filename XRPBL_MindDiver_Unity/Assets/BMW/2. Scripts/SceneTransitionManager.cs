using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 씬 전환 흐름 및 화면 페이드 효과를 제어하는 관리자 클래스
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    #region Singleton
    public static SceneTransitionManager Instance;
    #endregion

    #region Inspector Fields
    [Header("Components")]
    // 투명도 조절을 위한 캔버스 그룹
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    // 페이드 효과를 렌더링할 캔버스
    [SerializeField] private Canvas fadeCanvas;

    [Header("Settings")]
    // 페이드 효과 지속 시간
    [SerializeField] private float fadeDuration = 1.0f;
    // 최상위 렌더링을 위한 정렬 순서 설정
    [SerializeField] private int sortingOrder = 30000;
    #endregion

    #region Private Fields
    // 현재 페이드 진행 여부 확인
    private bool isFading = false;
    #endregion

    #region Unity Lifecycle
    /*
     * 싱글톤 인스턴스 초기화 및 캔버스 설정을 수행하는 함수
     */
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null, false);
            DontDestroyOnLoad(gameObject);

            // 카메라 설정과 무관하게 전체 화면을 덮도록 강제 설정
            SetupCanvasOverlay();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Helper Methods
    /*
     * 캔버스를 오버레이 모드로 설정하여 카메라 의존성을 제거하는 함수
     */
    private void SetupCanvasOverlay()
    {
        if (fadeCanvas != null)
        {
            // 카메라 없이 화면 버퍼에 직접 그리기 위한 Overlay 모드 강제 변경
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 그리기 순서를 최상위로 설정
            fadeCanvas.sortingOrder = sortingOrder;

            // 터치 등 물리적 레이캐스트 차단 설정
            if (fadeCanvas.TryGetComponent<UnityEngine.UI.GraphicRaycaster>(out var raycaster))
            {
                raycaster.blockingObjects = UnityEngine.UI.GraphicRaycaster.BlockingObjects.All;
            }
        }
    }
    #endregion

    #region Public Methods
    /*
     * 지정된 이름의 씬으로 전환을 시작하는 함수
     */
    public void LoadScene(string sceneName)
    {
        if (isFading) return;

        // IntroScene 진입 시 기존 매니저들의 파괴 로직 수행
        if (sceneName == "IntroScene")
        {
            Debug.Log("[SceneTransitionManager] IntroScene 로드 전 매니저 인스턴스 정리");
            if (DataManager.Instance != null) Destroy(DataManager.Instance.gameObject);
            if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
        }

        StartCoroutine(TransitionRoutine(sceneName));
    }
    #endregion

    #region Coroutines
    /*
     * 페이드 아웃, 씬 로딩, 페이드 인을 순차적으로 처리하는 코루틴
     */
    private IEnumerator TransitionRoutine(string sceneName)
    {
        isFading = true;

        // 투명에서 검정으로 페이드 아웃 실행
        yield return StartCoroutine(Fade(0f, 1f));

        // 비동기 씬 로딩 시작 및 자동 전환 방지 설정
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 로딩 진행률 90% 도달 시까지 대기
        while (op.progress < 0.9f)
        {
            yield return null;
        }

        // 씬 전환 승인
        op.allowSceneActivation = true;

        // 로딩 완료 대기
        while (!op.isDone)
        {
            yield return null;
        }

        // Overlay 모드 사용으로 별도의 카메라 재설정 로직 불필요

        // 검정에서 투명으로 페이드 인 실행
        yield return StartCoroutine(Fade(1f, 0f));

        isFading = false;
    }

    /*
     * 캔버스 그룹의 알파값을 조절하여 페이드 효과를 연출하는 코루틴
     */
    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0f;

        // 페이드 시작 전 초기값 및 레이캐스트 차단 설정
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            fadeCanvasGroup.alpha = startAlpha;
        }

        // 지정된 지속 시간 동안 알파값 보간
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);

            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = newAlpha;

            yield return null;
        }

        // 최종 알파값 확정 및 레이캐스트 차단 여부 갱신 (완전히 불투명할 때만 차단)
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = endAlpha;
            fadeCanvasGroup.blocksRaycasts = (endAlpha > 0.9f);
        }
    }
    #endregion
}