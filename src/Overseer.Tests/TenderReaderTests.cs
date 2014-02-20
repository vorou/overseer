using System.Linq;
using FakeItEasy;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Shouldly;
using Xunit;
using Xunit.Extensions;

namespace Overseer.Tests
{
    public class TenderReaderTests
    {
        private const string xml = @"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
</ns2:fcsNotificationZK>";
        private readonly IFixture fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());

        [Fact]
        public void Read_Always_DetectsType()
        {
            StubFileReader(xml);
            var sut = CreateSut();

            var actual = sut.Read();

            actual.Single().Type.ShouldBe("fcsNotificationZK");
        }

        [Fact]
        public void Read_Always_ReadsTenderId()
        {
            StubFileReader(xml);
            var sut = CreateSut();

            var actual = sut.Read();

            actual.Single().TenderId.ShouldBe("0361200002614001321");
        }

        [Theory]
        [InlineData(xml)]
        [InlineData("huj")]
        [InlineData("<empty/>")]
        public void Read_Always_SetsIdToFilePath(string content)
        {
            var path = "panda";
            var fileReader = fixture.Freeze<IFileReader>();
            A.CallTo(() => fileReader.ReadFiles()).Returns(new[] {new SourceFile {Path = path, Content = content}});
            var sut = CreateSut();

            var actual = sut.Read();

            actual.Single().Id.ShouldBe(path);
        }

        [Fact]
        public void Read_Success_OKisTrue()
        {
            StubFileReader(xml);
            var sut = CreateSut();

            var actual = sut.Read();

            actual.Single().Success.ShouldBe(true);
        }

        [Fact]
        public void Read_BadXml_OKisFalse()
        {
            StubFileReader("huj");
            var sut = CreateSut();

            var actual = sut.Read();

            actual.Single().Success.ShouldBe(false);
        }

        [Fact]
        public void Read_EmptyXml_OKisFalse()
        {
            StubFileReader("<hello/>");
            var sut = CreateSut();

            var actual = sut.Read();

            actual.Single().Success.ShouldBe(false);
        }

        private TenderReader CreateSut()
        {
            return fixture.Create<TenderReader>();
        }

        private void StubFileReader(string content)
        {
            var fileReader = fixture.Freeze<IFileReader>();
            A.CallTo(() => fileReader.ReadFiles()).Returns(new[] {new SourceFile {Content = content}});
        }
    }
}