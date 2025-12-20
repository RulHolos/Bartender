using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.UI.Utils;

public sealed class IconManager : IDisposable
{
    private readonly Dictionary<uint, IDalamudTextureWrap> iconCache = [];

    public ISharedImmediateTexture GetIcon(uint id)
    {
        try
        {
            return DalamudApi.TextureProvider.GetFromGameIcon(new GameIconLookup(id % 1_000_000, id >= 1_000_000));
        }
        catch (IconNotFoundException ex)
        {
            return null;
        }
    }

    public void Dispose()
    {
        foreach (var image in iconCache.Values)
            image.Dispose();
        iconCache.Clear();
    }
}
