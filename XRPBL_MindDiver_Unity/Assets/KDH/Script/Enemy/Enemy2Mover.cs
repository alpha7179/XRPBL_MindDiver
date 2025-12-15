using UnityEngine;
using Mover;

public class Enemy2Mover : EnemyMover
{
    public override void ScreenSelection()
    {
        // 1. 카메라를 기준으로 오브젝트의 상대적인 위치 벡터 계산
        // (카메라의 로컬 공간으로 변환)
        Vector3 localPos = primaryCamera.InverseTransformPoint(transform.position);

        // 2. 로컬 X축 값으로 좌우 판단
        // 로컬 X축이 양수(>0)면 카메라 기준 오른쪽에, 음수(<0)면 왼쪽에 있습니다.
        if (localPos.x > 0)
        {
            ScreenRight = true;
        }
        else
        {
            ScreenRight = false;
        }

    }
}
