using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// PlayerMover - 범용 캘리브레이션 적용 버전
/// 해결: 데이터가 Signed(-32768~32767)로 들어오든 Unsigned(0~65535)로 들어오든
/// 현재 위치를 무조건 중앙으로 인식하여 왼쪽 쏠림 해결.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMover : MonoBehaviour
{
    public static PlayerMover Instance { get; private set; }

    #region Inspector Fields
    [Header("Control Settings")]
    [SerializeField] private bool canMove = true;

    [Header("Physics Settings")]
    [SerializeField] private float forwardForce = 3000f;
    [SerializeField] private float reverseForce = 2000f;
    [SerializeField] private float strafeForce = 2500f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float xLimit = 12f;

    [Header("Drag Settings")]
    [SerializeField] private float coastingDrag = 1f;
    [SerializeField] private float brakingDrag = 3f;

    [Header("Logitech Settings")]
    [SerializeField] private float wheelDeadzone = 0.05f;
    [SerializeField] private bool useLogitechWheel = true;

    [Header("Force Feedback")]
    [Range(0, 100)][SerializeField] private int centeringSpringStrength = 50;
    [Range(0, 100)][SerializeField] private int damperStrength = 30;

    [Header("UI Trigger")]
    [SerializeField] private float wheelSteerThreshold = 0.15f;
    [SerializeField] private float pedalAccelThreshold = 0.1f;

    [Header("References")]
    [SerializeField] private GameObject shieldEffect;
    #endregion

    #region Private Fields
    private Rigidbody _rb;
    private Vector2 _input;

    private bool _isWheelInitialized = false;
    private bool _isWheelConnected = false;
    private LogitechGSDK.DIJOYSTATE2ENGINES _currentState;

    private bool[] _prevButtonStates = new bool[128];
    private int _currentUiState = -1;
    private Coroutine _initCoroutine;
    private OuttroUIManager outtroUIManager;

    // [핵심] 캘리브레이션 변수
    // 어떤 값이 들어오든 시작 위치를 저장할 변수
    private int _calibratedCenter = 0;
    private bool _hasCalibrated = false; // 보정 완료 여부

    // 로지텍 휠의 한쪽 회전 범위 (약 32767)
    private const float LOGITECH_RANGE = 32767f;
    private const float LOGITECH_MAX = 65535f;
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

        _currentState = new LogitechGSDK.DIJOYSTATE2ENGINES();
    }

    private void Start()
    {
        if (useLogitechWheel) StartConnectionSequence();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus && useLogitechWheel && !_isWheelInitialized) StartConnectionSequence();
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

            if (LogitechGSDK.LogiSteeringInitialize(false))
            {
                yield return null;
                LogitechGSDK.LogiUpdate();

                if (LogitechGSDK.LogiIsConnected(0))
                {
                    _isWheelInitialized = true;
                    _isWheelConnected = true;
                    _currentState = LogitechGSDK.LogiGetStateUnity(0);

                    // [강제 보정] 연결 즉시 현재 값을 중앙으로 설정
                    // 값이 -80이든 0이든 32767이든 상관없음. 현재 위치 = 중앙.
                    PerformCalibration();

                    HandleForceFeedback();
                    _initCoroutine = null;
                    yield break;
                }
            }
            yield return new WaitForSeconds(2f);
        }
    }

    // 외부에서 호출하거나 키 입력으로 보정 수행
    [ContextMenu("Calibrate Center Now")]
    public void PerformCalibration()
    {
        if (!_isWheelConnected) return;

        // 현재 휠 값을 가져와서 '중앙'으로 저장
        _calibratedCenter = _currentState.lX;
        _hasCalibrated = true;

        Debug.Log($"<color=cyan>[Calibration] 중앙점 설정 완료! 현재 값({_calibratedCenter})을 0점으로 인식합니다.</color>");
    }

    private void Update()
    {
        CheckGameOver();
        UpdateLogitechState();
        HandleForceFeedback();

        // [비상 키] 게임 중 핸들이 틀어지면 H키를 눌러서 재보정
        if (Input.GetKeyDown(KeyCode.H))
        {
            PerformCalibration();
        }

        if (!canMove)
        {
            ResetMovementAndUI();
            UpdatePrevButtonStates();
            return;
        }

        if (_isWheelConnected)
        {
            _input = GetInputFromWheelOnly();
            HandleDirectionUI_Wheel();
        }
        else
        {
            _input = GetInputFromKeyboardOnly();
            HandleDirectionUI_Keyboard();
        }

        HandleSkillInput();

        if (Input.GetKeyDown(KeyCode.Space)) ActivateShield();
        UpdatePrevButtonStates();
    }

    private void FixedUpdate()
    {
        if (!canMove) return;

#if UNITY_6000_0_OR_NEWER
        _rb.linearDamping = (_input.y < 0) ? brakingDrag : coastingDrag;
#else
        _rb.drag = (_input.y < 0) ? brakingDrag : coastingDrag;
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

    #region Input Logic (범용 보정 적용)

    private void UpdateLogitechState()
    {
        if (!useLogitechWheel || !_isWheelInitialized) return;

        if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
        {
            _isWheelConnected = true;
            _currentState = LogitechGSDK.LogiGetStateUnity(0);
        }
        else if (_isWheelConnected)
        {
            _isWheelConnected = false;
            _isWheelInitialized = false;
            _hasCalibrated = false;
            StartConnectionSequence();
        }
    }

    private Vector2 GetInputFromKeyboardOnly()
    {
        float x = 0f;
        float y = 0f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) y += 1f;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) y -= 1f;
        return new Vector2(x, y);
    }

    private Vector2 GetInputFromWheelOnly()
    {
        float x = 0f;
        float y = 0f;

        // 보정이 안되었다면 현재 값을 바로 중앙으로 잡음 (안전장치)
        if (!_hasCalibrated) PerformCalibration();

        int rawX = _currentState.lX;

        // [범용 계산식]
        // (현재값 - 중앙값) / 회전범위
        // 예: 중앙이 -80일 때, 입력이 -80이면 -> 0 / 32767 = 0 (정지)
        // 예: 중앙이 -80일 때, 입력이 -32767(왼쪽)이면 -> -32687 / 32767 = 약 -1 (좌회전)
        float diff = rawX - _calibratedCenter;
        x = diff / LOGITECH_RANGE;

        if (Mathf.Abs(x) < wheelDeadzone) x = 0f;
        x = Mathf.Clamp(x, -1f, 1f);

        // 페달 로직
        float rawAccel = _currentState.rglSlider[0];
        float rawBrake = _currentState.lRz;

        // 페달은 보통 Unsigned(0~65535)로 들어오지만 기기에 따라 다를 수 있음
        // 중앙값 로직보다는 기존 정규화 유지하되 값 확인 필요
        float accelVal = (32767f - rawAccel) / LOGITECH_MAX;
        float brakeVal = (32767f - rawBrake) / LOGITECH_MAX;

        if (accelVal < wheelDeadzone) accelVal = 0f;
        if (brakeVal < wheelDeadzone) brakeVal = 0f;
        y = accelVal - brakeVal;

        return new Vector2(x, y);
    }

    // ... UI, ForceFeedback, Button 등 기존 동일 코드 ...

    private void HandleForceFeedback()
    {
        if (!_isWheelConnected) return;
        LogitechGSDK.LogiPlaySpringForce(0, 0, 50, centeringSpringStrength);
        LogitechGSDK.LogiPlayDamperForce(0, damperStrength);
    }
    private void UpdatePrevButtonStates()
    {
        if (!_isWheelConnected) return;
        for (int i = 0; i < 128; i++) _prevButtonStates[i] = (_currentState.rgbButtons[i] == 128);
    }
    private bool GetLogiButtonDown(int buttonIndex)
    {
        if (!_isWheelConnected) return false;
        return (_currentState.rgbButtons[buttonIndex] == 128) && !_prevButtonStates[buttonIndex];
    }

    // UI Logic
    private const int ID_NONE = -1;
    private const int ID_FORWARD = 1;
    private const int ID_LEFT = 2;
    private const int ID_RIGHT = 3;
    private void HandleDirectionUI_Keyboard()
    {
        if (IngameUIManager.Instance == null) return;
        int targetState = ID_NONE;
        bool isLeft = _input.x < -0.1f;
        bool isRight = _input.x > 0.1f;
        bool isUp = _input.y > 0.1f;
        if (isLeft) targetState = ID_LEFT;
        else if (isRight) targetState = ID_RIGHT;
        else if (isUp) targetState = ID_FORWARD;
        UpdateArrowPanelState(targetState);
    }
    private void HandleDirectionUI_Wheel()
    {
        if (IngameUIManager.Instance == null) return;
        int targetState = ID_NONE;
        if (_input.x < -wheelSteerThreshold) targetState = ID_LEFT;
        else if (_input.x > wheelSteerThreshold) targetState = ID_RIGHT;
        else if (_input.y > pedalAccelThreshold) targetState = ID_FORWARD;
        UpdateArrowPanelState(targetState);
    }
    private void UpdateArrowPanelState(int newState)
    {
        if (_currentUiState != newState)
        {
            _currentUiState = newState;
            if (_currentUiState != ID_NONE) IngameUIManager.Instance.OpenArrowPanel(_currentUiState);
            else IngameUIManager.Instance.CloseArrowPanel();
        }
    }

    // Helper
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
    public void SetMoveAction(bool value) { canMove = value; if (!value) ResetMovementAndUI(); }
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
        if (Input.GetKeyDown(KeyCode.Z) || GetLogiButtonDown(11)) OnClickBuffButton();
        if (Input.GetKeyDown(KeyCode.X) || GetLogiButtonDown(10)) OnClickDebuffButton();
        if (Input.GetKeyDown(KeyCode.C) || GetLogiButtonDown(7))
        {
            if (IngameUIManager.Instance != null)
            {
                if (!IngameUIManager.Instance.GetDisplayPanel()) IngameUIManager.Instance.OnClickPauseButton();
                else { if (GameManager.Instance.IsPaused) IngameUIManager.Instance.OnClickContinueButton(); else if (GameManager.Instance.IsFailed) IngameUIManager.Instance.OnClickRetryButton(); else if (outtroUIManager != null) outtroUIManager.GoHome(); }
            }
        }
        if (Input.GetKeyDown(KeyCode.B) || GetLogiButtonDown(6)) { if (IngameUIManager.Instance != null && IngameUIManager.Instance.GetDisplayPanel()) { if (GameManager.Instance.IsPaused || GameManager.Instance.IsFailed) IngameUIManager.Instance.OnClickBackButton(); } }
    }
    public void OnClickBuffButton() { if (DataManager.Instance.GetBuffer() >= DataManager.Instance.bufferUse) { int cost = DataManager.Instance.bufferUse; DataManager.Instance.SetBuffer(Mathf.Max(0, DataManager.Instance.GetBuffer() - cost)); if (IngameUIManager.Instance != null) { IngameUIManager.Instance.Log("Buff Activated"); DataManager.Instance.SetShipShield(DataManager.Instance.maxShipShield); } } }
    public void OnClickDebuffButton() { if (DataManager.Instance.GetDeBuffer() >= DataManager.Instance.debufferUse) { int cost = DataManager.Instance.debufferUse; DataManager.Instance.SetDeBuffer(Mathf.Max(0, DataManager.Instance.GetDeBuffer() - cost)); if (IngameUIManager.Instance != null) IngameUIManager.Instance.Log("Debuff Activated"); } }
    private void CheckGameOver() { if (DataManager.Instance != null && IngameUIManager.Instance != null) { if (DataManager.Instance.GetShipHealth() <= 0 && !IngameUIManager.Instance.GetDisplayPanel()) { IngameUIManager.Instance.OnClickFailButton(); } } }
    private void ActivateShield() { if (shieldEffect != null) { shieldEffect.SetActive(true); CancelInvoke(nameof(DeactivateShield)); Invoke(nameof(DeactivateShield), 3f); } }
    private void DeactivateShield() { if (shieldEffect != null) shieldEffect.SetActive(false); }
    private void OnTriggerEnter(Collider other) { if (other.CompareTag("Obstacle")) { if (DataManager.Instance != null) DataManager.Instance.TakeDamage(20); if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("ShieldHit"); Destroy(other.gameObject); } }
    #endregion


#if UNITY_EDITOR
    #region Debug GUI
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 25;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(20, 20, 500, 30), $"연결: {_isWheelConnected} | 중앙 보정값: {_calibratedCenter}", style);
        GUI.Label(new Rect(20, 50, 500, 30), $"현재 핸들값: {_currentState.lX}", style);
        GUI.Label(new Rect(20, 80, 500, 30), $"최종 입력: {_input.x:F2}", style);
        GUI.Label(new Rect(20, 110, 500, 30), $"[H] 키를 눌러 중앙점 재설정 가능", style);
    }
    #endregion
#endif
}