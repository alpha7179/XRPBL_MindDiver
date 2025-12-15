using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MeshCore : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private int coredamage = 10;
    [SerializeField] private GameObject Prefab;// 생성할 오브젝트 프리팹
    [SerializeField] private int spawnNum = 4; // 생성할 개수
    [SerializeField] private float spawnRadius = 5f; // 폭파 후 적들이 흩어질 반경
    [SerializeField] private float minDistance = 50f;
    [SerializeField] private float maxDistance = 60f;
    public Transform primaryCamera;


    private void Start()
    {
        if (primaryCamera == null)
        {
            primaryCamera = Camera.main.transform;
            if (primaryCamera == null)
            {
                Debug.LogError("Primary Camera를 찾을 수 없습니다. Inspector에서 할당하거나, 씬에 Main Camera가 있는지 확인하세요.");
                enabled = false;
                return;
            }
        }
    }

    // 이 함수는 이 오브젝트가 다른 Collider를 가진 오브젝트와 실제로 충돌했을 때 호출됩니다.
    private void OnCollisionEnter(Collision collision)
    {
        // 1. 충돌한 오브젝트가 플레이어인지 확인
        // 플레이어 오브젝트에 "Player" 태그가 지정되어 있어야 합니다.
        if (collision.gameObject.CompareTag("Player"))
        {
            // 2. 적 처치 함수 호출
            Die();

            Debug.Log("플레이어와 충돌! 적 오브젝트가 파괴됩니다.");

            // Note: 플레이어에게 데미지를 주는 로직이 있다면 이 위치에 추가합니다.
            DataManager.Instance.TakeDamage(coredamage);
        }
    }

    // 오브젝트 처치 시 로직
    private void Die()
    {
        for (int i = 0; i < spawnNum; i++)
        {
            SpawnNewEntity();
        }
        Destroy(gameObject);
    }


    private void SpawnNewEntity()
    {
        float angle;
        // 스폰 위치 계산
        angle = Random.Range(-30, 30);

        // 최소/최대 거리 내에서 무작위 거리 선택
        float distance = Random.Range(minDistance, maxDistance);

        // 플레이어의 현재 회전(Rotation)을 기준으로 각도를 적용합니다.
        Quaternion rotation = primaryCamera.rotation * Quaternion.Euler(0, angle, 0);

        // 플레이어 위치로부터 랜덤 거리만큼 떨어진 위치를 계산합니다.
        Vector3 direction = rotation * Vector3.forward;
        Vector3 horizontalPosition = primaryCamera.position + direction * distance;

        // 수직 오프셋을 더해 최종 목표 위치를 반환합니다.
        Vector3 spawnPosition = horizontalPosition + Vector3.up * Random.Range(-200, 200);

        // 오브젝트 생성
        GameObject newEnemy = Instantiate(Prefab, spawnPosition, Quaternion.identity);

    }
}