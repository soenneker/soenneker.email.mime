using Soenneker.Email.Mime.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Email.Mime.Tests;

[Collection("Collection")]
public class MimeUtilTests : FixturedUnitTest
{
    private readonly IMimeUtil _util;

    public MimeUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IMimeUtil>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
