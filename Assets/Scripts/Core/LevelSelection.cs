/// <summary>
/// Lightweight static data holder that ferries the chosen LevelData
/// across the scene boundary. Not a MonoBehaviour; not a singleton.
/// Written only by LevelGridView, read only by ColoringController.
/// </summary>
public static class LevelSelection
{
    public static LevelData ActiveLevel { get; set; }
}
