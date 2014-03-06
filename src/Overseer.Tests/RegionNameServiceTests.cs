using System;
using Shouldly;
using Xunit;
using Xunit.Extensions;

namespace Overseer.Tests
{
    public class RegionNameServiceTests
    {
        [Theory]
        [InlineData("01", "Республика Адыгея")]
        [InlineData("24", "Красноярский край")]
        [InlineData("89", "Ямало-Ненецкий автономный округ")]
        public void Fetch_FetchedSuccessfully_ShouldReturnProperNameForRegion(string id, string expected)
        {
            var sut = new RegionNameService();

            sut.Fetch();

            var regionName = sut.GetName(id);
            regionName.ShouldBe(expected);
        }

        [Fact]
        public void GetName_CalledBeforeFetch_InvalidOperationException()
        {
            var sut = new RegionNameService();

            Should.Throw<InvalidOperationException>(() => sut.GetName("qqq"));
        }

        [Fact]
        public void GetName_UnknownRegion_ReturnsId()
        {
            var sut = new RegionNameService();
            sut.Fetch();
            var unknownRegionId = "panda";

            var actual = sut.GetName(unknownRegionId);

            actual.ShouldBe(unknownRegionId);
        }

        [Fact]
        public void GetName_NullId_ArgumentNullException()
        {
            var sut = new RegionNameService();
            sut.Fetch();

            var e = Should.Throw<ArgumentNullException>(() => sut.GetName(null));
            e.Message.ShouldContain("id");
        }
    }
}