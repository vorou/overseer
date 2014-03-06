using FakeItEasy;
using Nancy;
using Nancy.Testing;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Shouldly;
using Xunit;

namespace Overseer.WebApp.Tests
{
    public class OverseerModuleTests
    {
        [Fact]
        public void HomePage_Always_Responses()
        {
            var sut = CreateDefaultBrowser();

            sut.Get("/").StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public void HomePage_Always_ContainsPriceForMostExpensiveTender()
        {
            var totalPrice = 123456.78M;
            var tender = fixture.Create<Tender>();
            tender.TotalPrice = totalPrice;
            var totalPriceFormatted = "123 456,78 р.";

            AssertTenderViewContains(tender, totalPriceFormatted);
        }

        [Fact]
        public void HomePage_Always_ContainsNameForMostExpensiveTender()
        {
            var tenderName = fixture.Create<string>();
            var tender = fixture.Create<Tender>();
            tender.Name = tenderName;

            AssertTenderViewContains(tender, tenderName);
        }

        [Fact]
        public void HomePage_Always_ContainsRegionNameForTender()
        {
            var regionNameService = fixture.Freeze<IRegionNameService>();
            var regionId = fixture.Create<string>();
            var regionName = fixture.Create<string>();
            A.CallTo(() => regionNameService.GetName(regionId)).Returns(regionName);

            var tender = fixture.Create<Tender>();
            tender.Region = regionId;

            AssertTenderViewContains(tender, regionName);
        }

        [Fact]
        public void HomePage_Always_ContainsLinkToTender()
        {
            var tender = fixture.Create<Tender>();
            tender.Id = "panda";

            var actual = GetRoot(tender);

            actual.Body["a"].ShouldContainAttribute("href", "https://zakupki.kontur.ru/notification44?id=panda");
        }

        private void AssertTenderViewContains(Tender tender, string expected)
        {
            var repo = fixture.Freeze<ITenderRepository>();
            var tenders = new[] {tender};
            A.CallTo(() => repo.GetMostExpensive(A<int>._)).Returns(tenders);
            var sut = CreateDefaultBrowser();

            var actual = sut.Get("/").Body.AsString();

            actual.ShouldContain(expected);
        }

        private BrowserResponse GetRoot(Tender tender)
        {
            var repo = fixture.Freeze<ITenderRepository>();
            var tenders = new[] { tender };
            A.CallTo(() => repo.GetMostExpensive(A<int>._)).Returns(tenders);
            var sut = CreateDefaultBrowser();

            var actual = sut.Get("/");

            return actual;
        }

        private Browser CreateDefaultBrowser()
        {
            return new Browser(with =>
                               {
                                   with.Module<OverseerModule>();
                                   with.Dependency(fixture.Create<ITenderRepository>());
                                   with.Dependency(fixture.Create<IRegionNameService>());
                               });
        }

        private readonly IFixture fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
    }
}