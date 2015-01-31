using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

public class AtlasAdjustmentWindow : EditorWindow
{
    static public AtlasAdjustmentWindow Instance;

    private UITexturePacker.FreeRectChoiceHeuristic currentHeuristic = UITexturePacker.FreeRectChoiceHeuristic.RectBestShortSideFit;
    private int packedSize;

    private int maxHDSize;
    private int maxSDSize;

    private string hDName;
    private string sDName;


    void OnEnable() { Instance = this; }
    void OnDisable() { Instance = null; }



	/// <summary>
	/// Draw the UI for this tool.
	/// </summary>

	void OnGUI ()
	{
        bool recalculate = false;
        bool createHD = false;
        bool createSD = false;
	    List<Texture2D> textures = null;
		NGUIEditorTools.SetLabelWidth(100f);

		GUILayout.Space(6f);

        GUILayout.Label("Select textures in insperctor. You can select files or whole directories.");
        currentHeuristic = (UITexturePacker.FreeRectChoiceHeuristic)EditorGUILayout.EnumPopup("Pack Heuristic", currentHeuristic);

        GUILayout.Space(30f);


        GUILayout.BeginHorizontal();
        GUILayout.Label("Best packed atlas size");
        GUILayout.Label(packedSize.ToString());
        recalculate = GUILayout.Button("Recalculate");
        GUILayout.EndHorizontal();


        GUILayout.Space(30f);

	    maxHDSize = EditorGUILayout.IntField("Max HD Size", maxHDSize);
	    maxSDSize = EditorGUILayout.IntField("Max SD Size", maxSDSize);

        
        hDName = EditorGUILayout.TextField("HD Atlas Name", hDName);
        sDName = EditorGUILayout.TextField("SD Atlas Name", sDName);

       
        GUILayout.Space(30f);
        GUILayout.BeginHorizontal();
        createHD = GUILayout.Button("Create HD");
        createSD = GUILayout.Button("Create SD");
        GUILayout.EndHorizontal();
 

        if (recalculate && DisplayConfirmDialog())
	    {
            textures = GetSelectedTextures();

            EditorUtility.DisplayProgressBar("Calculating size", "", 0.1f);
           
	        try
            {
                packedSize = AtlasAdjustHelper.GetMinSize(textures.ToArray(), currentHeuristic);
            }
            catch (Exception e)
            {
                Debug.LogError("Exception \n" + e.Message);
                Debug.LogError("Stack trace: " + e.StackTrace);
            }

            EditorUtility.ClearProgressBar();
	    }

        if (createHD && DisplayConfirmDialog())
        {
            textures = GetSelectedTextures();

            EditorUtility.DisplayProgressBar("Creating atlases", "HD", 0.1f);

            try
            {
                AtlasAdjustHelper.CreateUIAtlas(textures, hDName, maxHDSize > 0 ? maxHDSize : (int?) null, currentHeuristic);
            }
            catch (Exception e)
            {
                Debug.LogError("Exception \n" + e.Message);
                Debug.LogError("Stack trace: " + e.StackTrace);
            }

            EditorUtility.ClearProgressBar();
        }


        if (createSD && DisplayConfirmDialog())
        {
            textures = GetSelectedTextures();

            EditorUtility.DisplayProgressBar("Creating atlases", "SD", 0.1f);

            try
            {
                AtlasAdjustHelper.CreateUIAtlas(textures, sDName, maxSDSize > 0 ? maxSDSize : (int?)null, currentHeuristic);
            }
            catch (Exception e)
            {
                Debug.LogError("Exception \n" + e.Message);
                Debug.LogError("Stack trace: " + e.StackTrace);
            }

            EditorUtility.ClearProgressBar();
        }

       
	}

    private static bool DisplayConfirmDialog()
    {
        return EditorUtility.DisplayDialog("Are you sure?", "Calculation process an take a long time, are sure that you selected right textures?", "Yes", "No");
    }

    private static List<Texture2D> GetSelectedTextures()
    {
        Object[] textures = Selection.GetFiltered(typeof (Texture2D), SelectionMode.DeepAssets);
        return textures.Cast<Texture2D>().ToList();
    }
}
