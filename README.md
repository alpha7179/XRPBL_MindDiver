# **마인드 다이버 (Mind Diver)**

## **1. 프로젝트 개요 (Project Overview)**

사이버 혐오 발언으로 고통받는 사람의 정신 세계(Mind)에 접속(Dive)하여, 악플을 정화하고 마음의 평화를 되찾아주는 사이버네틱 멘탈 케어 전문가가 되는 VR 액션 어드벤처.

### 주요 특징 및 기대효과:
사회적 메시지: 사이버 불링과 온라인 혐오 문제를 정면으로 다루어 플레이어의 공감대를 형성하고 사회적 경각심을 깨우기.
시각적 경험: 악플은 '몬스터'로, 피해자의 마음은 '어둡고 뒤틀린 공간'으로 시각화. 플레이어의 활약에 따라 공간이 정화되고 밝아지는 모습을 통해 시각적인 만족감과 치유의 경험을 제공.
영웅적인 역할(?): 플레이어는 단순한 해결사가 아닌, 상처받은 마음을 치유하는 '수호자' 역할을 수행하며 깊은 몰입감과 성취감을 느낄 수 있음.


### 공유 문서
 * **GitLab**
    * https://wemeet.twentyoz.kr/2025-xr-pbl/mind-diver

 * **Notion**
    * https://www.notion.so/kaywonpacemaker/XR-PBL-270303ad519f8040893ee9ffc5f2bf74

 * **Figma**
    * https://www.figma.com/design/BLxf2tZxQ5weZFxbRaBeCL/XR%EA%B8%B0%EC%88%A0PBL-%EA%B2%BD%ED%9D%AC%EB%8C%80-?node-id=0-1&t=6v04eMJWnNLrkAhg-1


## **2. 팀원 (Team Members)**

|이름|학번|역할|Mobile|e-maail|GitHub|
|---|---|---|---|---|---|
|김동건| 2025005106 |기획(팀장)|010-8334-5206|akfn8334@naver.com|@githubID|
|박지현| 2025005107 |모델링|010-5212-0524|555jihun@gmail.com|@githubID|
|정기주| 2025005108 |디자인|010-7732-8275|korag410@kawon.ac.kr|@githubID|
|배민우| 2025005103 |개발|010-4180-7179|alpha7179@ajou.ac.kr|@githubID|
|김동현| 2020104926 |개발|010-3624-9565|dongstar2000@khu.ac.kr|@githubID|


## **3. 기술 스택 (Tech Stack)**

  * **Engine:** Unity 6 (Version: `6000.0.58f2`)
  * **Language:** C\#
  * **Platform:** Meta Quest 2, Meta Quest 3, etc.
  * **Version Control:** Git, GitHub


## **4. 개발 규칙 (Development Convention)**

원활한 협업을 위해 아래 규칙을 준수합니다.

### **4.1. Issue Convention**

모든 작업은 이슈 생성을 원칙으로 합니다.

  * **✅ 기능 이슈 (`[Feat]`)**

      * 새로운 기능 구현 시 작성하며, 담당자를 Assign합니다.
      * 상세 설명과 완료를 위한 체크리스트(To-Do)를 포함해야 합니다.
      * **예시:** `[Feat] NAME_PlayerMovement`

  * **🙏 요청 사항**

      * 다른 팀원의 확인이나 작업이 필요한 경우 작성합니다.
      * 제목에 `[요청 대상]`을 명시합니다. (예: `[NAME]`)
      * **예시:** `[NAME] 3D 모델링 위치 조정 요청`

### **4.2. Commit Message Convention**

  * **커밋 메시지:** `[태그] 내용` 형식으로 작성합니다.

|태그|설명|
|---|---|
|`[Feat]`|새로운 기능 구현|
|`[Update]`|기존 기능 및 요소 강화|
|`[Change]`|기존 기능 및 요소 변경|
|`[Fix]`|버그 및 오류 해결|
|`[Refactor]`|코드 구조 개선 (기능 변경 없음)|
|`[Design]`|UI/UX 및 모델링 디자인 수정|
|`[Comment]`|주석 추가 및 수정|
|`[Docs]`|README 등 문서 수정|
|`[Setting]`|VR 및 프로젝트 초기 설정|
|`[Add]`|Feat 이외의 코드, 라이브러리, 에셋 등 추가|
|`[Remove]`|파일 및 리소스 삭제|

### **4.3. Branching Convention (개발 단계별 브랜치)**
개발 단계는 아래 4단계로 구분하며, 각 단계별 브랜치를 별도로 생성해야 합니다.
* **개발 단계:**
   * MVP (Minimum Viable Product)
   * Proto (초기 단계 핵심 기능 개발)
   * Alpha (기본 기능 개발 완료, 테스트 진행 단계)
   * Beta (기능 완성 및 디버깅, 최종 단계)

* **브랜치명 예시:**
    * MVP/[이니셜]
    * Proto/[이니셜]
    * Alpha/[이니셜]
    * Beta/[이니셜]

각 단계별 브랜치에서 작업 후 충분한 테스트 및 검증을 완료한 뒤, 단계별 병합(Merge)합니다.

### **4.4. Naming Convention**

#### **1) 폴더 구조**

  * Assets 폴더 내에 각자 `이니셜(PascalCase)` 폴더를 생성하여 작업합니다.
  * 개인 폴더 내부는 `Scripts`, `Scenes`, `Prefabs`, `Materials` 등으로 다시 분류합니다.

#### **2) 유니티 에셋 이름**

|Prefix|내용|예시|
|---|---|---|
|`PF_`|Prefab|`PF_Player`|
|`UI_`|UI 요소|`UI_Start`|
|`AC_`|Animation Controller|`AC_Player`|
|`AM_`|Animation|`AM_Run`|
|`M_`|Material|`M_Transparent`|
|`SPR_`|Sprite|`SPR_Arrow`|
|`SFX_`|Sound Effect|`SFX_Enter`|
|`BGM_`|Background Music|`BGM_Main`|

#### **3) C\# 스크립트**

  * **변수명**

      * `public` 멤버: PascalCase (예: `public int PlayerHealth;`)
      * `private`, `protected` 멤버: \_camelCase (예: `private int _playerHealth;`)
      * `bool` 변수: Is\~, Can\~ 등의 질문형 (예: `isActive`, `canJump`)
      * `const` 변수: 대문자와 언더바 사용 (예: `MAX_PLAYER_COUNT`)

  * **함수명**

      * 일반 함수: PascalCase (예: `StartGame();`)
      * `bool` 반환 함수: 질문형 (예: `IsPlayerAlive();`)
      * 이벤트 핸들러/콜백: On으로 시작 (예: `OnPlayerDeath()`)
