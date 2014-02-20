using Shouldly;
using Xunit;

namespace Overseer.Tests
{
    public class SourceRepositoryTests
    {
        [Fact]
        public void Save_Always_SavesSource()
        {
            var source = new Source {Id = "panda", TenderId = "123", Type = "bear"};
            var sut = new SourceRepository();

            sut.Save(source);

            var actual = sut.GetById("panda");
            actual.Id.ShouldBe(source.Id);
            actual.TenderId.ShouldBe(source.TenderId);
            actual.Type.ShouldBe(source.Type);
        }

        [Fact]
        public void Clear_Always_RemovesEverything()
        {
            var source = new Source {Id = "panda", TenderId = "123", Type = "bear"};
            var sut = new SourceRepository();
            sut.Save(source);

            sut.Clear();

            sut.GetById("panda").ShouldBe(null);
        }
    }
}