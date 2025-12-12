using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor; // 지즈모를 씬 뷰에서 더 예쁘게 그리기 위해 사용 (선택사항)
#endif

/// <summary>
/// 물리 엔진 기반(Rigidbody Physics) 플레이어 이동 클래스
/// 기능: 로지텍 휠/페달 + 관성 주행 + 버튼 매핑 + 포스 피드백 + [강화된 자동 재연결] + [시각적 디버그 지즈모]
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMover : MonoBehaviour
{
    public static PlayerMover Instance { get; private set; }

    #region Inspector Fields
    [Header("Control Settings")]
    [Tooltip("이 값이 True여야만 움직일 수 있습니다.")]
    [SerializeField] private bool canMove = true;

    [Header("Physics Settings (Realistic)")]
    [SerializeField] private float forwardForce = 3000f;
    [SerializeField] private float reverseForce = 2000f;
    [SerializeField] private float strafeForce = 2500f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float xLimit = 12f;

    [Header("Drag Settings (관성)")]
    [SerializeField] private float coastingDrag = 1f;
    [SerializeField] private float brakingDrag = 3f;

    [Header("Logitech Settings")]
    [SerializeField] private float wheelDeadzone = 0.05f;
    [SerializeField] private bool useLogitechWheel = true;

    [Header("Force Feedback (핸들 탄성)")]
    [Range(0, 100)][SerializeField] private int centeringSpringStrength = 50;
    [Range(0, 100)][SerializeField] private int damperStrength = 30;

    [Header("UI Settings")]
    [SerializeField] private float uiKeepTime = 2.0f;

    [Header("References")]
    [SerializeField] private GameObject shieldEffect;
    #endregion

    #region Private Fields
    private Rigidbody _rb;
    private Vector2 _input;

    // SDK 상태 변수
    private bool _isWheelInitialized = false;
    private bool _isWheelConnected = false;
    private LogitechGSDK.DIJOYSTATE2ENGINES _currentState;

    // 버튼 상태 및 UI
    private bool[] _prevButtonStates = new bool[128];
    private int _currentUiState = -1;
    private float _uiTimer = 0f;

    // [추가됨] 코루틴 중복 실행 방지용 변수
    private Coroutine _initCoroutine;

    private OuttroUIManager outtroUIManager;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _rb = GetComponent<Rigidbody>();
        outtroUIManager = GetComponent<OuttroUIManager>();

        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        // Unity 6 or newer API check
#if UNITY_6000_0_OR_NEWER
        _rb.linearDamping = coastingDrag;
#else
        _rb.drag = coastingDrag; // 구버전 호환용
#endif
        _rb.mass = 1000f;
    }

    private void Start()
    {
        if (useLogitechWheel)
        {
            StartConnectionSequence();
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        // 앱으로 돌아왔는데 연결이 끊겨있으면 재시도
        if (focus && useLogitechWheel && !_isWheelInitialized)
        {
            StartConnectionSequence();
        }
    }
    private void StartConnectionSequence()
    {
        // 이미 돌고 있는 연결 시도가 있다면 중단하고 새로 시작 (중복 방지)
        if (_initCoroutine != null) StopCoroutine(_initCoroutine);
        _initCoroutine = StartCoroutine(InitializeLogitechSDK());
    }
    private IEnumerator InitializeLogitechSDK()
    {
        // 이미 연결된 상태면 패스
        if (_isWheelInitialized && LogitechGSDK.LogiIsConnected(0)) yield break;

        Debug.Log("[PlayerMover] 로지텍 SDK 연결 시도 중...");

        while (!_isWheelInitialized)
        {
            // 1. 혹시 꼬여있을지 모르니 셧다운 먼저 호출 (초기화 실패 시 리셋 효과)
            LogitechGSDK.LogiSteeringShutdown();

            // 아주 잠깐 대기
            yield return null;

            // 2. 초기화 시도
            bool initResult = LogitechGSDK.LogiSteeringInitialize(false);

            // 3. 초기화가 됐다면 업데이트 한 번 돌려서 실제 연결 확인
            bool connectedResult = false;
            if (initResult)
            {
                // 업데이트를 한번 해줘야 IsConnected가 갱신되는 경우가 있음
                LogitechGSDK.LogiUpdate();
                connectedResult = LogitechGSDK.LogiIsConnected(0);
            }

            // 4. 최종 성공 판단
            if (initResult && connectedResult)
            {
                _isWheelInitialized = true;
                _isWheelConnected = true;
                Debug.Log("<color=green>[PlayerMover] 로지텍 SDK 초기화 및 연결 성공!</color>");

                // 성공 직후 포스 피드백 적용
                HandleForceFeedback();

                // 코루틴 변수 해제
                _initCoroutine = null;
                yield break; // 루프 종료
            }
            else
            {
                // 실패 시 경고 출력 후 2초 대기
                // Debug.LogWarning("[PlayerMover] 연결 실패. 2초 후 재시도...)");
                yield return new WaitForSeconds(2f);
            }
        }
    }

    private void Update()
    {
        CheckGameOver();

        // 1. SDK 업데이트 및 상태 체크
        UpdateLogitechState();
        HandleForceFeedback();

        // 2. 이동 불가 처리
        if (!canMove)
        {
            _input = Vector2.zero;
            if (_currentUiState != -1)
            {
                _currentUiState = -1;
                if (IngameUIManager.Instance != null) IngameUIManager.Instance.CloseArrowPanel();
            }
            _uiTimer = 0f;
            UpdatePrevButtonStates();
            return;
        }

        // 3. 입력 통합
        float h = GetCombinedHorizontalInput();
        float v = GetCombinedVerticalInput();
        _input = new Vector2(h, v);

        // 4. UI 및 스킬 입력
        HandleDirectionUI_PositionBased();
        HandleSkillInput();

        if (Input.GetKeyDown(KeyCode.Space)) ActivateShield();

        UpdatePrevButtonStates();
    }

    private void FixedUpdate()
    {
        if (!canMove) return;

#if UNITY_6000_0_OR_NEWER
        if (_input.y < 0) _rb.linearDamping = brakingDrag;
        else _rb.linearDamping = coastingDrag;
#else
        if (_input.y < 0) _rb.drag = brakingDrag;
        else _rb.drag = coastingDrag;
#endif

        Vector3 force = Vector3.zero;
        if (_input.y > 0) force += transform.forward * _input.y * forwardForce;
        else if (_input.y < 0) force += transform.forward * _input.y * reverseForce;
        force += transform.right * _input.x * strafeForce;

        _rb.AddForce(force, ForceMode.Force);

#if UNITY_6000_0_OR_NEWER
        if (_rb.linearVelocity.magnitude > maxSpeed)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
        }
#else
        if (_rb.velocity.magnitude > maxSpeed)
        {
            _rb.velocity = _rb.velocity.normalized * maxSpeed;
        }
#endif

        ApplyBoundaryLimit();
    }

    private void OnApplicationQuit()
    {
        if (_isWheelInitialized)
        {
            LogitechGSDK.LogiStopSpringForce(0);
            LogitechGSDK.LogiStopDamperForce(0);
            LogitechGSDK.LogiSteeringShutdown();
        }
    }
    #endregion

    #region Input & Force Feedback Methods
    private void UpdateLogitechState()
    {
        if (!useLogitechWheel || !_isWheelInitialized) return;

        // SDK 업데이트 실행
        bool updated = LogitechGSDK.LogiUpdate();

        // 연결 여부 재확인 (갑자기 USB 뽑혔을 때 대비)
        if (updated && LogitechGSDK.LogiIsConnected(0))
        {
            _isWheelConnected = true;
            _currentState = LogitechGSDK.LogiGetStateUnity(0);
        }
        else
        {
            // 연결이 끊겼다면 다시 재연결 시도 로직 트리거
            if (_isWheelConnected) // 연결되어 있다가 끊긴 순간
            {
                Debug.LogError("[PlayerMover] 로지텍 휠 연결 끊김!");
                _isWheelConnected = false;
                _isWheelInitialized = false;
                StartConnectionSequence(); // 재연결 시도 시작
            }
        }
    }

    private void UpdatePrevButtonStates()
    {
        if (!_isWheelConnected) return;
        for (int i = 0; i < 128; i++)
        {
            _prevButtonStates[i] = (_currentState.rgbButtons[i] == 128);
        }
    }

    private bool GetLogiButtonDown(int buttonIndex)
    {
        if (!_isWheelConnected) return false;
        bool isPressedNow = _currentState.rgbButtons[buttonIndex] == 128;
        bool wasPressedLastFrame = _prevButtonStates[buttonIndex];
        return isPressedNow && !wasPressedLastFrame;
    }

    private void HandleForceFeedback()
    {
        if (!_isWheelConnected) return;
        LogitechGSDK.LogiPlaySpringForce(0, 0, 50, centeringSpringStrength);
        LogitechGSDK.LogiPlayDamperForce(0, damperStrength);
    }

    private float GetCombinedHorizontalInput()
    {
        float keyboard = Input.GetAxis("Horizontal");
        float wheel = 0f;

        if (_isWheelConnected)
        {
            wheel = _currentState.lX / 32768f;
            if (Mathf.Abs(wheel) < wheelDeadzone) wheel = 0f;
        }
        return Mathf.Clamp(keyboard + wheel, -1f, 1f);
    }

    private float GetCombinedVerticalInput()
    {
        float keyboard = Input.GetAxis("Vertical");
        float pedals = 0f;

        if (_isWheelConnected)
        {
            float rawAccel = _currentState.rglSlider[0];
            float rawBrake = _currentState.lRz;

            float accelVal = (32767f - rawAccel) / 65535f;
            float brakeVal = (32767f - rawBrake) / 65535f;

            if (accelVal < wheelDeadzone) accelVal = 0f;
            if (brakeVal < wheelDeadzone) brakeVal = 0f;

            pedals = accelVal - brakeVal;
        }
        return Mathf.Clamp(keyboard + pedals, -1f, 1f);
    }
    #endregion

    #region Game Logic & UI
    private void HandleDirectionUI_PositionBased()
    {
        if (IngameUIManager.Instance == null) return;

#if UNITY_6000_0_OR_NEWER
        Vector3 velocity = _rb.linearVelocity;
#else
        Vector3 velocity = _rb.velocity;
#endif
        Vector3 localVel = transform.InverseTransformDirection(velocity);

        float sideThreshold = 2.0f;
        float forwardThreshold = 5.0f;

        int detectedState = -1;

        if (localVel.x < -sideThreshold) detectedState = 1;
        else if (localVel.x > sideThreshold) detectedState = 2;
        else if (localVel.z > forwardThreshold) detectedState = 0;

        if (detectedState != -1)
        {
            if (_currentUiState != detectedState)
            {
                _currentUiState = detectedState;
                IngameUIManager.Instance.OpenArrowPanel(_currentUiState);
            }
            _uiTimer = uiKeepTime;
        }
        else
        {
            if (_uiTimer > 0)
            {
                _uiTimer -= Time.deltaTime;
            }
            else
            {
                if (_currentUiState != -1)
                {
                    _currentUiState = -1;
                    IngameUIManager.Instance.CloseArrowPanel();
                }
            }
        }
    }

    public void SetMoveAction(bool value)
    {
        canMove = value;
        if (!value)
        {
            _input = Vector2.zero;
            if (IngameUIManager.Instance != null) IngameUIManager.Instance.CloseArrowPanel();
            _currentUiState = -1;
            _uiTimer = 0f;

            if (_rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                _rb.linearVelocity = Vector3.zero;
#else
                _rb.velocity = Vector3.zero;
#endif
                _rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void ApplyBoundaryLimit()
    {
#if UNITY_6000_0_OR_NEWER
        float velX = _rb.linearVelocity.x;
#else
        float velX = _rb.velocity.x;
#endif

        if (_rb.position.x < -xLimit && velX < 0)
        {
#if UNITY_6000_0_OR_NEWER
            Vector3 vel = _rb.linearVelocity; vel.x = 0; _rb.linearVelocity = vel;
#else
            Vector3 vel = _rb.velocity; vel.x = 0; _rb.velocity = vel;
#endif
            _rb.position = new Vector3(-xLimit, _rb.position.y, _rb.position.z);
        }
        else if (_rb.position.x > xLimit && velX > 0)
        {
#if UNITY_6000_0_OR_NEWER
            Vector3 vel = _rb.linearVelocity; vel.x = 0; _rb.linearVelocity = vel;
#else
            Vector3 vel = _rb.velocity; vel.x = 0; _rb.velocity = vel;
#endif
            _rb.position = new Vector3(xLimit, _rb.position.y, _rb.position.z);
        }
    }

    private void HandleSkillInput()
    {
        if (DataManager.Instance == null) return;

        if (Input.GetKeyDown(KeyCode.Z) || GetLogiButtonDown(11))
            OnClickBuffButton();

        if (Input.GetKeyDown(KeyCode.X) || GetLogiButtonDown(10))
            OnClickDebuffButton();

        if (Input.GetKeyDown(KeyCode.C) || GetLogiButtonDown(7))
        {
            if (IngameUIManager.Instance != null)
            {
                if (!IngameUIManager.Instance.GetDisplayPanel()) IngameUIManager.Instance.OnClickPauseButton();
                else
                {
                    if (GameManager.Instance.IsPaused) IngameUIManager.Instance.OnClickContinueButton();
                    else if (GameManager.Instance.IsFailed) IngameUIManager.Instance.OnClickRetryButton();
                    else if (outtroUIManager != null) outtroUIManager.GoHome();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.B) || GetLogiButtonDown(6))
        {
            if (IngameUIManager.Instance != null && IngameUIManager.Instance.GetDisplayPanel())
            {
                if (GameManager.Instance.IsPaused || GameManager.Instance.IsFailed) IngameUIManager.Instance.OnClickBackButton();
            }
        }
    }

    public void OnClickBuffButton()
    {
        if (DataManager.Instance.GetBuffer() >= DataManager.Instance.bufferUse)
        {
            int cost = DataManager.Instance.bufferUse;
            DataManager.Instance.SetBuffer(Mathf.Max(0, DataManager.Instance.GetBuffer() - cost));
            if (IngameUIManager.Instance != null) IngameUIManager.Instance.Log("Buff Activated");
        }
    }

    public void OnClickDebuffButton()
    {
        if (DataManager.Instance.GetDeBuffer() >= DataManager.Instance.debufferUse)
        {
            int cost = DataManager.Instance.debufferUse;
            DataManager.Instance.SetDeBuffer(Mathf.Max(0, DataManager.Instance.GetDeBuffer() - cost));
            if (IngameUIManager.Instance != null) IngameUIManager.Instance.Log("Debuff Activated");
        }
    }

    private void CheckGameOver()
    {
        if (DataManager.Instance != null && IngameUIManager.Instance != null)
        {
            if (DataManager.Instance.GetShipHealth() <= 0 && !IngameUIManager.Instance.GetDisplayPanel())
            {
                IngameUIManager.Instance.OnClickFailButton();
            }
        }
    }

    private void ActivateShield()
    {
        if (shieldEffect != null)
        {
            shieldEffect.SetActive(true);
            CancelInvoke(nameof(DeactivateShield));
            Invoke(nameof(DeactivateShield), 3f);
        }
    }

    private void DeactivateShield()
    {
        if (shieldEffect != null) shieldEffect.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            if (DataManager.Instance != null) DataManager.Instance.TakeDamage(20);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("ShieldHit");
            Destroy(other.gameObject);
        }
    }
    #endregion

    #region Debug & Gizmos (Requested Features)
    private void OnDrawGizmos()
    {
        // 1. 좌우 이동 제한 표시 (항상 표시됨)
        DrawMovementLimits();

        // 2. 도달 가능 반경 표시 (선택되지 않아도 표시하고 싶다면 여기 주석 해제)
        // DrawReachRadius(); 
    }

    private void OnDrawGizmosSelected()
    {
        // 도달 가능 반경은 오브젝트가 선택되었을 때만 표시 (화면 복잡도 감소)
        DrawReachRadius();
    }

    /// <summary>
    /// 좌우로 움직일 수 있는 범위를 녹색 라인으로 표시합니다.
    /// </summary>
    private void DrawMovementLimits()
    {
        Gizmos.color = Color.green;

        // 월드 좌표 기준 X축 제한선 계산
        // 차는 Z축으로 전진한다고 가정하고 길게 그려줍니다.
        float lineLength = 100f; // 앞뒤로 표시할 길이
        Vector3 center = transform.position;

        Vector3 leftLimitStart = new Vector3(-xLimit, center.y, center.z - lineLength / 2);
        Vector3 leftLimitEnd = new Vector3(-xLimit, center.y, center.z + lineLength);

        Vector3 rightLimitStart = new Vector3(xLimit, center.y, center.z - lineLength / 2);
        Vector3 rightLimitEnd = new Vector3(xLimit, center.y, center.z + lineLength);

        Gizmos.DrawLine(leftLimitStart, leftLimitEnd);
        Gizmos.DrawLine(rightLimitStart, rightLimitEnd);
    }

    /// <summary>
    /// 1초(Red) ~ 3초(Blue) 도달 예상 반경을 그라데이션 원으로 표시합니다.
    /// 
    /// </summary>
    private void DrawReachRadius()
    {
        if (!Application.isPlaying && maxSpeed <= 0) return; // 에디터 모드에서 maxSpeed가 0이면 그리지 않음

        Vector3 center = transform.position;
        float speed = maxSpeed;

        // 런타임이면 실제 현재 속도를 반영할 수도 있지만, 
        // "갈 수 있는 반경"을 묻는 것이므로 MaxSpeed 기준이 적합합니다.

        int steps = 20; // 그라데이션 부드러움 정도
        float minTime = 1.0f; // 빨간색 시작 시간 (1초)
        float maxTime = 3.0f; // 파란색 끝 시간 (3초)

        for (int i = 0; i <= steps; i++)
        {
            // 0 ~ 1 사이 진행률
            float t = (float)i / steps;

            // 시간 계산 (1초 ~ 3초)
            float currentTime = Mathf.Lerp(minTime, maxTime, t);

            // 색상 계산 (Red -> Blue)
            Color currentColor = Color.Lerp(Color.red, Color.blue, t);

            // 반경 계산 (거리 = 속도 * 시간)
            float currentRadius = speed * currentTime;

            Gizmos.color = currentColor;
            DrawGizmoCircle(center, currentRadius);
        }
    }

    /// <summary>
    /// XZ 평면에 원을 그리는 헬퍼 함수
    /// </summary>
    private void DrawGizmoCircle(Vector3 center, float radius)
    {
        int segments = 36;
        float angleStep = 360f / segments;

        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            Vector3 nextPoint = center + new Vector3(x, 0, z);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
    #endregion
}