using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// 플레이어 우주선의 평행 이동 및 속도 제어 클래스 (회전 없음)
/// </summary>
public class PlayerMover : MonoBehaviour
{
    public static PlayerMover Instance { get; private set; }

    #region Inspector Fields
    [Header("Control Settings")]
    [Tooltip("이 값이 True여야만 움직일 수 있습니다.")]
    [SerializeField] private bool canMove;

    [Header("Speed Settings")]
    [Tooltip("기본 전진 속도")]
    [SerializeField] private float defaultSpeed = 0f;
    [Tooltip("W키 누를 시 최대 속도")]
    [SerializeField] private float maxSpeed = 40f;
    [Tooltip("S키 누를 시 최소 속도 (감속)")]
    [SerializeField] private float minSpeed = 0.001f;
    [Tooltip("속도 변경 반응 속도")]
    [SerializeField] private float acceleration = 5f;

    [Header("Movement Settings")]
    [Tooltip("좌우(A/D) 이동 속도")]
    [SerializeField] private float strafeSpeed = 15f;
    [Tooltip("좌우 이동 범위 제한 (X축)")]
    [SerializeField] private float xLimit = 10f;

    [Header("References")]
    [SerializeField] private GameObject shieldEffect;
    #endregion

    #region Private Fields
    private Rigidbody _rb;
    private Vector2 _input;
    private float _currentForwardSpeed;

    // 로그 중복 출력을 막기 위한 상태 플래그들
    private bool _loggedForward = false;
    private bool _loggedBackward = false;
    private bool _loggedLeft = false;
    private bool _loggedRight = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _rb = GetComponent<Rigidbody>();
        // 초기 속도를 기본 속도로 설정
        _currentForwardSpeed = defaultSpeed;

        SetMoveAction(false);
    }

    private void Update()
    {
        // 1. 움직임이 불가능한 상태면 입력 처리도 하지 않음
        if (!canMove)
        {
            _input = Vector2.zero; // 입력값 초기화
            return;
        }

        // 2. 입력 처리
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        _input = new Vector2(h, v);

        // 3. 방향별 최초 1회 로그 출력 로직
        HandleInputLogging(h, v);

        // 방어막 테스트
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateShield();
        }
    }

    private void FixedUpdate()
    {
        // 움직임이 허용되지 않으면 물리 이동 로직 실행 안 함
        if (!canMove) return;

        // 1. 전진 속도 계산 (W/S 키 로직)
        float targetSpeed = defaultSpeed;

        if (_input.y > 0) // W키 (앞): 가속
        {
            targetSpeed = maxSpeed;
        }
        else if (_input.y < 0) // S키 (뒤): 감속
        {
            targetSpeed = minSpeed;
        }

        // 현재 속도를 목표 속도로 부드럽게 변경 (가속/감속)
        _currentForwardSpeed = Mathf.Lerp(_currentForwardSpeed, targetSpeed, Time.fixedDeltaTime * acceleration);

        // 2. 이동 벡터 계산
        float moveZ = _currentForwardSpeed * Time.fixedDeltaTime;
        float moveX = _input.x * strafeSpeed * Time.fixedDeltaTime;
        float moveY = 0f;

        // 3. 다음 위치 계산
        Vector3 nextPosition = _rb.position + new Vector3(moveX, moveY, moveZ);

        // 4. 이동 범위 제한 (좌우 X축만 제한)
        nextPosition.x = Mathf.Clamp(nextPosition.x, -xLimit, xLimit);

        // 5. 리지드바디 위치 갱신
        _rb.MovePosition(nextPosition);

        // 6. 회전 초기화
        _rb.rotation = Quaternion.identity;
    }
    #endregion

    #region Helper Methods
    public void SetMoveAction( bool value ) { canMove = value; Debug.Log($"Change MoveState : {canMove}"); }

    /// <summary>
    /// 입력값에 따라 처음 눌렀을 때만 로그를 출력합니다.
    /// </summary>
    private void HandleInputLogging(float h, float v)
    {
        // 임계값 (조이스틱 노이즈 방지용으로 약간의 여유를 둠)
        float threshold = 0.1f;

        // --- 전진 (W) ---
        if (v > threshold)
        {
            if (!_loggedForward)
            {
                Debug.Log("Forward Input (W) Started");
                IngameUIManager.Instance.CloseArrowPanel();
                IngameUIManager.Instance.OpenArrowPanel(1);
                _loggedForward = true; // 로그 찍음 표시
            }
        }
        else
        {
            _loggedForward = false; // 키를 떼면 다시 초기화
        }

        // --- 후진 (S) ---
        if (v < -threshold)
        {
            if (!_loggedBackward)
            {
                Debug.Log("Backward Input (S) Started");
                IngameUIManager.Instance.CloseArrowPanel();
                _loggedBackward = true;
            }
        }
        else
        {
            _loggedBackward = false;
        }

        // --- 좌측 (A) ---
        if (h < -threshold)
        {
            if (!_loggedLeft)
            {
                Debug.Log("Left Input (A) Started");
                IngameUIManager.Instance.CloseArrowPanel();
                IngameUIManager.Instance.OpenArrowPanel(2);
                _loggedLeft = true;
            }
        }
        else
        {
            _loggedLeft = false;
        }

        // --- 우측 (D) ---
        if (h > threshold)
        {
            if (!_loggedRight)
            {
                Debug.Log("Right Input (D) Started");
                IngameUIManager.Instance.CloseArrowPanel();
                IngameUIManager.Instance.OpenArrowPanel(3);
                _loggedRight = true;
            }
        }
        else
        {
            _loggedRight = false;
        }
    }

    // 외부에서 움직임을 제어하기 위한 함수
    public void SetControlState(bool state)
    {
        canMove = state;
        if (!state)
        {
            // 멈출 때 속도 관련 변수 초기화가 필요하다면 여기서 수행
            _currentForwardSpeed = 0f;
            _input = Vector2.zero;
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
    #endregion

    #region Collision Handling
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