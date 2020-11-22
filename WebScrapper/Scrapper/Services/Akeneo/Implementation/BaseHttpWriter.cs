using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using WebApplication.Scrapper.Services.Akeneo.Abstraction;

namespace WebApplication.Scrapper.Services.Akeneo.Implementation
{
    public class BaseHttpWriter : IBaseRequestHandler<Dictionary<string, string>, string>
    {
        public List<string> Headers { get; set; }
        public Dictionary<string, string> Body { get; set; }
        public string StringContextBody { get; set; }

        public string PostData(string requestUrl)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                HttpRequestMessage message = new HttpRequestMessage(new HttpMethod("POST"), requestUrl);
                message.Content = new FormUrlEncodedContent(Body);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = httpClient.SendAsync(message).Result;
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                throw new Exception($"An error during response processing, response code is {response.StatusCode}, expected code is: 200");
            }
        }

        public string PatchData(string requestUrl)
        {
            throw new NotImplementedException();
        }

        public string PostDataAsStringContext(string requestUrl)
        {
            throw new NotImplementedException();
        }

        public string GetData(string requestUrl, string authHeader)
        {
            throw new NotImplementedException();
        }
        public string GetData(string requestUrl, NetworkCredential authHeader)
        {
            throw new NotImplementedException();
        }

        public string GetData(string requestUrl, List<string> authHeader)
        {
            throw new NotImplementedException();
        }

        public string PutData(string requestUrl, NetworkCredential authHeader)
        {
            throw new NotImplementedException();
        }

        public void AddHeader(string header)
        {
            Headers.Add(header);
        }

        public void AddBodyParameter(Dictionary<string, string> body)
        {
            Body = body;
        }

        public void AddBodyParameter(string body)
        {
            throw new NotImplementedException();
        }
    }
}