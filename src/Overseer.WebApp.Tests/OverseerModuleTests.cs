using FakeItEasy;
using Nancy;
using Nancy.Testing;
using Overseer.Common;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Shouldly;
using Xunit;

namespace Overseer.WebApp.Tests
{
    public class OverseerModuleTests
    {
        [Fact]
        public void Root_Always_Responses()
        {
            var sut = CreateDefaultBrowser();

            sut.Get("/").StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public void Root_Always_ContainsPriceForMostExpensiveTender()
        {
            var totalPrice = 123456.78M;
            var tender = fixture.Create<Tender>();
            tender.TotalPrice = totalPrice;
            var totalPriceFormatted = "123 456,78 р.";

            AssertTenderViewContains(tender, totalPriceFormatted);
        }

        [Fact]
        public void Root_Always_ContainsNameForMostExpensiveTender()
        {
            var tenderName = fixture.Create<string>();
            var tender = fixture.Create<Tender>();
            tender.Name = tenderName;

            AssertTenderViewContains(tender, tenderName);
        }

        [Fact]
        public void Root_Always_ContainsRegionNameForTender()
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
        public void Root_Always_ContainsLinkToTender()
        {
            var tender = fixture.Create<Tender>();
            tender.Id = "panda";

            var actual = GetRoot(tender);

            actual.Body["a"].ShouldContainAttribute("href", "https://zakupki.kontur.ru/notification44?id=panda");
        }

        [Fact]
        public void Tenders_Always_ShouldRespond()
        {
            var sut = CreateDefaultBrowser();

            var actual = sut.Get("/tenders");

            actual.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public void Tenders_Always_ContainsTender()
        {
            var tenderName = fixture.Create<string>();
            var tender = fixture.Create<Tender>();
            tender.Name = tenderName;
            var repo = fixture.Freeze<ITenderRepository>();
            A.CallTo(() => repo.Find(null)).Returns(new[] {tender});
            var sut = CreateDefaultBrowser();

            var actual = sut.Get("/tenders");

            actual.Body["a"].AnyShouldContain(tenderName);
        }

        [Fact]
        public void Tenders_QueryIsPassed_ContainsResult()
        {
            var tenderName = fixture.Create<string>();
            var tender = fixture.Create<Tender>();
            tender.Name = tenderName;
            var repo = fixture.Freeze<ITenderRepository>();
            A.CallTo(() => repo.Find("panda")).Returns(new[] {tender});
            var sut = CreateDefaultBrowser();

            var actual = sut.Get("/tenders", with => with.Query("q", "panda"));

            actual.Body["a"].AnyShouldContain(tenderName);
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
            var tenders = new[] {tender};
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