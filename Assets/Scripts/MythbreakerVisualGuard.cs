using UnityEngine;

public class MythbreakerVisualGuard : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindFirstObjectByType<MythbreakerVisualGuard>() == null)
            new GameObject("Mythbreaker Visual Guard").AddComponent<MythbreakerVisualGuard>();
    }

    void LateUpdate()
    {
        if (MythbreakerGame.I == null || MythbreakerGame.I.Player == null) return;
        var hero = MythbreakerGame.I.Player.gameObject;
        if (!hero.activeInHierarchy) return;
        foreach (var r in hero.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
    }
}
