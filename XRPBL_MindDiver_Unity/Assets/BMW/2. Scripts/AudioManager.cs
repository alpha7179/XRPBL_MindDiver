using System.Collections; //  코루틴 사용을 위해 필요
using UnityEngine;
using static GameManager;
using static GamePhaseManager;

/// <summary>
/// 게임 내 배경음악(BGM)과 효과음(SFX) 재생을 총괄하는 오디오 관리 클래스
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region Singleton
    public static AudioManager Instance { get; private set; }
    #endregion

    #region Inspector Fields
    [Header("Audio Sources")]
    // 배경음악 재생용 오디오 소스
    public AudioSource bgmSource;
    // 효과음 재생용 오디오 소스
    public AudioSource sfxSource;

    [Header("BGM Clips")]
    // 메인 메뉴 배경음악
    public AudioClip bgm_MainMenu;
    // 캐릭터 선택 화면 배경음악
    public AudioClip bgm_CharacterSelect;
    // 프리페이즈 배경음악
    public AudioClip bgm_PrePhase;
    // 페이즈 1 배경음악
    public AudioClip bgm_Phase1;
    // 페이즈 2 배경음악
    public AudioClip bgm_Phase2;
    // 페이즈 3 배경음악
    public AudioClip bgm_Phase3;
    // 결과 화면 배경음악
    public AudioClip bgm_Result;

    [Header("SFX Clips")]
    // 터치 효과음
    public AudioClip sfx_Touch;
    // 폭발 효과음
    public AudioClip sfx_Explosion;
    // 쉴드 피격 효과음
    public AudioClip sfx_ShieldHit;
    // 아이템 획득 효과음
    public AudioClip sfx_ItemCollect;
    #endregion

    #region Private Fields
    //  BGM 초기 볼륨 기억용 변수
    private float _initialBgmVolume = 1f;
    //  현재 실행 중인 페이드 코루틴 저장용
    private Coroutine _bgmFadeCoroutine;
    #endregion

    #region Unity Lifecycle
    /*
     * 싱글톤 인스턴스 초기화 및 파괴 방지 설정 수행
     */
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.parent = null;
            DontDestroyOnLoad(gameObject);

            //  시작할 때 설정된 BGM 볼륨 저장 (나중에 페이드 아웃 후 복구할 때 사용)
            if (bgmSource != null) _initialBgmVolume = bgmSource.volume;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Public Methods - BGM
    /*
     * 특정 오디오 클립으로 BGM 재생 (중복 재생 방지)
     */
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        // 페이드 아웃 중에 다른 BGM을 틀어야 한다면, 페이드 중지 후 볼륨 복구
        if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);
        bgmSource.volume = _initialBgmVolume;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.volume = (float)DataManager.Instance.GetBGMVolume()/100;
        bgmSource.Play();
    }

    /*
     * 게임 상태 및 페이즈에 따른 BGM 자동 재생
     */
    public void PlayBGM(GameState gameState, Phase phaseState = Phase.Null)
    {
        switch (gameState)
        {
            case GameState.MainMenu:
                if (bgm_MainMenu != null) PlayBGM(bgm_MainMenu);
                break;
            case GameState.CharacterSelect:
                if (bgm_CharacterSelect != null) PlayBGM(bgm_CharacterSelect);
                break;
            case GameState.GameStage:
                switch (phaseState)
                {
                    case Phase.PrePhase: if (bgm_PrePhase != null) PlayBGM(bgm_PrePhase); break;
                    case Phase.Phase1: if (bgm_Phase1 != null) PlayBGM(bgm_Phase1); break;
                    case Phase.Phase2: if (bgm_Phase2 != null) PlayBGM(bgm_Phase2); break;
                    case Phase.Phase3: if (bgm_Phase3 != null) PlayBGM(bgm_Phase3); break;
                }
                break;
            case GameState.Result:
                if (bgm_Result != null) PlayBGM(bgm_Result);
                break;
        }
    }

    /// <summary>
    /// [수정됨] 현재 재생 중인 배경음악(BGM)을 페이드 아웃하며 정지합니다.
    /// </summary>
    /// <param name="fadeDuration">페이드 아웃에 걸리는 시간 (초)</param>
    public void StopBGM(float fadeDuration = 1.0f)
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            // 기존 페이드 코루틴이 있다면 중지
            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);

            // 페이드 아웃 시작
            _bgmFadeCoroutine = StartCoroutine(FadeOutRoutine(fadeDuration));
        }
    }

    //  서서히 볼륨을 줄이는 코루틴
    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVolume = bgmSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // 시간에 따라 볼륨을 줄임 (Lerp)
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        // 완전히 0으로 만든 후 정지
        bgmSource.volume = 0f;
        bgmSource.Stop();

        // 다음 재생을 위해 볼륨 원상복구 (미리 해둠)
        bgmSource.volume = _initialBgmVolume;
        _bgmFadeCoroutine = null;
    }
    #endregion

    #region Public Methods - SFX
    /*
     * 특정 오디오 클립을 효과음으로 1회 재생
     */
    public void PlaySFX(AudioClip clip)
    {
        sfxSource.volume = (float)DataManager.Instance.GetSFXVolume() / 100;
        sfxSource.PlayOneShot(clip);
    }

    /*
     * 문자열 키워드를 통한 효과음 재생 (편의성 제공)
     */
    public void PlaySFX(string sfxName)
    {
        switch (sfxName)
        {
            case "Touch": PlaySFX(sfx_Touch); break;
            case "Explosion": PlaySFX(sfx_Explosion); break;
            case "ShieldHit": PlaySFX(sfx_ShieldHit); break;
            case "Collect": PlaySFX(sfx_ItemCollect); break;
        }
    }

    /// <summary>
    /// [추가됨] 현재 재생 중인 모든 효과음(SFX)을 즉시 정지합니다.
    /// 주의: PlayOneShot으로 재생 중인 모든 소리가 함께 끊깁니다.
    /// </summary>
    public void StopSFX()
    {
        if (sfxSource != null)
        {
            sfxSource.Stop();
        }
    }
    #endregion

    #region Public Methods - All
    /// <summary>
    /// [추가됨] BGM과 SFX를 모두 정지합니다.
    /// </summary>
    public void StopAllAudio()
    {
        StopBGM(0.5f); // 전체 정지 시에는 조금 더 빨리(0.5초) 꺼지도록 설정
        StopSFX();
    }
    #endregion
}