
using UnityEngine;

public class RoleSelectionManager : MonoBehaviour
{
    public static string SelectedRole = "Engineer";

    public void SelectEngineer()
    {
        SelectedRole = "Engineer";
        Debug.Log("Selected role: " + SelectedRole);
    }

    public void SelectScout()
    {
        SelectedRole = "Scout";
        Debug.Log("Selected role: " + SelectedRole);
    }

    public void SelectFighter()
    {
        SelectedRole = "Fighter";
        Debug.Log("Selected role: " + SelectedRole);
    }
}
