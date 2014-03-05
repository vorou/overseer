using System;
using System.Linq.Expressions;
using FakeItEasy;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Xunit;
using Xunit.Extensions;

namespace Overseer.Tests
{
    public class TenderImporterTests
    {
        private const string validXml = @"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
</ns2:fcsNotificationZK>";
        private readonly IFixture fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());

        [Fact]
        public void Import_Always_DetectsType()
        {
            Import(validXml);
            AssertImportedTender(t => t.Type == "fcsNotificationZK");
        }

        [Fact]
        public void Import_TenderWasParsed_SetsIdToTenderId()
        {
            Import(validXml);
            AssertImportedTender(t => t.Id == "0361200002614001321");
        }

        [Fact]
        public void Import_BadXml_SavesNothing()
        {
            Import("panda");
            AssertNothingWasSaved();
        }

        [Fact]
        public void Import_EmptyXml_SavesNothing()
        {
            Import("<hello/>");
            AssertNothingWasSaved();
        }

        [Fact]
        public void Import_SingleLotElement_ReadsPrice()
        {
            Import(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <lot>
        <maxPrice>248261.2</maxPrice>
    </lot>
</ns2:fcsNotificationZK>");
            AssertImportedTender(t => t.TotalPrice == 248261.2M);
        }

        [Fact]
        public void Import_FewMaxPriceElements_SumsThem()
        {
            Import(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <lot>
        <maxPrice>1.1</maxPrice>
    </lot>
    <lot>
        <maxPrice>2.2</maxPrice>
    </lot>
</ns2:fcsNotificationZK>");
            AssertImportedTender(t => t.TotalPrice == 3.3M);
        }

        [Fact]
        public void Import_TenderNameExists_ReadsIt()
        {
            Import(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <purchaseObjectInfo>Атомный ледокол</purchaseObjectInfo>
</ns2:fcsNotificationZK>
");
            AssertImportedTender(t => t.Name == "Атомный ледокол");
        }

        [Fact]
        public void Import_PublishDateExists_ReadsItToUtc()
        {
            Import(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <docPublishDate>2014-01-11T19:07:34.112+04:00</docPublishDate>
</ns2:fcsNotificationZK>
");
            AssertImportedTender(t => t.PublishDate == new DateTime(2014, 1, 11, 15, 7, 34, 112, DateTimeKind.Utc));
        }

        [Fact]
        public void Import_NoPublishDate_SetsToMinDateTime()
        {
            Import(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
</ns2:fcsNotificationZK>
");
            AssertImportedTender(t => t.PublishDate == DateTime.MinValue);
        }

        [Fact]
        public void Import_InvalidPublishDate_SetsToMinDate()
        {
            Import(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <docPublishDate>panda</docPublishDate>
</ns2:fcsNotificationZK>
");
            AssertImportedTender(t => t.PublishDate == DateTime.MinValue);
        }

        [Fact]
        public void Import_Succeeded_MarksAsImported()
        {
            var src = "path";
            Import(validXml, src);

            var reader = fixture.Create<IFileReader>();
            A.CallTo(() => reader.MarkImported(src)).MustHaveHappened();
        }

        [Theory]
        [InlineData("ftp://localhost/fcs_regions/Adygeja_Resp/notifications/currMonth/panda.zip", "01")]
        [InlineData("ftp://localhost/fcs_regions/Omskaja_obl/notifications/currMonth/panda.zip", "55")]
        public void Import_KnownRegion_SetsRegion(string src, string expected)
        {
            Import(validXml, src);

            AssertImportedTender(t => t.Region == expected);
        }

        [Fact]
        public void Import_UnknownRegion_SetsRegionToNull()
        {
            Import(validXml, "ftp://localhost/fcs_regions/Panda_obl/notifications/currMonth/panda.zip");

            AssertImportedTender(t => t.Region == null);
        }

        private void Import(string xml, string path = null)
        {
            fixture.Freeze<ITenderRepository>();
            var fileReader = fixture.Freeze<IFileReader>();
            var sut = fixture.Create<TenderImporter>();
            A.CallTo(() => fileReader.ReadNewFiles()).Returns(new[] {new SourceFile {Content = xml, Path = path}});

            sut.Import();
        }

        private void AssertImportedTender(Expression<Func<Tender, bool>> predicate)
        {
            var repo = fixture.Create<ITenderRepository>();
            A.CallTo(() => repo.Save(A<Tender>.That.Matches(predicate))).MustHaveHappened();
        }

        private void AssertNothingWasSaved()
        {
            var repo = fixture.Create<ITenderRepository>();
            A.CallTo(() => repo.Save(A<Tender>._)).MustNotHaveHappened();
        }
    }
}