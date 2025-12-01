using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// 인트로 및 아웃트로 상황에 맞춰 3면 파노라마 비디오 재생을 관리하는 클래스
/// </summary>
public class VideoManager : MonoBehaviour
{
    #region Inspector Fields
    [Header("Video Players Settings (Order: 0=Front, 1=Left, 2=Right)")]
    // 3면 파노라마 재생을 위한 비디오 플레이어 배열
    public VideoPlayer[] videoPlayers;

    [Header("Video Clips Settings (Order: 0=Front, 1=Left, 2=Right)")]
    public List<GameObject> VideoPanels;
    // 인트로용 비디오 클립 배열
    public VideoClip[] introClips;
    // 아웃트로용 비디오 클립 배열
    public VideoClip[] outroClips;
    public AudioSource[] videoAudioSource;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float masterVolume;
    [SerializeField] private bool isSideDisplaySoundMute = true;
    
    [Header("Debug Settings")]
    // 디버그 로그 출력 여부
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Unity Lifecycle
    /*
     * 초기화 및 비디오 타입에 따른 재생 로직 수행
     */
    private void Start()
    {

        foreach (var video in VideoPanels) if (video) video.SetActive(false);
        masterVolume = ((float)DataManager.Instance.GetVideoVolume()) / 100;

        // 플레이어 배열 유효성 검사
        if (!CheckArrayValid(videoPlayers, "Video Players")) return;

        foreach (var video in VideoPanels) if (video) video.SetActive(true);

        // 1. DataManager에서 현재 재생할 비디오 타입 확인
        DataManager.VideoType type = DataManager.Instance.currentVideoType;

        Log($"[VideoManager] Playing {type} Video (3-Screen Panoramic)...");

        // 2. 타입에 따른 3면 클립 설정 및 재생
        // 배열 인덱스: 0=Front, 1=Left, 2=Right
        if (type == DataManager.VideoType.Intro)
        {
            if (CheckArrayValid(introClips, "Intro Clips"))
            {
                PlayPanoramicVideo(introClips);
            }
        }
        else // Outro
        {
            if (CheckArrayValid(outroClips, "Outro Clips"))
            {
                PlayPanoramicVideo(outroClips);
            }
        }

        // 3. 종료 이벤트 연결 (정면 스크린 [0] 기준)
        // 정면 화면이 존재할 때만 이벤트 연결
        if (videoPlayers[0] != null)
        {
            videoPlayers[0].loopPointReached += OnVideoFinished;
        }
    }
    #endregion

    #region Helper Methods
    /*
     * 배열의 최소 길이 만족 여부(3개 이상)를 확인하는 검사 함수
     */
    private bool CheckArrayValid(System.Array array, string arrayName)
    {
        if (array == null || array.Length < 3)
        {
            Log($"[VideoManager] Error: {arrayName} array must have at least 3 elements (Front, Left, Right).");

            // 안전 장치: 플레이어가 세팅되어 있다면 정면 화면 기준으로 종료 처리 시도
            if (videoPlayers != null && videoPlayers.Length > 0 && videoPlayers[0] != null)
            {
                OnVideoFinished(videoPlayers[0]);
            }
            return false;
        }
        return true;
    }

    /*
     * 3개의 화면에 비디오 클립을 할당하고 동시 재생하는 함수
     */
    private void PlayPanoramicVideo(VideoClip[] clips)
    {
        // 3면 반복 처리 (0: Front, 1: Left, 2: Right)
        for (int i = 0; i < 3; i++)
        {
            // 플레이어와 클립이 모두 존재해야 재생
            if (videoPlayers[i] != null && clips[i] != null)
            {
                videoPlayers[i].clip = clips[i];

                ApplyVolume(videoPlayers[i], i);

                videoPlayers[i].Play();
            }
        }

        // 정면(0) 플레이어가 없거나 클립이 없으면 강제 종료 (진행 막힘 방지)
        if (videoPlayers[0] == null || clips[0] == null)
        {
            OnVideoFinished(null);
        }
    }

    private void ApplyVolume(VideoPlayer vp, int screenIndex)
    {
        // 1. Audio Output Mode가 Direct(직접 출력)인지 확인
        if (vp.audioOutputMode == VideoAudioOutputMode.Direct)
        {
            // 좌/우 화면(인덱스 1, 2)이고 음소거 옵션이 켜져있으면 0, 아니면 설정된 볼륨
            float finalVolume = (isSideDisplaySoundMute && screenIndex > 0) ? 0f : masterVolume;

            // 트랙 0번의 볼륨을 설정
            vp.SetDirectAudioVolume(0, finalVolume);
        }
        // 2. Audio Output Mode가 AudioSource인 경우
        else if (vp.audioOutputMode == VideoAudioOutputMode.AudioSource)
        {
            AudioSource source = vp.GetTargetAudioSource(0);
            if (source != null)
            {
                float finalVolume = (isSideDisplaySoundMute && screenIndex > 0) ? 0f : masterVolume;
                source.volume = finalVolume;
            }
        }
    }

    /*
     * 디버그 모드 시 로그 출력 수행
     */
    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
    #endregion

    #region Event Handlers
    /*
     * 비디오 재생 완료 시 호출되어 다음 씬으로 전환하는 콜백 함수
     */
    private void OnVideoFinished(VideoPlayer vp)
    {
        Log("[VideoManager] Video Finished.");

        // 이벤트 중복 호출 방지를 위해 구독 해제 (정면 기준)
        if (videoPlayers != null && videoPlayers.Length > 0 && videoPlayers[0] != null)
        {
            videoPlayers[0].loopPointReached -= OnVideoFinished;
        }

        // 비디오 타입에 따른 다음 씬 전환 처리
        if (DataManager.Instance != null && GameManager.Instance != null)
        {
            DataManager.VideoType type = DataManager.Instance.currentVideoType;

            if (type == DataManager.VideoType.Intro)
            {
                GameManager.Instance.ChangeState(GameManager.GameState.CharacterSelect);
            }
            else if (type == DataManager.VideoType.Outro)
            {
                GameManager.Instance.ChangeState(GameManager.GameState.Result);
            }
        }
    }
    #endregion
}