namespace Screenly.SeleniumExample
{
    using System;

    public class Tests : TestBase
    {
        public Tests(string environment, string sitecoreUrl=null) : base(environment, sitecoreUrl)
        {
        }

        [Test]
        public bool StandardBusinessCardsUs(TestContext context)
        {
            SetProxy(context);
            var host = GetMonolithHost();

            context.Driver.GoToUrl($"{host}/vp/gateway.aspx?fv=29874");
            context.Driver.GoToUrl($"{host}/business-cards/standard");

            context.Driver.RemoveClasses("main-nav", "inline-ratings-container", "lower-footer");

            context.Driver.TakeReferenceImage(context.TestRun, context.Name, "Standard business cards GPP");

            return context.Driver.CaptureClasses(context.TestRun, context.Name, "product-configurator", "product-image-component", "breadcrumbs", "grid-container-line-wrap", "specs-banner");
        }

        [Test]
        public bool PostersUs(TestContext context)
        {
            SetProxy(context);
            var host = GetMonolithHost();

            context.Driver.GoToUrl($"{host}/signs-posters/posters");

            context.Driver.RemoveClasses("main-nav", "inline-ratings-container", "lower-footer");

            context.Driver.TakeReferenceImage(context.TestRun, context.Name, "Posters GPP");

            return context.Driver.CaptureClasses(context.TestRun, context.Name, "product-configurator", "product-image-component", "breadcrumbs", "short-description-container");
        }

        [Test]
        public bool FloorStandupsUs(TestContext context)
        {
            SetProxy(context);
            var host = GetMonolithHost();

            context.Driver.GoToUrl($"{host}/signs-posters/floor-standups");

            context.Driver.RemoveClasses("main-nav", "inline-ratings-container", "lower-footer");

            context.Driver.TakeReferenceImage(context.TestRun, context.Name, "Floor standups GPP");

            return context.Driver.CaptureClasses(context.TestRun, context.Name, "product-configurator", "short-description-container", "breadcrumbs", "product-image-component");
        }

        [Test]
        public bool StandardBusinessCardsFr(TestContext context)
        {
            SetProxy(context, "fr-fr");
            var host = GetMonolithHost("fr-fr");
            if (host == null)
            {
                // This environment doesn't support other locales, so we'll skip it.
                Console.WriteLine($"Skipping {context.Name} because it's not supported in this environment");
            }

            context.Driver.GoToUrl($"{host}/cartes-de-visite/standard");

            context.Driver.RemoveClasses("main-nav", "inline-ratings-container", "lower-footer", "cookie-policy-alert");

            context.Driver.TakeReferenceImage(context.TestRun, context.Name, "Standard business cards - fr");

            return context.Driver.CaptureClasses(context.TestRun, context.Name, "product-configurator", "short-description-container", "breadcrumbs", "product-image-component");
        }
    }
}