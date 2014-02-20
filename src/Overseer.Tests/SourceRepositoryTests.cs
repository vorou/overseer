using System;
using System.Linq;
using Nest;
using Ploeh.AutoFixture;
using Ploeh.SemanticComparison.Fluent;
using Shouldly;
using Xunit;

namespace Overseer.Tests
{
    public class SourceRepositoryTests
    {
        private static readonly string index = "overseer-test";
        private readonly IFixture fixture = new Fixture();

        public SourceRepositoryTests()
        {
            CreateSut().Clear();
        }

        [Fact]
        public void Save_Always_SavesSource()
        {
            var source = new Source {Id = "panda", TenderId = "123", Type = "bear"};
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
            var source = new Source {Id = "panda", TenderId = "123", Type = "bear"};
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
            Save(sut, fixture.Create<Source>());

            var actual = sut.GetMostExpensive(1);

            actual.Count().ShouldBe(1);
        }

        [Fact]
        public void GetMostExpensive_Always_ReturnsProperTender()
        {
            var tender = fixture.Create<Source>();
            var sut = CreateSut();
            Save(sut, tender);

            var actual = sut.GetMostExpensive(1);

            actual.Single().ShouldBe(tender.AsSource().OfLikeness<Source>().CreateProxy());
        }

        private static SourceRepository CreateSut()
        {
            return new SourceRepository(index);
        }

        private static void Save(SourceRepository sut, Source source)
        {
            sut.Save(source);
            new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200")).SetDefaultIndex(index)).Refresh<Source>();
        }
    }
}