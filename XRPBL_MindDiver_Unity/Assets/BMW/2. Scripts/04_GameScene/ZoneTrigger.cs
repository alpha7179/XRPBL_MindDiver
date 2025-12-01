using UnityEngine;

/// <summary>
/// 플레이어의 특정 구역 진입을 감지하여 목표 도달 여부나 위험 상태를 처리하는 트리거 클래스
/// </summary>
public class ZoneTrigger : MonoBehaviour
{
    #region Inspector Fields
    [Header("Settings")]
    // 목표 지점 여부 설정
    [SerializeField] private bool isGoal = true;
    // 디버그 로그 출력 여부 설정
    [SerializeField] private bool isDebug = true;
    // 감지할 플레이어 태그 설정
    [SerializeField] private string playerTag = "Player";
    #endregion

    #region Collision Handling
    /*
     * 플레이어의 트리거 진입 감지 및 페이즈 매니저 상태 갱신 수행
     */
    private void OnTriggerEnter(Collider other)
    {
        // 충돌체가 플레이어인지 태그를 통해 확인
        if (other.CompareTag(playerTag) || (other.transform.root != null && other.transform.root.CompareTag(playerTag)))
        {
            // 디버그 모드 활성화 시 진입 로그 출력
            if (isDebug) Debug.Log($"[ZoneTrigger] Player entered trigger: {gameObject.name}");

            // 게임 페이즈 매니저 참조 탐색
            var phaseManager = FindAnyObjectByType<GamePhaseManager>();

            // 매니저가 존재하고 목표 지점인 경우 도달 처리
            if (phaseManager != null && isGoal)
            {
                // 페이즈 매니저에 목표 도달 상태 전달
                phaseManager.SetZoneReached(true);
            }
        }
    }
    #endregion
}