using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class PlanetMotionPerfect : MonoBehaviour
{
    public enum RotateDirection { CounterClockwise, Clockwise }
    public enum LoopType { Repeat, PingPong, Once }

    [Header("트리거 및 제어")]
    public bool autoStart = true;
    public UnityEvent onPathFinished;

    [Header("초기 위치")]
    [Tooltip("0~360도 사이의 시작 위치")]
    [Range(0f, 360f)] public float initialPosAngle = 0.0f;

    [Header("궤도 형태")]
    public float radiusX = 5.0f;
    public float radiusZ = 3.0f;
    [Range(0f, 1f)] public float roundness = 0.5f;

    [Header("이동 설정")]
    public float straightMoveSpeed = 10.0f;
    public float cornerMoveSpeed = 5.0f;
    public RotateDirection orbitDirection = RotateDirection.CounterClockwise;
    public LoopType loopMode = LoopType.Repeat;

    [Header("구간 설정 (Once/PingPong용)")]
    [Range(0f, 360f)] public float startAngle = 0.0f;
    [Range(0f, 360f)] public float endAngle = 360.0f;

    [Header("기타 설정")]
    public float initialHeight = 0.0f;
    public RotateDirection selfRotationDirection = RotateDirection.CounterClockwise;
    public float selfRotationSpeed = 100.0f;
    public int pathResolution = 2000;
    public bool showGizmos = true;

    // 내부 변수
    private struct PathPoint
    {
        public Vector3 position;
        public float distFromStart; // 누적 거리
        public float speedAtPoint;
    }
    private List<PathPoint> pathPoints = new List<PathPoint>();

    private float totalPerimeter = 0f; // 전체 둘레 길이
    private float currentDist = 0f;    // 현재 이동한 누적 거리 (초기화 없이 계속 증가 가능)
    private bool isMoving = false;
    private bool isPingPongReversing = false;

    // 구간 모드용 변수
    private float rangeLength = 0f;
    private float rangeStartOffset = 0f;

    void Start()
    {
        BakePath();
        ResetToInitialPos(); // 초기 위치로 이동

        if (autoStart) Play();
    }

    public void Play() { isMoving = true; }
    public void Stop() { isMoving = false; }
    public void Restart() { ResetToInitialPos(); Play(); }

    // 초기 위치 설정 함수 (오차 없이 정확한 지점 매핑)
    public void ResetToInitialPos()
    {
        isMoving = false;
        isPingPongReversing = false;

        // 1. 구간 길이 및 오프셋 계산
        CalculateRangeParams();

        // 2. 초기 각도를 전체 궤도상의 거리로 변환
        float targetGlobalDist = AngleToDistance(initialPosAngle);

        // 3. Repeat 모드(무한루프)일 때는 전체 궤도 기준, 그 외에는 구간 기준 매핑
        if (loopMode == LoopType.Repeat && IsFullLoop())
        {
            currentDist = targetGlobalDist;
        }
        else
        {
            // 구간 시작점으로부터 얼마나 떨어져 있는지 계산 (방향 고려)
            float startGlobalDist = AngleToDistance(startAngle);
            float diff = 0f;

            if (orbitDirection == RotateDirection.CounterClockwise)
                diff = (targetGlobalDist >= startGlobalDist) ? targetGlobalDist - startGlobalDist : (totalPerimeter - startGlobalDist) + targetGlobalDist;
            else
                diff = (startGlobalDist >= targetGlobalDist) ? startGlobalDist - targetGlobalDist : startGlobalDist + (totalPerimeter - targetGlobalDist);

            currentDist = Mathf.Clamp(diff, 0f, rangeLength);
        }

        ApplyTransform();
    }

    void Update()
    {
        if (!isMoving || pathPoints.Count == 0) return;

        // 1. 현재 위치에서의 속도 조회
        //    Repeat 모드: 전체 궤도 순환 조회
        //    구간 모드: 구간 내 위치 기반 조회
        float lookupDist = 0f;

        if (loopMode == LoopType.Repeat && IsFullLoop())
        {
            lookupDist = Mathf.Repeat(currentDist, totalPerimeter);
        }
        else
        {
            lookupDist = GetGlobalDistFromRangeProgress(currentDist);
        }

        float speed = GetSpeedAtDistance(lookupDist);
        float moveStep = speed * Time.deltaTime;

        // 2. 이동 처리 logic
        if (loopMode == LoopType.PingPong)
        {
            if (isPingPongReversing)
            {
                currentDist -= moveStep;
                if (currentDist <= 0f)
                {
                    currentDist = 0f;
                    isPingPongReversing = false;
                }
            }
            else
            {
                currentDist += moveStep;
                if (currentDist >= rangeLength)
                {
                    currentDist = rangeLength;
                    isPingPongReversing = true;
                    onPathFinished.Invoke();
                }
            }
        }
        else if (loopMode == LoopType.Once)
        {
            currentDist += moveStep;
            if (currentDist >= rangeLength)
            {
                currentDist = rangeLength;
                isMoving = false;
                onPathFinished.Invoke();
            }
        }
        else // LoopType.Repeat
        {
            // [핵심] Repeat 모드는 거리를 계속 더하고, 위치 조회 때만 Modulo(나머지) 연산을 씀
            // 이렇게 하면 '백스텝'이나 '초기화 시점의 오차'가 발생하지 않음.
            currentDist += moveStep;

            // 만약 구간 반복(Start != End)이라면 수동으로 리셋 필요
            if (!IsFullLoop())
            {
                if (currentDist >= rangeLength)
                {
                    currentDist -= rangeLength; // 0으로 셋팅하지 않고 뺀다 (오차 보존)
                    onPathFinished.Invoke();
                }
            }
            else
            {
                // 완전한 360도 루프의 경우, currentDist가 무한히 커지는 것을 방지하기 위해
                // totalPerimeter보다 아주 클 때만 살짝 줄여줌 (티 안 나게)
                if (currentDist > totalPerimeter * 1000f) currentDist -= totalPerimeter * 1000f;
            }
        }

        ApplyTransform();

        // 자전
        float selfDir = (selfRotationDirection == RotateDirection.CounterClockwise) ? 1f : -1f;
        transform.Rotate(Vector3.up, selfRotationSpeed * selfDir * Time.deltaTime);
    }

    void ApplyTransform()
    {
        float globalDist = 0f;
        if (loopMode == LoopType.Repeat && IsFullLoop())
        {
            // 방향에 따라 거리 증가/감소
            float dir = (orbitDirection == RotateDirection.CounterClockwise) ? 1f : -1f;
            // Mathf.Repeat가 핵심: 360.1 -> 0.1로 부드럽게 변환됨
            globalDist = Mathf.Repeat(currentDist * dir, totalPerimeter);
        }
        else
        {
            globalDist = GetGlobalDistFromRangeProgress(currentDist);
        }

        transform.position = GetPositionAtDistance(globalDist);
    }

    // =========================================================
    // [중요] 순환 참조가 가능한 위치 조회 함수
    // =========================================================
    Vector3 GetPositionAtDistance(float dist)
    {
        // 1. 안전장치
        dist = Mathf.Repeat(dist, totalPerimeter);

        // 2. 이진 탐색 대신 선형 탐색 (데이터가 정렬되어 있음)
        // 마지막 포인트와 첫 포인트를 연결하는 구간 처리가 핵심

        int count = pathPoints.Count;
        for (int i = 0; i < count; i++)
        {
            float currentPointDist = pathPoints[i].distFromStart;

            // 다음 점의 거리 구하기 (순환 고려)
            int nextIndex = (i + 1) % count;
            float nextPointDist = pathPoints[nextIndex].distFromStart;

            // 마지막 구간 처리 (예: 99m -> 0m)
            if (nextIndex == 0) nextPointDist += totalPerimeter;

            if (dist >= currentPointDist && dist < nextPointDist)
            {
                float segmentLength = nextPointDist - currentPointDist;
                float t = (dist - currentPointDist) / segmentLength;

                return Vector3.Lerp(pathPoints[i].position, pathPoints[nextIndex].position, t);
            }
        }

        return pathPoints[0].position; // Fallback
    }

    float GetSpeedAtDistance(float dist)
    {
        dist = Mathf.Repeat(dist, totalPerimeter);
        int count = pathPoints.Count;

        for (int i = 0; i < count; i++)
        {
            int nextIndex = (i + 1) % count;
            float currentDist = pathPoints[i].distFromStart;
            float nextDist = pathPoints[nextIndex].distFromStart;
            if (nextIndex == 0) nextDist += totalPerimeter;

            if (dist >= currentDist && dist < nextDist)
            {
                float t = (dist - currentDist) / (nextDist - currentDist);
                return Mathf.Lerp(pathPoints[i].speedAtPoint, pathPoints[nextIndex].speedAtPoint, t);
            }
        }
        return straightMoveSpeed;
    }

    // =========================================================
    // 경로 베이킹 (Baking)
    // =========================================================
    void BakePath()
    {
        pathPoints.Clear();
        totalPerimeter = 0f;

        Vector3 prevPos = CalculateSuperellipse(0);
        // 첫 점 추가
        pathPoints.Add(new PathPoint { position = prevPos, distFromStart = 0, speedAtPoint = GetTargetSpeed(0) });

        // 360도 전체를 쪼개서 점 생성 (마지막 360도 점은 0도와 겹치므로 리스트에 넣지 않음 -> 순환 연결을 위해)
        for (int i = 1; i < pathResolution; i++)
        {
            float angle = (360f * i) / pathResolution;
            Vector3 newPos = CalculateSuperellipse(angle);

            totalPerimeter += Vector3.Distance(prevPos, newPos);

            pathPoints.Add(new PathPoint
            {
                position = newPos,
                distFromStart = totalPerimeter,
                speedAtPoint = GetTargetSpeed(angle)
            });

            prevPos = newPos;
        }
        // 마지막 점과 첫 점 사이의 거리 추가 (루프 완성)
        totalPerimeter += Vector3.Distance(prevPos, pathPoints[0].position);
    }

    // 헬퍼 함수들
    bool IsFullLoop()
    {
        // 시작과 끝이 거의 같으면 전체 루프로 간주
        return Mathf.Abs(Mathf.Abs(startAngle - endAngle) - 360f) < 0.1f || Mathf.Abs(startAngle - endAngle) < 0.1f;
    }

    void CalculateRangeParams()
    {
        rangeStartOffset = AngleToDistance(startAngle);
        float endDist = AngleToDistance(endAngle);

        if (orbitDirection == RotateDirection.CounterClockwise)
            rangeLength = (endDist >= rangeStartOffset) ? endDist - rangeStartOffset : (totalPerimeter - rangeStartOffset) + endDist;
        else
            rangeLength = (rangeStartOffset >= endDist) ? rangeStartOffset - endDist : rangeStartOffset + (totalPerimeter - endDist);

        if (IsFullLoop()) rangeLength = totalPerimeter;
    }

    float GetGlobalDistFromRangeProgress(float progress)
    {
        float target = 0f;
        if (orbitDirection == RotateDirection.CounterClockwise)
            target = rangeStartOffset + progress;
        else
            target = rangeStartOffset - progress;

        return Mathf.Repeat(target, totalPerimeter);
    }

    float AngleToDistance(float angle)
    {
        if (pathPoints.Count == 0) return 0f;
        angle = Mathf.Repeat(angle, 360f);
        // 단순 비율로 근사치 찾기 (베이킹이 촘촘하므로 충분히 정확)
        int index = Mathf.RoundToInt((angle / 360f) * pathPoints.Count);
        index %= pathPoints.Count;
        return pathPoints[index].distFromStart;
    }

    Vector3 CalculateSuperellipse(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float exponent = Mathf.Lerp(2.0f, 20.0f, roundness);
        float power = 2.0f / exponent;
        return new Vector3(
            Mathf.Sign(Mathf.Cos(rad)) * Mathf.Pow(Mathf.Abs(Mathf.Cos(rad)), power) * radiusX,
            initialHeight,
            Mathf.Sign(Mathf.Sin(rad)) * Mathf.Pow(Mathf.Abs(Mathf.Sin(rad)), power) * radiusZ
        );
    }

    float GetTargetSpeed(float angle)
    {
        return Mathf.Lerp(straightMoveSpeed, cornerMoveSpeed, Mathf.Abs(Mathf.Sin(2 * angle * Mathf.Deg2Rad)));
    }

    void OnValidate()
    {
        if (pathResolution < 100) pathResolution = 100;
        BakePath();
        if (!Application.isPlaying && pathPoints.Count > 0) ResetToInitialPos();
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || pathPoints.Count == 0) return;

        Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.2f);
        for (int i = 0; i < pathPoints.Count; i++)
        {
            Vector3 p1 = pathPoints[i].position;
            Vector3 p2 = pathPoints[(i + 1) % pathPoints.Count].position;
            Gizmos.DrawLine(p1, p2);
        }

        // 초기 위치 표시
        if (!Application.isPlaying) CalculateRangeParams();
        float targetGlobalDist = AngleToDistance(initialPosAngle);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(GetPositionAtDistance(targetGlobalDist), 0.3f);
    }
}