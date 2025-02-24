using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WebsocketClients : SingletonMonoBehavior<WebsocketClients>
{
    [SerializeField] TMP_InputField inputField;
    [SerializeField] private Button _button;
    [SerializeField] private Button _button2;
    private ClientWebSocket ws = null;
    private SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
    private UserData _userData = new();
    private CancellationTokenSource _ctsReceiving = new();
    string _url = "ws://192.168.0.3:8080";
    public Action<string> OnBattleMassege;

    private async void Start()
    {
        _button.onClick.AddListener(async () =>
        {
            _userData.Name = inputField.text;
            await ConnectAsync();
            Debug.Log(_userData.ID);
            await SendCommand("{\"ID\" : \""+_userData.ID+"\", \"Command\" : \"StartBattle\", \"JsonBody\" : \"\"}");
        });
        
        _button2.onClick.AddListener(() =>
        {
            SendCommand("EndBattle");
        });
        
    }

    public void SetUserName(string username)
    {
        _userData.Name = username;
    }
    public void SetUserID(string userID)
    {
        _userData.ID = userID;
    }

    public async UniTask<ClientWebSocket> ConnectAsync()
    {
        if (_userData.Name == default)
        {
            Debug.LogWarning("名前がありません。先にログインして下さい");
            return null;
        }

        ws = new ClientWebSocket();
        Uri serverUri = new Uri(_url);

        try
        {
            Debug.Log("WebSocket に接続中...");
            await ws.ConnectAsync(serverUri, CancellationToken.None);

            if (ws.State == WebSocketState.Open)
            {
                Debug.Log($"WebSocket に接続成功: {_url}");

                // メッセージの受信開始（非同期で実行）
                _ = StartReceiving(_ctsReceiving.Token);
                return ws;
            }
            else
            {
                Debug.LogError("WebSocket 接続失敗");
                ws = null;
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"WebSocket 接続エラー: {ex.Message}");
            ws = null;
            return null;
        }
    }

    /// <summary>
    /// WebSocket サーバーにコマンドを送信する
    /// </summary>
    public async UniTask SendCommand(string command, string jsonbody = "")
    {
        if (ws == null || ws.State != WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket に接続されていないため、コマンドを送信できません。");
            return;
        }

        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes(command);

            await sendLock.WaitAsync();
            try
            {
                await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                    CancellationToken.None);
                Debug.Log($"コマンド送信: {command}");
            }
            finally
            {
                sendLock.Release();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"WebSocket コマンド送信エラー: {ex.Message}");
        }
    }

    /// <summary>
    /// WebSocket メッセージの受信
    /// </summary>
    private async UniTask StartReceiving(CancellationToken cancellationToken)
    {
        Debug.Log("StartReceiving");
        var buffer = new byte[4096]; // バッファサイズを調整
        try
        {
            while (ws != null && ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result =
                    await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Debug.Log($"Received: {message}");
                if (TryParseCommand(message, out CommunicateData commandPack))
                {
                    if (commandPack.Command == "say hallo")
                    {
                        Debug.Log("Hallo");
                    }
                    else if (commandPack.Command == "quit game")
                    {
                        Debug.Log("Quit game");
                        UnityEditor.EditorApplication.isPlaying = false;
                    }
                    else if (commandPack.Command == "message")
                    {
                        Debug.Log(commandPack.JsonBody);
                    }
                    else if (commandPack.Command == "enemy deck")
                    {
                    }
                    else if (commandPack.Command == "enemy input")
                    {
                        Debug.Log($"WS Battle Message Get{commandPack.JsonBody}");
                        OnBattleMassege?.Invoke(commandPack.JsonBody);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("WebSocket 受信処理がキャンセルされました。");
        }
        catch (Exception ex)
        {
            Debug.LogError($"WebSocket 受信エラー: {ex.Message}");
            await Reconnect();
        }
    }

    /// <summary>
    /// WebSocket 再接続処理
    /// </summary>
    private async UniTask Reconnect()
    {
        Debug.Log("WebSocket 再接続中...");
        ws = null;
        await UniTask.Delay(3000); // 3秒後に再接続
        await ConnectAsync();
    }

    private static bool TryParseCommand(string message, out CommunicateData command)
    {
        try
        {
            command = JsonUtility.FromJson<CommunicateData>(message);
            return true;
        }
        catch
        {
            command = default;
            return false;
        }
    }

    private async void OnDestroy()
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            try
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection Closed", CancellationToken.None);
                Debug.Log("WebSocket を正常に閉じました。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket を閉じる際のエラー: {ex.Message}");
            }
            finally
            {
                ws = null;
            }
        }
    }
}
