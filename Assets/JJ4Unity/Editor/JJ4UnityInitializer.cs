using UnityEditor;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;

namespace JJ4Unity.Editor
{
    [InitializeOnLoad]
    public static class JJ4UnityInitializer
    {
        private const string KeywordRefresh = "#refresh";
        private const int Port = 28999;

        private static Thread _serverThread;
        private static TcpListener _listener;
        private static bool _isRunning = false;

        static JJ4UnityInitializer()
        {
            JJ4UnityEditorConfig.Initialize();
            
            StopServer();

            if (false == JJ4UnityEditorConfig.IsConnectToVSCode)
            {
                Runtime.Extension.Debug.Log("Abort running JJ4Unity for VSCode server - JJ4UnityEditorConfig.IsConnectToVSCode is false.");
                return;
            }
            
            StartServer();

            EditorApplication.quitting -= StopServer;
            EditorApplication.quitting += StopServer;
        }

        private static void RefreshAssets()
        {
            // NOTE(JJO): 1번만 실행하게 하기 위한 구문
            EditorApplication.update -= RefreshAssets;

            // NOTE(JJO): 아래 코드를 쓰면 저장할 때마다 무조건 컴파일해서 Refresh만 호출
            // EditorUtility.RequestScriptReload();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Console.WriteLine("[JJ4Unity] Script refreshed!!");
        }

        private static void StartServer()
        {
            if (_isRunning && null != _listener)
            {
                Runtime.Extension.Debug.LogWarning("JJ4Unity for VSCode Server is already running!");
                return;
            }

            _serverThread = new(() =>
            {
                _listener = new(IPAddress.Any, Port);
                _listener.Start();
                _isRunning = true;

                while (_isRunning)
                {
                    if (false == _listener.Pending())
                    {
                        Thread.Sleep(500); // Check every 500ms
                        continue;
                    }

                    var client = _listener.AcceptTcpClient();
                    var stream = client.GetStream();
                    var buffer = new byte[1024];
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (KeywordRefresh == message)
                    {
                        EditorApplication.update += RefreshAssets;
                    }

                    client.Close();
                }
            });

            _serverThread.IsBackground = true;
            _serverThread.Start();

            Runtime.Extension.Debug.Log("Try to run a server about JJ4Unity for VSCode.");
        }

        private static void StopServer()
        {
            _isRunning = false;
            _listener?.Stop();
            _listener = null;
            _serverThread?.Abort();
            _serverThread = null;

            Runtime.Extension.Debug.Log("JJ4Unity for VSCode Server is stopped.");
        }
    }
}
