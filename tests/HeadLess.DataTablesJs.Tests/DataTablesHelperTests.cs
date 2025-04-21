using Xunit;
using HeadLess.DataTablesJs.Core;

namespace HeadLess.DataTablesJs.Tests;

public class DataTablesHelperTests
{
    [Fact]
    public void CanInstantiateHelper()
    {
        var helper = new DataTablesHelper();
        Assert.NotNull(helper);
    }
}
