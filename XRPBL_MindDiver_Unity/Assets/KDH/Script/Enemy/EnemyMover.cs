using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;

namespace Mover
{
    public class EnemyMover : MonoBehaviour
    {
        [Header("위치 설정")]
        //public Transform target; // 플레이어 Transform
        public float minDistance = 1800f; // 플레이어로부터 최소 거리
        public float maxDistance = 2000f; // 플레이어로부터 최대 거리
                                          //public float flightHeight = 0f; // 플레이어 기준 수직 높이 오프셋
                                          // [수정] 고정 높이 대신 높이 범위 설정
        public float minFlightHeight = -600f; // 카메라를 기준으로 최소 높이
        public float maxFlightHeight = 180f; // 카메라를 기준으로 최대 높이
                                             // 90도(오른쪽)와 270도(왼쪽)를 중심으로 허용되는 각도 범위 (예: 30이면 60~120도, 240~300도 허용)
        public float sectorAngleRange = 40f;
        private float addition = 1f;

        [Header("이동 및 불규칙성")]
        public float movementSpeed = 2f; // 목표 지점으로 이동하는 속도
        public float targetChangeInterval = 5f; // 목표 지점을 변경할 주기 (초)

        [Header("감지 및 상태")]
        public float detectionRadius = 70f; // 추적을 시작할 최대 거리 (감지 범위)
        public bool isTracking = false; // 현재 추적 중인지 상태

        [Header("적 공격력")]
        public int enemydamage = 5;

        public bool printDebug = false;

        // 디버프 여부
        public bool isDebuffed;
        private float commonMS;
        private float commonTCI;


        // 추적 기준이 되는 카메라 (3개 중 전방 카메라 하나만 지정해도 충분합니다)
        public Transform primaryCamera;
        // 이 변수에 저장되는 위치는 카메라의 로컬 공간에서의 목표 위치입니다.
        private Vector3 currentLocalTargetPosition;
        private float timer;

        public bool ScreenRight;

        // 나중에 유저 키 높이 인식을 통해 적 이동 위치 조정용으로 사용할 것
        private float userheight = 0f;

        //구버전
        /*private void Start()
        {
            if (target == null)
            {
                // "MainCamera" 태그를 가진 오브젝트를 자동으로 찾습니다.
                GameObject player = GameObject.FindGameObjectWithTag("MainCamera");
                if (player != null)
                {
                    target = player.transform;
                }
                else
                {
                    Debug.LogError("MainCamera 오브젝트를 찾을 수 없습니다. 'MainCamera' 태그를 확인하거나, Inspector에서 직접 할당해 주세요.");
                    enabled = false; // 스크립트 비활성화
                    return;
                }
            }*/
        private void Start()
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
            //currentLocalTargetPosition = GetNewLocalTargetPosition(ScreenRight);
        }

        /*private void Update()
        {
            if (target == null) return;

            // 1. 목표 위치 갱신 타이머
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                // 플레이어가 시간 안에 터치에 실패한 경우 적 공격
                if (DataManager.Instance != null)
                {
                    DataManager.Instance.TakeDamage(enemydamage);
                }
                currentTargetPosition = GetNewTargetPosition(ScreenRight);
                timer = targetChangeInterval;
            }

            // 2. 목표 지점으로 이동
            // 목표 위치로 부드럽게 이동합니다.
            transform.position = Vector3.Lerp(
                transform.position,
                currentTargetPosition,
                Time.deltaTime * movementSpeed
            );

            // 3. 플레이어 바라보기 (선택 사항)
            // 오브젝트가 플레이어를 향하도록 회전시킵니다.
            transform.LookAt(target);
        }*/

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
                    ScreenSelection();
                    currentLocalTargetPosition = GetNewLocalTargetPosition(ScreenRight);
                    timer = targetChangeInterval;
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

            //// 디버프 구현------------------------
            //commonMS = movementSpeed;
            //commonTCI = targetChangeInterval;
            //if (DataManger.Instance.GetDebuffState())
            //{
            //    movementSpeed /= 2;
            //    targetChangeInterval *= 2;
            //}
            //else
            //{
            //    movementSpeed = commonMS;
            //    targetChangeInterval = commonTCI;
            //}

