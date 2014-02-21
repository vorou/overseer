using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Ploeh.AutoFixture.Xunit;
using Shouldly;
using Xunit.Extensions;

namespace Overseer.Tests
{
    public class TenderReaderTests
    {
        private const string defaultXml = @"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
</ns2:fcsNotificationZK>";
        private readonly IFixture fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());

        [Theory, AutoFake]
        public void Read_Always_DetectsType([Frozen] IFileReader fileReader, TenderReader sut)
        {
            FileReaderReturnsContent(fileReader, defaultXml);

            var actual = sut.Read();

            actual.Single().Type.ShouldBe("fcsNotificationZK");
        }

        [Theory, AutoFake]
        public void Read_Always_ReadsTenderId([Frozen] IFileReader fileReader, TenderReader sut)
        {
            FileReaderReturnsContent(fileReader, defaultXml);

            var actual = sut.Read();

            actual.Single().TenderId.ShouldBe("0361200002614001321");
        }

        [Theory]
        [InlineData(defaultXml)]
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

        [Theory, AutoFake]
        public void Read_Success_OKisTrue([Frozen] IFileReader fileReader, TenderReader sut)
        {
            FileReaderReturnsContent(fileReader, defaultXml);

            var actual = sut.Read();

            actual.Single().Success.ShouldBe(true);
        }

        [Theory, AutoFake]
        public void Read_BadXml_OKisFalse([Frozen] IFileReader fileReader, TenderReader sut)
        {
            FileReaderReturnsContent(fileReader, "huj");

            var actual = sut.Read();

            actual.Single().Success.ShouldBe(false);
        }

        [Theory, AutoFake]
        public void Read_EmptyXml_OKisFalse([Frozen] IFileReader fileReader, TenderReader sut)
        {
            FileReaderReturnsContent(fileReader, "<hello/>");

            var actual = sut.Read();

            actual.Single().Success.ShouldBe(false);
        }

        public static IEnumerable<object[]> PriceTests
        {
            get
            {
                yield return new object[] {@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <lot>
        <maxPrice>248261.2</maxPrice>
    </lot>
</ns2:fcsNotificationZK>", 248261.2M};
            }
        }

        [Theory, PropertyData("PriceTests")]
        public void Read_SingleLotElement_ReadsPrice(string xml, decimal totalPrice)
        {
            var fileReader = fixture.Freeze<IFileReader>();
            var sut = CreateSut();
            FileReaderReturnsContent(fileReader, xml);

            var actual = sut.Read();

            actual.Single().TotalPrice.ShouldBe(totalPrice);
        }

        private TenderReader CreateSut()
        {
            return fixture.Create<TenderReader>();
        }

        private static void FileReaderReturnsContent(IFileReader fileReader, string content)
        {
            A.CallTo(() => fileReader.ReadFiles()).Returns(new[] {new SourceFile {Content = content}});
        }
    }
}