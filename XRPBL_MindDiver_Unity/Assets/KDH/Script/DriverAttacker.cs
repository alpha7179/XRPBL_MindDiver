using UnityEngine;

public class ScopeController : MonoBehaviour
{
    [Header("조준점 및 조작 설정")]
    public float scopeSpeed = 500f; // 키보드 입력에 따른 조준점 이동 속도 (픽셀/초)
    public float maxRange = 100f; // Raycast 최대 사거리

    // 조준점의 현재 화면 좌표 (초기값은 화면 중앙)
    private Vector2 scopeScreenPosition;

    [Header("키보드 입력 설정")]
    public KeyCode fireKey = KeyCode.Space; // 발사 키 (예: 스페이스바)
    public KeyCode moveUp = KeyCode.W;
    public KeyCode moveDown = KeyCode.S;
    public KeyCode moveLeft = KeyCode.A;
    public KeyCode moveRight = KeyCode.D;

    private Camera mainCam;

    void Start()
    {
        mainCam = GetComponent<Camera>();
        if (mainCam == null)
        {
            // 이 스크립트가 카메라에 부착되지 않았다면, Main Camera를 찾습니다.
            mainCam = Camera.main;
        }

        if (mainCam == null)
        {
            Debug.LogError("ScopeController가 사용할 카메라를 찾을 수 없습니다.");
            enabled = false;
            return;
        }

        // 초기 조준점 위치를 화면 중앙으로 설정
        scopeScreenPosition = new Vector2(Screen.width / 2, Screen.height / 2);
    }

    void Update()
    {
        HandleScopeMovement();

        // 발사 키가 눌렸을 때 Raycast 발사 및 상호작용
        if (Input.GetKeyDown(fireKey))
        {
            FireRaycast();
        }
    }

    private void HandleScopeMovement()
    {
        // 1. 키보드 입력 벡터 계산
        float xInput = 0f;
        float yInput = 0f;

        if (Input.GetKey(moveLeft)) xInput -= 1f;
        if (Input.GetKey(moveRight)) xInput += 1f;
        if (Input.GetKey(moveDown)) yInput -= 1f;
        if (Input.GetKey(moveUp)) yInput += 1f;

        Vector2 moveVector = new Vector2(xInput, yInput).normalized;

        // 2. 조준점 위치 업데이트 (시간과 속도를 곱하여 프레임에 독립적으로 이동)
        scopeScreenPosition += moveVector * scopeSpeed * Time.deltaTime;

        // 3. 조준점을 화면 경계 안에 가두기
        scopeScreenPosition.x = Mathf.Clamp(scopeScreenPosition.x, 0, Screen.width);
        scopeScreenPosition.y = Mathf.Clamp(scopeScreenPosition.y, 0, Screen.height);

        // TODO: UI에 조준점을 표시하는 로직이 있다면, 이 위치(scopeScreenPosition)를 사용합니다.
    }

    private void FireRaycast()
    {
        // 1. 현재 조준점 화면 좌표를 기준으로 Ray 생성
        Ray ray = mainCam.ScreenPointToRay(scopeScreenPosition);
        RaycastHit hit;

        // 2. Raycast 수행
        if (Physics.Raycast(ray, out hit, maxRange))
        {
            // 3. 충돌한 오브젝트에서 EnemyHealth 컴포넌트 찾기
            EnemyHealth enemy = hit.transform.GetComponent<EnemyHealth>();

            if (enemy != null)
            {
                // 적 발견 시 데미지를 줍니다.
                enemy.TakeDamage(enemy.damagePerClick);
                Debug.Log("스코프 조준 사격 성공! (" + hit.transform.name + ")");
            }
        }
    }
}