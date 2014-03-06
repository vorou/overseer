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
    }
}