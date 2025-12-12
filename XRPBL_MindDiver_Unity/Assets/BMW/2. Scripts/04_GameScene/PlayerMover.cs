using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 물리 엔진 기반(Rigidbody Physics) 플레이어 이동 클래스
/// 수정사항: 화살표 상태 초기화 로직 강화 (입력 없으면 즉시 소멸 보장)
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

    [Header("UI Trigger Settings")]
    [Tooltip("휠 사용 시 화살표가 뜨는 조향각 임계값 (0.0 ~ 1.0)")]
    [SerializeField] private float wheelSteerThreshold = 0.15f;
    [Tooltip("페달 사용 시 화살표가 뜨는 엑셀 임계값 (0.0 ~ 1.0)")]
    [SerializeField] private float pedalAccelThreshold = 0.1f;

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

    // 버튼 상태
    private bool[] _prevButtonStates = new bool[128];

    // [UI 상태] -1: 꺼짐, 1: 전진, 2: 왼쪽, 3: 오른쪽
    private int _currentUiState = -1;

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

#if UNITY_6000_0_OR_NEWER
        _rb.linearDamping = coastingDrag;
#else
        _rb.drag = coastingDrag;
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
        if (focus && useLogitechWheel && !_isWheelInitialized)
        {
            StartConnectionSequence();
        }
    }

    private void StartConnectionSequence()
    {
        if (_initCoroutine != null) StopCoroutine(_initCoroutine);
        _initCoroutine = StartCoroutine(InitializeLogitechSDK());
    }

    private IEnumerator InitializeLogitechSDK()
    {
        if (_isWheelInitialized && LogitechGSDK.LogiIsConnected(0)) yield break;

        Debug.Log("[PlayerMover] 로지텍 SDK 연결 시도 중...");

        while (!_isWheelInitialized)
        {
            LogitechGSDK.LogiSteeringShutdown();
            yield return null;

            bool initResult = LogitechGSDK.LogiSteeringInitialize(false);
            bool connectedResult = false;

            if (initResult)
            {
                LogitechGSDK.LogiUpdate();
                connectedResult = LogitechGSDK.LogiIsConnected(0);
            }

            if (initResult && connectedResult)
            {
                _isWheelInitialized = true;
                _isWheelConnected = true;
                Debug.Log("<color=green>[PlayerMover] 로지텍 SDK 초기화 및 연결 성공!</color>");
                HandleForceFeedback();
                _initCoroutine = null;
                yield break;
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }
        }
    }

    private void Update()
    {
        CheckGameOver();

        UpdateLogitechState();
        HandleForceFeedback();

        if (!canMove)
        {
            ResetMovementAndUI();
            UpdatePrevButtonStates();
            return;
        }

        float h = GetCombinedHorizontalInput();
        float v = GetCombinedVerticalInput();
        _input = new Vector2(h, v);

        // UI 화살표 로직
        if (_isWheelConnected)
            HandleDirectionUI_Wheel();
        else
            HandleDirectionUI_Keyboard();

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
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
#else
        if (_rb.velocity.magnitude > maxSpeed)
            _rb.velocity = _rb.velocity.normalized * maxSpeed;
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

    #region Input Methods
    private void UpdateLogitechState()
    {
        if (!useLogitechWheel || !_isWheelInitialized) return;

        bool updated = LogitechGSDK.LogiUpdate();

        if (updated && LogitechGSDK.LogiIsConnected(0))
        {
            _isWheelConnected = true;
            _currentState = LogitechGSDK.LogiGetStateUnity(0);
        }
        else
        {
            if (_isWheelConnected)
            {
                Debug.LogError("[PlayerMover] 로지텍 휠 연결 끊김!");
                _isWheelConnected = false;
                _isWheelInitialized = false;
                StartConnectionSequence();
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
            wheel = ((_currentState.lX / 65535f) * 2f) - 1f;
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

    #region UI Logic (Strict Mode)

    // IngameUIManager 인덱스 매칭: 1(앞), 2(왼), 3(오)
    private const int ID_NONE = -1;
    private const int ID_FORWARD = 1;
    private const int ID_LEFT = 2;
    private const int ID_RIGHT = 3;

    /// <summary>
    /// 키보드 입력 UI 처리 (양자택일 적용)
    /// </summary>
    private void HandleDirectionUI_Keyboard()
    {
        if (IngameUIManager.Instance == null) return;

        // [핵심] 일단 꺼짐(NONE)으로 초기화
        int targetState = ID_NONE;

        bool isLeft = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        bool isRight = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        bool isUp = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);

        // 1. 좌우 체크
        if (isLeft && !isRight)
        {
            targetState = ID_LEFT;
        }
        else if (isRight && !isLeft)
        {
            targetState = ID_RIGHT;
        }
        // 2. 좌우가 아닐 때만 전진 체크
        else if (isUp)
        {
            targetState = ID_FORWARD;
        }

        // 아무 키도 안 누르면 초기값인 ID_NONE이 유지됨 -> 화살표 꺼짐

        UpdateArrowPanelState(targetState);
    }

    /// <summary>
    /// 휠/페달 입력 UI 처리 (임계값 미만 시 즉시 소멸)
    /// </summary>
    private void HandleDirectionUI_Wheel()
    {
        if (IngameUIManager.Instance == null) return;

        // [핵심] 일단 꺼짐(NONE)으로 초기화
        int targetState = ID_NONE;

        // 1. 핸들 (좌우) 우선 체크
        if (_input.x < -wheelSteerThreshold)
        {
            targetState = ID_LEFT;
        }
        else if (_input.x > wheelSteerThreshold)
        {
            targetState = ID_RIGHT;
        }
        // 2. 핸들이 중립(임계값 이내)이면 엑셀 체크
        // _input.y 값이 pedalAccelThreshold보다 커야만 ID_FORWARD가 됨.
        // 작거나 같으면 조건문에 걸리지 않아 초기값인 ID_NONE이 유지됨.
        else if (_input.y > pedalAccelThreshold)
        {
            targetState = ID_FORWARD;
        }

        UpdateArrowPanelState(targetState);
    }

    private void UpdateArrowPanelState(int newState)
    {
        // 상태 변경 시에만 UI 매니저 호출
        if (_currentUiState != newState)
        {
            _currentUiState = newState;

            if (_currentUiState != ID_NONE)
            {
                IngameUIManager.Instance.OpenArrowPanel(_currentUiState);
            }
            else
            {
                // ID_NONE일 경우 무조건 닫기
                IngameUIManager.Instance.CloseArrowPanel();
            }
        }
    }

    private void ResetMovementAndUI()
    {
        _input = Vector2.zero;
        if (IngameUIManager.Instance != null) IngameUIManager.Instance.CloseArrowPanel();
        _currentUiState = ID_NONE;

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
    #endregion

    #region Game Logic & Skill
    public void SetMoveAction(bool value)
    {
        canMove = value;
        if (!value)
        {
            ResetMovementAndUI();
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
            if (IngameUIManager.Instance != null)
            {
                IngameUIManager.Instance.Log("Buff Activated");
                DataManager.Instance.SetShipShield(DataManager.Instance.maxShipShield);
            }
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
}