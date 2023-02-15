using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Gs2.Unity.Core;
using Gs2.Unity.Util;
using Gs2.Unity.Gs2Account.Model;
using Gs2.Core.Model;
using Gs2.Gs2Chat.Model;
using Gs2.Unity.Gs2Chat.Model;

public class GS2TestOld : MonoBehaviour
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

    void Start()
    {
        StartCoroutine(this.MainProcess());
    }

    public IEnumerator MainProcess()
    {
        yield return StartCoroutine(Init());
        yield return StartCoroutine(CreateUser());
        yield return StartCoroutine(Login());
        yield return StartCoroutine(CreateRoom());
    }

    public void DoPostMessage()
    {
        StartCoroutine(this.PostMessagge());
    }

    public IEnumerator Init()
    {
        var profile = new Profile(clientId, clientSecret, new Gs2BasicReopener(), Region.ApNortheast1);
        var initializeFuture = profile.InitializeFuture();
        yield return initializeFuture;
        if (initializeFuture.Error != null)
        {
            throw initializeFuture.Error;
        }
        this.gs2 = initializeFuture.Result;
    }

    public IEnumerator CreateUser()
    {
        {
            var domain = gs2.Account.Namespace(this.nameSpace);
            var future = domain.Create();
            yield return future;
            if (future.Error != null)
            {
                throw future.Error;
            }
            var future2 = future.Result.Model();
            yield return future2;
            if (future2.Error != null)
            {
                throw future2.Error;
            }
            var result = future2.Result;
            this.userId = result.UserId;
            this.password = result.Password;
            Debug.Log($"CreateUser : {this.userId} : {this.password}");
        }

        {
            var domain = gs2.Account.Namespace(this.nameSpace).Account(this.userId);
            var future = domain.Authentication(this.key, this.password);
            yield return future;
            if (future.Error != null)
            {
                throw future.Error;
            }
            var future2 = future.Result.Model();
            yield return future2;
            if (future2.Error != null)
            {
                throw future2.Error;
            }
            var result = future2.Result;
            this.body = future.Result.Body;
            this.signature = future.Result.Signature;
            Debug.Log($"Auth : {this.body} : {this.signature}");
        }
    }

    public IEnumerator Login()
    {
        {
            var domain = gs2.Auth.AccessToken();
            var future = domain.Login(this.key, this.body, this.signature);
            yield return future;
            if (future.Error != null)
            {
                throw future.Error;
            }
            var token = future.Result.Token;
            var userId = future.Result.UserId;
            var expire = future.Result.Expire;
            var future2 = future.Result.Model();
            yield return future2;
            if (future2.Error != null)
            {
                throw future2.Error;
            }
            var item = future2.Result;
            this.gameSession = item;
            Debug.Log($"Login : {token} : {userId} : {expire}");
        }

        {
            var domain = gs2.Gateway.Namespace(this.nameSpace).Me(this.gameSession).WebSocketSession();
            var future = domain.SetUserId();
            yield return future;
            if (future.Error != null)
            {
                throw future.Error;
            }
            var future2 = future.Result.Model();
            yield return future2;
            if (future2.Error != null)
            {
                throw future2.Error;
            }
            var result = future2.Result;
            Debug.Log($"Gateway : {result.NamespaceName} : {result.UserId}");
        }
    }

    public IEnumerator CreateRoom()
    {
        {
            //var domain = gs2.Chat.Namespace(this.nameSpace).Me(this.gameSession);
            //var future = domain.CreateRoom(this.roomName, "testRoom", null, null);
            //yield return future;
            //if (future.Error != null)
            //{
            //    throw future.Error;
            //}
            //var future2 = future.Result.Model();
            //yield return future2;
            //if (future2.Error != null)
            //{
            //    throw future2.Error;
            //}
            //var result = future2.Result;
            //Debug.Log($"CreateRoom : {result.Name} : {result.Metadata}");
        }

        {
            var domain = gs2.Chat.Namespace(this.nameSpace).Me(this.gameSession).Subscribe(this.roomName);
            var future = domain.Subscribe(notificationTypes: new Gs2.Unity.Gs2Chat.Model.EzNotificationType[] {new Gs2.Unity.Gs2Chat.Model.EzNotificationType {},});
            yield return future;
            if (future.Error != null)
            {
                throw future.Error;
            }
            var future2 = future.Result.Model();
            yield return future2;
            if (future2.Error != null)
            {
                throw future2.Error;
            }
            var result = future2.Result;
            Debug.Log($"SubScribe : {result.UserId} : {result.RoomName}");

            gs2.Chat.OnPostNotification += OnPost;
        }
    }

    public IEnumerator PostMessagge()
    {
        var domain = gs2.Chat.Namespace(this.nameSpace).Me(this.gameSession).Room(this.roomName, null);
        var future = domain.Post(this.message);
        yield return future;
        if (future.Error != null)
        {
            throw future.Error;
        }
        var future2 = future.Result.Model();
        yield return future2;
        if (future2.Error != null)
        {
            throw future2.Error;
        }
        var result = future2.Result;
        Debug.Log($"PostMessage : {result.RoomName} : {result.Metadata} : {result.Name} : {result.CreatedAt}");
    }

    private void OnPost(PostNotification notification)
    {
        var namespaceName = notification.NamespaceName;
        var roomName = notification.RoomName;
        var userId = notification.UserId;
        var category = notification.Category;
        var createdAt = notification.CreatedAt;
        Debug.Log($"OnPost : {namespaceName} : {roomName} : {userId} : {category} : {createdAt}");
        a();
    }

    private void a()
    {
        StartCoroutine(this.GetMessages());
    }

    private IEnumerator GetMessages()
    {
        var domain = gs2.Chat.Namespace(this.nameSpace).Me(this.gameSession).Room(this.roomName, null);
        var it = domain.Messages();
        List<EzMessage> items = new List<EzMessage>();
        while (it.HasNext())
        {
            yield return it.Next();
            if (it.Error != null)
            {
                throw it.Error;
            }
            if (it.Current != null)
            {
                items.Add(it.Current);
            }
            else
            {
                break;
            }
        }

        foreach(var msg in items)
        {
            Debug.Log($"Message : {msg.RoomName} : {msg.Name} : {msg.Metadata} : {msg.UserId} : {msg.CreatedAt}");
        }
    }

    void Update()
    {

    }
}
