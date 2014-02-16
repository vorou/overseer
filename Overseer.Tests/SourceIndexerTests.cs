using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace Overseer.Tests
{
    public class SourceIndexerTests
    {
        private const string SourceFolder = @".\FTP";

        [Test]
        public void Index_Always_SetsIdToFullPath()
        {
            var sut = CreateSut();

            var actual = sut.Index(SourceFolder);

            actual.Single()
                  .Id.ShouldBe(
                               @"ftp.zakupki.gov.ru\fcs_regions\Sakhalinskaja_obl\notifications\currMonth\notification_Sakhalinskaja_obl_2014011100_2014011200_021.xml.zip");
        }

        [Test]
        public void Index_Always_DetectsType()
        {
            var sut = CreateSut();

            var actual = sut.Index(SourceFolder);

            actual.Single().Type.ShouldBe(TenderType.fcsNotificationZK);
        }

        [Test]
        public void Index_Always_SetsTenderId()
        {
            var sut = CreateSut();

            var actual = sut.Index(SourceFolder);

            actual.Single().TenderId.ShouldBe("0361200002614001321");
        }

        private static SourceIndexer CreateSut()
        {
            return new SourceIndexer();
        }
    }
}