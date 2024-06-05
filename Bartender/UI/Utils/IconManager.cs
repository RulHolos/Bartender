using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.UI.Utils;

public sealed class IconManager : IDisposable
{
    private readonly Dictionary<uint, IDalamudTextureWrap> iconCache = [];

    public IDalamudTextureWrap GetIcon(uint id)
    {
        if (!iconCache.TryGetValue(id, out var ret))
            iconCache.Add(id, ret = DalamudApi.TextureProvider.GetIcon(id) ??
                throw new ArgumentException($"Invalid icon id {id}", nameof(id)));
        return ret;
    }

    public void Dispose()
    {
        foreach (var image in iconCache.Values)
            image.Dispose();
        iconCache.Clear();
    }
}
