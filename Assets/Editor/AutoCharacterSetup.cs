// ─────────────────────────────────────────────────────────────────────────────
// AutoCharacterSetup.cs  –  Assets/Editor/AutoCharacterSetup.cs
// Unity 2022.3 (URP) · UniVRM v0.131.0 (namespace VRM)
//
// Menu: Tools > Auto Setup Anime Character
// Finds "3D Anime Character girl full" in the active scene and wires up:
//   • CharacterController (sized for a standing anime character)
//   • PlayerMovement
//   • ThirdPersonCamera on Main Camera
//   • Ground Plane (if absent)
//   • Removes stray VRMSpringBoneColliderGroup on root
// All changes are Undo-recorded and SetDirty so they persist across reloads.
// ─────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRM;   // VRMSpringBone, VRMSpringBoneColliderGroup

public static class AutoCharacterSetup
{
    // ── constants ─────────────────────────────────────────────────────────────
    private const string CHARACTER_NAME = "3D Anime Character girl full";
    private const string GROUND_NAME    = "Ground";
    private const string MENU_PATH      = "Tools/Auto Setup Anime Character";

    // CharacterController sizing
    private static readonly Vector3 CC_CENTER = new Vector3(0f, 0.8f, 0f);
    private const float             CC_RADIUS = 0.25f;
    private const float             CC_HEIGHT = 1.6f;

    // ── entry point ───────────────────────────────────────────────────────────
    [MenuItem(MENU_PATH)]
    private static void Run()
    {
        Debug.Log("[AutoSetup] ▶ Starting Auto Setup Anime Character …");

        // ── 1. Locate root character ──────────────────────────────────────
        GameObject root = FindInScene(CHARACTER_NAME);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Auto Setup",
                $"Cannot find \"{CHARACTER_NAME}\" in the active scene.\n" +
                "Make sure the prefab/FBX is placed in the scene and try again.",
                "OK");
            return;
        }

        // Record the whole scene for Undo (single undo group).
        Undo.SetCurrentGroupName("Auto Setup Anime Character");
        int undoGroup = Undo.GetCurrentGroup();

        // ── 2. CharacterController ────────────────────────────────────────
        SetupCharacterController(root);

        // ── 3. PlayerMovement ─────────────────────────────────────────────
        SetupPlayerMovement(root);

        // ── 4. Remove stray VRMSpringBoneColliderGroup on root ────────────
        RemoveStrayColliderGroups(root);

        // ── 5. Ground plane ───────────────────────────────────────────────
        EnsureGround();

        // ── 6. ThirdPersonCamera on Main Camera ───────────────────────────
        SetupCamera(root);

        // ── 7. Mark dirty & save scene ────────────────────────────────────
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
        EditorUtility.DisplayDialog("Auto Setup", "Auto Setup Anime Character complete!\nCheck the Console for details.", "OK");
    }

    // ── Validation: disable menu when no scene is loaded ─────────────────────
    [MenuItem(MENU_PATH, true)]
    private static bool ValidateRun()
    {
        return SceneManager.GetActiveScene().IsValid();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Finds a root-level GameObject by name in the active scene.</summary>
    private static GameObject FindInScene(string name)
    {
        // Search root objects first (fastest).
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name == name) return go;
        }

        // Fall back to searching all objects (handles nested prefab roots).
        GameObject found = GameObject.Find(name);
        return found;
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
            Debug.Log("[AutoSetup] PlayerMovement already present — skipping reconfigure.");
        }

        // Wire Animator if present on the character.
        Animator anim = root.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            // Use serialized property so Undo captures it properly.
            SerializedObject so = new SerializedObject(pm);
            // PlayerMovement caches its animator in Awake; no serialized field needed.
            // But the Animator must be on the same GameObject or child — Unity will find it.
            EditorUtility.SetDirty(pm);
        }
    }

    // ── 4. Remove stray VRMSpringBoneColliderGroup on root ────────────────────
    private static void RemoveStrayColliderGroups(GameObject root)
    {
        // Only remove groups that sit directly on the root (not on bone children).
        VRMSpringBoneColliderGroup[] groups =
            root.GetComponents<VRMSpringBoneColliderGroup>();

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
        ground.name           = GROUND_NAME;
        ground.transform.position  = Vector3.zero;
        ground.transform.localScale = new Vector3(5f, 1f, 5f);
        EditorUtility.SetDirty(ground);
        Debug.Log("[AutoSetup] Created Ground plane (10m × 10m world size).");
    }

    // ── 6. ThirdPersonCamera on Main Camera ───────────────────────────────────
    private static void SetupCamera(GameObject characterRoot)
    {
        // Prefer Camera.main; fall back to first camera in scene.
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = UnityEngine.Object.FindObjectOfType<Camera>();
        }
        if (cam == null)
        {
            Debug.LogWarning("[AutoSetup] No camera found in scene. ThirdPersonCamera NOT added.");
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
            Debug.Log("[AutoSetup] ThirdPersonCamera already present — reconfiguring target.");
        }

        // Assign the character root as the follow target via SerializedObject
        // so the reference survives scene reload and shows in Inspector.
        SerializedObject so = new SerializedObject(tpc);
        SerializedProperty targetProp = so.FindProperty("targetTransform");
        if (targetProp != null)
        {
            targetProp.objectReferenceValue = characterRoot.transform;
            so.ApplyModifiedProperties();
        }
        else
        {
            // Fallback: set directly (works at edit-time before first play).
            tpc.targetTransform = characterRoot.transform;
        }

        // Position camera behind and above the character.
        Transform charT = characterRoot.transform;
        cam.transform.position = charT.position + charT.forward * -4f + Vector3.up * 2.5f;
        cam.transform.LookAt(charT.position + Vector3.up * 1.4f);

        EditorUtility.SetDirty(cam.gameObject);
    }
}
#endif
