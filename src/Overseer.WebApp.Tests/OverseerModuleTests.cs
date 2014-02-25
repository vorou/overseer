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
        public void GetRoot_Always_Responses()
        {
            var sut = CreateDefaultBrowser();

            sut.Get("/").StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public void GetRoot_Always_ContainsPriceForMostExpensiveTender()
        {
            var totalPrice = 123456.78M;
            var tender = fixture.Create<Tender>();
            tender.TotalPrice = totalPrice;

            var repo = fixture.Freeze<ITenderRepository>();
            A.CallTo(() => repo.GetMostExpensive(5)).Returns(new[] {tender});
            var sut = CreateDefaultBrowser();

            var actual = sut.Get("/").Body.AsString();

            actual.ShouldContain("123 456,78 р.");
        }

        [Fact]
        public void GetRoot_Always_ContainsNameForMostExpensiveTender()
        {
            var tenderName = fixture.Create<string>();
            var tender = fixture.Create<Tender>();
            tender.Name = tenderName;

            var repo = fixture.Freeze<ITenderRepository>();
            var tenders = new[] {tender};
            A.CallTo(() => repo.GetMostExpensive(5)).Returns(tenders);
            var sut = CreateDefaultBrowser();

            var actual = sut.Get("/").Body.AsString();

            actual.ShouldContain(tenderName);
        }

        private Browser CreateDefaultBrowser()
        {
            return new Browser(with =>
                               {
                                   with.Module<OverseerModule>();
                                   with.Dependency(fixture.Create<ITenderRepository>());
                               });
        }

        private readonly IFixture fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
    }
}