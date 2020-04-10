using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO;
using UnityEngine.UI;
using System;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using syp.biz.SockJS.NET.Client.Event;
using syp.biz.SockJS.NET.Client;
using syp.biz.SockJS.NET.Common.Interfaces;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public class StompWebSocketManager : MonoBehaviour
{
    public string url = "https://api/stomp";
    Thread thread;
    SockJS sockJs;
    float socketEchoCountdown = 20f;
    float secd;

    public const string serialNumber = "Test";
    public const string code = "code";

    public List<string> messageList;

    CancellationTokenSource applicationCloseToken;
    // Start is called before the first frame update
    private void Awake()
    {
        //WebRequest.DefaultWebProxy = new WebProxy("127.0.0.1", 8888);
        //Debug.Log(WebRequest.DefaultWebProxy.ToString());
    }
    void Start()
    {
        applicationCloseToken = new CancellationTokenSource();
        Open();
    }
    void Open()
    {
        Task.Run(() => { CreateClient(); }, applicationCloseToken.Token);
        //thread.IsBackground = false;
        //thread.Start();
        UnityEngine.Debug.Log("Open");
    }
    void Update()
    {
        if (secd > 0) secd -= Time.deltaTime;
        else
        {
            secd = socketEchoCountdown;
            if (sockJs != null) sockJs.Close();
            Open();

        }
    }
    private void OnDestroy()
    {
        if (applicationCloseToken != null) applicationCloseToken.Cancel();
        sockJs.Close();
    }

    async void CreateClient()
    {
        SockJS.SetLogger(new ConsoleLogger());
        sockJs = new SockJS(url);
        sockJs.onPong = () => secd = socketEchoCountdown;
        await Task.Delay(100);
        sockJs.AddEventListener("open", (sender, e) =>
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("clientType", "TYPE");
            headers.Add("serialNumber", serialNumber);
            headers.Add("code", MyHash(code + GetTimeStamp()));
            sockJs.Connect(headers);
            UnityEngine.Debug.Log("Connecting");
        });
        sockJs.AddEventListener("CONNECTED", (sender, e) =>
        {
            UnityEngine.Debug.Log($"****************** CONNECTED: Message: {string.Join(",", e.Select(o => o?.ToString()))}");
            sockJs.Subscribe("/user/queue/control", new Dictionary<string, string>());
            sockJs.Subscribe("/user/queue/errors", new Dictionary<string, string>());
        });
        sockJs.AddEventListener("MESSAGE", (sender, e) =>
        {
            UnityEngine.Debug.Log($"****************** Main: Message: {string.Join(",", e.Select(o => o?.ToString()))}");
            if (e[0] is TransportMessageEvent msg)
            {
                var dataString = msg.Data.ToString();
                if (dataString == "test")
                {
                    UnityEngine.Debug.Log($"****************** Main: Got back echo -> sending shutdown");
                    //                                sockJs.Send("shutdown");
                    //                            }
                    //                            else if (dataString == "ok")
                    //                            {
                    //                                Console.WriteLine($"****************** Main: Got back shutdown confirmation");
                    sockJs.Close();
                }
            }
        });
        sockJs.AddEventListener("close", (sender, e) =>
        {
            UnityEngine.Debug.Log($"****************** Main: Closed");
        });
    }
    [ContextMenu("DebugState")]
    public void DebugStage()
    {
        UnityEngine.Debug.Log("当前连接状态:" + sockJs.ReadyState.ToString());
    }
    private static string MyHash(string data)
    {
        Console.WriteLine($"data: {data}");
        MD5 ma5Hash = MD5.Create();
        String result = data;
        for (int i = 0; i < 10; i++)
        {
            result = HashCode(ma5Hash, result);
        }
        return result;
    }
    private static string HashCode(MD5 myHash, string data)
    {
        byte[] Hdata = Encoding.UTF8.GetBytes(data);
        Hdata = myHash.ComputeHash(Hdata);
        StringBuilder str = new StringBuilder();
        for (int j = 0; j < Hdata.Length; j++)
        {
            str.Append(Hdata[j].ToString("x2"));
        }
        String result = str.ToString();
        return result;
    }
    static string GetTimeStamp()
    {
        TimeSpan st = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0, 0) - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        string time = (Convert.ToInt64(st.TotalMilliseconds)).ToString();
        return time;
    }

    public static void OnDebug(string massage)
    {
        if (massage.ToLower().Contains("timeout"))
        {

        };
    }

    internal class ConsoleLogger : syp.biz.SockJS.NET.Common.Interfaces.ILogger
    {
        [DebuggerStepThrough, DebuggerNonUserCode]
        public void Debug(string message) => OnDebug(message);

        [DebuggerStepThrough, DebuggerNonUserCode]
        public void Error(string message) => UnityEngine.Debug.Log($"{DateTime.Now:s} [ERR] {message}");

        [DebuggerStepThrough, DebuggerNonUserCode]
        public void Info(string message) => UnityEngine.Debug.Log($"{DateTime.Now:s} [INF] {message}");
    }





}
