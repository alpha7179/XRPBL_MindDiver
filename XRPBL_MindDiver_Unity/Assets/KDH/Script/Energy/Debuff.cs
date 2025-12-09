using UnityEngine;
using Energy;

public class Debuff : EnergyClass
{
    public override void Die()
    {
        // 플레이어에게 포인트 추가
        playerEnergy += scoreValue;

        if (DataManager.Instance != null)
        {
            DataManager.Instance.AddScore(scoreValue);
            DataManager.Instance.AddDeBuffer(debuffValue);
        }
        // (디버그용) 현재 총 포인트 출력
        Debug.Log("플레이어 에너지 획득! 현재 총점: " + playerEnergy);

        // 오브젝트 제거
        Destroy(gameObject);

        // TODO: 폭발 이펙트 재생, 사운드 출력 등 추가
    }
}
