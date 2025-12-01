using System;
using System.Collections;
using UnityEngine;
using static GamePhaseManager;

/// <summary>
/// 게임의 진행 단계(페이즈)와 각 단계별 목표 및 시간 제한을 관리하는 클래스
/// </summary>
public class GamePhaseManager : MonoBehaviour
{
    #region Enums
    public enum Phase { PrePhase,Phase1, Phase2, Phase3, Complete, Null }
    #endregion

    #region Inspector Fields
    // 현재 진행 중인 페이즈 상태
    public Phase currentPhase;

    [Header("Phase Settings")]
    // 1페이즈 제한 시간
    public float phase1Duration = 80f;
    // 2페이즈 제한 시간
    public float phase2Duration = 110f;

    [Header("Phase 2 Conditions")]
    // 2페이즈 목표 처치 수
    public int phase2KillGoal = 20;

    [Header("Target Zone")]
    [Tooltip("Phase 1에서 도달해야 할 목표 지점 오브젝트")]
    [SerializeField] private GameObject phase1TargetZone;

    [Header("References")]
    [Tooltip("거리 계산을 위한 플레이어 트랜스폼 (필수 할당)")]
    [SerializeField] private Transform playerTransform; // [추가] 플레이어 위치 참조

    [Header("Debug Settings")]
    // 디버그 로그 출력 여부
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Private Fields
    // 현재 페이즈 내 처치 수 누적
    private int _phaseKillCount;
    // 목표 지점 도달 여부 확인
    private bool isZoneReached = false;

    // [추가] 거리 계산용 변수
    private Vector3 _startPosition; // 페이즈 1 시작 위치
    private float _totalDistance;   // 시작점 ~ 목표점 총 거리
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.InitializeGameData();

        StartCoroutine(GameFlowRoutine());
    }

    // [추가] 매 프레임 진행도 업데이트
    private void Update()
    {
        // 1페이즈 진행 중일 때만 거리 계산 수행
        if (currentPhase == Phase.Phase1)
        {
            UpdatePhase1Progress();
        }
    }

    #endregion

    #region Coroutines
    private IEnumerator GameFlowRoutine()
    {
        // PrePhase 시작
        StartPhase(Phase.PrePhase);

        if (PlayerMover.Instance != null) PlayerMover.Instance.SetMoveAction(false);

        IngameUIManager.Instance.OpenMainPanel();
        IngameUIManager.Instance.OpenInfoPanel();

        // Phase 1 시작
        StartPhase(Phase.Phase1);

        if (PlayerMover.Instance != null) PlayerMover.Instance.SetMoveAction(true);

        if (phase1TargetZone != null) phase1TargetZone.SetActive(true);
        isZoneReached = false;

        yield return StartCoroutine(WaitForConditionOrTime(() => isZoneReached, phase1Duration));

        if (phase1TargetZone != null) phase1TargetZone.SetActive(false);

        // Phase 2 시작
        StartPhase(Phase.Phase2);

        yield return StartCoroutine(WaitForConditionOrTime(() => _phaseKillCount >= phase2KillGoal, phase2Duration));

        // Phase 3 시작
        StartPhase(Phase.Phase3);
    }

    private IEnumerator WaitForConditionOrTime(Func<bool> condition, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (condition != null && condition.Invoke())
            {
                Log($"[GamePhaseManager] End Phase: {currentPhase}. Proceeding to next phase.");
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Log("[GamePhaseManager] Time's up! Proceeding to next phase.");
    }
    #endregion

    #region Public Methods
    public void SetZoneReached(bool reached)
    {
        isZoneReached = reached;
        Log($"[GamePhaseManager] Zone Reached: {reached}");
    }

    public void StartPhase(Phase phase)
    {
        currentPhase = phase;
        _phaseKillCount = 0;

        Log($"[GamePhaseManager] Start Phase: {phase}");
        if (AudioManager.Instance != null) AudioManager.Instance.StopBGM();
        if (AudioManager.Instance != null && GameManager.Instance != null) AudioManager.Instance.PlayBGM(GameManager.Instance.currentState, currentPhase);

        switch (phase)
        {
            case Phase.PrePhase:
                break;

            case Phase.Phase1:
                InitializePhase1Distance();
                break;

            case Phase.Phase2:
                break;

            case Phase.Phase3:
                break;

            case Phase.Complete:
                break;
        }
    }

    public void OnEnemyKilled()
    {
        _phaseKillCount++;
        if (DataManager.Instance) DataManager.Instance.IncrementKillCount();
    }

    public void OnBossDefeated()
    {
        if (DataManager.Instance) DataManager.Instance.StopTimer();
        StopAllCoroutines();
        StartPhase(Phase.Complete);
    }

    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
    #endregion

    #region Private Helper Methods (New)

    // [추가] 페이즈 1 거리 데이터 초기화
    private void InitializePhase1Distance()
    {
        if (playerTransform != null && phase1TargetZone != null)
        {
            _startPosition = playerTransform.position;
            _totalDistance = Vector3.Distance(_startPosition, phase1TargetZone.transform.position) - playerTransform.localScale.z/2;

            Log($"[Distance Init] Start: {_startPosition}, Target Dist: {_totalDistance}");
        }
        else
        {
            Debug.LogWarning("[GamePhaseManager] PlayerTransform or TargetZone is missing!");
        }
    }

    private void UpdatePhase1Progress()
    {
        if (playerTransform == null || _totalDistance <= 0.001f) return;

        // 시작점에서 현재 플레이어 위치까지 이동한 거리 계산
        float distanceCovered = Vector3.Distance(_startPosition, playerTransform.position);

        // 진행률 계산 (이동 거리 / 총 거리) * 100
        float progressPercentage = (distanceCovered / _totalDistance) * 100f;

        // 0 ~ 100 사이로 값 제한 (목표 지점을 지나쳐도 100을 넘지 않도록)
        int progressInt = Mathf.RoundToInt(Mathf.Clamp(progressPercentage, 0f, 100f));

        // DataManager에 값 반영 (SetProgress 사용)
        // AddProgress는 '증감'이므로 현재 절대값을 넣기 위해 SetProgress를 사용합니다.
        if (DataManager.Instance != null) DataManager.Instance.SetProgress(progressInt);
    }

    #endregion
}