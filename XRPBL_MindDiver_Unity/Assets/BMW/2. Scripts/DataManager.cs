using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 게임 내 데이터(점수, 스탯, 진행도 등) 및 통계를 관리하는 클래스
/// </summary>
public class DataManager : MonoBehaviour
{
    #region Singleton
    public static DataManager Instance { get; private set; }
    #endregion

    #region Enums
    public enum VideoType { Intro, Outro }
    public enum LangType { Kor }
    #endregion

    #region Inspector Fields
    [Header("Video Settings")]
    // 현재 설정된 비디오 타입
    [SerializeField] public VideoType currentVideoType;

    [Header("Setting Data")]
    [SerializeField] [Range(0, 100)] private int BGMVolume;
    [SerializeField] [Range(0, 100)] private int SFXVolume;
    [SerializeField] [Range(0, 100)] private int videoVolume;
    [SerializeField] [Range(0, 100)] private int NARVolume;
    [SerializeField] private LangType currentLangType;

    [Header("In-Game Data")]
    // 게임 진행도
    [SerializeField][Range(0, 100)] private float progress;
    // 팀 점수
    [SerializeField] private int teamScore;
    // 우주선 체력
    [SerializeField][Range(0, 100)] private int shipHealth;
    // 최대 체력 수치
    [SerializeField] public int maxShipHealth = 100;
    // 우주선 쉴드
    [SerializeField][Range(0, 100)] private int shipShield;
    // 최대 쉴드 수치
    [SerializeField] public int maxShipShield = 100;
    // 버퍼 게이지 사용량
    [SerializeField][Range(0, 10)] public int bufferUse = 5;
    // 버퍼 게이지 충전량
    [SerializeField][Range(0, 10)] private int bufferCharge;
    // 버퍼 게이지 사용량
    [SerializeField][Range(0, 10)] public int debufferUse = 5;
    // 디버퍼 게이지 충전량
    [SerializeField][Range(0, 10)] private int debufferCharge;
    // 게이지 최대 충전량
    [SerializeField] private int maxCharge = 10;
    // 현재 보유 총알 수
    [SerializeField] [Range(0, 9999)] private int bullet;
    // 최대 보유 가능 총알 수
    [SerializeField]public int maxBullet = 9999;

    [Header("Statistics")]
    // 총 플레이 시간 (초 단위)
    [SerializeField] private float totalPlayTime;
    // 처치한 적 수
    [SerializeField] private int totalEnemiesKilled;
    // 받은 총 데미지
    [SerializeField] private int totalDamageTaken;

    [Header("Debug Settings")]
    // 디버그 로그 출력 여부
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Private Fields
    // 타이머 작동 여부 확인
    private bool _isTimerRunning = false;
    #endregion

    #region Events
    // 체력 수치 변경 시 발생 (현재 쉴드, 최대 쉴드)
    public event Action<int, int> OnHealthChanged;
    // 쉴드 수치 변경 시 발생 (현재 쉴드, 최대 쉴드)
    public event Action<int, int> OnShieldChanged;
    // 버퍼 충전 완료 또는 추가 시 발생
    public event Action OnBufferAdded;
    // 디버퍼 충전 완료 또는 추가 시 발생
    public event Action OnDeBufferAdded;
    #endregion

