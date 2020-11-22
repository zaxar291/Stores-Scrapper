using System;
using System.IO;
using System.IO.Compression;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using WebScrapper.Scrapper.Abstraction;
using WebScrapper.Scrapper.Services;
using WebScrapper.Scrapper.Entities.StrategiesEntities.Driver;
using WebScrapper.Scrapper.Entities;

using WebApplication.Scrapper.Abstraction;
using WebApplication.Scrapper.Services;

namespace WebScrapper.Scrapper.Implementation.Driver {
    public class ChromeDriverResolver : IWebDriverResolver {
        private ChromeDriver _cd;
        private BaseWebDriverStrategy settings;
        private BaseLogger _l;
        private IBaseDirectoriesProcessor _d;
        private AbstractWriter _f;
        private string _baseUrl;
        private string DriverDependenciesDirectory { get; set; }
        private string DriverResourcesDirectory { get; set; }
        private string DriverBaseExtensionsPath { get; set; }
        private string ProxyContainerPath;
        private WebScrapperBaseProxyEntity _ps { get; set; }
        public ChromeDriverResolver(BaseWebDriverStrategy options, WebScrapperBaseProxyEntity _proxy, string url,  BaseLogger _logger) {
            settings = options;
            _l = _logger;
            _baseUrl = url;

            _d = new DirectoriesService();
            _f = new FilesWriter();

            _ps = _proxy;

            DriverDependenciesDirectory = $@"{Directory.GetCurrentDirectory()}/Scrapper/Resources/scripts/ChromeDriver/js/";
            DriverResourcesDirectory = $"{Directory.GetCurrentDirectory()}/Scrapper/Resources/";
            DriverBaseExtensionsPath = $"{DriverResourcesDirectory}selenium/chrome/proxy.plugin/";
        }

        public bool Initialize() {
            try {
                var _ds = new ChromeOptions(); 

                if (settings.DisableInfoBar) 
                {
                    _ds.AddArgument("disable-infobars");
                }

                if (settings.LaunchIncognito)
                {
                    _ds.AddArgument("--incognito");
                }

                if (settings.IgnoreCertificateErrors)
                {
                    _ds.AddArgument("ignore-certificate-errors");
                }

                if (settings.LaunchHeadless)
                {
                    // _ds.AddArgument("--headless");
                }

                if (settings.UseProxy)
                {
                    // var _prPath = CreateTempProxyExtension();
                    var _prPath = $"{Directory.GetCurrentDirectory()}/temp/test.zip";
                    if (ReferenceEquals(_prPath, null))
                    {
                        throw new Exception("Failed to create proxy service extension");
                    }
                    _ds.AddExtension(_prPath);
                }

                _cd = new ChromeDriver($"{Directory.GetCurrentDirectory()}/Scrapper/Implementation/Driver", _ds);

                //_cd.Manage().Window.Minimize();
                _cd.Navigate().GoToUrl(_baseUrl);
                return true;
            }
            catch (WebDriverException e)
            {
                _l.error($"Chrome Driver: While trying to initialize, exception occured! {e.Message} -> {e.StackTrace}");
                _l.error(e.Message);
                this.Dispose();
                return false;
            }
        }

        public void NavigateToPage (string page) {
            if (ReferenceEquals(page, null) || page.Equals(string.Empty)) {
                throw new Exception("Empty or invalid url for navigation!");
            }
            _cd.Navigate().GoToUrl(page);
        }

        public bool ExecuteScript(string script) {
            try {
                _cd.ExecuteScript(script);
                return true;
            } catch (WebDriverException e) {
                _l.error(e.Message);
                return false;
            }
        }

        public bool ExecuteScript(string script, object obj) {
            try {
                _cd.ExecuteScript(script, obj);
                return true;
            } catch (WebDriverException e) {
                _l.error(e.Message);
                return false;
            }
        }
    
        public bool UpdateFieldData(string element, string data) {
            _l.info($"Webdriver: Attemp to update data [{data}] in element [{element}]...");

            By se = GetElement(element);

            if (ReferenceEquals(se, null))
            {
                _l.error($"Webdriver: Can't find element -> [{element}]");
                return false;
            }
            try {
                var e = _cd.FindElement(se);
                e.Clear();
                e.SendKeys(data);
                _l.info($"Webdriver: Value [{data}] was successfully updated in element [{element}]");
                return true;
            } catch (StaleElementReferenceException e)
            {
                _l.error(e.Message);

                return false;
            }
            catch (InvalidElementStateException e)
            {
                _l.error(e.Message);

                return false;
            }
            catch (WebDriverException e)
            {
                _l.error(e.Message);
                return false;
            }

        }

        public bool InvokeMemberClick(string element) {
            return true;
        }

        public string GetDataFromPage(string element, string script = null) {
            By se = null;
            _l.info($"Trying to select text from element {element}");

            if (!ReferenceEquals(element, null)) {
                se = GetElement(element);
                if (ReferenceEquals(se, null)) {
                    _l.error($"Chrome driver: can't find element {element}");
                    return String.Empty;
                }
            }
            try 
            {
                if (!ReferenceEquals(script, null)) {
                    return (string)_cd.ExecuteScript(script);
                }
                return _cd.FindElement(se).Text;
            } 
            catch (StaleElementReferenceException e) {
                                _l.error(
                    $"Chrome driver Can't get valid text from element {element}, exception details: {e.Message}");

                _l.error(e.Message);

                return string.Empty;
            }
            catch (WebDriverException e)
            {
                _l.error(e.Message);

                return string.Empty;
            }
        }

