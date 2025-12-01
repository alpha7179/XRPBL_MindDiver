using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // 같은 오브젝트에 있는 IrregularBoundaryFollower 컴포넌트 참조
    private EnemyMover enemyMover;

    [Header("적 설정")]
    public float currentHealth = 10f; // 현재 체력
    public int scoreValue = 100; // 처치 시 획득할 포인트
    public float damagePerClick = 5f; // 클릭당 입는 데미지

    [Header("이펙트 설정 (선택 사항)")]
    public GameObject hitEffectPrefab; // 피격 이펙트 (선택 사항)

    // 플레이어의 점수를 업데이트할 정적(static) 변수 (간단한 예시)
    public static int playerScore = 0;

    // --- 체력 관리 로직 ---
    private void Awake()
    {
        // 컴포넌트 참조 가져오기
        enemyMover = GetComponent<EnemyMover>();
        if (enemyMover == null)
        {
            Debug.LogError("같은 오브젝트에 IrregularBoundaryFollower 컴포넌트가 없습니다. 비행 오브젝트에 이 두 스크립트가 모두 있는지 확인하세요.");
        }
    }
    // 외부에서 호출될 데미지 적용 함수
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        // 1. 피격 이펙트 재생 (선택 사항)
        if (hitEffectPrefab != null)
        {
            // 피격 위치에 이펙트 생성
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        // 2. 체력 확인 및 처리
        if (currentHealth <= 0)
        {
            Die();
        }
        // 3. 피격 시 이동 목표 변경
        if (enemyMover != null)
        {
            enemyMover.ChangeTargetImmediately();
        }
    }

    // 오브젝트 처치 시 로직
    private void Die()
    {
        // 1. 플레이어에게 포인트 추가
        playerScore += scoreValue;

        if (DataManager.Instance != null)
        {
            DataManager.Instance.AddScore(scoreValue);
            DataManager.Instance.IncrementKillCount();
        }
        // (디버그용) 현재 총 포인트 출력
        Debug.Log("플레이어 포인트 획득! 현재 총점: " + playerScore);

        // 2. 오브젝트 제거
        Destroy(gameObject);

        // TODO: 폭발 이펙트 재생, 사운드 출력 등 추가
    }
}