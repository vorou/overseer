using System.Linq;
using FakeItEasy;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Shouldly;

namespace Overseer.Tests
{
    public class TenderReaderTests
    {
        private const string xml = @"
<ns2:fcsNotificationZK schemeVersion=""1.0"" xmlns=""http://zakupki.gov.ru/oos/types/1"" xmlns:ns2=""http://zakupki.gov.ru/oos/printform/1"">
    <purchaseNumber>0361200002614001321</purchaseNumber>
</ns2:fcsNotificationZK>";
        private readonly IFixture fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());

        [Test]
        public void Read_Always_DetectsType()
        {
            StubFileReader(xml);
            var sut = CreateSut();

            var actual = sut.Read();

            actual.Single().Type.ShouldBe("fcsNotificationZK");
        }

        [Test]
        public void Read_Always_ReadsTenderId()
        {
            StubFileReader(xml);
            var sut = CreateSut();

            var actual = sut.Read();

            actual.Single().TenderId.ShouldBe("0361200002614001321");
        }

        [Test]
        [TestCase(xml)]
        [TestCase("huj")]
        [TestCase("<empty/>")]
        public void Read_Always_SetsIdToFilePath(string content)
        {
            var path = "panda";
            var fileReader = fixture.Freeze<IFileReader>();
            A.CallTo(() => fileReader.ReadFiles()).Returns(new[] {new SourceFile {Path = path, Content = content}});
            var sut = CreateSut();

            var actual = sut.Read();

            actual.Single().Id.ShouldBe(path);
        }

        [Test]
        public void Read_Success_OKisTrue()
        {
            StubFileReader(xml);
            var sut = CreateSut();

            var actual = sut.Read();

            actual.Single().Success.ShouldBe(true);
        }

        [Test]
        public void Read_BadXml_OKisFalse()
        {
            StubFileReader("huj");
            var sut = CreateSut();

            var actual = sut.Read();

            actual.Single().Success.ShouldBe(false);
        }

        [Test]
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