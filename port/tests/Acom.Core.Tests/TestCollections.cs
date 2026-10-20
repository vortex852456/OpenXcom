namespace Acom.Tests;

/// <summary>Tests that change the global Options run alone, so other collections (e.g. TimerTests) cannot race with them.</summary>
[CollectionDefinition("OptionsTests", DisableParallelization = true)]
public sealed class OptionsTestsSerial;
