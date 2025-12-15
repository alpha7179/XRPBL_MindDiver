using UnityEngine;
using System.Collections;

namespace Energy
{
    public class EnergyClass : MonoBehaviour
    {
        // 추적 기준이 되는 카메라 
        public Transform primaryCamera;
        // 이 변수에 저장되는 위치는 카메라의 로컬 공간에서의 목표 위치
        public Vector3 currentLocalTargetPosition;

        [Header("위치 설정")]
        public Transform target; // 플레이어 Transform
        public float minDistance = 1800f; // 플레이어로부터 최소 거리
        public float maxDistance = 2000f; // 플레이어로부터 최대 거리
        public float minFlightHeight = -200f; // 카메라를 기준으로 최소 높이
        public float maxFlightHeight = 180f; // 카메라를 기준으로 최대 높이
        public float sectorAngleRange = 40f;
        public bool ScreenRight; //화면 좌우

        [Header("감지 및 상태")]
        public float detectionRadius = 70f; // 추적을 시작할 최대 거리 (감지 범위)
        private bool isTracking = false; // 현재 추적 중인지 상태

        [Header("에너지 설정")]
        public float currentHealth = 1f; // 현재 체력
        public int scoreValue = 50; // 처치 시 획득할 포인트
        public float damagePerClick = 1f; // 클릭당 입는 데미지
        public int buffValue = 10; // 처치 시 획득할 포인트
        public int shieldValue = 10; // 처치 시 획득할 포인트
        public int debuffValue = 10; // 처치 시 획득할 포인트

        [Header("이펙트 설정 (선택 사항)")]
        public GameObject hitEffectPrefab; // 피격 이펙트 (선택 사항)

        [Header("이동 속도")]
        public float movementSpeed = 2f; // 목표 지점으로 이동하는 속도

        public float invulnerabilityDuration = 0.5f; // 무적 시간 (초 단위)
        private float nextDamageTime; // 다음 피해를 입을 수 있는 시간

        // 플레이어의 점수를 업데이트할 정적(static) 변수 (간단한 예시)
        public static int playerEnergy = 0;

        public virtual void Start()
        {
            // Target 대신 PrimaryCamera 할당 로직을 확인합니다.
            if (primaryCamera == null)
            {
                // Main Camera를 기본값으로 사용합니다.
                primaryCamera = Camera.main.transform;
                if (primaryCamera == null)
                {
                    Debug.LogError("Primary Camera를 찾을 수 없습니다. Inspector에서 할당하거나, 씬에 Main Camera가 있는지 확인하세요.");
                    enabled = false;
                    return;
                }
            }
            // 초기 목표 화면 및 위치 설정
            if (Random.value < 0.5f)
                ScreenRight = true;
            currentLocalTargetPosition = GetNewLocalTargetPosition(ScreenRight);

            // 초기화: 게임 시작 시 바로 피해를 입을 수 있도록 설정
            nextDamageTime = Time.time;
        }

        // LateUpdate에서 처리하여 카메라 움직임 후 위치를 상쇄시킵니다.
        void LateUpdate()
        {
            if (primaryCamera == null) return;

            // 감지 범위 확인
            // 카메라 (또는 플레이어)와 오브젝트 사이의 월드 거리를 계산합니다.
            float distanceToTarget = Vector3.Distance(transform.position, primaryCamera.position);

            // 거리 확인용
            //Debug.Log("플레이어와 적 거리: " + distanceToTarget);

            if (!isTracking)
            {
                // 아직 추적 중이 아니라면, 감지 범위에 들어왔는지 확인합니다.
                if (distanceToTarget <= detectionRadius)
                {
                    isTracking = true;
                    // 추적 시작 시 초기 목표 위치 설정
                    currentLocalTargetPosition = GetNewLocalTargetPosition(ScreenRight);
                    Debug.Log("오브젝트가 플레이어 감지 범위에 진입했습니다. 추적 시작!");
                }
                else
                {
                    // 감지 범위 밖에 있다면 아무것도 하지 않습니다. (추적 중단)
                    return;
                }
            }
            else // isTracking == true (추적 중)
            {
                // 감지 범위를 벗어나면 추적을 중단할지 결정할 수 있습니다.
                // (일반적으로 한번 추적을 시작하면 범위를 벗어나도 계속 추적하지만, 여기서는 벗어나면 멈추도록 구현합니다.)
                if (distanceToTarget > detectionRadius * 1.2f) // 감지 범위보다 조금 더 멀어져야 멈추도록(여유 공간) 설정
                {
                    isTracking = false;
                    Debug.Log("오브젝트가 감지 범위를 벗어났습니다. 추적 중단.");
                    return;
                }
            }



            // 오브젝트가 '카메라의 자식'인 것처럼 움직이도록 월드 위치를 계산합니다.
            // primaryCamera.TransformPoint: 로컬 좌표를 월드 좌표로 변환
            Vector3 worldTargetPosition = primaryCamera.TransformPoint(currentLocalTargetPosition);

            // 3. 부드러운 이동 (Lerp)
            transform.position = Vector3.Lerp(
                transform.position,
                worldTargetPosition,
                Time.deltaTime * movementSpeed
            );
        }
        public Vector3 GetNewLocalTargetPosition(bool ScreenDirection)
        {
            // 1. 랜덤 각도 및 거리 생성
            float angle;
            // true가 오른쪽
            if (ScreenDirection == true)
            {
                // 오른쪽 섹터: 90도 ± sectorAngleRange
                angle = 90f + Random.Range(-sectorAngleRange, sectorAngleRange);

            }
            else
            {
                // 왼쪽 섹터: 270도 ± sectorAngleRange
                angle = 270f + Random.Range(-sectorAngleRange, sectorAngleRange);
                // 각도를 -180 ~ 180 범위로 조정
                if (angle > 360f) angle -= 360f;
            }

            float distance = Random.Range(minDistance, maxDistance);

            // 2. 수직 높이 랜덤 생성 (새로운 로직!)
            float randomHeight = Random.Range(minFlightHeight, maxFlightHeight);

            // 3. 로컬 위치 계산
            // Quaternion.Euler(0, angle, 0)은 카메라의 로컬 Y축(위쪽)을 기준으로 회전합니다.
            Quaternion rotation = Quaternion.Euler(0, angle, 0);

            // 카메라의 로컬 forward 방향(0, 0, 1)을 기준으로 회전 및 거리를 적용합니다.
            Vector3 horizontalDirection = rotation * Vector3.forward;

            // 로컬 XZ 평면 위치 계산
            Vector3 localXZPosition = horizontalDirection * distance;

            // 수직 오프셋을 더해 최종 로컬 목표 위치를 반환합니다.
            return localXZPosition + Vector3.up * randomHeight;
        }

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
        }
        public virtual void Die()
        {
            // 플레이어에게 포인트 추가
            playerEnergy += scoreValue;

            if (DataManager.Instance != null)
            {
                DataManager.Instance.AddScore(scoreValue);
            }
            // (디버그용) 현재 총 포인트 출력
            Debug.Log("플레이어 에너지 획득! 현재 총점: " + playerEnergy);

            // 오브젝트 제거
            Destroy(gameObject);

            // TODO: 폭발 이펙트 재생, 사운드 출력 등 추가
        }

    }
}