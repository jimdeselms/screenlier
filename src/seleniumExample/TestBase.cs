namespace Screenly.SeleniumExample
{
    using System;

    public class TestBase
    {
        private readonly string _environment;
        private readonly string _sitecoreUrl;

        public TestBase(string environment, string sitecoreUrl=null)
        {
            _environment = environment;
            _sitecoreUrl = sitecoreUrl;
        }

        // If we're proxied, then we'll do that here.
        protected void SetProxy(TestContext context, string locale="en-us")
        {
            if (_sitecoreUrl == null) return;

            if (_environment != "test" && _environment == "dev")
            {
                throw new Exception("Can only set Sitecore proxy if we're in dev or test");
            }

            var uri = new Uri(_sitecoreUrl);

            Console.WriteLine($"Proxying {_environment} to {_sitecoreUrl}");

            var host = GetMonolithHost(locale);
            context.Driver.GoToUrl(host);

            var cookie = new OpenQA.Selenium.Cookie("SETTINGS", $"RemoteContentServer_Sitecore={_sitecoreUrl}");
            context.Driver.Manage().Cookies.AddCookie(cookie);
        }

        protected string GetMonolithHost(string locale="en-us")
        {
            switch (_environment)
            {
                case "prod":
                    switch (locale)
                    {
                        case "en-us": return "https://www.vistaprint.com";
                        case "fr-fr": return "https://www.vistaprint.fr";
                        case "de-de": return "https://www.vistaprint.de";
                        case "en-gb": return "https://www.vistaprint.co.uk";
                        default: throw new Exception("Unknown environment " + _environment);
                    }
                case "test":
                    switch (locale)
                    {
                        case "en-us": return "https://www.vptest.com";
                        case "fr-fr": return "https://www.vptest.fr";
                        case "de-de": return "https://www.vptest.de";
                        case "en-gb": return "https://www.vptest.co.uk";
                        default: throw new Exception("Unknown environment " + _environment);
                    }
                case "preprod":
                    switch (locale)
                    {
                        case "en-us": return "https://www.vppreprod.com";
                        case "fr-fr": return "https://www.vppreprod.fr";
                        case "de-de": return "https://www.vppreprod.de";
                        case "en-gb": return "https://www.vppreprod.co.uk";
                        default: throw new Exception("Unknown environment " + _environment);
                    }
                case "dev":
                    switch (locale)
                    {
                        case "en-us": return "https://www.vpdev.com";
                        case "fr-fr": return "https://www.vpdev.fr";
                        case "de-de": return "https://www.vpdev.de";
                        case "en-gb": return "https://www.vpdev.co.uk";
                        default: throw new Exception("Unknown environment " + _environment);
                    }
                default:
                    throw new Exception("Unknown locale " + locale);
            }
        }
    }
}