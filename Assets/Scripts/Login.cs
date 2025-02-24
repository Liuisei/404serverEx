using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
public class SendDataToServer : MonoBehaviour
{
    private  string iPAdress = "192.168.0.3";
    [SerializeField] private  string port = "5000";
    [SerializeField] private  Button sendButtnn;
    [SerializeField] TMP_InputField username;
    
    string PlayerID = string.Empty;
    // Start is called before the first frame update
    private void Start()
    {
        sendButtnn.onClick.AddListener(onClickSendButton);
    }
    private void onClickSendButton()
    {
        string URL = "http://";
        URL += iPAdress;
        URL += ":";
        URL += port;
        URL += @"/";
        Debug.Log("Server URL = " + URL);
        StartCoroutine("OnSend", URL);
    }
    //コルーチン
    private IEnumerator OnSend(string url)
    {
        //POSTする情報
        WWWForm form = new WWWForm();
        form.AddField("name", username.text);
        //URLをPOSTで用意
        UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
        //UnityWebRequestにバッファをセット
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        //URLに接続して結果が戻ってくるまで待機
        yield return webRequest.SendWebRequest();
        //エラーが出ていないかチェック
        if (webRequest.isNetworkError)
        {
            //通信失敗
            Debug.Log(webRequest.error);
        }
        else
        {
            //通信成功
            Debug.Log(webRequest.downloadHandler.text);
            WebsocketClients ws = FindAnyObjectByType<WebsocketClients>(FindObjectsInactive.Exclude);
            ws.SetUserID(webRequest.downloadHandler.text);
        }
    }
}