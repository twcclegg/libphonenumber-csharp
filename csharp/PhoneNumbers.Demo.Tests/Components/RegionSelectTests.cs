using Bunit;
using PhoneNumbers.Demo.Components;
using Xunit;

namespace PhoneNumbers.Demo.Tests.Components;

public class RegionSelectTests : BunitContext
{
    [Fact]
    public void lists_every_supported_region_in_code_order_with_its_calling_code()
    {
        var cut = Render<RegionSelect>(p => p.Add(s => s.Value, "US"));

        var options = cut.FindAll("option").Select(o => o.TextContent).ToList();

        Assert.Equal(PhoneNumberUtil.GetInstance().GetSupportedRegions().Count, options.Count);
        Assert.Equal(options.OrderBy(o => o, StringComparer.Ordinal), options);
        Assert.Contains("GB (+44)", options);
    }

    [Fact]
    public void marks_the_current_value_as_selected()
    {
        var cut = Render<RegionSelect>(p => p.Add(s => s.Value, "JP"));

        Assert.Equal("JP", cut.Find("option[selected]").GetAttribute("value"));
    }

    [Fact]
    public void reports_the_region_the_user_picks()
    {
        string? picked = null;
        var cut = Render<RegionSelect>(p => p
            .Add(s => s.Value, "US")
            .Add(s => s.ValueChanged, (string r) => picked = r));

        cut.Find("select").Change("DE");

        Assert.Equal("DE", picked);
    }

    [Fact]
    public void forwards_its_id_to_the_select_so_a_label_can_target_it()
    {
        var cut = Render<RegionSelect>(p => p
            .Add(s => s.Value, "US")
            .AddUnmatched("id", "some-region"));

        Assert.NotNull(cut.Find("select#some-region"));
    }
}
