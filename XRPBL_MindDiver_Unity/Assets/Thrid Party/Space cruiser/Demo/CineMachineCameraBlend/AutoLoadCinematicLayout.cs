using UnityEditor;

namespace CinematicSampleUtilities 
{
    [InitializeOnLoad]
    class AutoLoadCinematicLayout
    {
        static string cinematicLayoutPath = "Assets/CinematicTemplate/Layouts/CinematicToolbox.wlt";
        static string layoutAppliedOncePath = "Library/cinematicLayoutApplied";
    
        static AutoLoadCinematicLayout()
        {
            if (!System.IO.File.Exists(layoutAppliedOncePath))
                EditorApplication.delayCall += LoadCinematicLayout;
        }

        static void LoadCinematicLayout()
        {
            if (!System.IO.File.Exists(layoutAppliedOncePath) && System.IO.File.Exists(cinematicLayoutPath))
            {
                EditorUtility.LoadWindowLayout(cinematicLayoutPath);
                System.IO.File.Create(layoutAppliedOncePath).Dispose();
            }
        }
    }
}