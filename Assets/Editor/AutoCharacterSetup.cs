// ─────────────────────────────────────────────────────────────────────────────
// AutoCharacterSetup.cs  –  Assets/Editor/AutoCharacterSetup.cs
// Unity 2022.3 (URP) · UniVRM v0.131.0 (namespace VRM)
//
// Menu: Tools > Auto Setup Anime Character
// Menu: Tools > Fix Skirt Leg Colliders   ← baru
// ─────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRM;

public static class AutoCharacterSetup
{
    // ── constants ─────────────────────────────────────────────────────────────
    private const string CHARACTER_NAME = "3D Anime Character girl full";
    private const string GROUND_NAME    = "Ground";
    private const string MENU_PATH      = "Tools/Auto Setup Anime Character";
    private const string MENU_FIX_SKIRT = "Tools/Fix Skirt Leg Colliders";

    // CharacterController sizing
    private static readonly Vector3 CC_CENTER = new Vector3(0f, 0.8f, 0f);
    private const float             CC_RADIUS = 0.25f;
    private const float             CC_HEIGHT = 1.6f;

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Tools > Auto Setup Anime Character
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem(MENU_PATH)]
    private static void Run()
    {
        Debug.Log("[AutoSetup] ▶ Starting Auto Setup Anime Character …");

        GameObject root = FindInScene(CHARACTER_NAME);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Auto Setup",
                $"Cannot find \"{CHARACTER_NAME}\" in the active scene.\n" +
                "Make sure the prefab/FBX is placed in the scene and try again.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Auto Setup Anime Character");
        int undoGroup = Undo.GetCurrentGroup();

        SetupCharacterController(root);
        SetupPlayerMovement(root);
        RemoveStrayColliderGroups(root);
        EnsureGround();
        SetupCamera(root);
        SetupSkirtLegColliders(root);   // ← fix rok menembus paha

        EditorUtility.SetDirty(root);
        Undo.CollapseUndoOperations(undoGroup);

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[AutoSetup] ✔ Scene saved: {activeScene.path}");
        }
        else
        {
            Debug.LogWarning("[AutoSetup] Scene has no path; please save it manually (Ctrl+S).");
        }

        Debug.Log("[AutoSetup] ✔ Auto Setup complete!");
        EditorUtility.DisplayDialog("Auto Setup", "Auto Setup complete!\nCheck the Console for details.", "OK");
    }

    [MenuItem(MENU_PATH, true)]
    private static bool ValidateRun() => SceneManager.GetActiveScene().IsValid();

    // ─────────────────────────────────────────────────────────────────────────
    // MENU: Tools > Fix Skirt Leg Colliders  (bisa dijalankan sendiri)
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem(MENU_FIX_SKIRT)]
    private static void RunFixSkirt()
    {
        GameObject root = FindInScene(CHARACTER_NAME);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Fix Skirt",
                $"Cannot find \"{CHARACTER_NAME}\" in the active scene.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Fix Skirt Leg Colliders");
        int undoGroup = Undo.GetCurrentGroup();

        SetupSkirtLegColliders(root);

        EditorUtility.SetDirty(root);
        Undo.CollapseUndoOperations(undoGroup);

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(activeScene.path))
            EditorSceneManager.SaveScene(activeScene);

        Debug.Log("[AutoSetup] ✔ Skirt leg colliders fixed!");
        EditorUtility.DisplayDialog("Fix Skirt", "Skirt leg colliders fixed!\nTekan Play untuk melihat hasilnya.", "OK");
    }

    [MenuItem(MENU_FIX_SKIRT, true)]
    private static bool ValidateFixSkirt() => SceneManager.GetActiveScene().IsValid();

    // ─────────────────────────────────────────────────────────────────────────
    // CORE: Setup leg colliders agar rok tidak menembus paha
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 1. Temukan tulang "Left leg" dan "Right leg" di dalam Armature.
    /// 2. Buat / replace VRMSpringBoneColliderGroup di masing-masing dengan
    ///    2 sphere yang mensimulasikan kapsul paha (atas + tengah paha).
    /// 3. Cari semua VRMSpringBone yang mengontrol tulang Skirt_* dan
    ///    assign kedua collider group itu ke ColliderGroups array-nya.
    /// </summary>
    private static void SetupSkirtLegColliders(GameObject root)
    {
        // ── Temukan tulang kaki ───────────────────────────────────────────
        Transform leftLeg  = FindBoneByName(root.transform, "Left leg");
        Transform rightLeg = FindBoneByName(root.transform, "Right leg");

        if (leftLeg == null || rightLeg == null)
        {
            Debug.LogWarning("[AutoSetup] 'Left leg' atau 'Right leg' tidak ditemukan. " +
                             "Pastikan nama tulang sesuai.");
            return;
        }

        // ── Konfigurasi collider per kaki ─────────────────────────────────
        // Dua sphere per kaki = kapsul paha:
        //   • Sphere 0  → pangkal paha     (Y=0,    local space tulang leg)
        //   • Sphere 1  → tengah paha      (Y=-0.18, turun sepanjang paha)
        // Radius 0.10 cukup besar untuk memblok rok anime berukuran standar.
        var leftGroup  = ConfigureLegColliderGroup(leftLeg,  isLeft: true);
        var rightGroup = ConfigureLegColliderGroup(rightLeg, isLeft: false);

        // ── Assign ke VRMSpringBone skirt ─────────────────────────────────
        AssignCollidersToSkirtSpringBones(root, leftGroup, rightGroup);

        Debug.Log("[AutoSetup] ✔ Leg colliders untuk skirt berhasil dikonfigurasi.");
    }

    /// <summary>
    /// Buat / reconfigure VRMSpringBoneColliderGroup pada tulang kaki.
    /// Radius dikonfigurasi supaya menutupi area paha.
    /// </summary>
    private static VRMSpringBoneColliderGroup ConfigureLegColliderGroup(
        Transform legBone, bool isLeft)
    {
        string side = isLeft ? "L" : "R";

        // Hapus collider group lama yang ada (supaya radius tidak tumpang tindih)
        var existing = legBone.GetComponents<VRMSpringBoneColliderGroup>();
        foreach (var old in existing)
        {
            Undo.DestroyObjectImmediate(old);
            Debug.Log($"[AutoSetup] Replaced old VRMSpringBoneColliderGroup on {legBone.name}.");
        }

        // Tambahkan baru
        var group = Undo.AddComponent<VRMSpringBoneColliderGroup>(legBone.gameObject);

        // --- Dua sphere : mensimulasikan silinder paha ---
        // Offset Y negatif = arah bawah sepanjang tulang paha (local space)
        // X sedikit ke dalam badan agar simetri kanan-kiri
        float xSign   = isLeft ? 0.01f : -0.01f;   // hampir nol, cukup untuk simetri

        group.Colliders = new VRMSpringBoneColliderGroup.SphereCollider[]
        {
            // Sphere 0 — pangkal paha (di dekat hip)
            new VRMSpringBoneColliderGroup.SphereCollider
            {
                Offset = new Vector3(xSign, 0.00f, 0.00f),
                Radius = 0.10f
            },
            // Sphere 1 — tengah paha
            new VRMSpringBoneColliderGroup.SphereCollider
            {
                Offset = new Vector3(xSign, -0.18f, 0.00f),
                Radius = 0.09f
            },
        };

        EditorUtility.SetDirty(legBone.gameObject);
        Debug.Log($"[AutoSetup] Configured VRMSpringBoneColliderGroup on '{legBone.name}' ({side}) " +
                  $"with 2 sphere colliders (r=0.10 / r=0.09).");
        return group;
    }

    /// <summary>
    /// Cari semua VRMSpringBone yang RootBones-nya mengandung tulang Skirt_*
    /// lalu tambahkan leftGroup + rightGroup ke ColliderGroups-nya.
    /// </summary>
    private static void AssignCollidersToSkirtSpringBones(
        GameObject root,
        VRMSpringBoneColliderGroup leftGroup,
        VRMSpringBoneColliderGroup rightGroup)
    {
        // VRMSpringBone biasanya ada di root atau Armature
        var allSpringBones = root.GetComponentsInChildren<VRMSpringBone>(includeInactive: true);

        int assigned = 0;
        foreach (var sb in allSpringBones)
        {
            // Cek apakah spring bone ini mengontrol tulang rok
            bool isSkirtBone = sb.RootBones.Any(rb =>
                rb != null && rb.name.StartsWith("Skirt_", System.StringComparison.OrdinalIgnoreCase));

            if (!isSkirtBone) continue;

            Undo.RegisterCompleteObjectUndo(sb, "Assign Skirt Leg Colliders");

            // Gabungkan collider grup lama + grup kaki baru (hindari duplikat)
            var currentGroups = sb.ColliderGroups != null
                ? sb.ColliderGroups.ToList()
                : new List<VRMSpringBoneColliderGroup>();

            if (!currentGroups.Contains(leftGroup))  currentGroups.Add(leftGroup);
            if (!currentGroups.Contains(rightGroup)) currentGroups.Add(rightGroup);

            // Hapus null entry
            currentGroups.RemoveAll(g => g == null);

            sb.ColliderGroups = currentGroups.ToArray();
            EditorUtility.SetDirty(sb);
            assigned++;
            Debug.Log($"[AutoSetup] Assigned leg colliders to VRMSpringBone '{sb.name}' " +
                      $"(total collider groups: {sb.ColliderGroups.Length}).");
        }

        if (assigned == 0)
            Debug.LogWarning("[AutoSetup] Tidak ada VRMSpringBone skirt yang ditemukan. " +
                             "Pastikan RootBones-nya berisi tulang bernama 'Skirt_*'.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Utilities
    // ─────────────────────────────────────────────────────────────────────────

    private static GameObject FindInScene(string name)
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == name) return go;
        return GameObject.Find(name);
    }

    /// <summary>DFS cari Transform pertama yang namanya cocok (case-sensitive).</summary>
    private static Transform FindBoneByName(Transform parent, string boneName)
    {
        if (parent.name == boneName) return parent;
        foreach (Transform child in parent)
        {
            var found = FindBoneByName(child, boneName);
            if (found != null) return found;
        }
        return null;
    }

    // ── 2. CharacterController ────────────────────────────────────────────────
    private static void SetupCharacterController(GameObject root)
    {
        CharacterController cc = root.GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = Undo.AddComponent<CharacterController>(root);
            Debug.Log("[AutoSetup] Added CharacterController.");
        }
        else
        {
            Undo.RegisterCompleteObjectUndo(cc, "Configure CharacterController");
            Debug.Log("[AutoSetup] CharacterController already present — reconfiguring.");
        }
        cc.center = CC_CENTER;
        cc.radius = CC_RADIUS;
        cc.height = CC_HEIGHT;
        EditorUtility.SetDirty(cc);
    }

    // ── 3. PlayerMovement ─────────────────────────────────────────────────────
    private static void SetupPlayerMovement(GameObject root)
    {
        PlayerMovement pm = root.GetComponent<PlayerMovement>();
        if (pm == null)
        {
            pm = Undo.AddComponent<PlayerMovement>(root);
            Debug.Log("[AutoSetup] Added PlayerMovement.");
        }
        else
        {
            Undo.RegisterCompleteObjectUndo(pm, "Configure PlayerMovement");
            Debug.Log("[AutoSetup] PlayerMovement already present.");
        }
        EditorUtility.SetDirty(pm);
    }

    // ── 4. Remove stray VRMSpringBoneColliderGroup on root ────────────────────
    private static void RemoveStrayColliderGroups(GameObject root)
    {
        var groups = root.GetComponents<VRMSpringBoneColliderGroup>();
        foreach (var g in groups)
        {
            Undo.DestroyObjectImmediate(g);
            Debug.Log($"[AutoSetup] Removed stray VRMSpringBoneColliderGroup from \"{root.name}\".");
        }
    }

    // ── 5. Ground plane ───────────────────────────────────────────────────────
    private static void EnsureGround()
    {
        if (GameObject.Find(GROUND_NAME) != null)
        {
            Debug.Log("[AutoSetup] Ground already exists — skipping.");
            return;
        }
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
        ground.name             = GROUND_NAME;
        ground.transform.position  = Vector3.zero;
        ground.transform.localScale = new Vector3(5f, 1f, 5f);
        EditorUtility.SetDirty(ground);
        Debug.Log("[AutoSetup] Created Ground plane.");
    }

    // ── 6. ThirdPersonCamera on Main Camera ───────────────────────────────────
    private static void SetupCamera(GameObject characterRoot)
    {
        Camera cam = Camera.main ?? Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            Debug.LogWarning("[AutoSetup] No camera found. ThirdPersonCamera NOT added.");
            return;
        }

        ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
        if (tpc == null)
        {
            tpc = Undo.AddComponent<ThirdPersonCamera>(cam.gameObject);
            Debug.Log($"[AutoSetup] Added ThirdPersonCamera to \"{cam.gameObject.name}\".");
        }
        else
        {
            Undo.RegisterCompleteObjectUndo(tpc, "Configure ThirdPersonCamera");
        }

        SerializedObject so = new SerializedObject(tpc);
        SerializedProperty targetProp = so.FindProperty("targetTransform");
        if (targetProp != null)
        {
            targetProp.objectReferenceValue = characterRoot.transform;
            so.ApplyModifiedProperties();
        }
        else
        {
            tpc.targetTransform = characterRoot.transform;
        }

        Transform charT = characterRoot.transform;
        cam.transform.position = charT.position + charT.forward * -4f + Vector3.up * 2.5f;
        cam.transform.LookAt(charT.position + Vector3.up * 1.4f);
        EditorUtility.SetDirty(cam.gameObject);
    }
}
#endif
