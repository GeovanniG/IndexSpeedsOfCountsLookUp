using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Runtime.CompilerServices;

namespace IndexSpeedsOfCountsLookUp;

/*
---------------------------------------------------

// * Summary *

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3155/23H2/2023Update/SunValley3)
Intel Core i7-8550U CPU 1.80GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.201
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2


| Method                       | textCacheTime | charLength | Mean     | Error   | StdDev   |
|----------------------------- |-------------- |----------- |---------:|--------:|---------:|
| SaveDataUpdateText_Fixed     | 10            | ?          | 397.2 us | 7.86 us | 16.93 us |
| SaveDataInsertNewText_Fixed  | 10            | ?          | 421.4 us | 8.18 us |  7.66 us |
| SaveDataUpdateText_Random    | 60            | 500        | 394.3 us | 6.29 us |  5.58 us |
| SaveDataInsertNewText_Random | 60            | 500        | 417.7 us | 7.17 us |  7.37 us |

// * Hints *
Outliers
  IndexSpeedTest.SaveDataUpdateText_Fixed: Default     -> 7 outliers were removed, 16 outliers were detected (354.59 us..373.49 us, 427.83 us..466.68 us)
  IndexSpeedTest.SaveDataInsertNewText_Fixed: Default  -> 2 outliers were removed (447.09 us, 479.38 us)
  IndexSpeedTest.SaveDataUpdateText_Random: Default    -> 1 outlier  was  removed (481.56 us)
  IndexSpeedTest.SaveDataInsertNewText_Random: Default -> 2 outliers were removed (445.14 us, 758.71 us)

Data:
  Both tables had over 2 million rows

 */
public class IndexSpeedTest
{
    private readonly IndexSpeedDal _indexSpeedDal;
    private static Random random1 = new Random(); // Random not thread safe
    private static Random random2 = new Random(); // Random not thread safe
    public IndexSpeedTest()
    {
        _indexSpeedDal = new IndexSpeedDal();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static string RandomString1(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random1.Next(s.Length)]).ToArray());
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static string RandomString2(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random2.Next(s.Length)]).ToArray());
    }


    [Benchmark]
    [Arguments(500, 60)]
    public async Task SaveDataUpdateText_Random(int charLength, int textCacheTime)
    {
        await _indexSpeedDal.SaveDataUpdateText(RandomString1(charLength), textCacheTime);
    }

    [Benchmark]
    [Arguments(500, 60)]
    public async Task SaveDataInsertNewText_Random(int charLength, int textCacheTime)
    {
        await _indexSpeedDal.SaveDataInsertNewText(RandomString2(charLength), textCacheTime);
    }

    [Benchmark]
    [Arguments(10)]
    public async Task SaveDataUpdateText_Fixed(int textCacheTime)
    {
        await _indexSpeedDal.SaveDataUpdateText("google.com", textCacheTime);
    }

    [Benchmark]
    [Arguments(10)]
    public async Task SaveDataInsertNewText_Fixed(int textCacheTime)
    {
        await _indexSpeedDal.SaveDataInsertNewText("google.com", textCacheTime);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<IndexSpeedTest>();
    }
}
