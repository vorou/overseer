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
        public void Tenders_Always_ShouldRespond()
        {
            var sut = CreateDefaultBrowser();
            var actual = sut.Get("/tenders");
            actual.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public void Tenders_NoQuery_ResponseContainsPriceForMostExpensiveTender()
        {
            var totalPrice = 123456.01M;
            var tender = fixture.Create<Tender>();
            tender.TotalPrice = totalPrice;

            var totalPriceFormatted = "123 456";
            AssertResponseForTenderContains(tender, totalPriceFormatted);
        }

        [Fact]
        public void Tenders_NoQuery_ResponseContainsNameForMostExpensiveTender()
        {
            var tenderName = fixture.Create<string>();
            var tender = fixture.Create<Tender>();
            tender.Name = tenderName;

            AssertResponseForTenderContains(tender, tenderName);
        }

        [Fact]
        public void Tenders_NoQuery_ResponseContainsRegionNameForMostExpensiveTender()
        {
            var regionNameService = fixture.Freeze<IRegionNameService>();
            var regionId = fixture.Create<string>();
            var regionName = fixture.Create<string>();
            A.CallTo(() => regionNameService.GetName(regionId)).Returns(regionName);

            var tender = fixture.Create<Tender>();
            tender.Region = regionId;

            AssertResponseForTenderContains(tender, regionName);
        }

        [Fact]
        public void Tenders_NoQuery_ResponseContainsUrlForMostExpensiveTender()
        {
            var tender = fixture.Create<Tender>();
            tender.Id = "panda";

            var tenderUrl = "https://zakupki.kontur.ru/notification44?id=panda";
            AssertResponseForTenderContains(tender, tenderUrl);
        }

        [Fact]
        public void Tenders_QueryWasPassed_ResponseContainsResult()
        {
            var tenderName = fixture.Create<string>();
            var tender = fixture.Create<Tender>();
            tender.Name = tenderName;

            var q = "panda";
            var repo = fixture.Freeze<ITenderRepository>();
            A.CallTo(() => repo.Find(q)).Returns(new[] {tender});

            var sut = CreateDefaultBrowser();
            var actual = sut.Get("/tenders", with => with.Query("q", q));

            actual.Body.AsString().ShouldContain(tenderName);
        }

        private readonly IFixture fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());

        private Browser CreateDefaultBrowser()
        {
            return new Browser(with =>
                               {
                                   with.Module<OverseerModule>();
                                   with.Dependency(fixture.Create<ITenderRepository>());
                                   with.Dependency(fixture.Create<IRegionNameService>());
                               });
        }

        private void AssertResponseForTenderContains(Tender tender, string totalPriceFormatted)
        {
            var repo = fixture.Freeze<ITenderRepository>();
            A.CallTo(() => repo.GetMostExpensive(A<int>._)).Returns(new[] {tender});
            var actual = CreateDefaultBrowser().Get("/tenders").Body.AsString();
            actual.ShouldContain(totalPriceFormatted);
        }
    }
}