// 定义 windows dll 的入口

#pragma once

#define WIN32_LEAN_AND_MEAN // 让 windows.h 跳过一些不常用的子模块头
#include <windows.h> // Windows 头文件

#include <iostream>
#include <fstream>


BOOL APIENTRY DllMain(HMODULE hModule,
                      DWORD ul_reason_for_call,
                      LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

long GetThreadId()
{
    return GetCurrentThreadId();
}
