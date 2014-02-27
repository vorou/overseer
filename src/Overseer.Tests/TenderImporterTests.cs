using System;
using System.Linq.Expressions;
using FakeItEasy;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Xunit;

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
        public void Read_Always_DetectsType()
        {
            Import(validXml);
            AssertImportedTender(t => t.Type == "fcsNotificationZK");
        }

        [Fact]
        public void Read_TenderWasParsed_SetsIdToTenderId()
        {
            Import(validXml);
            AssertImportedTender(t => t.Id == "0361200002614001321");
        }

        [Fact]
        public void Read_BadXml_SavesNothing()
        {
            Import("panda");
            AssertNothingWasSaved();
        }

        [Fact]
        public void Read_EmptyXml_SavesNothing()
        {
            Import("<hello/>");
            AssertNothingWasSaved();
        }

        [Fact]
        public void Read_SingleLotElement_ReadsPrice()
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
        public void Read_FewMaxPriceElements_SumsThem()
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
        public void Read_TenderNameExists_ReadsIt()
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
        public void Read_PublishDateExists_ReadsItToUtc()
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
        public void Read_NoPublishDate_SetsToNull()
        {
            Import(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
</ns2:fcsNotificationZK>
");
            AssertImportedTender(t => t.PublishDate == null);
        }

        [Fact]
        public void Read_InvalidPublishDate_SetsToNull()
        {
            Import(@"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
    <docPublishDate>panda</docPublishDate>
</ns2:fcsNotificationZK>
");
            AssertImportedTender(t => t.PublishDate == null);
        }

        private void Import(string xml)
        {
            fixture.Freeze<ITenderRepository>();
            var fileReader = fixture.Freeze<IFileReader>();
            var sut = fixture.Create<TenderImporter>();
            A.CallTo(() => fileReader.ReadFiles()).Returns(new[] {new SourceFile {Content = xml}});

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