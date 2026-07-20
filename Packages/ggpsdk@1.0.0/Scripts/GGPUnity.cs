using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System;

public static class GGPUnity 
{
	private const string version = "1";

	public static int Init()
	{
		int nRet = 0;
#if UNITY_ANDROID
		nRet = cxa_current_primary_free_exception(101, 0, 0, 0, 0, null, IntPtr.Zero);
#endif
		return nRet;
	}

	public static int setMatchId(string matchId)
	{
		int nRet = 0;
#if UNITY_ANDROID
		nRet = cxa_current_primary_set_exception(107, 0, matchId, null, null, null, null, null);
#endif
		return nRet;
	}
	public static int setUserInfo(string roleName, string roleAccount, string roleId)
	{
		int nRet = 0;
#if UNITY_ANDROID
		nRet = cxa_current_primary_set_exception(110,0, roleName, roleAccount, roleId, null, null, null);
#endif
		return nRet;
	}

	public static int setUserInfoEx(string roleName, string roleAccount, string roleId, string serverName, string channelName, string gameJson)
	{
		int nRet = 0;
#if UNITY_ANDROID
		nRet = cxa_current_primary_set_exception(102, 0,roleName, roleAccount, roleId, serverName, channelName, gameJson);
#endif
		return nRet;
	}

	public static int setSGData(string sgData)
	{
		int nRet = 0;
#if UNITY_ANDROID
		nRet = cxa_current_primary_set_exception(120, 0, sgData, null, null, null, null, null);
#endif
		return nRet;
	}

	public static byte[] getSGData()
	{
		byte[] sgData=null;
#if UNITY_ANDROID
			IntPtr addr = cxa_current_primary_mal_exception(103, 0x10010, 0, 0, 0, null, null);
			if (addr == IntPtr.Zero)
			{
				return null;
			}
			int dataSize = (int)Marshal.ReadInt32(addr, 0);
			//UnityEngine.Debug.Log("dataSize:" + dataSize);
			IntPtr dataPtr = new IntPtr(addr.ToInt64() + 4);
			sgData = new byte[dataSize];
			Marshal.Copy(dataPtr, sgData, 0, dataSize);

			//释放native的结果
			cxa_current_primary_free_exception(105, 0, 0, 0, 0, null, addr);			
#endif
		return sgData;
	}

	public static string getSign(string inputData)
	{
		string strRet = "";
#if UNITY_ANDROID
		IntPtr addr = cxa_current_primary_mal_exception(201, 0x10001, 0, 0, 0, inputData, null);
		if (addr == IntPtr.Zero)
		{
			return "";
		}
		int dataSize = (int)Marshal.ReadInt32(addr, 0);
		//UnityEngine.Debug.Log("dataSize:" + dataSize);
		IntPtr dataPtr = new IntPtr(addr.ToInt64() + 4);
		byte[] bytes = new byte[dataSize];
		Marshal.Copy(dataPtr, bytes, 0, dataSize);

		//释放native的结果
		cxa_current_primary_free_exception(105, 0, 0, 0, 0, null, addr);
		strRet = System.Text.Encoding.ASCII.GetString(bytes);
#endif
		return strRet;
	}

	public static string getBytesSign(byte[] byteArray)
	{
		string strRet = "";
#if UNITY_ANDROID
		IntPtr addr = cxa_current_primary_mal_exception(201, 0x10010, byteArray.Length, 0, 0, null, byteArray);
		if (addr == IntPtr.Zero)
		{
			return "";
		}
		int dataSize = (int)Marshal.ReadInt32(addr, 0);
		//UnityEngine.Debug.Log("dataSize:" + dataSize);
		IntPtr dataPtr = new IntPtr(addr.ToInt64() + 4);
		byte[] bytes = new byte[dataSize];
		Marshal.Copy(dataPtr, bytes, 0, dataSize);

		//释放native的结果
		cxa_current_primary_free_exception(105, 0, 0, 0, 0, null, addr);
		strRet = System.Text.Encoding.ASCII.GetString(bytes);
#endif
		return strRet;
	}

	public static string getUltraSign(string inputData)
	{
		string strRet = "";
#if UNITY_ANDROID
		IntPtr addr = cxa_current_primary_mal_exception(201, 0x10002, 0, 0, 0, inputData, null);
		if (addr == IntPtr.Zero)
		{
			return "";
		}
		int dataSize = (int)Marshal.ReadInt32(addr, 0);
		//UnityEngine.Debug.Log("dataSize:" + dataSize);
		IntPtr dataPtr = new IntPtr(addr.ToInt64() + 4);
		byte[] bytes = new byte[dataSize];
		Marshal.Copy(dataPtr, bytes, 0, dataSize);

		//释放native的结果
		cxa_current_primary_free_exception(105, 0, 0, 0, 0, null, addr);
		strRet = System.Text.Encoding.ASCII.GetString(bytes);
#endif
		return strRet;
	}

