﻿using System;
using UnityEngine.Networking;
using CatLib.API.Network;

namespace CatLib.Network
{


    /// <summary>
    /// 请求参数
    /// </summary>
    public class WebRequestEventArgs : EventArgs , IHttpResponse
    {

        public UnityWebRequest WebRequest { get; set; }

        public object Request { get { return WebRequest; } }

        public byte[] Bytes { get { return WebRequest.downloadHandler == null ? null : WebRequest.downloadHandler.data; } }

        public string Text { get { return WebRequest.downloadHandler == null ? string.Empty : WebRequest.downloadHandler.text; } }

        public bool IsError { get { return WebRequest.isError; } }

        public string Error { get { return WebRequest.error; } }

        public long ResponseCode { get { return WebRequest.responseCode; } }

        public ERestful Restful { get { return (ERestful)Enum.Parse(typeof(ERestful), WebRequest.method); } }

        public WebRequestEventArgs(UnityWebRequest request)
        {
            WebRequest = request;
        }
    }

}
