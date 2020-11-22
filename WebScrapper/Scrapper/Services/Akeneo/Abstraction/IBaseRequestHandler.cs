using System;
using System.Collections.Generic;
using System.Net;

namespace WebApplication.Scrapper.Services.Akeneo.Abstraction
{
    public interface IBaseRequestHandler <Tdata, THeader>
    {
        List<THeader> Headers { get; set; }
        Tdata Body { get; set; }
        string StringContextBody { get; set; }
        string PostData(string requestUrl);
        string PatchData(string requestUrl);
        string PostDataAsStringContext(string requestUrl);
        string GetData(string requestUrl, THeader authHeader);
        string GetData(string requestUrl, NetworkCredential credential);
        string PutData(string requestUrl, NetworkCredential credential);
        void AddHeader(THeader header);
        void AddBodyParameter(Tdata body);
        void AddBodyParameter(string body);
    }
}