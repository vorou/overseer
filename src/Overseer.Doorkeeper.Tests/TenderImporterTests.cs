using System;
using System.Linq.Expressions;
using FakeItEasy;
using Overseer.Common;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Xunit;
using Xunit.Extensions;

namespace Overseer.Doorkeeper.Tests
{
    public class TenderImporterTests
    {
        private const string ValidUri = "ftp://valid/region/Adygeja_Resp";
        private readonly IFixture fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());

        [Fact]
        public void Import_Always_DetectsType()
        {
            Import(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <maxPrice>123.3</maxPrice>
</ns2:fcsNotificationZK>");
            AssertImportedTender(t => t.Type == "fcsNotificationZK");
        }

        [Fact]
        public void Import_TenderWasParsed_SetsIdToTenderId()
        {
            Import(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <maxPrice>123.3</maxPrice>
</ns2:fcsNotificationZK>");
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
            ImportValidXmlWithoutPrice(@"
<lot>
    <maxPrice>248261.2</maxPrice>
</lot>
");
            AssertImportedTender(t => t.TotalPrice == 248261.2M);
        }

        [Fact]
        public void Import_FewMaxPriceElementsWithDifferentPrices_DontImport()
        {
            ImportValidXmlWith(@"
<lot>
    <maxPrice>123.4</maxPrice>
</lot>
<lot>
    <maxPrice>9999.1</maxPrice>
</lot>
");
            AssertNothingWasSaved();
        }

        [Fact]
        public void Import_FewMaxPriceElementsWithSamePrice_TakeThePriceOnce()
        {
            ImportValidXmlWithoutPrice(@"
<lot>
    <maxPrice>11.1</maxPrice>
</lot>
<lot>
    <maxPrice>11.1</maxPrice>
</lot>
");
            AssertImportedTender(t => t.TotalPrice == 11.1M);
        }

        [Fact]
        public void Import_TenderNameExists_ReadsIt()
        {
            ImportValidXmlWith(@"
<purchaseObjectInfo>Атомный ледокол</purchaseObjectInfo>
");
            AssertImportedTender(t => t.Name == "Атомный ледокол");
        }

        [Fact]
        public void Import_PublishDateExists_ReadsItToUtc()
        {
            ImportValidXmlWith(@"
<docPublishDate>2014-01-11T19:07:34.112+04:00</docPublishDate>
");
            AssertImportedTender(t => t.PublishDate == new DateTime(2014, 1, 11, 15, 7, 34, 112, DateTimeKind.Utc));
        }

        [Fact]
        public void Import_NoPublishDate_SetsToMinDateTime()
        {
            ImportValidXmlWith("");
            AssertImportedTender(t => t.PublishDate == DateTime.MinValue);
        }

        [Fact]
        public void Import_InvalidPublishDate_SetsToMinDate()
        {
            ImportValidXmlWith(@"
<docPublishDate>panda</docPublishDate>
");
            AssertImportedTender(t => t.PublishDate == DateTime.MinValue);
        }

        [Fact]
        public void Import_Succeeded_MarksAsImported()
        {
            var src = ValidUri;
            ImportValidXmlWith("", src);

            var reader = fixture.Create<IFileReader>();
            A.CallTo(() => reader.MarkImported(new Uri(src))).MustHaveHappened();
        }

        [Theory]
        [InlineData("ftp://localhost/fcs_regions/Adygeja_Resp/notifications/currMonth/panda.zip", "01")]
        [InlineData("ftp://localhost/fcs_regions/Omskaja_obl/notifications/currMonth/panda.zip", "55")]
        public void Import_KnownRegion_SetsRegion(string src, string expected)
        {
            ImportValidXmlWith("", src);

            AssertImportedTender(t => t.Region == expected);
        }

        [Fact]
        public void Import_UnknownRegion_ShouldNotSave()
        {
            ImportValidXmlWith("", "ftp://localhost/fcs_regions/Panda_obl/notifications/currMonth/panda.zip");

            var repo = fixture.Create<ITenderRepository>();
            A.CallTo(() => repo.Save(A<Tender>._)).MustNotHaveHappened();
        }

        [Fact]
        public void Import_Always_SetsSource()
        {
            var source = ValidUri;
            ImportValidXmlWith("", source);

            AssertImportedTender(t => t.Source == source);
        }

        [Fact]
        public void Import_NoMaxPrice_DontImport()
        {
            ImportValidXmlWithoutPrice("");
            AssertNothingWasSaved();
        }

        [Fact]
        public void Import_InvalidPrice_DontImport()
        {
            ImportValidXmlWithoutPrice("<maxPrice>panda</maxPrice>");
            AssertNothingWasSaved();
        }

        private void ImportValidXmlWith(string body = "", string path = ValidUri)
        {
            ImportValidXmlWithoutPrice(@"<maxPrice>999.99</maxPrice>" + body, path);
        }

        private void ImportValidXmlWithoutPrice(string body = "", string path = ValidUri)
        {
            Import(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>"
                   + body + @"
</ns2:fcsNotificationZK>", path);
        }

        private void Import(string xml, string path = ValidUri)
        {
            fixture.Freeze<ITenderRepository>();
            var fileReader = fixture.Freeze<IFileReader>();
            var sut = fixture.Create<TenderImporter>();
            A.CallTo(() => fileReader.ReadNewFiles()).Returns(new[] {new SourceFile {Content = xml, Uri = new Uri(path)}});

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