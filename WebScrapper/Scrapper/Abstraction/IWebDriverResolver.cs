using System;

namespace WebScrapper.Scrapper.Abstraction {
    public interface IWebDriverResolver : IDisposable {
        string GetDriverDependenciesDirectory();
        bool Initialize();
        void NavigateToPage(string page);
        bool ExecuteScript(string script);
        bool ExecuteScript(string script, object obj);
        bool UpdateFieldData(string element, string text);
        string GetDataFromPage(string element, string script = null);
        bool InvokeMemberClick(string element);
        bool SwitchToFrame(string selector);
        bool SwitchToDefaultContext(string selector);
        bool IsElementExists(string selector);
    }
}