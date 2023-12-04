

using Godot;

namespace LandlessSkies.Core;

public interface ILoadable {
    
    bool IsLoaded { get; set; }


    
    void LoadModel();
    
    void UnloadModel();

    void ReloadModel(bool forceLoad = false);
}