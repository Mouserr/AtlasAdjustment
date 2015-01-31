using UnityEditor;

public class AtlasAdjustmentMenu
{
    [MenuItem("Tools/AtlasAdjustment/Open atlas adjustment window", false, 0)]
    public static void OpenAdjustmentWindow()
    {
        EditorWindow.GetWindow<AtlasAdjustmentWindow>(false, "Atlas Adjustment Window", true);
    }
}