	public static string getBytesUltraSign(byte[] byteArray)
	{
		string strRet = "";
#if UNITY_ANDROID
		IntPtr addr = cxa_current_primary_mal_exception(201, 0x10020, byteArray.Length, 0, 0, null, byteArray);
		if (addr == IntPtr.Zero)
		{
			return "";
		}
		int dataSize = (int)Marshal.ReadInt32(addr, 0);
		//UnityEngine.Debug.Log("dataSize:" + dataSize);
		IntPtr dataPtr = new IntPtr(addr.ToInt64() + 4);
		byte[] bytes = new byte[dataSize];
		Marshal.Copy(dataPtr, bytes, 0, dataSize);

		//释放native的结果
		cxa_current_primary_free_exception(105, 0, 0, 0, 0, null, addr);
		strRet = System.Text.Encoding.ASCII.GetString(bytes);
#endif
		return strRet;
	}
	//返回模拟器和虚拟机检测结果的加密字符串
	public static string getEVData()
    {
		string strRet = "";
#if UNITY_ANDROID
		IntPtr addr = cxa_current_primary_mal_exception(103, 0x10003, 0, 0, 0, null, null);
		if (addr == IntPtr.Zero)
		{
			return "";
		}
		int dataSize = (int)Marshal.ReadInt32(addr, 0);
		//UnityEngine.Debug.Log("dataSize:" + dataSize);
		IntPtr dataPtr = new IntPtr(addr.ToInt64() + 4);
		byte[] bytes = new byte[dataSize];
		Marshal.Copy(dataPtr, bytes, 0, dataSize);

		//释放native的结果
		cxa_current_primary_free_exception(105, 0, 0, 0, 0, null, addr);
		strRet = System.Text.Encoding.ASCII.GetString(bytes);
#endif
		return strRet;
	}

	public static string setGuardId(int guardId)
	{
		string strRet = "";
#if UNITY_ANDROID
		IntPtr addr = cxa_current_primary_mal_exception(221, 0x20001, guardId, 0, 0, null, null);
		if (addr == IntPtr.Zero)
		{
			return "";
		}
		int dataSize = (int)Marshal.ReadInt32(addr, 0);
		//UnityEngine.Debug.Log("dataSize:" + dataSize);
		IntPtr dataPtr = new IntPtr(addr.ToInt64() + 4);
		byte[] bytes = new byte[dataSize];
		Marshal.Copy(dataPtr, bytes, 0, dataSize);

		//释放native的结果
		cxa_current_primary_free_exception(105, 0, 0, 0, 0, null, addr);
		strRet = System.Text.Encoding.ASCII.GetString(bytes);
#endif
		return strRet;
	}

	public static string getGuardData()
	{
		string strRet = "";
#if UNITY_ANDROID
		IntPtr addr = cxa_current_primary_mal_exception(103, 0x10002, 0, 0, 0, null, null);
		if (addr == IntPtr.Zero)
		{
			return "E925059DE8B8FEBBB12F8B6CFE2DE89A";
		}
		int dataSize = (int)Marshal.ReadInt32(addr, 0);
        //UnityEngine.Debug.Log("dataSize:" + dataSize);
        IntPtr dataPtr = new IntPtr(addr.ToInt64() + 4);
		byte[] bytes = new byte[dataSize];
		Marshal.Copy(dataPtr, bytes, 0, dataSize);

		//释放native的结果
		cxa_current_primary_free_exception(105, 0, 0, 0, 0, null, addr);
		strRet = System.Text.Encoding.ASCII.GetString(bytes);
#endif
		return strRet;
	}

	//是否是模拟器
	public static bool getSEResult()
    {
		bool bRet = false;
#if UNITY_ANDROID
		IntPtr addr = cxa_current_primary_mal_exception(211, 0x10001, 0, 0, 0, null, null);
		if (addr != IntPtr.Zero)
		{		
			int dataSize = (int)Marshal.ReadInt32(addr, 0);
			//UnityEngine.Debug.Log("dataSize:" + dataSize);
			if (dataSize == 8)
			{
				IntPtr dataPtr1 = new IntPtr(addr.ToInt64() + 4);
				IntPtr dataPtr2 = new IntPtr(addr.ToInt64() + 8);
				byte[] bytes1 = new byte[4];
				Marshal.Copy(dataPtr1, bytes1, 0, 4);
				byte[] bytes2 = new byte[4];
				Marshal.Copy(dataPtr2, bytes2, 0, 4);
				uint   fstUn   =   System.BitConverter.ToUInt32(bytes1,0); 
				uint   secUn   =   System.BitConverter.ToUInt32(bytes2,0); 
				//UnityEngine.Debug.Log("key:" + fstUn);
				//UnityEngine.Debug.Log("value:" + secUn);
				uint decKey = (fstUn>>2) | (fstUn<<30);
				uint valueUn = decKey^secUn;
				//UnityEngine.Debug.Log("ret:" + valueUn);
				if (valueUn>0)
				{
					bRet = true;
				}
			}
			//释放native的结果
			cxa_current_primary_free_exception(105, 0, 0, 0, 0, null, addr);
		}
#endif
		return bRet;
	}


	[DllImport("GGP")]
	private static extern int cxa_current_primary_free_exception(int nFunId, int nArg1, int nArg2, int nArg3, long llArg1, string pInput1, IntPtr pInt);
	[DllImport("GGP")]
	private static extern IntPtr cxa_current_primary_mal_exception(int nFunId, int nArg1, int nArg2, int nArg3, long llArg1, string pInput1, [MarshalAs(UnmanagedType.LPTStr)]byte[] pInput2);
	[DllImport("GGP")]
	private static extern int cxa_current_primary_set_exception(int nFunId,int nArg1,string pInput1, string pInput2, string pInput3, string pInput4, string pInput5, string pInput6);
}