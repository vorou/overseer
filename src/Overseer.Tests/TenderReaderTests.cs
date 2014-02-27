using System;
using System.Collections.Generic;
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

        [Fact]
        public void Read_Always_DetectsType()
        {
            var actual = ReadTenders(validXml);

            actual.Single().Type.ShouldBe("fcsNotificationZK");
        }

        [Theory, AutoFake]
        public void Read_TenderWasParsed_SetsIdToTenderId([Frozen] IFileReader fileReader, TenderRetriever sut)
        {
            var path = "panda";
            A.CallTo(() => fileReader.ReadFiles()).Returns(new[] {new SourceFile {Path = path, Content = validXml}});

            var actual = sut.GetNew();

            actual.Single().Id.ShouldBe("0361200002614001321");
        }

        [Fact]
        public void Read_BadXml_ReturnsNothing()
        {
            var actual = ReadTenders("huj");

            actual.ShouldBeEmpty();
        }

        [Fact]
        public void Read_EmptyXml_ReturnsNothing()
        {
            var actual = ReadTenders("<hello/>");

            actual.ShouldBeEmpty();
        }

        [Fact]
        public void Read_SingleLotElement_ReadsPrice()
        {
            var actual = ReadTenders(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <lot>
        <maxPrice>248261.2</maxPrice>
    </lot>
</ns2:fcsNotificationZK>");

            actual.Single().TotalPrice.ShouldBe(248261.2M);
        }

        [Fact]
        public void Read_FewMaxPriceElements_SumsThem()
        {
            var actual = ReadTenders(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <lot>
        <maxPrice>1.1</maxPrice>
    </lot>
    <lot>
        <maxPrice>2.2</maxPrice>
    </lot>
</ns2:fcsNotificationZK>");

            actual.Single().TotalPrice.ShouldBe(3.3M);
        }

        [Fact]
        public void Read_TenderNameExists_ReadsIt()
        {
            var actual = ReadTenders(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <purchaseObjectInfo>Атомный ледокол</purchaseObjectInfo>
</ns2:fcsNotificationZK>
");

            actual.Single().Name.ShouldBe("Атомный ледокол");
        }

        [Fact]
        public void Read_PublishDateExists_ReadsItToUtc()
        {
            var actual = ReadTenders(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <docPublishDate>2014-01-11T19:07:34.112+04:00</docPublishDate>
</ns2:fcsNotificationZK>
");

            actual.Single().PublishDate.ShouldBe(new DateTime(2014, 1, 11, 15, 7, 34, 112, DateTimeKind.Utc));
        }

        [Fact]
        public void Read_NoPublishDate_SetsToNull()
        {
            var actual = ReadTenders(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
</ns2:fcsNotificationZK>
");

            actual.Single().PublishDate.ShouldBe(null);
        }

        [Fact]
        public void Read_InvalidPublishDate_SetsToNull()
        {
            var actual = ReadTenders(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <docPublishDate>panda</docPublishDate>
</ns2:fcsNotificationZK>
");

            actual.Single().PublishDate.ShouldBe(null);
        }

        private IEnumerable<Tender> ReadTenders(string xml)
        {
            var fileReader = fixture.Freeze<IFileReader>();
            var sut = fixture.Create<TenderRetriever>();
            A.CallTo(() => fileReader.ReadFiles()).Returns(new[] {new SourceFile {Content = xml}});

            return sut.GetNew();
        }
    }
}