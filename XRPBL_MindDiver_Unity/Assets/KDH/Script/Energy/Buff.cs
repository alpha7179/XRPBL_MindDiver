using UnityEngine;
using Energy;

public class Buff : EnergyClass
{
    public override void Start()
    {
        // Target 대신 PrimaryCamera 할당 로직을 확인합니다.
        if (primaryCamera == null)
        {
            // Main Camera를 기본값으로 사용합니다.
            primaryCamera = Camera.main.transform;
            if (primaryCamera == null)
            {
                Debug.LogError("Primary Camera를 찾을 수 없습니다. Inspector에서 할당하거나, 씬에 Main Camera가 있는지 확인하세요.");
                enabled = false;
                return;
            }
        }
        // 초기 목표 화면 및 위치 설정

        ScreenRight = false;
        currentLocalTargetPosition = GetNewLocalTargetPosition(ScreenRight);

    }

    public override void Die()
    {
        // 플레이어에게 포인트 추가
        playerEnergy += scoreValue;

        if (DataManager.Instance != null)
        {
            DataManager.Instance.AddScore(scoreValue);
            DataManager.Instance.AddBuffer(buffValue);
        }
        // (디버그용) 현재 총 포인트 출력
        Debug.Log("플레이어 에너지 획득! 현재 총점: " + playerEnergy);

        // 오브젝트 제거
        Destroy(gameObject);

        // TODO: 폭발 이펙트 재생, 사운드 출력 등 추가
    }
}
