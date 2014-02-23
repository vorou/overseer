using System.Linq;
using FakeItEasy;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Ploeh.AutoFixture.Xunit;
using Shouldly;
using Xunit;
using Xunit.Extensions;

namespace Overseer.Tests
{
    public class TenderReaderTests
    {
        private const string validXml = @"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
</ns2:fcsNotificationZK>";
        private readonly IFixture fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());

        [Theory, AutoFake]
        public void Read_Always_DetectsType([Frozen] IFileReader fileReader, TenderReader sut)
        {
            FileReaderReturnsContent(fileReader, validXml);

            var actual = sut.Read();

            actual.Single().Type.ShouldBe("fcsNotificationZK");
        }

        [Theory, AutoFake]
        public void Read_Always_ReadsTenderId([Frozen] IFileReader fileReader, TenderReader sut)
        {
            FileReaderReturnsContent(fileReader, validXml);

            var actual = sut.Read();

            actual.Single().TenderId.ShouldBe("0361200002614001321");
        }

        [Theory, AutoFake]
        public void Read_TenderWasParsed_SetsIdToFilePath([Frozen] IFileReader fileReader, TenderReader sut)
        {
            var path = "panda";
            A.CallTo(() => fileReader.ReadFiles()).Returns(new[] {new SourceFile {Path = path, Content = validXml}});

            var actual = sut.Read();

            actual.Single().Id.ShouldBe(path);
        }

        [Theory, AutoFake]
        public void Read_BadXml_ReturnsNothing([Frozen] IFileReader fileReader, TenderReader sut)
        {
            FileReaderReturnsContent(fileReader, "huj");

            var actual = sut.Read();

            actual.ShouldBeEmpty();
        }

        [Theory, AutoFake]
        public void Read_EmptyXml_ReturnsNothing([Frozen] IFileReader fileReader, TenderReader sut)
        {
            FileReaderReturnsContent(fileReader, "<hello/>");

            var actual = sut.Read();

            actual.ShouldBeEmpty();
        }

        [Fact]
        public void Read_SingleLotElement_ReadsPrice()
        {
            AssertTotalPrice(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <lot>
        <maxPrice>248261.2</maxPrice>
    </lot>
</ns2:fcsNotificationZK>", 248261.2M);
        }

        [Fact]
        public void Read_FewMaxPriceElements_SumsThem()
        {
            AssertTotalPrice(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <lot>
        <maxPrice>1.1</maxPrice>
    </lot>
    <lot>
        <maxPrice>2.2</maxPrice>
    </lot>
</ns2:fcsNotificationZK>", 3.3M);
        }

        private TenderReader CreateSut()
        {
            return fixture.Create<TenderReader>();
        }

        private static void FileReaderReturnsContent(IFileReader fileReader, string content)
        {
            A.CallTo(() => fileReader.ReadFiles()).Returns(new[] {new SourceFile {Content = content}});
        }

        private void AssertTotalPrice(string xml, decimal totalPrice)
        {
            var fileReader = fixture.Freeze<IFileReader>();
            var sut = CreateSut();
            FileReaderReturnsContent(fileReader, xml);

            var actual = sut.Read();

            actual.Single().TotalPrice.ShouldBe(totalPrice);
        }
    }
}