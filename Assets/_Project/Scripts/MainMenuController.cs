using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// Pasang script ini ke objek UIDocument yang sama dengan MainMenu.uxml.
/// Isi kolom "Game Scene Name" di Inspector dengan nama Scene game 3D Anda.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Nama Scene Arena 3D Tujuan")]
    [Tooltip("Nama scene yang akan dimuat saat tombol MULAI PETUALANGAN diklik.")]
    public string gameSceneName = "SampleScene"; // Ganti sesuai nama Scene Anda

    // ----------------------------------------------------------------
    // OnEnable dipanggil setiap kali objek aktif — lebih aman dari Start
    // untuk UIDocument karena dokumen sudah pasti siap saat ini.
    // ----------------------------------------------------------------
    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("[MainMenuController] Komponen UIDocument tidak ditemukan di GameObject ini!");
            return;
        }

        var root = uiDocument.rootVisualElement;

        // Cari tombol berdasarkan atribut 'name' di UXML (setara getElementById)
        Button playButton     = root.Q<Button>("btn-play");
        Button settingsButton = root.Q<Button>("btn-settings");
        Button quitButton     = root.Q<Button>("btn-quit");

        // Pasang event listener
        if (playButton     != null) playButton.clicked     += OnPlayClicked;
        if (settingsButton != null) settingsButton.clicked += OnSettingsClicked;
        if (quitButton     != null) quitButton.clicked     += OnQuitClicked;
    }

    // Lepas event listener agar tidak menumpuk saat OnEnable dipanggil ulang
    private void OnDisable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        Button playButton     = root.Q<Button>("btn-play");
        Button settingsButton = root.Q<Button>("btn-settings");
        Button quitButton     = root.Q<Button>("btn-quit");

        if (playButton     != null) playButton.clicked     -= OnPlayClicked;
        if (settingsButton != null) settingsButton.clicked -= OnSettingsClicked;
        if (quitButton     != null) quitButton.clicked     -= OnQuitClicked;
    }

    // ----------------------------------------------------------------
    // Handler tombol
    // ----------------------------------------------------------------

    private void OnPlayClicked()
    {
        Debug.Log("[MainMenuController] Memasuki dunia 3D RPG: " + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[MainMenuController] Tombol Pengaturan diklik — sambungkan panel Settings di sini.");
        // TODO: Aktifkan panel pengaturan (audio, grafis, kontrol, dll.)
    }

    private void OnQuitClicked()
    {
        Debug.Log("[MainMenuController] Keluar dari game...");

#if UNITY_EDITOR
        // Hentikan Play Mode saat di dalam Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
