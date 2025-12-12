using System;
using System.Collections;
using UnityEngine;
using static GamePhaseManager;

/// <summary>
/// 게임의 진행 단계(페이즈)와 각 단계별 목표 및 시간 제한을 관리하는 클래스
/// 수정사항: 목표 도달(Trigger) 시 강제로 진행도 100% 동기화 기능 추가
/// </summary>
public class GamePhaseManager : MonoBehaviour
{
    #region Enums
    public enum Phase { PrePhase, Phase1, Phase2, Phase3, Complete, Null }
    #endregion

    #region Inspector Fields
    // 현재 진행 중인 페이즈 상태
    public Phase currentPhase;

    [Header("Phase Settings")]
    // 1페이즈 제한 시간
    public float phase1Duration = 300f;
    // 2페이즈 제한 시간
    public float phase2Duration = 300f;

    [Header("Phase 2 Conditions")]
    // 2페이즈 목표 처치 수
    public int phase2KillGoal = 20;

    [Header("Target Zone")]
    [Tooltip("Phase 1에서 도달해야 할 목표 지점 오브젝트")]
    [SerializeField] private GameObject phase1TargetZone;

    [Header("References")]
    [Tooltip("거리 계산을 위한 플레이어 트랜스폼 (필수 할당)")]
    [SerializeField] private Transform playerTransform;

    [Header("Debug Settings")]
    // 디버그 로그 출력 여부
    [SerializeField] private bool isDebugMode = true;

    private OuttroUIManager outtroUIManager;
    #endregion

    #region Private Fields
    // 현재 페이즈 내 처치 수 누적
    private int _phaseKillCount;
    // 목표 지점 도달 여부 확인
    private bool isZoneReached = false;

    // 거리 계산용 변수
    private Vector3 _startPosition;
    private float _initialCenterDistance;
    private float _actualTravelDistance;

    // 매 프레임 UI를 호출하지 않기 위해 상태가 변할 때만 호출하도록 함
    private bool isHappyState = false;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        outtroUIManager = GetComponent<OuttroUIManager>();
        if (DataManager.Instance != null) DataManager.Instance.InitializeGameData();
        if (IngameUIManager.Instance != null) IngameUIManager.Instance.InitializePanels();

        StartCoroutine(GameFlowRoutine());
    }

    // 매 프레임 진행도 업데이트
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

        // 종료 후 처리
        if (phase1TargetZone != null) phase1TargetZone.SetActive(false);

        /*
        // Phase 2 시작
        StartPhase(Phase.Phase2);

        yield return StartCoroutine(WaitForConditionOrTime(() => _phaseKillCount >= phase2KillGoal, phase2Duration));

        // Phase 3 시작
        StartPhase(Phase.Phase3);
        */

        StartPhase(Phase.Complete);
        if (PlayerMover.Instance != null) PlayerMover.Instance.SetMoveAction(false);
        if (IngameUIManager.Instance != null)
        {
            Debug.Log("Call ShowOuttroUI via IngameUIManager");
            IngameUIManager.Instance.ShowOuttroUI();
        }
        else
        {
            Debug.LogError("IngameUIManager Instance is null!");
        }
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

    // [핵심 수정] 외부에서 도달했다고 신호를 주면, 즉시 100%로 강제 설정
    public void SetZoneReached(bool reached)
    {
        isZoneReached = reached;
        Log($"[GamePhaseManager] Zone Reached: {reached}");

        if (reached && currentPhase == Phase.Phase1)
        {
            // 거리 계산상 99%라도 물리적으로 도달했으면 100%로 고정
            if (DataManager.Instance != null)
            {
                DataManager.Instance.SetProgress(100);
            }

            // UI도 즉시 행복한 상태(성공)로 전환
            if (IngameUIManager.Instance != null)
            {
                IngameUIManager.Instance.SetCharacterState(3); // 3: Success
                isHappyState = true;
            }
        }
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

    #region Private Helper Methods

    private void InitializePhase1Distance()
    {
        if (playerTransform != null && phase1TargetZone != null)
        {
            _startPosition = playerTransform.position;
            _initialCenterDistance = Vector3.Distance(_startPosition, phase1TargetZone.transform.position);

            float playerRadius = GetObjectRadiusZ(playerTransform);
            float targetRadius = GetObjectRadiusZ(phase1TargetZone.transform);

            _actualTravelDistance = _initialCenterDistance - playerRadius - targetRadius;
            if (_actualTravelDistance <= 0.1f) _actualTravelDistance = 0.1f;

            Log($"[Distance Init] CenterDist: {_initialCenterDistance}, ActualTravel: {_actualTravelDistance}");
        }
        else
        {
            Debug.LogWarning("[GamePhaseManager] PlayerTransform or TargetZone is missing!");
        }
    }

    private float GetObjectRadiusZ(Transform tr)
    {
        Collider col = tr.GetComponent<Collider>();
        if (col != null) return col.bounds.extents.z;
        else return tr.localScale.z * 0.5f;
    }

    private void UpdatePhase1Progress()
    {
        // [핵심 수정] 이미 도달했다면 거리 계산을 중단하고 100% 상태 유지
        if (isZoneReached)
        {
            if (DataManager.Instance != null && DataManager.Instance.GetProgress() < 100)
            {
                DataManager.Instance.SetProgress(100);
            }
            return;
        }

        if (playerTransform == null || phase1TargetZone == null || _actualTravelDistance <= 0.001f) return;

        float currentCenterDist = Vector3.Distance(playerTransform.position, phase1TargetZone.transform.position);
        float traveledCenterDist = _initialCenterDistance - currentCenterDist;
        float progressPercentage = (traveledCenterDist / _actualTravelDistance) * 100f;

        // 약간의 오차를 허용하여 99.x%에서 멈추는 것 방지 (가중치 1.02배)
        progressPercentage *= 1.02f;

        int progressInt = Mathf.RoundToInt(Mathf.Clamp(progressPercentage, 0f, 100f));

        if (DataManager.Instance != null) DataManager.Instance.SetProgress(progressInt);

        // UI 상태 업데이트
        if (progressInt >= 95)
        {
            if (!isHappyState)
            {
                if (IngameUIManager.Instance != null) IngameUIManager.Instance.SetCharacterState(3);
                isHappyState = true;
            }
        }
        else
        {
            if (isHappyState)
            {
                if (IngameUIManager.Instance != null) IngameUIManager.Instance.SetCharacterState(0);
                isHappyState = false;
            }
        }
    }

    #endregion
}