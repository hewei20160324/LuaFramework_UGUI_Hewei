using UnityEngine;
using System.Collections;
using LuaInterface;
using System;
using System.IO;

//展示searchpath 使用，require 与 dofile 区别
public class ScriptsFromFile : MonoBehaviour 
{
    LuaState lua = null;
    private string strLog = "";    

	void Start () 
    {
#if UNITY_5		
        Application.logMessageReceived += Log;
#else
        Application.RegisterLogCallback(Log);
#endif         
        lua = new LuaState();
        
        lua.BeginPreLoad();
        lua.RegFunction("socket.core", LuaOpen_Socket_Core);
        lua.RegFunction("mime.core", LuaOpen_Mime_Core);
        lua.EndPreLoad();
        OpenZbsDebugger();

        lua.Start();
    }

    [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
    static int LuaOpen_Socket_Core(IntPtr L)
    {
        return LuaDLL.luaopen_socket_core(L);
    }

    [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
    static int LuaOpen_Mime_Core(IntPtr L)
    {
        return LuaDLL.luaopen_mime_core(L);
    }

    public void OpenZbsDebugger(string ip = "localhost")
    {
        lua.LuaDoString(string.Format("DebugServerIp = '{0}' require(\"mobdebug\").start(DebugServerIp)", ip));
    }

    void Log(string msg, string stackTrace, LogType type)
    {
        strLog += msg;
        strLog += "\r\n";
    }

    void OnGUI()
    {
        GUI.Label(new Rect(100, Screen.height / 2 - 100, 600, 400), strLog);

        if (GUI.Button(new Rect(50, 50, 120, 45), "test"))
        {
            strLog = "";
            lua.Require("test");                        
        }
        else if (GUI.Button(new Rect(50, 150, 120, 45), "hewei_test.test"))
        {
            strLog = "";
            lua.DoFile("hewei_test.test");
        }

        lua.Collect();
        lua.CheckTop();
    }

    void OnApplicationQuit()
    {
        lua.Dispose();
        lua = null;

#if UNITY_5		
        Application.logMessageReceived -= Log;
#else
        Application.RegisterLogCallback(null);
#endif 
    }
}
