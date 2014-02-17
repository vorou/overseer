using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace Overseer.Tests
{
    public class SourceRepositoryTests
    {
        [Test]
        public void Saving_Always_ShouldSaveSource()
        {
            var source = new Source {Id = "panda", TenderId = "123", Type = "bear"};
            var sut = new SourceRepository();

            sut.Save(source);

            var actual = sut.FindAll().Single();
            actual.Id.ShouldBe(source.Id);
            actual.TenderId.ShouldBe(source.TenderId);
            actual.Type.ShouldBe(source.Type);
        }
    }
}