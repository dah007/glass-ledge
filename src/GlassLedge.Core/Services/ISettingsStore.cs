using GlassLedge.Core.Models;

namespace GlassLedge.Core.Services;

public interface ISettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}
