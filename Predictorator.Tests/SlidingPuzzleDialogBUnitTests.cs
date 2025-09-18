using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using Predictorator.Components;

namespace Predictorator.Tests;

public class SlidingPuzzleDialogBUnitTests
{
    [Fact]
    public async Task TouchStart_Moves_Tile_Into_Blank_Space()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();

        var cut = ctx.Render<SlidingPuzzleDialog>();

        var instance = cut.Instance;
        var tilesField = typeof(SlidingPuzzleDialog).GetField("_tiles", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var blankIndexField = typeof(SlidingPuzzleDialog).GetField("_blankIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var solvedField = typeof(SlidingPuzzleDialog).GetField("_solved", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var tiles = (int[])tilesField.GetValue(instance)!;
        var arrangement = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 14 };
        Array.Copy(arrangement, tiles, arrangement.Length);
        blankIndexField.SetValue(instance, 14);
        solvedField.SetValue(instance, false);

        cut.Render();

        var touchMethod = typeof(SlidingPuzzleDialog).GetMethod("OnTileTouchStart", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(touchMethod);

        touchMethod!.Invoke(instance, new object[] { 15, new TouchEventArgs() });

        var blankIndex = (int)blankIndexField.GetValue(instance)!;
        Assert.Equal(15, blankIndex);
    }
}