    #region Unity Lifecycle
    /*
     * 싱글톤 인스턴스 초기화 및 파괴 방지 설정
     */
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        InitializeSettingData();
    }

    /*
     * 타이머 작동 시 플레이 시간 누적
     */
    private void Update()
    {
        if (_isTimerRunning)
        {
            totalPlayTime += Time.deltaTime;
        }
    }
    #endregion

    #region Public Methods - Control
    /*
     * 재생할 비디오 타입 설정
     */
    public void SetVideoType(VideoType type)
    {
        currentVideoType = type;
        GameManager.Instance.Log($"[DataManager] Set Video Type: {type}");
    }

    /*
     * 게임 시작 시 모든 데이터 초기화
     */
    public void InitializeSettingData()
    {
        SetBGMVolume(100);
        SetSFXVolume(100);
        SetVideoVolume(100);
        SetNARVolume(100);

        _isTimerRunning = true;
        Log("[DataManager] Setting Data Initialized");
    }
    public void InitializeGameData()
    {
        SetProgress(0);
        SetScore(0);
        if(IngameUIManager.Instance != null) IngameUIManager.Instance.UpdateScore(0);
        SetShipHealth(maxShipHealth);
        if (IngameUIManager.Instance != null) IngameUIManager.Instance.UpdateHP(maxShipHealth);
        SetShipShield(maxShipShield);
        if (IngameUIManager.Instance != null) IngameUIManager.Instance.UpdateHP(maxShipShield);
        SetBuffer(0);
        if (IngameUIManager.Instance != null) IngameUIManager.Instance.UpdateBuff(0);
        SetDeBuffer(0);
        if (IngameUIManager.Instance != null) IngameUIManager.Instance.UpdateDeBuff(0);
        SetBullet(maxBullet);
        if (IngameUIManager.Instance != null) IngameUIManager.Instance.UpdateBullet(maxBullet);
        SetIncrementKillCount(0);
        SetTotalDamageTaken(0);
        SetTotalPlayTime(0);

        _isTimerRunning = true;
        GameManager.Instance.Log("[DataManager] Game Data Initialized");
    }

    /*
     * 플레이 시간 측정 중단
     */
    public void StopTimer() => _isTimerRunning = false;
    #endregion

    #region Public Methods - Data Access
    // --- Progress ---

    public int GetBGMVolume() { return BGMVolume; }
    public void SetBGMVolume(int value) { BGMVolume = value; }

    public int GetSFXVolume() { return SFXVolume; }
    public void SetSFXVolume(int value) { SFXVolume = value;}

    public int GetVideoVolume() { return videoVolume; }
    public void SetVideoVolume(int value) { videoVolume = value; }

    public int GetNARVolume() { return NARVolume; }
    public void SetNARVolume(int value) { NARVolume = value; }

    /*
     * 진행도 수치 누적
     */
    public float GetProgress() { return progress; }
    public void SetProgress(float value) { progress = value; IngameUIManager.Instance.UpdateProgress(progress); }

    // --- Score ---
    /*
     * 점수 수치 누적
     */
    public void AddScore(int amount) { teamScore += amount; IngameUIManager.Instance.UpdateScore(teamScore); }
    public int GetScore() { return teamScore; }
    public void SetScore(int value) { teamScore = value; IngameUIManager.Instance.UpdateScore(teamScore); }

    // --- Shield / Health ---
    /*
     * 데미지 피격 처리 및 쉴드 감소
     */
    public void TakeDamage(int amount)
    {
        if (shipShield > 0)
        {
            if (shipShield >= amount) shipShield -= amount;
            else if ((shipShield + shipHealth) >= amount) { shipHealth -= (amount - shipShield); shipShield = 0;}
            else { shipHealth = 0; IngameUIManager.Instance.UpdateHP(shipHealth); shipShield = 0; }
            IngameUIManager.Instance.UpdateShield(shipShield);
            OnShieldChanged?.Invoke(shipShield, maxShipShield);
        }
        else
        {
            if (shipHealth >= amount) shipHealth -= amount;
            else { shipHealth = 0; }
            IngameUIManager.Instance.UpdateHP(shipHealth);
            OnHealthChanged?.Invoke(shipHealth, maxShipHealth);
        }
        totalDamageTaken += amount;
    }

    public int GetShipHealth() { return shipHealth; }
    public void SetShipHealth(int value)
    {
        shipHealth = value;
        IngameUIManager.Instance.UpdateHP(shipHealth);
        OnHealthChanged?.Invoke(shipHealth, maxShipHealth);
    }

    public int GetShipShield() { return shipShield; }
    public void SetShipShield(int value)
    {
        shipShield = value;
        IngameUIManager.Instance.UpdateShield(shipShield);
        OnShieldChanged?.Invoke(shipShield, maxShipShield);
    }

    public int GetTotalDamageTaken() { return totalDamageTaken; }
    public void SetTotalDamageTaken(int value) { totalDamageTaken = value; }

    // --- Buffer / Debuffer ---
    /*
    * 버퍼 게이지 충전
    */
    public void AddBuffer(int amount) { bufferCharge = Mathf.Min(bufferCharge + amount, maxCharge); IngameUIManager.Instance.UpdateBuff(bufferCharge); }
    public int GetBuffer() { return debufferCharge; }
    public void SetBuffer(int value) { bufferCharge = value; IngameUIManager.Instance.UpdateBuff(bufferCharge); }

    /*
     * 디버퍼 게이지 충전
     */
    public void AddDeBuffer(int amount) { debufferCharge = Mathf.Min(debufferCharge + amount, maxCharge); IngameUIManager.Instance.UpdateDeBuff(debufferCharge); }
    public int GetDeBuffer() { return debufferCharge; }
    public void SetDeBuffer(int value) { debufferCharge = value; IngameUIManager.Instance.UpdateDeBuff(debufferCharge); }

    // --- Bullet ---
    /*
     * 총알 수량 추가
     */
    public void AddBullet(int amount) { bullet += amount; IngameUIManager.Instance.UpdateBullet(bullet); }
    public int GetBullet() { return bullet; }
    public void SetBullet(int value) { bullet = value; IngameUIManager.Instance.UpdateBullet(bullet); }

    // --- Statistics ---
    /*
     * 처치한 적 카운트 증가
     */
    public void IncrementKillCount() => totalEnemiesKilled++;
    public int GetIncrementKillCount() { return totalEnemiesKilled; }
    public void SetIncrementKillCount(int value) { totalEnemiesKilled = value; }

    public int GetTotalPlayTime() { return (int)totalPlayTime; }
    public void SetTotalPlayTime(int value) { totalPlayTime = value; }
    #endregion

    #region Helper Methods
    /*
     * 디버그 모드 시 로그 출력 수행
     */
    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
    #endregion
}