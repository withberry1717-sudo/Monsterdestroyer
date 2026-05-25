using UnityEngine;
using UnityEditor;

public static class MissingScriptCleaner
{
    [MenuItem("Tools/Cleanup/Remove Missing Scripts In Current Scene")]
    public static void RemoveMissingScriptsInCurrentScene()
    {
        int totalRemoved = 0;

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include
        );

        foreach (GameObject obj in allObjects)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);

            if (removed > 0)
            {
                totalRemoved += removed;
                Debug.Log("Removed " + removed + " missing script(s) from: " + GetFullPath(obj));
                EditorUtility.SetDirty(obj);
            }
        }

        Debug.Log("Missing Script cleanup finished. Total removed: " + totalRemoved);
    }

    private static string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}