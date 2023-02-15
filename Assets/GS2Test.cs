using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

using Gs2.Unity.Core;
using Gs2.Unity.Util;
using Gs2.Unity.Gs2Account.Model;
using Gs2.Core.Model;
using Gs2.Gs2Chat.Model;
using Cysharp.Threading.Tasks.Linq;
using Gs2.Unity.Gs2Realtime;

public class GS2Test : MonoBehaviour
{
    public string clientId = "GKIi3QK6oOBcqTomrI60WvSIPrUP9uz5Sh8-_RJP4aCIiIkSqNkGy_do-sVlSMy94CM";
    public string clientSecret = "oBobACJIiVEiRZKAFzrbGcyghdNsPjiL";
    public string key = "grn:gs2:ap-northeast-1:bCHffHMb-GS2Learn:key:test:key:test";
    public string nameSpace = "test";
    public string roomName = "test_room";
    public string message = "test_message";

    private GameSession gameSession;
    private Profile profile;
    private Gs2Domain gs2;
    private string userId;
    private string password;
    private string body;
    private string signature;

    private Subject<Unit> onPostSUbject = new Subject<Unit>();

    void Start()
    {
        this.onPostSUbject.ObserveOnMainThread().Subscribe(_ =>
        {
            GetMessages().Forget();
        });

        this.MainProcess().Forget();
    }

    public async UniTask MainProcess()
    {
        await this.Init();
        await this.CreateUser();
        await this.Login();
        await this.CreateRoom();
        //await this.ConnectGameServer();
    }

    public void DoPostMessage()
    {
        this.PostMessagge().Forget();
    }

    public async UniTask Init()
    {
        var profile = new Profile(clientId, clientSecret, new Gs2BasicReopener(), Region.ApNortheast1);
        this.gs2 = await profile.InitializeAsync();
    }

    public async UniTask CreateUser()
    {
        {
            var domain = gs2.Account.Namespace(this.nameSpace);
            var result = await domain.CreateAsync();
            var item = await result.ModelAsync();
            this.userId = item.UserId;
            this.password = item.Password;
            Debug.Log($"CreateUser : {this.userId} : {this.password}");
        }

        {
            var domain = gs2.Account.Namespace(this.nameSpace).Account(this.userId);
            var result = await domain.AuthenticationAsync(this.key, this.password);
            var item = await result.ModelAsync();
            this.body = result.Body;
            this.signature = result.Signature;
            Debug.Log($"Auth : {this.body} : {this.signature}");
        }
    }

    public async UniTask Login()
    {
        {
            var domain = gs2.Auth.AccessToken();
            var result = await domain.LoginAsync(this.key, this.body, this.signature);
            var token = result.Token;
            var userId = result.UserId;
            var expire = result.Expire;
            var item = await result.ModelAsync();
            this.gameSession = item;
            Debug.Log($"Login : {token} : {userId} : {expire}");
        }

        {
            var domain = gs2.Gateway.Namespace(this.nameSpace).Me(this.gameSession).WebSocketSession();
            var result = await domain.SetUserIdAsync();
            var item = await result.ModelAsync();
            Debug.Log($"Gateway : {item.NamespaceName} : {item.UserId}");
        }
    }

    public async UniTask CreateRoom()
    {
        {
            /*
            var domain = gs2.Chat.Namespace(this.nameSpace).Me(this.gameSession);
            var result = await domain.CreateRoomAsync(this.roomName, "testRoom", null, null);
            var item = await result.ModelAsync();
            Debug.Log($"CreateRoom : {item.Name} : {item.Metadata}");
            */
        }

        {
            var domain = gs2.Chat.Namespace(this.nameSpace).Me(this.gameSession).Subscribe(this.roomName);
            var result = await domain.SubscribeAsync(notificationTypes: new Gs2.Unity.Gs2Chat.Model.EzNotificationType[] { new Gs2.Unity.Gs2Chat.Model.EzNotificationType { }, });
            var item = await result.ModelAsync();
            Debug.Log($"SubScribe : {item.UserId} : {item.RoomName}");

            gs2.Chat.OnPostNotification += OnPost;
        }
    }

    public async UniTask PostMessagge()
    {
        var domain = gs2.Chat.Namespace(this.nameSpace).Me(this.gameSession).Room(this.roomName, null);
        var result = await domain.PostAsync(this.message);
        var item = await result.ModelAsync();
        Debug.Log($"PostMessage : {item.RoomName} : {item.Metadata} : {item.Name} : {item.CreatedAt}");
    }

    private void OnPost(PostNotification notification)
    {
        var namespaceName = notification.NamespaceName;
        var roomName = notification.RoomName;
        var userId = notification.UserId;
        var category = notification.Category;
        var createdAt = notification.CreatedAt;
        Debug.Log($"OnPost : {namespaceName} : {roomName} : {userId} : {category} : {createdAt}");
        this.onPostSUbject.OnNext(Unit.Default);

    }

    private async UniTask GetMessages()
    {
        Debug.LogError(Time.frameCount + " : " + Time.time);
       var domain = gs2.Chat.Namespace(this.nameSpace).Me(this.gameSession).Room(this.roomName, null);
        var items = await domain.MessagesAsync().ToListAsync();
        Debug.LogError(Time.frameCount + " : " + Time.time);
        foreach (var msg in items)
        {
            Debug.Log($"Message : {msg.RoomName} : {msg.Name} : {msg.Metadata} : {msg.UserId} : {msg.CreatedAt}");
        }
    }

    private async UniTask ConnectGameServer()
    {
        var item = await gs2.Realtime.Namespace(this.nameSpace).Room("lobby").ModelAsync();
        var ipAddress = item.IpAddress;
        var port = item.Port;
        var encryptionKey = item.EncryptionKey;

        using (var session = new RelayRealtimeSession(this.gameSession.AccessToken.Token,ipAddress, port,encryptionKey))
        {
            // イベントハンドラを設定
            session.OnJoinPlayer += player => 
            {
                Debug.Log("OnJoin : " + player.UserId);
            };
            session.OnLeavePlayer += player => 
            {
                Debug.Log("OnLeave : " + player.UserId);
            };

            await session.ConnectAsync(this);

            // セッションが有効なスコープ
        }
    }




    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            GetMessages().Forget();
        }
    }

    private void OnApplicationQuit()
    {
        Debug.LogError("Quit");
        PostMessagge().Forget();
    }
}