        public bool SwitchToFrame(string selector) 
        {
            By e = GetElement(selector);
            if (ReferenceEquals(e, null))
            {
                return false;
            }
            try
            {
                _cd.SwitchTo().Frame(_cd.FindElement(e));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public bool SwitchToDefaultContext(string selector) 
        {
            try
            {
                _cd.SwitchTo().DefaultContent();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsElementExists(string selector)
        {
            By se = GetElement(selector);
            if (ReferenceEquals(se, null))
            {
                return false;
            }
            try
            {
                _cd.FindElement(se);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetDriverDependenciesDirectory() 
        {
            return DriverDependenciesDirectory;
        }

        private By GetElement(string s)
        {
            try
            {
                if (s.IndexOf(".", StringComparison.Ordinal) != -1)
                    return By.ClassName(s.Remove(0, s.IndexOf(".", StringComparison.Ordinal) + 1));
                if (s.IndexOf("#", StringComparison.Ordinal) != -1)
                    return By.Id(s.Remove(0, s.IndexOf("#", StringComparison.Ordinal) + 1));
                if (s.IndexOf("$", StringComparison.Ordinal) != -1)
                    return By.XPath(s.Remove(0, s.IndexOf("$", StringComparison.Ordinal) + 1));
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string CreateTempProxyExtension() 
        {
            var _rb = $"{Directory.GetCurrentDirectory()}/temp/";
            if (!_d.Exists(_rb))
            {
                if (!_d.Create(_rb))
                {
                    _l.error($"Fatal: cannot create directory {_rb}: {_d.GetLastExcept()}");
                    return null;
                }
            }
            if (!_d.Exists($"{_rb}selenium/"))
            {
                if (!_d.Create($"{_rb}selenium/"))
                {
                    _l.error($"Fatal: cannot create directory {_rb}selenium/: {_d.GetLastExcept()}");
                    return null;
                }
            }
            if (!_d.Exists($"{_rb}selenium/chrome/"))
            {
                if (!_d.Create($"{_rb}selenium/chrome/"))
                {
                    _l.error($"Fatal: cannot create directory {_rb}selenium/chrome/: {_d.GetLastExcept()}");
                    return null;
                }
            }
            var _driverTemp = CreateDefaultContainer($"{_rb}selenium/chrome/");
            if (!_f.IsFileExists($"{DriverBaseExtensionsPath}background.js"))
            {
                _l.error($"Fatal error: cannot find file {DriverBaseExtensionsPath}/background.js. Failed to init proxies plugin!");
                return null;
            }
            string _c = _f.Read($"{DriverBaseExtensionsPath}/background.js");
            if (ReferenceEquals(_c, String.Empty))
            {
                _l.error($"Fatal error: cannot read file {DriverBaseExtensionsPath}/background.js. Failed to init proxies plugin!");
                return null;
            }
            string prepared = _c.Replace("%proxy_host%", _ps.ProxyUrl).Replace("%proxy_port%", _ps.ProxyPort.ToString()).Replace("%proxy_user%", _ps.AuthLogin).Replace("%proxy_password%", _ps.AuthPassword);
            string _fn = $"{_driverTemp}background.js";
            if (!_f.Create("background.js", _driverTemp, prepared))
            {
                _l.error($"Fatal error: cannot write infromation to file {_driverTemp}/background.js. Failed to init proxies plugin!");
                return null;
            }
            _c = _f.Read($"{DriverBaseExtensionsPath}/manifest.json");
            if (ReferenceEquals(_c, String.Empty))
            {
                _l.error($"Fatal error: cannot write infromation to file {DriverBaseExtensionsPath}/manifest.json. Failed to init proxies plugin!");
                return null;
            }
            if (!_f.Create("manifest.json", _driverTemp, _c))
            {
                _l.error($"Fatal error: cannot write infromation to file {_driverTemp}/manifest.json. Failed to init proxies plugin!");
                return null;
            }
            try
            {
                using (var _arc = ZipFile.Open($"{_driverTemp}proxies.plugin.zip", ZipArchiveMode.Create))
                {
                    _arc.CreateEntryFromFile(_fn, Path.GetFileName(_fn));
                    _arc.CreateEntryFromFile($"{_driverTemp}manifest.json", Path.GetFileName($"{_driverTemp}manifest.json"));
                }
            }
            catch (Exception e) 
            {
                _l.error($"Failed to create zip archive {_driverTemp}proxies.plugin.zip : {e.Message} -> {e.StackTrace}");
                return null;
            }

            return $"{_driverTemp}proxies.plugin.zip";
        }

        private string CreateDefaultContainer(string dir)
        {
            string containerId =  new Random().Next().ToString();
            if (_d.Create($"{dir}{containerId}/"))
            {
                ProxyContainerPath = $"{dir}{containerId}/";
                return $"{dir}{containerId}/";
            }
            return dir;
        }

        public void Dispose() {
            _cd?.Quit();
            if (_d.Exists(ProxyContainerPath))
            {
                _f.Delete($"{ProxyContainerPath}manifest.json");
                _f.Delete($"{ProxyContainerPath}background.js");
                _f.Delete($"{ProxyContainerPath}proxies.plugin.zip");
                _d.Remove(ProxyContainerPath);
            }
        }
    }

}