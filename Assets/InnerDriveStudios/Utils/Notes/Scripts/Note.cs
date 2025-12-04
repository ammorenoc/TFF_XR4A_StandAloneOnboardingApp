using UnityEngine;

namespace InnerDriveStudios.Util
{
    /**
     * Simple component that, together with the NoteEditor, allows you to add notes to your GameObject's.
     * 
     * @author J.C. Wichman - InnerDriveStudios.com
     */
    [AddComponentMenu("Tools/Notes/Note")]
    public class Note : MonoBehaviour
    {
        public string noteText = "";
        
        //"Documentation", "Todo", "Nice to have", "Minor bug", "Critical bug" 
        public int noteType = 0;
    }

}
