using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 종료 후 결과(Result) 화면의 UI 흐름과 연출을 관리하는 매니저
/// </summary>
public class OuttroUIManager : MonoBehaviour
{
    #region Inspector Settings (Panels)
    [Header("Panels")]
    [Tooltip("게임 결과(별점)를 보여주는 패널")]
    [SerializeField] public GameObject resultPanel;

    [Tooltip("게임 요약(페이지)을 보여주는 패널 (현재 사용 안함)")]
    [SerializeField] private GameObject summaryPanel;
    #endregion

    #region Inspector Settings (Result UI)
    [Header("Result Elements")]
    [Tooltip("별점 아이콘 배열 (순서대로 1점, 2점, 3점)")]
    [SerializeField] private GameObject[] starIcons;

    [Tooltip("점수 텍스트 (사용하지 않을 경우 비워두세요)")]
    [SerializeField] private TextMeshProUGUI scoreText;
    #endregion

    #region Inspector Settings (Animation)
    [Header("UI Fade & Pulse Settings")]
    [SerializeField] private float panelFadeDuration = 0.5f;
    [SerializeField] private float imageFadeDuration = 0.3f;
    [SerializeField] private float pulseSpeed = 5.0f;
    [SerializeField] private float minPulseAlpha = 0.4f;
    #endregion

    #region Internal State
    // 입력 가능 여부 플래그
    private bool canInput = false;

    // 코루틴 관리 (중복 실행 방지)
    private Dictionary<GameObject, Coroutine> panelCoroutines = new Dictionary<GameObject, Coroutine>();
    private Dictionary<Image, Coroutine> imageCoroutines = new Dictionary<Image, Coroutine>();

    private float cachedOriginalAlpha = 1.0f;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {

        // 초기화: 패널들은 꺼두기
        if (resultPanel != null) resultPanel.SetActive(false);
        if (summaryPanel != null) summaryPanel.SetActive(false);
    }

    private void Update()
    {
        // 결과 화면이 다 나오고 입력이 가능해지면 처리
        if (canInput)
        {
            // 키보드 Space, Enter 또는 게임패드 'A' 버튼 대응
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit"))
            {
                GoHome();
            }
        }
    }
    #endregion

    #region Public Methods

    /// <summary>
    /// 게임 종료 시 외부(GameManager 등)에서 호출
    /// </summary>
    public void ShowResult()
    {
        StartCoroutine(InitializeRoutine());
    }

    #endregion

    #region Logic Methods (Initialization & Navigation)

    /// <summary>
    /// 결과 화면 초기화 코루틴
    /// </summary>
    private IEnumerator InitializeRoutine()
    {
        canInput = false; // 연출 중 입력 방지

        // 1. 패널 켜기
        FadePanel(resultPanel, true);
        if (summaryPanel != null) summaryPanel.SetActive(false);

        // 2. 데이터 불러오기 (DataManager가 없으면 기본값 0)
        int scoreCount = 0;
        int damageCount = 0;
        float playTime = 0f;

        if (DataManager.Instance != null)
        {
            scoreCount = DataManager.Instance.GetScore();
            damageCount = DataManager.Instance.GetShipHealth() + DataManager.Instance.GetShipShield();
            playTime = DataManager.Instance.GetTotalPlayTime();
        }

        // 점수 텍스트 표시 (옵션)
        if (scoreText != null)
        {
            scoreText.text = $"Time: {playTime:F1}s\nMistakes: {damageCount}";
        }

        // 3. 별점 계산
        int starCount = CalculateStarCount(scoreCount, damageCount, playTime);

        // 4. 별 초기화 (모두 끄기)
        foreach (var star in starIcons)
        {
            if (star != null)
            {
                star.SetActive(true); // 활성화는 하되
                Image img = star.GetComponent<Image>();
                if (img != null)
                {
                    Color c = img.color;
                    img.color = new Color(c.r, c.g, c.b, 0f); // 투명하게 시작
                }
            }
        }

        yield return new WaitForSeconds(panelFadeDuration);

        // 5. 별 애니메이션 재생 (순차적 켜짐)
        for (int i = 0; i < starCount; i++)
        {
            if (i < starIcons.Length && starIcons[i] != null)
            {
                FadeInAndPulseStar(starIcons[i]);
                // 쾅! 하는 느낌을 주기 위해 잠시 대기
                yield return new WaitForSeconds(0.4f);
            }
        }

        yield return new WaitForSeconds(0.5f);
        canInput = true; // 이제 입력 가능
        Debug.Log("Result UI Ready. Press Space/Enter to go Home.");
    }

