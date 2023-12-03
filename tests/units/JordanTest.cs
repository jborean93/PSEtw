using PSETW;
using System;
using Xunit;

namespace PSETWTests;

public class JordanTests
{
    [Fact]
    public void Abc()
    {
        string expected = "foo";
        string actual = Jordan.MyMethod1();
        Assert.Equal(expected, actual);
    }
}
