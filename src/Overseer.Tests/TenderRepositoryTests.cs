using System;
using System.Linq;
using Nest;
using Ploeh.AutoFixture;
using Ploeh.SemanticComparison.Fluent;
using Shouldly;
using Xunit;

namespace Overseer.Tests
{
    public class TenderRepositoryTests
    {
        private static readonly string index = "overseer-test";
        private readonly IFixture fixture = new Fixture();

        public TenderRepositoryTests()
        {
            CreateSut().Clear();
        }

        [Fact]
        public void Save_Always_SavesSource()
        {
            var source = new Tender {Id = "panda", TenderId = "123", Type = "bear"};
            var sut = CreateSut();

            Save(sut, source);

            var actual = sut.GetById("panda");
            actual.Id.ShouldBe(source.Id);
            actual.TenderId.ShouldBe(source.TenderId);
            actual.Type.ShouldBe(source.Type);
        }

        [Fact]
        public void Clear_Always_RemovesEverything()
        {
            var source = new Tender {Id = "panda", TenderId = "123", Type = "bear"};
            var sut = CreateSut();
            sut.Save(source);

            sut.Clear();

            sut.GetById("panda").ShouldBe(null);
        }

        [Fact]
        public void GetMostExpensive_NoTenders_ReturnsEmpty()
        {
            var sut = CreateSut();

            var actual = sut.GetMostExpensive();

            actual.ShouldBeEmpty();
        }

        [Fact]
        public void GetMostExpensive_LimitIsMoreThanTotal_ReturnsAll()
        {
            var sut = CreateSut();
            Save(sut, fixture.Create<Tender>());

            var actual = sut.GetMostExpensive(1);

            actual.Count().ShouldBe(1);
        }

        [Fact]
        public void GetMostExpensive_Always_ReadsTenderProperly()
        {
            var tender = fixture.Create<Tender>();
            var sut = CreateSut();
            Save(sut, tender);

            var actual = sut.GetMostExpensive(1);

            actual.Single().ShouldBe(tender.AsSource().OfLikeness<Tender>().CreateProxy());
        }

        private static TenderRepository CreateSut()
        {
            return new TenderRepository(index);
        }

        private static void Save(TenderRepository sut, Tender tender)
        {
            sut.Save(tender);
            new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200")).SetDefaultIndex(index)).Refresh<Tender>();
        }
    }
}