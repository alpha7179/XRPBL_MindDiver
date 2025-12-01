using UnityEngine;

public class IrregularBoundaryFollower : MonoBehaviour
{
    [Header("추적 설정")]
    public Transform target; // 플레이어 Transform
    public float minDistance = 5f; // 플레이어로부터 최소 거리
    public float maxDistance = 7f; // 플레이어로부터 최대 거리
    public float flightHeight = 0f; // 플레이어 기준 수직 높이 오프셋

    [Header("이동 및 불규칙성")]
    public float movementSpeed = 2f; // 목표 지점으로 이동하는 속도
    public float targetChangeInterval = 5f; // 목표 지점을 변경할 주기 (초)

    [Header("비행 영역 (좌/우 카메라 시야)")]
    // 90도(오른쪽)와 270도(왼쪽)를 중심으로 허용되는 각도 범위 (예: 30이면 60~120도, 240~300도 허용)
    public float sectorAngleRange = 40f;

    [Header("적 공격력")]
    public int enemydamage = 1;

    private Vector3 currentTargetPosition;
    private float timer;

    private bool ScreenRight;

    // 나중에 유저 키 높이 인식을 통해 적 이동 위치 조정용으로 사용할 것
    private float userheight = 0f;

    private void Start()
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
        }

        // 초기 목표 화면 및 위치 설정
        if (Random.value < 0.5f)
            ScreenRight = true;
        currentTargetPosition = GetNewTargetPosition(ScreenRight);
    }

    private void Update()
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
    }

    /// <summary>
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
    }
}