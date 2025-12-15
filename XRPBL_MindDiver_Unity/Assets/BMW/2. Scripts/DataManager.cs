using Mover;
using System;
using UnityEngine;

/// <summary>
/// 게임 내 데이터(점수, 스탯, 진행도 등) 및 통계를 관리하는 클래스
/// - 수정됨: FindObjectsOfType -> FindObjectsByType (최신 유니티 API 적용)
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
    [SerializeField][Range(0, 100)] private int BGMVolume;
    [SerializeField][Range(0, 100)] private int SFXVolume;
    [SerializeField][Range(0, 100)] private int videoVolume;
    [SerializeField][Range(0, 100)] private int NARVolume;
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
    // 버퍼 지속시간
    [SerializeField][Range(0, 10)] public float buffDuration = 10f;
    // 버퍼 상태
    [SerializeField] public bool isBuffState;
    // 디버퍼 게이지 사용량
    [SerializeField][Range(0, 10)] public int debufferUse = 5;
    // 디버퍼 게이지 충전량
    [SerializeField][Range(0, 10)] private int debufferCharge;
    // 디버퍼 지속시간
    [SerializeField][Range(0, 10)] public float DebuffDuration = 10f;
    // 디버퍼 상태
    [SerializeField] public bool isDebuffState;
    // 게이지 최대 충전량
    [SerializeField] private int maxCharge = 10;
    // 현재 보유 총알 수
    [SerializeField][Range(0, 9999)] private int bullet;
    // 최대 보유 가능 총알 수
    [SerializeField] public int maxBullet = 9999;

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
    // 버퍼 타이머
    private float _bufferTimer = 0f;
    // 디버퍼 타이머
    private float _debufferTimer = 0f;

    // 쉴드 회복량 계산을 위한 누적 변수
    private float _shieldRegenAccumulator = 0f;
    #endregion

    #region Events
    public event Action<int, int> OnHealthChanged;
    public event Action<int, int> OnShieldChanged;
    public event Action OnBufferAdded;
    public event Action OnDeBufferAdded;
    #endregion

    #region Unity Lifecycle
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

    private void Update()
    {
        if (_isTimerRunning)
        {
            totalPlayTime += Time.deltaTime;
        }

        // --- 버퍼(무적 + 쉴드 회복) 지속시간 체크 및 로직 ---
        if (isBuffState)
        {
            // 1. 지속시간 감소
            _bufferTimer -= Time.deltaTime;

            // 2. 쉴드 서서히 회복 로직
            if (shipShield < maxShipShield)
            {
                float totalRegenAmount = maxShipShield * 0.5f; // 목표 회복량 (절반)
                float regenPerSecond = totalRegenAmount / buffDuration; // 초당 회복량

                // 프레임당 회복량을 누적
                _shieldRegenAccumulator += regenPerSecond * Time.deltaTime;

                if (_shieldRegenAccumulator >= 1f)
                {
                    int amountToAdd = (int)_shieldRegenAccumulator;
                    _shieldRegenAccumulator -= amountToAdd;

                    // 쉴드 적용
                    SetShipShield(Mathf.Min(shipShield + amountToAdd, maxShipShield));
                }
            }

            // 3. 종료 체크
            if (_bufferTimer <= 0)
            {
                isBuffState = false;
                _shieldRegenAccumulator = 0f;
                Log("[DataManager] Buffer Effect Ended (Invincibility OFF)");
            }
        }

        // --- 디버퍼 지속시간 체크 ---
        if (isDebuffState)
        {
            _debufferTimer -= Time.deltaTime;

            // 디버프 시간 종료 시
            if (_debufferTimer <= 0)
            {
                isDebuffState = false;

                // [수정됨] 최신 API 사용 (FindObjectsSortMode.None으로 성능 최적화)
                EnemyMover[] enemies = FindObjectsByType<EnemyMover>(FindObjectsSortMode.None);
                foreach (var enemy in enemies)
                {
                    if (enemy != null) enemy.DebuffEnd();
                }

                Log("[DataManager] Debuffer Effect Ended (All Enemies Restored)");
            }
        }
    }
    #endregion

    #region Public Methods - Control
    public void SetVideoType(VideoType type)
    {
        currentVideoType = type;
        GameManager.Instance.Log($"[DataManager] Set Video Type: {type}");
    }

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
        IngameUIManager.Instance.InitializeSliders();

        SetProgress(0);
        SetScore(0);
        SetShipHealth(maxShipHealth);
        SetShipShield(maxShipShield);

        SetBuffer(0);
        SetBuffState(false);
        _bufferTimer = 0f;
        _shieldRegenAccumulator = 0f;

        SetDeBuffer(0);
        SetDebuffState(false);
        _debufferTimer = 0f;

        SetBullet(maxBullet);
        SetIncrementKillCount(0);
        SetTotalDamageTaken(0);
        SetTotalPlayTime(0);

        _isTimerRunning = true;
        GameManager.Instance.Log("[DataManager] Game Data Initialized");
    }

    public void StopTimer() => _isTimerRunning = false;
    #endregion

    #region Public Methods - Data Access
    // --- Progress ---
    public int GetBGMVolume() { return BGMVolume; }
    public void SetBGMVolume(int value) { BGMVolume = value; }
    public int GetSFXVolume() { return SFXVolume; }
    public void SetSFXVolume(int value) { SFXVolume = value; }
    public int GetVideoVolume() { return videoVolume; }
    public void SetVideoVolume(int value) { videoVolume = value; }
    public int GetNARVolume() { return NARVolume; }
    public void SetNARVolume(int value) { NARVolume = value; }
    public float GetProgress() { return progress; }
    public void SetProgress(float value) { progress = value; IngameUIManager.Instance.UpdateProgress(progress); }

    // --- Score ---
    public void AddScore(int amount) { teamScore += amount; IngameUIManager.Instance.UpdateScore(teamScore); }
    public int GetScore() { return teamScore; }
    public void SetScore(int value) { teamScore = value; IngameUIManager.Instance.UpdateScore(teamScore); }

    // --- Shield / Health ---
    /*
     * 데미지 피격 처리 및 쉴드 감소
     * 버퍼 활성화 시 무적 처리
     */
    public void TakeDamage(int amount)
    {
        // 1. 버퍼 상태(무적) 확인
        if (isBuffState)
        {
            Log("[DataManager] Damage Ignored (Buff Active)");
            return;
        }

        // 2. 기존 데미지 로직 수행
        if (shipShield > 0)
        {
            if (shipShield >= amount) shipShield -= amount;
            else if ((shipShield + shipHealth) >= amount) { shipHealth -= (amount - shipShield); shipShield = 0; }
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
    public void AddBuffer(int amount) { bufferCharge = Mathf.Min(bufferCharge + amount, maxCharge); IngameUIManager.Instance.UpdateBuff(bufferCharge); }
    public int GetBuffer() { return bufferCharge; }
    public void SetBuffer(int value) { bufferCharge = value; IngameUIManager.Instance.UpdateBuff(bufferCharge); }

    public bool GetBuffState() { return isBuffState; }
    public void SetBuffState(bool value) { isBuffState = value; }

    /*
     * 버퍼 활성화
     */
    public void ActivateBuff()
    {
        if (bufferCharge >= bufferUse)
        {
            isBuffState = true;
            _bufferTimer = buffDuration;
            _shieldRegenAccumulator = 0f;
            AddBuffer(-bufferUse);
            Log("[DataManager] Buffer Activated: Invincible + Shield Regen");
        }
        else
        {
            Log("[DataManager] Not enough Buffer Charge");
        }
    }


    public void AddDeBuffer(int amount) { debufferCharge = Mathf.Min(debufferCharge + amount, maxCharge); IngameUIManager.Instance.UpdateDeBuff(debufferCharge); }
    public int GetDeBuffer() { return debufferCharge; }
    public void SetDeBuffer(int value) { debufferCharge = value; IngameUIManager.Instance.UpdateDeBuff(debufferCharge); }

    public bool GetDebuffState() { return isDebuffState; }
    public void SetDebuffState(bool value) { isDebuffState = value; }

    /*
     * 디버퍼 활성화
     */
    public void ActivateDebuff()
    {
        if (debufferCharge >= debufferUse)
        {
            isDebuffState = true;
            _debufferTimer = DebuffDuration;
            AddDeBuffer(-debufferUse);

            // [수정됨] 최신 API 사용 (FindObjectsSortMode.None으로 성능 최적화)
            EnemyMover[] enemies = FindObjectsByType<EnemyMover>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy != null) enemy.DebuffStart();
            }

            Log("[DataManager] Debuffer Activated (All Enemies Weakened)");
        }
        else
        {
            Log("[DataManager] Not enough Debuffer Charge");
        }
    }

    // --- Bullet ---
    public void AddBullet(int amount) { bullet += amount; IngameUIManager.Instance.UpdateBullet(bullet); }
    public int GetBullet() { return bullet; }
    public void SetBullet(int value) { bullet = value; IngameUIManager.Instance.UpdateBullet(bullet); }

    // --- Statistics ---
    public void IncrementKillCount() => totalEnemiesKilled++;
    public int GetIncrementKillCount() { return totalEnemiesKilled; }
    public void SetIncrementKillCount(int value) { totalEnemiesKilled = value; }

    public int GetTotalPlayTime() { return (int)totalPlayTime; }
    public void SetTotalPlayTime(int value) { totalPlayTime = value; }
    #endregion

    #region Helper Methods
    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
    #endregion
}