            // 목표 위치 갱신 타이머
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                // 로컬 목표 위치 갱신 (플레이어 데미지 여기에)
                currentLocalTargetPosition = GetNewLocalTargetPosition(ScreenRight);
                timer = targetChangeInterval;
                DataManager.Instance.TakeDamage(enemydamage);
            }

            // 로컬 위치를 월드 위치로 변환

            // 오브젝트가 '카메라의 자식'인 것처럼 움직이도록 월드 위치를 계산합니다.
            // primaryCamera.TransformPoint: 로컬 좌표를 월드 좌표로 변환
            Vector3 worldTargetPosition = primaryCamera.TransformPoint(currentLocalTargetPosition);

            // 부드러운 이동 (Lerp)
            transform.position = Vector3.Lerp(
                transform.position,
                worldTargetPosition,
                Time.deltaTime * movementSpeed
            );

            // 오브젝트가 플레이어를 향하도록 회전시킵니다.
            transform.LookAt(primaryCamera);

            // 회전 (카메라 기준으로 좌/우 시야각을 유지해야 하므로, 카메라는 바라보지 않습니다.)
            // 오브젝트의 회전은 로컬 움직임에 맞춰 자연스럽게 따라가게 둡니다.
        }

        /*/// <summary>
        /// 플레이어 주변의 허용된 각도와 거리 내에서 새로운 목표 위치를 생성합니다.
        /// </summary>
        private Vector3 GetNewTargetPosition(bool ScreenDirection)
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

            // 최소/최대 거리 내에서 무작위 거리 선택
            float distance = Random.Range(minDistance, maxDistance);

            // 2. 회전 및 위치 계산

            // 플레이어의 현재 회전(Rotation)을 기준으로 각도를 적용합니다.
            Quaternion rotation = target.rotation * Quaternion.Euler(0, angle, 0);

            // 플레이어 위치로부터 랜덤 거리만큼 떨어진 위치를 계산합니다.
            Vector3 direction = rotation * Vector3.forward;
            Vector3 horizontalPosition = target.position + direction * distance;

            // 수직 오프셋을 더해 최종 목표 위치를 반환합니다.
            return horizontalPosition + Vector3.down * (0.5f + Random.Range(-1, 1));// + Vector3.up * (flightHeight + userheight);
        }
        public void ChangeTargetImmediately()
        {
            // 새로운 목표 위치를 즉시 계산하고 목표 갱신 타이머를 초기화합니다.
            currentTargetPosition = GetNewTargetPosition(ScreenRight);
            timer = targetChangeInterval;
        }*/
        /// <summary>
        /// 카메라의 로컬 공간에서 새로운 목표 위치를 생성합니다.
        /// </summary>
        private Vector3 GetNewLocalTargetPosition(bool ScreenDirection)
        {
            // 랜덤 각도 및 거리 생성
            float angle;
            // true가 오른쪽
            if (ScreenDirection == true)
            {
                // 오른쪽 섹터: 90도 ± sectorAngleRange
                angle = 90f + Random.Range(-sectorAngleRange, sectorAngleRange - 5f);

            }
            else
            {
                // 왼쪽 섹터: 270도 ± sectorAngleRange
                angle = 270f + Random.Range(-sectorAngleRange + 5f, sectorAngleRange);
                // 각도를 -180 ~ 180 범위로 조정
                if (angle > 360f) angle -= 360f;
            }

            float distance = Random.Range(minDistance, maxDistance);

            // 수직 높이 랜덤 생성
            float randomHeight = Random.Range(minFlightHeight, maxFlightHeight);

            // 로컬 위치 계산
            // Quaternion.Euler(0, angle, 0)은 카메라의 로컬 Y축(위쪽)을 기준으로 회전합니다.
            Quaternion rotation = Quaternion.Euler(0, angle, 0);

            // 카메라의 로컬 forward 방향(0, 0, 1)을 기준으로 회전 및 거리를 적용합니다.
            Vector3 horizontalDirection = rotation * Vector3.forward;

            // 로컬 XZ 평면 위치 계산
            if (angle >= 35)
            {
                addition = 1.3f;
            }
            else
            {
                addition = 1f;
            }
            Vector3 localXZPosition = horizontalDirection * distance * addition;

            // 수직 오프셋을 더해 최종 로컬 목표 위치를 반환합니다.
            return localXZPosition + Vector3.up * randomHeight;
        }

        // --- EnemyHealth에서 호출하는 함수 수정 ---
        public void ChangeTargetImmediately()
        {
            // 로컬 목표 위치를 즉시 새로운 랜덤 위치로 변경합니다.
            currentLocalTargetPosition = GetNewLocalTargetPosition(ScreenRight);
            timer = targetChangeInterval;
        }

        // 초기 목표 화면 및 위치 설정 (좌/우 숫자 적은 쪽으로 배정, 같을 시 랜덤)
        public virtual void ScreenSelection()
        {
            GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy1");
            //GameObject[] existingEnemy1s = GameObject.FindGameObjectsWithTag("Enemy1");
            //GameObject[] existingEnemy2s = GameObject.FindGameObjectsWithTag("Enemy2");
            //GameObject[] existingEnemies = existingEnemy1s.Concat(existingEnemy2s).ToArray();
            int currentCount = existingEnemies.Length;
            int screenCount = 0;
            foreach (GameObject enemy in existingEnemies)
            {
                EnemyMover follower = enemy.GetComponent<EnemyMover>();
                if (follower != null && follower.ScreenRight)
                {
                    screenCount++;
                }
            }
            if (currentCount == 0 || screenCount == currentCount / 2)
            {
                if (Random.value < 0.5f)
                    ScreenRight = true;
            }
            else if (screenCount > currentCount / 2)
            {
                ScreenRight = false;
            }
            else
            {
                ScreenRight = true;
            }
            Debug.Log("- ScreenRight : " + ScreenRight);
        }

        // 디버프 발동 시 호출 (사용X)
        //public void Debuffed()
        //{
        //    movementSpeed /= 2;
        //    targetChangeInterval *= 2;
        //    EnemyHealth.currentHealth
        //}
    }
}