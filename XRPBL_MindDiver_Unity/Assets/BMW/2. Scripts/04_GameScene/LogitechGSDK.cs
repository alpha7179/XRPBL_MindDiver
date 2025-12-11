using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;
using System;

public class LogitechGSDK
{
    // 상수 정의
    public const int LOGI_MAX_CONTROLLERS = 4;

    // 구조체 정의
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct DIJOYSTATE2ENGINES
    {
        public int lX;              // 스티어링 휠
        public int lY;              // 가속 페달 / 브레이크
        public int lZ;
        public int lRx;
        public int lRy;
        public int lRz;             // 브레이크 / 클러치
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] rglSlider;     // 슬라이더 (추가 페달)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] rgdwPOV;      // 십자키 (D-Pad)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] rgbButtons;   // 버튼 배열
        public int lVX;
        public int lVY;
        public int lVZ;
        public int lVRx;
        public int lVRy;
        public int lVRz;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] rglVSlider;
        public int lAX;
        public int lAY;
        public int lAZ;
        public int lARx;
        public int lARy;
        public int lARz;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] rglASlider;
        public int lFX;
        public int lFY;
        public int lFZ;
        public int lFRx;
        public int lFRy;
        public int lFRz;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] rglFSlider;
    }

    // ------------------------------------------------------------------------
    // 기본 DLL Import (초기화, 업데이트, 입력 읽기)
    // ------------------------------------------------------------------------
    [DllImport("LogitechSteeringWheelEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiSteeringInitialize(bool ignoreXInputControllers);

    [DllImport("LogitechSteeringWheelEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiUpdate();

    [DllImport("LogitechSteeringWheelEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr LogiGetState(int index);

    [DllImport("LogitechSteeringWheelEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern void LogiSteeringShutdown();

    [DllImport("LogitechSteeringWheelEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiIsConnected(int index);

    // ------------------------------------------------------------------------
    // [추가됨] 포스 피드백 (Force Feedback) 관련 DLL Import
    // ------------------------------------------------------------------------

    // 1. 스프링 포스 (중앙 복귀 탄성)
    [DllImport("LogitechSteeringWheelEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiPlaySpringForce(int index, int offsetPercentage, int saturationPercentage, int coefficientPercentage);

    [DllImport("LogitechSteeringWheelEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiStopSpringForce(int index);

    // 2. 댐퍼 포스 (묵직한 저항감)
    [DllImport("LogitechSteeringWheelEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiPlayDamperForce(int index, int coefficientPercentage);

    [DllImport("LogitechSteeringWheelEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiStopDamperForce(int index);

    // ------------------------------------------------------------------------
    // 유니티용 편의 함수
    // ------------------------------------------------------------------------
    public static DIJOYSTATE2ENGINES LogiGetStateUnity(int index)
    {
        DIJOYSTATE2ENGINES ret = new DIJOYSTATE2ENGINES();
        IntPtr ptr = LogiGetState(index);
        try
        {
            if (ptr != IntPtr.Zero)
            {
                ret = (DIJOYSTATE2ENGINES)Marshal.PtrToStructure(ptr, typeof(DIJOYSTATE2ENGINES));
            }
        }
        catch
        {
            Debug.LogError("Logitech SDK: 구조체 데이터를 읽어오는데 실패했습니다.");
        }
        return ret;
    }
}