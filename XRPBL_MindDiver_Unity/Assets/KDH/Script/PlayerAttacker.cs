using Energy;
using System.Collections.Generic; // List를 사용하기 위해 추가
using UnityEngine;

public class PlayerAttacker : MonoBehaviour
{
    // 카메라들을 저장할 리스트 (씬의 모든 카메라를 가져올 수도 있고, Inspector에서 수동 할당할 수도 있습니다.)
    private List<Camera> activeCameras = new List<Camera>();

    private void Start()
    {
        // 씬에 있는 모든 활성화된 카메라 컴포넌트를 가져옵니다.
        // 플레이어에게 부착된 카메라만 따로 필터링할 수도 있지만, 이 방법이 가장 간단합니다.
        activeCameras.AddRange(FindObjectsByType<Camera>(FindObjectsSortMode.None));

        if (activeCameras.Count == 0)
        {
            Debug.LogError("씬에서 활성화된 Camera 컴포넌트를 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        // 마우스 왼쪽 버튼(0번)이 눌렸는지 확인합니다.
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        // 모든 카메라를 순회하며 Raycast 검사
        foreach (Camera cam in activeCameras)
        {
            // 카메라가 활성화되어 있지 않거나, 널(null)이면 건너뜁니다.
            if (cam == null || !cam.isActiveAndEnabled)
            {
                continue;
            }

            // 현재 카메라의 뷰포트를 기준으로 Ray를 생성합니다.
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Raycast 수행: Ray가 오브젝트에 맞았는지 확인합니다.
            if (Physics.Raycast(ray, out hit, 100f))
            {
                // 충돌한 오브젝트에서 EnemyHealth 컴포넌트 찾기
                EnemyHealth enemy = hit.transform.GetComponent<EnemyHealth>();
                EnergyClass energy = hit.transform.GetComponent<EnergyClass>();

                if (enemy != null)
                {
                    // 적 발견 시 데미지를 줍니다.
                    enemy.TakeDamage(enemy.damagePerClick);

                    //클릭이 성공했으므로, 더 이상 다른 카메라를 확인할 필요 없이 함수를 종료합니다.
                    return;
                }
                if (energy!= null)
                {
                    // 적 발견 시 데미지를 줍니다.
                    energy.TakeDamage(energy.damagePerClick);
                    return;
                }
            }
        }
    }
}