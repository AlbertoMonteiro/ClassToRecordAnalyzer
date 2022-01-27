namespace ClassToRecordAnalyzer;

public interface IOla
{
}

public interface IHello
{
}

public class Class1
{
    private readonly IOla ola;
    private readonly IHello hello;
    public Class1(IOla ola, IHello hello)
    {
        this.ola = ola;
        this.hello = hello;
    }
}

class Class1Test
{
    private readonly Class1 _sut;
    private readonly IOla _ola = default;
    private readonly IHello _hello = default;
    public Class1Test() => _sut = new Class1(_ola, _hello);
}
