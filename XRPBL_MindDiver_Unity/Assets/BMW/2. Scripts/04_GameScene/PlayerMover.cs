using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// PlayerMover - 최종 수정 (키보드 조작 버그 해결)
/// 해결: 휠 미연결 시 키보드 입력값을 _input 변수에 할당하는 코드 누락 수정
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMover : MonoBehaviour
{
    public static PlayerMover Instance { get; private set; }

    public enum LogitechAxis
    {
        lX, lY, lZ, lRx, lRy, lRz, rglSlider0, rglSlider1
    }

    #region Inspector Fields
    [Header("Control Settings")]
    [SerializeField] private bool canMove = true;

    [Header("Steering")]
    public bool steeringCenterIsZero = true;

    [Header("Pedal Configuration (직접 값 입력)")]
    public LogitechAxis accelAxis = LogitechAxis.rglSlider0;
    public float accelMin = 0f;
    public float accelMax = 65535f;

    public LogitechAxis brakeAxis = LogitechAxis.lRz;
    public float brakeMin = 32767f;
    public float brakeMax = -32768f;

    public LogitechAxis clutchAxis = LogitechAxis.lY;
    public float clutchMin = 32767f;
    public float clutchMax = -32768f;

    [Header("Physics Settings")]
    [SerializeField] private float forwardForce = 3000f;
    [SerializeField] private float reverseForce = 2000f;
    [SerializeField] private float strafeForce = 2500f;
    [SerializeField] private float liftForce = 2000f;
    [SerializeField] private float maxSpeed = 30f;

    [Header("Boundaries")]
    [SerializeField] private float xLimit = 12f;
    [SerializeField] private float yLimit = 8f;

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
    private Vector3 _input;

    private bool _isWheelInitialized = false;
    private bool _isWheelConnected = false;
    private LogitechGSDK.DIJOYSTATE2ENGINES _currentState;

    private bool[] _prevButtonStates = new bool[128];
    private int _currentUiState = -1;
    private Coroutine _initCoroutine;
    private OuttroUIManager outtroUIManager;

    private const int PEDAL_RELEASED = 32767;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _rb = GetComponent<Rigidbody>();
        outtroUIManager = GetComponent<OuttroUIManager>();

        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

#if UNITY_6000_0_OR_NEWER
        _rb.linearDamping = coastingDrag;
#else
        _rb.drag = coastingDrag;
#endif
        _rb.mass = 1000f;

        _currentState = new LogitechGSDK.DIJOYSTATE2ENGINES();
        InitializeStructDefaults();
    }

    private void Start()
    {
        if (GameManager.Instance.currentPlayState == GameManager.PlayState.Play) useLogitechWheel = true;
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

                    if (_currentState.rglSlider[0] == 0) _currentState.rglSlider[0] = PEDAL_RELEASED;
                    if (_currentState.lRz == 0) _currentState.lRz = PEDAL_RELEASED;

                    HandleForceFeedback();
                    _initCoroutine = null;
                    yield break;
                }
            }
            yield return new WaitForSeconds(2f);
        }
    }

    private void InitializeStructDefaults()
    {
        if (_currentState.rglSlider == null) _currentState.rglSlider = new int[4];
        if (_currentState.rgbButtons == null) _currentState.rgbButtons = new byte[128];
        if (_currentState.rgdwPOV == null) _currentState.rgdwPOV = new uint[4];

        _currentState.lX = steeringCenterIsZero ? 0 : 32767;
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

        if (_isWheelConnected)
        {
            _input = GetInputFromWheelOnly();
            HandleDirectionUI_Wheel();
        }
        else
        {
            // [수정 완료] 키보드 입력 함수 호출 추가
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
        _rb.linearDamping = (_input.z <= 0) ? brakingDrag : coastingDrag;
#else
        _rb.drag = (_input.z <= 0) ? brakingDrag : coastingDrag;
#endif

        Vector3 force = Vector3.zero;
        if (_input.z > 0) force += transform.forward * _input.z * forwardForce;
        else if (_input.z < 0) force += transform.forward * _input.z * reverseForce;

        force += transform.right * _input.x * strafeForce;
        force += transform.up * _input.y * liftForce;

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
            StartConnectionSequence();
        }
    }

    private int GetAxisValue(LogitechAxis axis)
    {
        switch (axis)
        {
            case LogitechAxis.lX: return _currentState.lX;
            case LogitechAxis.lY: return _currentState.lY;
            case LogitechAxis.lZ: return _currentState.lZ;
            case LogitechAxis.lRx: return _currentState.lRx;
            case LogitechAxis.lRy: return _currentState.lRy;
            case LogitechAxis.lRz: return _currentState.lRz;
            case LogitechAxis.rglSlider0: return _currentState.rglSlider[0];
            case LogitechAxis.rglSlider1: return _currentState.rglSlider[1];
            default: return 0;
        }
    }

    private float CalculatePedalInput(int rawValue, float min, float max)
    {
        float val = Mathf.InverseLerp(min, max, rawValue);
        if (val < wheelDeadzone) val = 0f;
        return val;
    }

    private Vector3 GetInputFromKeyboardOnly()
    {
        float x = 0f; float y = 0f; float z = 0f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) x -= 1f;

        // 전후
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) z += 1f;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) z -= 1f;

        // 상하 (Q/E)
        if (Input.GetKey(KeyCode.Q)) y += 1f;
        if (Input.GetKey(KeyCode.E)) y -= 1f;

        return new Vector3(x, y, z);
    }

    private Vector3 GetInputFromWheelOnly()
    {
        float x = 0f; float y = 0f; float z = 0f;

        int rawX = _currentState.lX;
        if (steeringCenterIsZero) x = rawX / 32767f;
        else x = (rawX - 32767f) / 32767f;

        if (Mathf.Abs(x) < wheelDeadzone) x = 0f;
        x = Mathf.Clamp(x, -1f, 1f);

        float accelVal = CalculatePedalInput(GetAxisValue(accelAxis), accelMin, accelMax);
        float brakeVal = CalculatePedalInput(GetAxisValue(brakeAxis), brakeMin, brakeMax);
        float clutchVal = CalculatePedalInput(GetAxisValue(clutchAxis), clutchMin, clutchMax);

        z = accelVal;
        y = clutchVal - brakeVal;

        return new Vector3(x, y, z);
    }

    private void HandleForceFeedback() { if (!_isWheelConnected) return; LogitechGSDK.LogiPlaySpringForce(0, 0, 50, centeringSpringStrength); LogitechGSDK.LogiPlayDamperForce(0, damperStrength); }
    private void UpdatePrevButtonStates() { if (!_isWheelConnected) return; for (int i = 0; i < 128; i++) _prevButtonStates[i] = (_currentState.rgbButtons[i] == 128); }
    private bool GetLogiButtonDown(int buttonIndex) { if (!_isWheelConnected) return false; return (_currentState.rgbButtons[buttonIndex] == 128) && !_prevButtonStates[buttonIndex]; }

    private const int ID_NONE = -1; private const int ID_FORWARD = 1; private const int ID_LEFT = 2; private const int ID_RIGHT = 3;
    private void HandleDirectionUI_Keyboard() { if (IngameUIManager.Instance == null) return; int targetState = ID_NONE; bool isLeft = _input.x < -0.1f; bool isRight = _input.x > 0.1f; bool isForward = _input.z > 0.1f; if (isLeft) targetState = ID_LEFT; else if (isRight) targetState = ID_RIGHT; else if (isForward) targetState = ID_FORWARD; UpdateArrowPanelState(targetState); }
    private void HandleDirectionUI_Wheel() { if (IngameUIManager.Instance == null) return; int targetState = ID_NONE; if (_input.x < -wheelSteerThreshold) targetState = ID_LEFT; else if (_input.x > wheelSteerThreshold) targetState = ID_RIGHT; else if (_input.z > pedalAccelThreshold) targetState = ID_FORWARD; UpdateArrowPanelState(targetState); }
    private void UpdateArrowPanelState(int newState) { if (_currentUiState != newState) { _currentUiState = newState; if (_currentUiState != ID_NONE) IngameUIManager.Instance.OpenArrowPanel(_currentUiState); else IngameUIManager.Instance.CloseArrowPanel(); } }
    private void ResetMovementAndUI()
    {
        _input = Vector3.zero; if (IngameUIManager.Instance != null) IngameUIManager.Instance.CloseArrowPanel(); _currentUiState = ID_NONE; if (_rb != null)
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
        if (_rb.position.x < -xLimit && vel.x < 0) { vel.x = 0; _rb.position = new Vector3(-xLimit, _rb.position.y, _rb.position.z); } else if (_rb.position.x > xLimit && vel.x > 0) { vel.x = 0; _rb.position = new Vector3(xLimit, _rb.position.y, _rb.position.z); }
        if (_rb.position.y < -yLimit && vel.y < 0) { vel.y = 0; _rb.position = new Vector3(_rb.position.x, -yLimit, _rb.position.z); } else if (_rb.position.y > yLimit && vel.y > 0) { vel.y = 0; _rb.position = new Vector3(_rb.position.x, yLimit, _rb.position.z); }
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = vel;
#else
        _rb.velocity = vel;
#endif
    }
    private void HandleSkillInput() { if (DataManager.Instance == null) return; if (Input.GetKeyDown(KeyCode.Z) || GetLogiButtonDown(11)) OnClickBuffButton(); if (Input.GetKeyDown(KeyCode.X) || GetLogiButtonDown(10)) OnClickDebuffButton(); if (Input.GetKeyDown(KeyCode.C) || GetLogiButtonDown(7)) { if (IngameUIManager.Instance != null) { if (!IngameUIManager.Instance.GetDisplayPanel()) IngameUIManager.Instance.OnClickPauseButton(); else { if (GameManager.Instance.IsPaused) IngameUIManager.Instance.OnClickContinueButton(); else if (GameManager.Instance.IsFailed) IngameUIManager.Instance.OnClickRetryButton(); else if (outtroUIManager != null) outtroUIManager.GoHome(); } } } if (Input.GetKeyDown(KeyCode.B) || GetLogiButtonDown(6)) { if (IngameUIManager.Instance != null && IngameUIManager.Instance.GetDisplayPanel()) { if (GameManager.Instance.IsPaused || GameManager.Instance.IsFailed) IngameUIManager.Instance.OnClickBackButton(); } } }
    public void OnClickBuffButton() { if (DataManager.Instance.GetBuffer() >= DataManager.Instance.bufferUse) { int cost = DataManager.Instance.bufferUse; DataManager.Instance.SetBuffer(Mathf.Max(0, DataManager.Instance.GetBuffer() - cost)); if (IngameUIManager.Instance != null) { IngameUIManager.Instance.Log("Buff Activated"); DataManager.Instance.SetShipShield(DataManager.Instance.maxShipShield); } } }
    public void OnClickDebuffButton() { if (DataManager.Instance.GetDeBuffer() >= DataManager.Instance.debufferUse) { int cost = DataManager.Instance.debufferUse; DataManager.Instance.SetDeBuffer(Mathf.Max(0, DataManager.Instance.GetDeBuffer() - cost)); if (IngameUIManager.Instance != null) IngameUIManager.Instance.Log("Debuff Activated"); } }
    private void CheckGameOver() { if (DataManager.Instance != null && IngameUIManager.Instance != null) { if (DataManager.Instance.GetShipHealth() <= 0 && !IngameUIManager.Instance.GetDisplayPanel()) { IngameUIManager.Instance.OnClickFailButton(); } } }
    private void ActivateShield() { if (shieldEffect != null) { shieldEffect.SetActive(true); CancelInvoke(nameof(DeactivateShield)); Invoke(nameof(DeactivateShield), 3f); } }
    private void DeactivateShield() { if (shieldEffect != null) shieldEffect.SetActive(false); }
    private void OnTriggerEnter(Collider other) { if (other.CompareTag("Obstacle")) { if (DataManager.Instance != null) DataManager.Instance.TakeDamage(20); if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SFXType.ShieldHit); Destroy(other.gameObject); } }
    private void OnDrawGizmos() { Gizmos.color = Color.yellow; Vector3 center = new Vector3(0, 0, transform.position.z); Vector3 size = new Vector3(xLimit * 2, yLimit * 2, 10f); Gizmos.DrawWireCube(center, size); }
    #endregion

#if UNITY_EDITOR
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(); style.fontSize = 20; style.normal.textColor = Color.white;
        string rawValues = $"Raw: A({GetAxisValue(accelAxis)}) B({GetAxisValue(brakeAxis)}) C({GetAxisValue(clutchAxis)})";
        GUI.Label(new Rect(20, 20, 1000, 30), rawValues, style);
        GUI.Label(new Rect(20, 50, 1000, 30), $"Result: X({_input.x:F2}) Y({_input.y:F2}) Z({_input.z:F2})", style);
    }
#endif
}