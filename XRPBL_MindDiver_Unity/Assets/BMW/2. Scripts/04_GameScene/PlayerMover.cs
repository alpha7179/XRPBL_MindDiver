using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// PlayerMover - 범용 캘리브레이션 + Y축 이동(Q/E, 클러치/브레이크) 추가 버전
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
    [Tooltip("상하 이동 힘")]
    [SerializeField] private float liftForce = 2000f; // [추가] 상승/하강 힘
    [SerializeField] private float maxSpeed = 30f;

    [Header("Boundaries")]
    [SerializeField] private float xLimit = 12f;
    [Tooltip("Y축 이동 제한 범위")]
    [SerializeField] private float yLimit = 8f; // [추가] Y축 범위

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
    // [변경] Vector2 -> Vector3 (x:조향, y:상하, z:전진)
    private Vector3 _input;

    private bool _isWheelInitialized = false;
    private bool _isWheelConnected = false;
    private LogitechGSDK.DIJOYSTATE2ENGINES _currentState;

    private bool[] _prevButtonStates = new bool[128];
    private int _currentUiState = -1;
    private Coroutine _initCoroutine;
    private OuttroUIManager outtroUIManager;

    private int _calibratedCenter = 0;
    private bool _hasCalibrated = false;

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

        // [수정] Y축 이동을 위해 FreezePositionY 제거 (회전만 고정)
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

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
                    PerformCalibration();
                    HandleForceFeedback();
                    _initCoroutine = null;
                    yield break;
                }
            }
            yield return new WaitForSeconds(2f);
        }
    }

    [ContextMenu("Calibrate Center Now")]
    public void PerformCalibration()
    {
        if (!_isWheelConnected) return;
        _calibratedCenter = _currentState.lX;
        _hasCalibrated = true;
        Debug.Log($"<color=cyan>[Calibration] 중앙점 설정 완료! ({_calibratedCenter})</color>");
    }

    private void Update()
    {
        CheckGameOver();
        UpdateLogitechState();
        HandleForceFeedback();

        if (Input.GetKeyDown(KeyCode.H)) PerformCalibration();

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

        // 드래그 적용 (전진 키 안 누를 때 감속) - z축 기준
#if UNITY_6000_0_OR_NEWER
        _rb.linearDamping = (_input.z <= 0) ? brakingDrag : coastingDrag;
#else
        _rb.drag = (_input.z <= 0) ? brakingDrag : coastingDrag;
#endif

        Vector3 force = Vector3.zero;

        // 1. 전후 이동 (Z축)
        if (_input.z > 0) force += transform.forward * _input.z * forwardForce;
        else if (_input.z < 0) force += transform.forward * _input.z * reverseForce;

        // 2. 좌우 이동 (X축)
        force += transform.right * _input.x * strafeForce;

        // 3. 상하 이동 (Y축) [추가]
        // _input.y > 0 (상승), _input.y < 0 (하강)
        force += transform.up * _input.y * liftForce;

        _rb.AddForce(force, ForceMode.Force);

        // 속도 제한
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

    #region Input Logic

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

    // [수정] Vector2 -> Vector3 반환
    private Vector3 GetInputFromKeyboardOnly()
    {
        float x = 0f;
        float y = 0f; // 상하
        float z = 0f; // 전후

        // 좌우
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) x -= 1f;

        // 전후
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) z += 1f;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) z -= 1f;

        // [추가] 상하 (Q: 상승, E: 하강)
        if (Input.GetKey(KeyCode.Q)) y += 1f;
        if (Input.GetKey(KeyCode.E)) y -= 1f;

        return new Vector3(x, y, z);
    }

    // [수정] Vector2 -> Vector3 반환
    private Vector3 GetInputFromWheelOnly()
    {
        float x = 0f;
        float y = 0f;
        float z = 0f;

        if (!_hasCalibrated) PerformCalibration();

        // 1. 조향 (X)
        int rawX = _currentState.lX;
        float diff = rawX - _calibratedCenter;
        x = diff / LOGITECH_RANGE;
        if (Mathf.Abs(x) < wheelDeadzone) x = 0f;
        x = Mathf.Clamp(x, -1f, 1f);

        // 2. 페달 매핑
        // rglSlider[0]: 엑셀 -> 전진 (Z)
        // lRz: 브레이크 -> 하강 (Y-) [요청사항]
        // rglSlider[1]: 클러치 -> 상승 (Y+) [요청사항]

        float rawAccel = _currentState.rglSlider[0];
        float rawBrake = _currentState.lRz;
        float rawClutch = _currentState.rglSlider[1]; // 클러치는 보통 slider[1]

        float accelVal = (32767f - rawAccel) / LOGITECH_MAX;
        float brakeVal = (32767f - rawBrake) / LOGITECH_MAX;
        float clutchVal = (32767f - rawClutch) / LOGITECH_MAX;

        if (accelVal < wheelDeadzone) accelVal = 0f;
        if (brakeVal < wheelDeadzone) brakeVal = 0f;
        if (clutchVal < wheelDeadzone) clutchVal = 0f;

        // 전진은 엑셀
        z = accelVal;

        // 상하는 클러치(Up) - 브레이크(Down)
        y = clutchVal - brakeVal;

        return new Vector3(x, y, z);
    }

    private void HandleForceFeedback()
    {
        if (!_isWheelConnected) return;
        LogitechGSDK.LogiPlaySpringForce(0, 0, 50, centeringSpringStrength);
        LogitechGSDK.LogiPlayDamperForce(0, damperStrength);
    }

    // ... Button Logic ...
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

        // 키보드 UI 로직 업데이트
        bool isLeft = _input.x < -0.1f;
        bool isRight = _input.x > 0.1f;
        bool isForward = _input.z > 0.1f; // z축으로 판단

        if (isLeft) targetState = ID_LEFT;
        else if (isRight) targetState = ID_RIGHT;
        else if (isForward) targetState = ID_FORWARD;
        UpdateArrowPanelState(targetState);
    }

    private void HandleDirectionUI_Wheel()
    {
        if (IngameUIManager.Instance == null) return;
        int targetState = ID_NONE;

        // 휠 UI 로직 업데이트
        if (_input.x < -wheelSteerThreshold) targetState = ID_LEFT;
        else if (_input.x > wheelSteerThreshold) targetState = ID_RIGHT;
        else if (_input.z > pedalAccelThreshold) targetState = ID_FORWARD; // z축(엑셀) 체크
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
        _input = Vector3.zero;
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
        Vector3 vel = _rb.linearVelocity;
#else
        Vector3 vel = _rb.velocity;
#endif

        // X축 제한
        if (_rb.position.x < -xLimit && vel.x < 0)
        {
            vel.x = 0;
            _rb.position = new Vector3(-xLimit, _rb.position.y, _rb.position.z);
        }
        else if (_rb.position.x > xLimit && vel.x > 0)
        {
            vel.x = 0;
            _rb.position = new Vector3(xLimit, _rb.position.y, _rb.position.z);
        }

        // [추가] Y축 제한
        if (_rb.position.y < -yLimit && vel.y < 0)
        {
            vel.y = 0;
            _rb.position = new Vector3(_rb.position.x, -yLimit, _rb.position.z);
        }
        else if (_rb.position.y > yLimit && vel.y > 0)
        {
            vel.y = 0;
            _rb.position = new Vector3(_rb.position.x, yLimit, _rb.position.z);
        }

        // 속도 재적용
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = vel;
#else
        _rb.velocity = vel;
#endif
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
    private void OnTriggerEnter(Collider other) { if (other.CompareTag("Obstacle")) { if (DataManager.Instance != null) DataManager.Instance.TakeDamage(20); if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SFXType.ShieldHit); Destroy(other.gameObject); } }
    #endregion

    // [추가] 기즈모로 이동 범위 표시
    private void OnDrawGizmos()
    {
        // X, Y 한계 범위를 박스로 시각화
        Gizmos.color = Color.yellow;

        // 중심점은 (0, 0, 현재Z) 라고 가정하고 그리거나, 플레이어 기준이면 플레이어 Z 사용
        // 여기서는 월드 중심(0,0)을 기준으로 범위를 표시합니다 (게임 로직이 -limit ~ limit 이므로)
        // 만약 플레이어가 계속 전진하는 게임이라면 Z축은 길게 그립니다.
        Vector3 center = new Vector3(0, 0, transform.position.z);
        Vector3 size = new Vector3(xLimit * 2, yLimit * 2, 10f); // Z축 길이는 시각화용

        Gizmos.DrawWireCube(center, size);
    }

#if UNITY_EDITOR
    #region Debug GUI
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 25;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(20, 20, 500, 30), $"연결: {_isWheelConnected} | 보정: {_calibratedCenter}", style);
        GUI.Label(new Rect(20, 50, 500, 30), $"Handle(lX): {_currentState.lX}", style);
        GUI.Label(new Rect(20, 80, 500, 30), $"Input(X/Y/Z): {_input.x:F1} / {_input.y:F1} / {_input.z:F1}", style);
        GUI.Label(new Rect(20, 110, 500, 30), $"[H] 중앙점 재설정", style);
    }
    #endregion
#endif
}