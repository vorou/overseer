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
        public void Tenders_NoQuery_ContainsPriceForMostExpensiveTender()
        {
            var totalPriceFormatted = "123 456";
            var totalPrice = 123456.01M;
            var tender = fixture.Create<Tender>();
            tender.TotalPrice = totalPrice;
            StubMostExpensive(tender);

            var actual = GetResponseAsString("/tenders");

            actual.ShouldContain(totalPriceFormatted);
        }

        [Fact]
        public void Tenders_NoQuery_ContainsNameForMostExpensiveTender()
        {
            var tenderName = fixture.Create<string>();
            var tender = fixture.Create<Tender>();
            tender.Name = tenderName;
            StubMostExpensive(tender);

            var actual = GetResponseAsString("/tenders");

            actual.ShouldContain(tenderName);
        }

        [Fact]
        public void Tenders_NoQuery_ContainsRegionNameForTender()
        {
            var regionNameService = fixture.Freeze<IRegionNameService>();
            var regionId = fixture.Create<string>();
            var regionName = fixture.Create<string>();
            A.CallTo(() => regionNameService.GetName(regionId)).Returns(regionName);
            var tender = fixture.Create<Tender>();
            tender.Region = regionId;
            StubMostExpensive(tender);

            var actual = GetResponseAsString("/tenders");

            actual.ShouldContain(regionName);
        }

        [Fact]
        public void Root_Always_ContainsLinkToTender()
        {
            var tender = fixture.Create<Tender>();
            tender.Id = "panda";
            StubMostExpensive(tender);

            var actual = GetResponseAsString("/tenders");

            actual.ShouldContain("https://zakupki.kontur.ru/notification44?id=panda");
        }

        [Fact]
        public void Tenders_Always_ShouldRespond()
        {
            var sut = CreateDefaultBrowser();

            var actual = sut.Get("/tenders");

            actual.StatusCode.ShouldBe(HttpStatusCode.OK);
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

            var actual = sut.Get("/tenders", with => with.Query("q", "panda")).Body.AsString();

            actual.ShouldContain(tenderName);
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

        private void StubMostExpensive(Tender tender)
        {
            var repo = fixture.Freeze<ITenderRepository>();
            A.CallTo(() => repo.GetMostExpensive(A<int>._)).Returns(new[] {tender});
        }

        private string GetResponseAsString(string route)
        {
            return CreateDefaultBrowser().Get(route).Body.AsString();
        }
    }
}