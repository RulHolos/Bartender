using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
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
        /*if (!iconCache.TryGetValue(id, out var ret))
            iconCache.Add(id, ret = DalamudApi.TextureProvider.GetIcon(id) ??
                throw new ArgumentException($"Invalid icon id {id}", nameof(id)));*/
        return DalamudApi.TextureProvider.GetFromGameIcon(new GameIconLookup(id % 1000000, id >= 1000000));
    }

    public void Dispose()
    {
        foreach (var image in iconCache.Values)
            image.Dispose();
        iconCache.Clear();
    }
}
