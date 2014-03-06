using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Ploeh.AutoFixture.Xunit;

namespace Overseer.Common.Tests
{
    public class AutoFakeAttribute : AutoDataAttribute
    {
        public AutoFakeAttribute() : base(new Fixture().Customize(new AutoFakeItEasyCustomization()))
        {
        }
    }
}