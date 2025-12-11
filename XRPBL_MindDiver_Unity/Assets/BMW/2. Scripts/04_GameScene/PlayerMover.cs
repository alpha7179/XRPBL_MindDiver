using UnityEngine;
using System.Collections;

/// <summary>
/// 물리 엔진 기반(Rigidbody Physics) 플레이어 이동 클래스
/// 기능: 로지텍 휠/페달 + 관성 주행 + 버튼 매핑(GetKeyDown 적용) + 포스 피드백 + 자동 재연결
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

    private bool[] _prevButtonStates = new bool[128];

    private OuttroUIManager outtroUIManager;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _rb = GetComponent<Rigidbody>();
        outtroUIManager = GetComponent<OuttroUIManager>();

        // 물리 설정
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        _rb.linearDamping = coastingDrag;
        _rb.mass = 1000f;
    }

    private void Start()
    {
        if (useLogitechWheel)
        {
            StartCoroutine(InitializeLogitechSDK());
        }
    }

    private IEnumerator InitializeLogitechSDK()
    {
        if (_isWheelInitialized) yield break;
        Debug.Log("[PlayerMover] 로지텍 SDK 연결 대기 중...");

        while (!_isWheelInitialized)
        {
            bool result = LogitechGSDK.LogiSteeringInitialize(false);
            if (result)
            {
                _isWheelInitialized = true;
                Debug.Log("<color=green>[PlayerMover] 로지텍 SDK 초기화 성공!</color>");
                HandleForceFeedback();
            }
            else
            {
                Debug.LogWarning("[PlayerMover] SDK 연결 실패. 재시도 중...");
                yield return new WaitForSeconds(2f);
            }
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus && useLogitechWheel && !_isWheelInitialized)
        {
            StartCoroutine(InitializeLogitechSDK());
        }
    }

    private void Update()
    {
        CheckGameOver();

        // 1. SDK 업데이트 및 포스 피드백
        UpdateLogitechState();
        HandleForceFeedback();

        // 2. 이동 불가 처리
        if (!canMove)
        {
            _input = Vector2.zero;
            // 이동은 막아도 버튼 상태 업데이트는 해야 다음 프레임에 오작동 안함
            UpdatePrevButtonStates();
            return;
        }

        // 3. 입력 통합
        float h = GetCombinedHorizontalInput();
        float v = GetCombinedVerticalInput();
        _input = new Vector2(h, v);

        // 4. 스킬 및 버튼 입력
        HandleSkillInput();

        if (Input.GetKeyDown(KeyCode.Space)) ActivateShield();

        UpdatePrevButtonStates();
    }

    private void FixedUpdate()
    {
        if (!canMove) return;

        if (_input.y < 0) _rb.linearDamping = brakingDrag;
        else _rb.linearDamping = coastingDrag;

        Vector3 force = Vector3.zero;
        if (_input.y > 0) force += transform.forward * _input.y * forwardForce;
        else if (_input.y < 0) force += transform.forward * _input.y * reverseForce;
        force += transform.right * _input.x * strafeForce;

        _rb.AddForce(force, ForceMode.Force);

        if (_rb.linearVelocity.magnitude > maxSpeed)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
        }

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

        if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
        {
            _isWheelConnected = true;
            _currentState = LogitechGSDK.LogiGetStateUnity(0);
        }
        else
        {
            _isWheelConnected = false;
        }
    }

    // 버튼 상태 저장 함수 (Update 끝에 호출)
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

        // "지금 눌렀고" AND "아까는 안 눌렀었다" = 막 누른 순간
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

    #region Game Logic
    public void SetMoveAction(bool value)
    {
        canMove = value;
        if (!value)
        {
            _input = Vector2.zero;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void ApplyBoundaryLimit()
    {
        if (_rb.position.x < -xLimit && _rb.linearVelocity.x < 0)
        {
            Vector3 vel = _rb.linearVelocity; vel.x = 0; _rb.linearVelocity = vel;
            _rb.position = new Vector3(-xLimit, _rb.position.y, _rb.position.z);
        }
        else if (_rb.position.x > xLimit && _rb.linearVelocity.x > 0)
        {
            Vector3 vel = _rb.linearVelocity; vel.x = 0; _rb.linearVelocity = vel;
            _rb.position = new Vector3(xLimit, _rb.position.y, _rb.position.z);
        }
    }

    private void HandleSkillInput()
    {
        if (DataManager.Instance == null) return;

        // [모든 버튼 입력을 GetLogiButtonDown으로 변경]

        // 버프 (Z or 11번)
        if (Input.GetKeyDown(KeyCode.Z) || GetLogiButtonDown(11))
        {
            if (DataManager.Instance.GetBuffer() >= DataManager.Instance.bufferUse)
            {
                int cost = DataManager.Instance.bufferUse;
                DataManager.Instance.SetBuffer(Mathf.Max(0, DataManager.Instance.GetBuffer() - cost));
                if (IngameUIManager.Instance != null) IngameUIManager.Instance.Log("Buff Activated");
            }
        }

        // 디버프 (X or 10번)
        if (Input.GetKeyDown(KeyCode.X) || GetLogiButtonDown(10))
        {
            if (DataManager.Instance.GetDeBuffer() >= DataManager.Instance.debufferUse)
            {
                int cost = DataManager.Instance.debufferUse;
                DataManager.Instance.SetDeBuffer(Mathf.Max(0, DataManager.Instance.GetDeBuffer() - cost));
                if (IngameUIManager.Instance != null) IngameUIManager.Instance.Log("Debuff Activated");
            }
        }

        // 일시정지 (C or L2/7번)
        if (Input.GetKeyDown(KeyCode.C) || GetLogiButtonDown(7))
        {
            if (IngameUIManager.Instance != null)
            {
                // 패널이 닫혀있으면 -> 일시정지 (열기)
                if (!IngameUIManager.Instance.GetDisplayPanel())
                {
                    IngameUIManager.Instance.OnClickPauseButton();
                }
                // 패널이 열려있으면 -> 상황에 맞게 닫기 (계속하기/홈으로 등)
                else
                {
                    if (GameManager.Instance.IsPaused) IngameUIManager.Instance.OnClickContinueButton();
                    else if (GameManager.Instance.IsFailed) IngameUIManager.Instance.OnClickRetryButton();
                    else if (outtroUIManager != null) outtroUIManager.GoHome();
                }
            }
        }

        // 뒤로가기 (B or R2/6번)
        if (Input.GetKeyDown(KeyCode.B) || GetLogiButtonDown(6))
        {
            if (IngameUIManager.Instance != null && IngameUIManager.Instance.GetDisplayPanel())
            {
                if (GameManager.Instance.IsPaused || GameManager.Instance.IsFailed) IngameUIManager.Instance.OnClickBackButton();
            }
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