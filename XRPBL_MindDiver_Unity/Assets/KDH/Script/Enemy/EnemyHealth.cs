using UnityEngine;
using System.Collections;
using Mover;

public class EnemyHealth : MonoBehaviour
{
    // 같은 오브젝트에 있는 EnemyMover 컴포넌트 참조
    private EnemyMover enemyMover;
    private Vector3 originalScale; // 오브젝트의 원래 크기를 저장할 변수

    [Header("적 설정")]
    public float currentHealth = 10f; // 현재 체력
    public int scoreValue = 100; // 처치 시 획득할 포인트
    public float damagePerClick = 5f; // 클릭당 입는 데미지

    [Header("무적 시간 설정")]
    public float invulnerabilityDuration = 1f; // 무적 시간 (초 단위)
    private float nextDamageTime; // 다음 피해를 입을 수 있는 시간

    [Header("피격 후 이동 지연")]
    // 지연 시간을 설정할 수 있는 변수 추가
    public float targetChangeDelay = 0.8f;
    private Coroutine targetChangeCoroutine; // 중복 코루틴 실행을 막기 위한 변수
    
    [Header("시각적 피격 효과")]
    public float scaleDownAmount = 0.8f; // 원래 크기의 80%로 축소 (0.8f)
    public float scaleEffectDuration = 0.5f; // 크기 축소 효과가 지속되는 시간
    private Coroutine scaleCoroutine; // 크기 조절 코루틴 참조 변수

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
            Debug.LogError("같은 오브젝트에 EnemyMover 컴포넌트가 없습니다. 비행 오브젝트에 이 두 스크립트가 모두 있는지 확인하세요.");
        }

        // 오브젝트의 원래 크기를 Awake 시점에 저장합니다.
        originalScale = transform.localScale;

        // 초기화: 게임 시작 시 바로 피해를 입을 수 있도록 설정
        nextDamageTime = Time.time;
    }

    // 외부에서 호출될 데미지 적용 함수
    public void TakeDamage(float damage)
    {
        // 현재 시간이 다음 피해를 입을 수 있는 시간보다 작은 경우, 데미지 처리를 중단합니다.
        if (Time.time < nextDamageTime)
        {
            // 무적 상태이므로 피해를 입지 않고 함수 종료
            return;
        }
        currentHealth -= damage;
        
        // 다음 피해를 입을 수 있는 시간을 현재 시간 + 무적 시간으로 갱신
        nextDamageTime = Time.time + invulnerabilityDuration;

        // 목표 변경을 코루틴으로 지연 처리
        if (enemyMover != null)
        {
            // 이전에 실행 중인 목표 변경 코루틴이 있다면 중지합니다.
            // (잦은 피격 시 목표 변경 명령이 쌓이는 것을 방지)
            if (targetChangeCoroutine != null)
            {
                StopCoroutine(targetChangeCoroutine);
            }

            // 지연 후 목표를 변경하는 코루틴 시작
            targetChangeCoroutine = StartCoroutine(DelayTargetChange());
        }

        // 피격 시 크기 변화 코루틴 시작!
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine); // 이전 효과가 진행 중이라면 중지
        }

        // 새로운 크기 조절 코루틴 시작
        scaleCoroutine = StartCoroutine(ScaleOnHit(scaleDownAmount, scaleEffectDuration));

        // 피격 이펙트 재생 (선택 사항)
        if (hitEffectPrefab != null)
        {
            // 피격 위치에 이펙트 생성
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // 체력 확인 및 처리
        if (currentHealth <= 0)
        {
            Die();
        }
        /*// 피격 시 이동 목표 변경
        if (enemyMover != null)
        {
            enemyMover.ChangeTargetImmediately();
        }*/
    }

    /// <summary>
    /// 지정된 지연 시간 후에 오브젝트의 이동 목표를 변경하는 코루틴
    /// </summary>
    IEnumerator DelayTargetChange()
    {
        // targetChangeDelay 시간만큼 기다립니다.
        yield return new WaitForSeconds(targetChangeDelay);

        // 대기 시간이 끝나면 이동 목표를 변경합니다.
        enemyMover.ChangeTargetImmediately();

        // 코루틴 완료 후 변수를 null로 설정
        targetChangeCoroutine = null;
    }

    /// <summary>
    /// 오브젝트 크기를 잠시 줄였다가 원래 크기로 되돌리는 코루틴
    /// </summary>
    IEnumerator ScaleOnHit(float targetScale, float duration)
    {
        // 1. 즉시 목표 크기로 축소
        transform.localScale = originalScale * targetScale;

        // 2. 지정된 시간만큼 대기
        yield return new WaitForSeconds(duration);

        // 3. 원래 크기로 복귀
        // Lerp를 사용하여 부드럽게 복귀시킬 수도 있지만, 즉각적인 피드백을 위해 즉시 복귀시킵니다.
        transform.localScale = originalScale;

        scaleCoroutine = null; // 코루틴 완료
    }

    // 오브젝트 처치 시 로직
    private void Die()
    {
        // 플레이어에게 포인트 추가
        playerScore += scoreValue;

        if (DataManager.Instance != null)
        {
            DataManager.Instance.AddScore(scoreValue);
            DataManager.Instance.IncrementKillCount();
        }
        // (디버그용) 현재 총 포인트 출력
        Debug.Log("플레이어 포인트 획득! 현재 총점: " + playerScore);

        // 오브젝트 제거
        Destroy(gameObject);

        // TODO: 폭발 이펙트 재생, 사운드 출력 등 추가
    }
}