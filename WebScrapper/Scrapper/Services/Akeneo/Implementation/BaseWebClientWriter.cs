using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using WebApplication.Scrapper.Entities;
using WebApplication.Scrapper.Services.Akeneo.Abstraction;

namespace WebApplication.Scrapper.Services.Akeneo.Implementation
{
    public class BaseWebClientWriter : IBaseRequestHandler<NameValueCollection, WebClientHeader>
    {
        private const string PROTOCOL_POST = "POST";
        private const string PROTOCOL_PATCH = "PATCH";
        private const string PROTOCOL_GET = "GET";
        private const string PROTOCOL_PUT = "PUT";
        public List<WebClientHeader> Headers { get; set; }
        public NameValueCollection Body { get; set; }
        public string LastHeader { get; set; }
        public string StringContextBody { get; set; }
        public BaseWebClientWriter()
        {
            Headers = new List<WebClientHeader>();
        }

        public string GetData(string requestUrl, NetworkCredential credential)
        {
            if (requestUrl != null && !requestUrl.Equals(String.Empty))
            {
                using (var _wc = new WebClient())
                {
                    _wc.Headers.Add("Content-Type", "application/json");
                    _wc.Credentials = credential;
                    var str = _wc.DownloadString(requestUrl);
                    var header = String.Empty;
                    if (_wc.ResponseHeaders[18] != null)
                    {
                        header = _wc.ResponseHeaders[18];
                    }
                    LastHeader = header;
                    if (header.Contains(","))
                    {
                        var exploded = header.Split(",");
                        if (exploded.Length > 0)
                        {
                            if (!ReferenceEquals(exploded[1], null))
                            {
                                LastHeader = exploded[1];
                            }
                        }
                    }
                    return str;
                }
            }
            return String.Empty;
        }
        public string PutData(string requestUrl, NetworkCredential authHeader)
        {
            if (!ReferenceEquals(requestUrl, null) 
                && !requestUrl.Equals(String.Empty)
                && !ReferenceEquals(authHeader, null))
            {
                using (var _wc = new WebClient())
                {
                    _wc.Headers.Add("Content-Type", "application/json");
                    _wc.Credentials = authHeader;
                    return _wc.UploadString(requestUrl, PROTOCOL_PUT, StringContextBody);
                }
            }

            throw new Exception("Webclient: Fatal error, cannot validate entities");
        }

        public string GetData(string requestUrl, WebClientHeader authHeader)
        {
            if (requestUrl != null && !requestUrl.Equals(String.Empty))
            {
                HttpWebRequest request = (HttpWebRequest) HttpWebRequest.Create(requestUrl);
                request.Method = PROTOCOL_GET;
                request.Headers.Add(authHeader.Name, authHeader.Value);
                string test = String.Empty;
                using (var response = request.GetResponse())
                {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    test = reader.ReadToEnd();
                    reader.Close();
                    dataStream.Close();
                }

                return test;
            }
            return String.Empty;
        }

        public string PostData(string requestUrl)
        {
            if (Body != null && Body.HasKeys())
            {
                WebClient client = new WebClient();
                if (Headers.Count > 0)
                {
                    foreach (var header in Headers)
                    {
                        client.Headers.Add(header.Name, header.Value);
                    }
                    Headers = new List<WebClientHeader>();
                }
                
                return Encoding.UTF8.GetString(client.UploadValues(requestUrl, PROTOCOL_POST, Body));
            }
            return String.Empty;
        }

        public string PatchData(string requestUrl)
        {
            if (StringContextBody != null && !StringContextBody.Equals(String.Empty))
            {
                WebClient client = new WebClient();
                if (Headers.Count > 0)
                {
                    foreach (var header in Headers)
                    {
                        client.Headers.Add(header.Name, header.Value);
                    }
                    Headers = new List<WebClientHeader>();
                }

                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                return client.UploadString(requestUrl, PROTOCOL_PATCH, StringContextBody);
            }
            return String.Empty;
        }

        public string PostDataAsStringContext(string requestUrl)
        {
            if (StringContextBody != null && !StringContextBody.Equals(String.Empty))
            {
                WebClient client = new WebClient();
                if (Headers.Count > 0)
                {
                    foreach (var header in Headers)
                    {
                        client.Headers.Add(header.Name, header.Value);
                    }
                    Headers = new List<WebClientHeader>();
                }

                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                return client.UploadString(requestUrl, PROTOCOL_POST, StringContextBody);
            }
            return String.Empty;
        }

        public void AddHeader(WebClientHeader header)
        {
            Headers.Add(header);
        }

        public void AddBodyParameter(NameValueCollection body)
        {
            Body = body;
        }

        public void AddBodyParameter(string body)
        {
            StringContextBody = body;
        }
    }
}