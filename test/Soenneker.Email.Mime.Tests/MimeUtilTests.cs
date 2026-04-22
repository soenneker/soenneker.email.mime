using Soenneker.Email.Mime.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Email.Mime.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class MimeUtilTests : HostedUnitTest
{
    private readonly IMimeUtil _util;

    public MimeUtilTests(Host host) : base(host)
    {
        _util = Resolve<IMimeUtil>(true);
    }

    [Test]
    public void Default()
    {

    }
}