    /// <summary>
    /// 별점 계산 로직
    /// </summary>
    private int CalculateStarCount(int scoreCount, int damageCount, float playTime)
    {
        // 기본 3점 시작
        int starCount = 0;

        // 페널티 1: 
        if (scoreCount >= 2000) starCount += 1;

        // 페널티 2: 
        if (damageCount >= 50) starCount += 1;

        // 페널티 3: 
        float timeLimit = 180f;
        if (playTime < timeLimit) starCount += 1;

        // 최소 0점 ~ 최대 3점 (별 아이콘 개수에 맞춤)
        return Mathf.Clamp(starCount, 0, starIcons.Length);
    }

    public void GoHome()
    {
        Debug.Log("Go Home");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.MainMenu);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("01_MainMenuScene");
        }
    }
    #endregion

    #region Visual Effects (Coroutines)

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

        if (show)
        {
            panel.SetActive(true);
            // 켜질 때는 0부터 시작
            if (!panel.activeSelf || cg.alpha > 0.9f) cg.alpha = 0f;
            startAlpha = cg.alpha;
        }

        float elapsed = 0f;
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

    // --- Star Effect Helpers ---

    private void FadeInAndPulseStar(GameObject starObject)
    {
        if (starObject == null) return;
        Image starImage = starObject.GetComponent<Image>();
        if (starImage == null) return;

        if (imageCoroutines.ContainsKey(starImage) && imageCoroutines[starImage] != null)
        {
            StopCoroutine(imageCoroutines[starImage]);
            imageCoroutines.Remove(starImage);
        }

        // FadeIn 후 Pulse 시작
        imageCoroutines[starImage] = StartCoroutine(FadeImageRoutine(starImage, 1.0f, true));
    }

    /// <summary>
    /// [누락되었던 부분 구현] 이미지를 페이드 인/아웃 하고 옵션에 따라 펄스 효과 실행
    /// </summary>
    private IEnumerator FadeImageRoutine(Image targetImage, float targetAlpha, bool pulseAfter)
    {
        Color startColor = targetImage.color;
        float startAlpha = startColor.a;
        float elapsed = 0f;

        while (elapsed < imageFadeDuration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / imageFadeDuration);
            targetImage.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
            yield return null;
        }

        targetImage.color = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);

        // 페이드가 끝난 후 펄스 효과 실행
        if (pulseAfter)
        {
            if (imageCoroutines.ContainsKey(targetImage))
                StopCoroutine(imageCoroutines[targetImage]);

            imageCoroutines[targetImage] = StartCoroutine(PulseImageRoutine(targetImage));
        }
    }

    private IEnumerator PulseImageRoutine(Image targetImage)
    {
        Color originalColor = targetImage.color;
        // 펄스 시작 시점의 알파값을 원본으로 간주
        float baseAlpha = originalColor.a;

        while (true)
        {
            // Sine 파동 (0 ~ 1)
            float alphaRatio = (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) / 2.0f;

            // Min ~ Max Alpha 사이 보간
            float targetAlpha = Mathf.Lerp(minPulseAlpha, baseAlpha, alphaRatio);

            targetImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha);
            yield return null;
        }
    }

    #endregion
}