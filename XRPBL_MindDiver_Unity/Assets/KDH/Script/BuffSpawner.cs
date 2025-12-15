using UnityEngine;
using System.Linq; // 배열에서 Linq 기능을 사용하기 위해 추가
using Mover;

public class BuffSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public GameObject enemyPrefab; // 스폰할 오브젝트 프리팹 (필수)
    public int maxEnemies = 5; // 씬에 유지할 최대 오브젝트 개수
    public float spawnInterval = 10f; // 몇 초마다 개수를 체크하고 스폰할지
    public int spawnNum = 1;

    [Header("스폰 위치 설정")]
    public float spawnDistance = 120f; // 플레이어/카메라로부터 스폰될 거리
    public float spawnHeightRange = 30f; // 스폰될 때 추가적인 수직 위치 랜덤 범위

    private Transform cameraTransform;

    private int energiesToSpawn = 0;

    void Start()
    {
        // 기준이 될 카메라 Transform을 가져옵니다. (주로 전방 카메라)
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Main Camera를 찾을 수 없습니다. 스폰 기준이 되는 카메라를 확인해 주세요.");
            enabled = false;
            return;
        }

        // spawnInterval마다 CheckAndSpawn 함수를 반복적으로 호출합니다.
        InvokeRepeating("CheckAndSpawn", 0f, spawnInterval);
    }

    void CheckAndSpawn()
    {
        // 1. 현재 씬에 있는 "Buff" 태그를 가진 오브젝트의 개수를 셉니다.
        GameObject[] existingBuffs = GameObject.FindGameObjectsWithTag("Buff");
        int currentCount = existingBuffs.Length;

        //int trackingCount = 0;
        //foreach (GameObject BuffE in existingBuffs)
        //{
        //    Buff follower = BuffE.GetComponent<Buff>();
        //    if (follower != null && follower.isTracking)
        //    {
        //        trackingCount++;
        //    }
        //}


        // 2. 최대 개수와의 차이를 계산하여 스폰할 개수를 결정합니다.
        energiesToSpawn = maxEnemies - currentCount;

        if (energiesToSpawn > 0)
        {
            //Debug.Log(energiesToSpawn + "개의 오브젝트가 부족합니다. 스폰을 시작합니다.");
            //for (int i = 0; i < energiesToSpawn; i++)
            //{
            //    SpawnNewEnemy();
            //}
            SpawnNewEnemy();
            //SpawnNewEnemy(trackingCount, currentCount);
        }
    }

    void SpawnNewEnemy()
    {
        // 스폰 위치 계산

        Debug.Log("--- distance : " + spawnDistance);
        Vector3 spawnPosition = cameraTransform.position + Vector3.forward * spawnDistance;

        // 랜덤 위치 적용 (약간의 변화)
        spawnPosition.x += Random.Range(-spawnHeightRange/2, spawnHeightRange/2);
        spawnPosition.y += Random.Range(-spawnHeightRange, spawnHeightRange);

        // 오브젝트 생성 및 태그 설정
        GameObject newBuff = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        //newEnemy.tag = "Enemy";

    }
}