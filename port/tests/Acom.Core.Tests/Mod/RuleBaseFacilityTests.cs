#pragma warning disable CA1716 // namespace mirrors src/Mod like the other test folders
namespace Acom.Tests.Mod;

public class RuleBaseFacilityTests
{
	[Fact]
	public void Behaves_like_std_bitset_128()
	{
		var f = new RuleBaseFacilityFunctions();
		Assert.True(f.None());
		f.Set(3);
		f[100] = true;
		Assert.True(f.Test(3));
		Assert.True(f[100]);
		Assert.False(f.Test(64));
		Assert.Equal(2, f.Count());
		Assert.True(f.Any());

		var g = new RuleBaseFacilityFunctions(0b1010UL); // bits 1 and 3
		Assert.Equal(1, (f & g).Count());
		Assert.True((f & g).Test(3));
		Assert.Equal(3, (f | g).Count());
		var x = f ^ g;
		Assert.True(x.Test(1));
		Assert.False(x.Test(3));
		Assert.True(x.Test(100));
		Assert.Equal(126, (~f).Count());

		f.Reset(3);
		Assert.Equal(1, f.Count());
		f.Set();
		Assert.True(f.All());
		f.Reset();
		Assert.True(f.None());

		Assert.Throws<ArgumentOutOfRangeException>(() => f.Test(128));
		Assert.Throws<ArgumentOutOfRangeException>(() => f.Set(-1));
	}

	[Fact]
	public void Is_a_value_type_and_prints_highest_bit_first()
	{
		var f = new RuleBaseFacilityFunctions(1);
		var copy = f;
		copy.Set(127);
		Assert.False(f.Test(127));
		Assert.NotEqual(f, copy);
		Assert.Equal(new RuleBaseFacilityFunctions(1), f);

		string s = copy.ToString();
		Assert.Equal(128, s.Length);
		Assert.Equal('1', s[0]);
		Assert.Equal('1', s[127]);
		Assert.Equal(2, s.Count(c => c == '1'));
	}

	[Fact]
	public void Base_facility_defaults()
	{
		var facility = new RuleBaseFacility("STR_LAB", 2);
		Assert.Equal("STR_LAB", facility.UfopediaType);
		Assert.True(facility.IsSmall);
		Assert.True(facility.SpriteEnabled);
		Assert.False(facility.SpriteEnabledRaw);
		Assert.Equal(-1, facility.PlaceSound);
		Assert.True(facility.IsAllowedForBaseType(true));
		Assert.True(facility.IsAllowedForBaseType(false));

		var other = new RuleBaseFacility("STR_HANGAR", 3);
		Assert.Equal(BasePlacementErrors.BPE_UpgradeDisallowed, facility.GetCanBuildOverOtherFacility(other));
	}
}
